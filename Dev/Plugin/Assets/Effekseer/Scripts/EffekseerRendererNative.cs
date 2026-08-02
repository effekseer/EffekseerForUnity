using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace Effekseer.Internal
{
	internal class EffekseerRendererNative : IEffekseerRenderer
	{
		const CameraEvent cameraEvent = CameraEvent.AfterForwardAlpha;
		private StandardBlitter standardBlitter = new StandardBlitter();

		private class RenderPath : RenderPathBase
		{
			public Material fakeMaterial = null;

			public override void Init(Camera camera, CameraEvent cameraEvent, int renderId, bool isCommandBufferFromExternal, bool isScriptable)
			{
				this.camera = camera;
				this.renderId = renderId;
				this.cameraEvent = cameraEvent;
				this.isCommandBufferFromExternal = isCommandBufferFromExternal;
				this._isScriptable = isScriptable;

				var fakeShader = EffekseerDependentAssets.Instance.fakeMaterial;
#if UNITY_EDITOR
				if (fakeShader == null)
				{
					EffekseerDependentAssets.AssignAssets();
				}
				fakeShader = EffekseerDependentAssets.Instance.fakeMaterial;
#endif

#if UNITY_EDITOR || UNITY_PS4
				if (fakeShader != null)
				{
					fakeMaterial = new Material(EffekseerDependentAssets.Instance.fakeMaterial);
				}
#endif
			}

			public override void ResetParameters(bool enableDistortion, bool enableDepth, RenderTargetProperty renderTargetProperty,
				IEffekseerBlitter blitter, StereoRendererUtil.StereoRenderingTypes stereoRenderingType = StereoRendererUtil.StereoRenderingTypes.None)
			{
				isDistortionEnabled = enableDistortion;
				isDepthEnabled = enableDepth;

				// Create a command buffer that is effekseer renderer
				if (!isCommandBufferFromExternal)
				{
					this.commandBuffer = new CommandBuffer();
					this.commandBuffer.name = "Effekseer Rendering";
				}

				if (stereoRenderingType == StereoRendererUtil.StereoRenderingTypes.SinglePass)
				{
					// In SinglePass Stereo Rendering, draw eyes twice on the left and right with one CommandBuffer
					this.isDistortionEnabled = false;
					this.isDistortionMakeDisabledForcely = true;
				}

				RendererUtils.SetupBackgroundBuffer(ref renderTexture, isDistortionEnabled, camera, renderTargetProperty);
				RendererUtils.SetupDepthBuffer(ref depthTexture, isDepthEnabled, camera, renderTargetProperty);

				// register the command to a camera
				if (!isCommandBufferFromExternal && !_isScriptable)
				{
					this.camera.AddCommandBuffer(this.cameraEvent, this.commandBuffer);
				}
			}

			public override void Dispose()
			{
				base.Dispose();
			}

			public void AssignExternalCommandBuffer(CommandBuffer commandBuffer)
			{
				if (!isCommandBufferFromExternal)
				{
					Debug.LogError("External command buffer is assigned even if isCommandBufferFromExternal is true.");
				}

				this.commandBuffer = commandBuffer;
			}
		}

		RenderPathContainer<RenderPath> renderPathContainer = new RenderPathContainer<RenderPath>();

		public int layer { get; set; }

		public bool disableCullingMask { get; set; } = false;

		public void SetVisible(bool visible)
		{
			if (visible)
			{
				Camera.onPreCull += Render;
				Camera.onPostRender += OnPostRender;
			}
			else
			{
				Camera.onPreCull -= Render;
				Camera.onPostRender -= OnPostRender;
			}
		}

		public void CleanUp()
		{
			renderPathContainer.CleanUp();
		}

		public void Render(Camera camera)
		{
			if (!EffekseerSettings.Instance.renderAsPostProcessingStack)
			{
				EffekseerRenderCoordinator.Render(this, new EffekseerRenderFrameInput(camera, int.MaxValue, null, null, false, standardBlitter));
			}
		}

		public EffekseerPreparedFrame PrepareFrame(EffekseerRenderFrameInput input)
		{
			RenderPath path;
			int allEffectMask;
			int cameraMask;
			renderPathContainer.UpdateRenderPath(disableCullingMask, input.Camera, input.AdditionalMask,
				input.RenderTargetProperty, input.TargetCommandBuffer, input.IsScriptable, input.Blitter,
				cameraEvent, out path, out allEffectMask, out cameraMask, input.UsesExternalCommands);
			if (path == null)
			{
				return null;
			}

			if (path.isCommandBufferFromExternal && input.TargetCommandBuffer != null)
			{
				path.AssignExternalCommandBuffer(input.TargetCommandBuffer);
			}

			if (path.commandBuffer != null && !path.isCommandBufferFromExternal)
			{
				path.commandBuffer.Clear();
			}

			if (input.RenderTargetProperty != null)
			{
				Plugin.EffekseerSetRenderSettings(path.renderId, true);
				Plugin.EffekseerSetIsBackgroundTextureFlipped(0);
			}
			else
			{
#if UNITY_SWITCH && !UNITY_EDITOR
				Plugin.EffekseerSetIsBackgroundTextureFlipped(1);
#else
				Plugin.EffekseerSetIsBackgroundTextureFlipped(0);
#endif
			}

			if (path.renderTexture != null)
			{
				Plugin.EffekseerSetExternalTexture(path.renderId, ExternalTextureType.Background, path.renderTexture.ptr);
			}
			else
			{
				Plugin.EffekseerSetExternalTexture(path.renderId, ExternalTextureType.Background, IntPtr.Zero);
			}

			if (path.depthTexture != null)
			{
				Plugin.EffekseerSetExternalTexture(path.renderId, ExternalTextureType.Depth, path.depthTexture.ptr);
			}
			else
			{
				Plugin.EffekseerSetExternalTexture(path.renderId, ExternalTextureType.Depth, IntPtr.Zero);
			}

			var screenSize = BackgroundRenderTexture.GetRequiredSize(input.Camera, input.RenderTargetProperty);
			Plugin.EffekseerSetRenderTargetProperty(path.renderId, TextureFormatType.R8G8B8A8_UNORM, TextureFormatType.D32S8, screenSize.x, screenSize.y);

			SpecifyRenderingMatrix(input.Camera, path);

			return new EffekseerPreparedFrame
			{
				BackendPath = path,
				CommandBuffer = path.commandBuffer,
				Background = path.renderTexture,
				Depth = path.depthTexture,
				TargetCommitMaterial = input.RenderTargetProperty == null && (path.renderTexture != null || path.depthTexture != null)
					? path.fakeMaterial
					: null,
				IsNativeRenderer = true,
				Camera = input.Camera,
				HasVisibleEffects = (allEffectMask & cameraMask) != 0,
			};
		}

		public void RecordPhase(EffekseerPreparedFrame frame, EffekseerRenderPhase phase, IEffekseerCommandBuffer commandBuffer)
		{
			var path = frame.BackendPath as RenderPath;
			if (path == null || commandBuffer == null)
			{
				return;
			}

			var callback = phase == EffekseerRenderPhase.Back
				? Plugin.EffekseerGetRenderBackFunc()
				: Plugin.EffekseerGetRenderFrontFunc();
			commandBuffer.IssuePluginEvent(callback, path.renderId);
		}

		public void EndFrame(EffekseerPreparedFrame frame)
		{
		}

		private static void SpecifyRenderingMatrix(Camera camera, RenderPath path)
		{
			// specify matrixes for stereo rendering
			if (camera.stereoEnabled)
			{
				var stereoRenderType = StereoRendererUtil.GetStereoRenderingType();
				if (stereoRenderType != StereoRendererUtil.StereoRenderingTypes.None)
				{
					float[] camCenterMat = Utility.Matrix2Array(camera.worldToCameraMatrix);
					float[] projMatL = Utility.Matrix2Array(GL.GetGPUProjectionMatrix(camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left), false));
					float[] projMatR = Utility.Matrix2Array(GL.GetGPUProjectionMatrix(camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right), false));
					float[] camMatL = Utility.Matrix2Array(camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left));
					float[] camMatR = Utility.Matrix2Array(camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right));
					Plugin.EffekseerSetStereoRenderingMatrix(path.renderId, (int)stereoRenderType, camCenterMat, projMatL, projMatR, camMatL, camMatR);
				}
			}
			else
			{
				// update view matrixes
				Plugin.EffekseerSetProjectionMatrix(path.renderId, Utility.Matrix2Array(
					GL.GetGPUProjectionMatrix(camera.projectionMatrix, false)));
				Plugin.EffekseerSetCameraMatrix(path.renderId, Utility.Matrix2Array(
					camera.worldToCameraMatrix));
			}
		}

		public void OnPostRender(Camera camera)
		{
			renderPathContainer.OnPostRender(camera);
		}
	}

}
