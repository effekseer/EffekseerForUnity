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
			Material fakeMaterial = null;

			public override void Init(Camera camera, CameraEvent cameraEvent, int renderId, bool isCommandBufferFromExternal)
			{
				this.camera = camera;
				this.renderId = renderId;
				this.cameraEvent = cameraEvent;
				this.isCommandBufferFromExternal = isCommandBufferFromExternal;

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

				if (!isCommandBufferFromExternal)
				{
					SetupEffekseerRenderCommandBuffer(commandBuffer, this.isDistortionEnabled, renderTargetProperty, blitter);
				}

				// register the command to a camera
				if (!isCommandBufferFromExternal)
				{
					this.camera.AddCommandBuffer(this.cameraEvent, this.commandBuffer);
				}
			}

			private void SetupEffekseerRenderCommandBuffer(
				CommandBuffer cmbBuf,
				bool enableDistortion,
				RenderTargetProperty renderTargetProperty,
				IEffekseerBlitter blitter)
			{
				// add a command to render effects.
				if (cmbBuf == null)
				{
					return;
				}

				Action copyBackground = () =>
				{
					if (this.renderTexture != null)
					{
						// Add a blit command that copy to the distortion texture
						// this.commandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, this.renderTexture.renderTexture);
						// this.commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

						if (renderTargetProperty != null)
						{
							if (renderTargetProperty.colorBufferID.HasValue)
							{
								blitter.Blit(cmbBuf, renderTargetProperty.colorBufferID.Value, this.renderTexture.renderTexture, renderTargetProperty.xrRendering);
								cmbBuf.SetRenderTarget(renderTargetProperty.colorBufferID.Value);

								if (renderTargetProperty.Viewport.width > 0)
								{
									cmbBuf.SetViewport(renderTargetProperty.Viewport);
								}
							}
							else
							{
								renderTargetProperty.ApplyToCommandBuffer(cmbBuf, this.renderTexture, blitter);

								if (renderTargetProperty.Viewport.width > 0)
								{
									cmbBuf.SetViewport(renderTargetProperty.Viewport);
								}
							}
						}
						else
						{
							// TODO : Fix
							bool xrRendering = false;

							blitter.Blit(cmbBuf, BuiltinRenderTextureType.CameraTarget, this.renderTexture.renderTexture, xrRendering);
							cmbBuf.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

							// to reset shader settings. SetRenderTarget is not applied until drawing
							if (fakeMaterial != null)
							{
								cmbBuf.DrawProcedural(new Matrix4x4(), fakeMaterial, 0, MeshTopology.Triangles, 3);
							}
						}
					}
				};

				copyBackground();

				if (this.depthTexture != null)
				{
					if (renderTargetProperty != null)
					{
						renderTargetProperty.ApplyToCommandBuffer(cmbBuf, this.depthTexture);

						if (renderTargetProperty.Viewport.width > 0)
						{
							cmbBuf.SetViewport(renderTargetProperty.Viewport);
						}
					}
					else
					{
						// TODO : Fix
						bool xrRendering = false;

						blitter.Blit(cmbBuf, BuiltinRenderTextureType.Depth, this.depthTexture.renderTexture, xrRendering);
						cmbBuf.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

						// to reset shader settings. SetRenderTarget is not applied until drawing
						if (fakeMaterial != null)
						{
							cmbBuf.DrawProcedural(new Matrix4x4(), fakeMaterial, 0, MeshTopology.Triangles, 3);
						}
					}
				}

				cmbBuf.IssuePluginEvent(Plugin.EffekseerGetRenderBackFunc(), this.renderId);

				copyBackground();

				cmbBuf.IssuePluginEvent(Plugin.EffekseerGetRenderFrontFunc(), this.renderId);
			}

			public override void Dispose()
			{
				base.Dispose();
			}

			public void AssignExternalCommandBuffer(CommandBuffer commandBuffer, RenderTargetProperty renderTargetProperty, IEffekseerBlitter blitter)
			{
				if (!isCommandBufferFromExternal)
				{
					Debug.LogError("External command buffer is assigned even if isCommandBufferFromExternal is true.");
				}

				this.commandBuffer = commandBuffer;
				SetupEffekseerRenderCommandBuffer(commandBuffer, this.isDistortionEnabled, renderTargetProperty, blitter);
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

		public CommandBuffer GetCameraCommandBuffer(Camera camera)
		{
			return renderPathContainer.GetCameraCommandBuffer(camera);
		}

		public void Render(Camera camera)
		{
			if (!EffekseerSettings.Instance.renderAsPostProcessingStack)
			{
				Render(camera, null, null, standardBlitter);
			}
		}

		public void Render(Camera camera, RenderTargetProperty renderTargetProperty, CommandBuffer targetCommandBuffer, IEffekseerBlitter blitter)
		{
			RenderPath path;
			int mask;
			renderPathContainer.UpdateRenderPath(disableCullingMask, camera, renderTargetProperty, targetCommandBuffer, blitter, cameraEvent, out path, out mask);
			if (path == null)
			{
				return;
			}

			// effects shown don't exists
			if ((camera.cullingMask & mask) == 0)
			{
				// Because rendering thread is asynchronous
				SpecifyRenderingMatrix(camera, path);
				return;
			}

			if (path.isCommandBufferFromExternal)
			{
				path.AssignExternalCommandBuffer(targetCommandBuffer, renderTargetProperty, blitter);
			}

			// if LWRP
			if (renderTargetProperty != null)
			{
				// flip a rendertaget
				// Direct11 : OK (2019, LWRP 5.13)
				// Android(OpenGL) : OK (2019, LWRP 5.13)
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

			// assign a dinsotrion texture
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

			// TODO : specify correct texture formats
			var screenSize = BackgroundRenderTexture.GetRequiredSize(camera, renderTargetProperty);
			Plugin.EffekseerSetRenderTargetProperty(path.renderId, TextureFormatType.R8G8B8A8_UNORM, TextureFormatType.D32S8, screenSize.x, screenSize.y);

			SpecifyRenderingMatrix(camera, path);
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