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

		private class RenderPath : IDisposable
		{
			public Camera camera;
			public CommandBuffer commandBuffer;
			public bool isCommandBufferFromExternal = false;
			public CameraEvent cameraEvent;
			public int renderId;
			public BackgroundRenderTexture renderTexture;
			public DepthRenderTexture depthTexture;

			public int LifeTime = 5;

			bool isDistortionEnabled = false;

			bool isDepthEnabled = false;

			/// <summary>
			/// Distortion is disabled forcely because of VR
			/// </summary>
			bool isDistortionMakeDisabledForcely = false;

			Material fakeMaterial = null;

			public RenderPath(Camera camera, CameraEvent cameraEvent, int renderId, bool isCommandBufferFromExternal)
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

			public void Init(bool enableDistortion, bool enableDepth, RenderTargetProperty renderTargetProperty,
				IEffekseerBlitter blitter, StereoRendererUtil.StereoRenderingTypes stereoRenderingType = StereoRendererUtil.StereoRenderingTypes.None)
			{
				this.isDistortionEnabled = enableDistortion;
				isDepthEnabled = enableDepth;
				isDistortionMakeDisabledForcely = false;

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

				Action copyBackground = () => {
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

			public void Dispose()
			{
				if (this.commandBuffer != null && !isCommandBufferFromExternal)
				{
					if (this.camera != null)
					{
						this.camera.RemoveCommandBuffer(this.cameraEvent, this.commandBuffer);
					}
					this.commandBuffer.Dispose();
					this.commandBuffer = null;
				}

				if (this.renderTexture != null)
				{
					this.renderTexture.Release();
					this.renderTexture = null;
				}

				if (depthTexture != null)
				{
					depthTexture.Release();
					depthTexture = null;
				}
			}

			public bool IsValid(RenderTargetProperty renderTargetProperty)
			{
				if (isDistortionMakeDisabledForcely)
				{

				}
				else
				{
					if (this.isDistortionEnabled != EffekseerRendererUtils.IsDistortionEnabled) return false;
					if (this.isDepthEnabled != EffekseerRendererUtils.IsDepthEnabled) return false;
				}

				if (depthTexture != null)
				{
					var targetSize = BackgroundRenderTexture.GetRequiredSize(this.camera, renderTargetProperty);

					if(targetSize.x != this.depthTexture.width ||
						targetSize.y != this.depthTexture.height)
					{
						return false;
					}
				}

				if (this.renderTexture != null)
				{
					var targetSize = BackgroundRenderTexture.GetRequiredSize(this.camera, renderTargetProperty);

					if (targetSize.x != this.renderTexture.width ||
						targetSize.y != this.renderTexture.height)
					{
						return false;
					}
				}

				return true;
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

		// RenderPath per Camera
		private Dictionary<Camera, RenderPath> renderPaths = new Dictionary<Camera, RenderPath>();
		int nextRenderID = 0;

		public int layer { get; set; }

#if UNITY_EDITOR
		public bool disableCullingMask { get; set; } = false;
#endif
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
			// dispose all path
			foreach (var pair in renderPaths)
			{
				pair.Value.Dispose();
				Plugin.EffekseerAddRemovingRenderPath(pair.Value.renderId);
			}
			renderPaths.Clear();
		}

		public CommandBuffer GetCameraCommandBuffer(Camera camera)
		{
			if (renderPaths.ContainsKey(camera))
			{
				return renderPaths[camera].commandBuffer;
			}
			return null;
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
			var settings = EffekseerSettings.Instance;

#if UNITY_EDITOR
			if (camera.cameraType == CameraType.SceneView)
			{
				// check a camera in the scene view
				if (settings.drawInSceneView == false)
				{
					return;
				}
			}
#endif

			// check a culling mask
			var mask = Effekseer.Plugin.EffekseerGetCameraCullingMaskToShowAllEffects();

#if UNITY_EDITOR
			if (disableCullingMask)
			{
				mask = camera.cullingMask;
			}
#endif

			// don't need to update because doesn't exists and need not to render
			if ((camera.cullingMask & mask) == 0 && !renderPaths.ContainsKey(camera))
			{

				return;
			}

			// GC renderpaths
			bool hasDisposed = false;
			foreach (var path_ in renderPaths)
			{
				path_.Value.LifeTime--;
				if (path_.Value.LifeTime < 0)
				{
					path_.Value.Dispose();
					hasDisposed = true;
				}
			}

			// dispose renderpaths
			if (hasDisposed)
			{
				List<Camera> removed = new List<Camera>();
				foreach (var path_ in renderPaths)
				{
					if (path_.Value.LifeTime >= 0) continue;

					removed.Add(path_.Key);
					Plugin.EffekseerAddRemovingRenderPath(path_.Value.renderId);
				}

				foreach (var r in removed)
				{
					renderPaths.Remove(r);
				}
			}

			RenderPath path;

			if (renderPaths.ContainsKey(camera))
			{
				path = renderPaths[camera];
			}
			else
			{
				// render path doesn't exists, create a render path
				while (true)
				{
					bool found = false;
					foreach (var kv in renderPaths)
					{
						if (kv.Value.renderId == nextRenderID)
						{
							found = true;
							break;
						}
					}

					if (found)
					{
						nextRenderID++;
					}
					else
					{
						break;
					}
				}

				path = new RenderPath(camera, cameraEvent, nextRenderID, targetCommandBuffer != null);
				var stereoRenderingType = (camera.stereoEnabled) ? StereoRendererUtil.GetStereoRenderingType() : StereoRendererUtil.StereoRenderingTypes.None;
				path.Init(EffekseerRendererUtils.IsDistortionEnabled, EffekseerRendererUtils.IsDepthEnabled, renderTargetProperty, blitter, stereoRenderingType);
				renderPaths.Add(camera, path);
				nextRenderID = (nextRenderID + 1) % EffekseerRendererUtils.RenderIDCount;
			}

			if (!path.IsValid(renderTargetProperty))
			{
				path.Dispose();
				var stereoRenderingType = (camera.stereoEnabled) ? StereoRendererUtil.GetStereoRenderingType() : StereoRendererUtil.StereoRenderingTypes.None;
				path.Init(EffekseerRendererUtils.IsDistortionEnabled, EffekseerRendererUtils.IsDepthEnabled, renderTargetProperty, blitter, stereoRenderingType);
			}

			var screenSize = BackgroundRenderTexture.GetRequiredSize(camera, renderTargetProperty);

			path.LifeTime = 60;
			Plugin.EffekseerSetRenderingCameraCullingMask(path.renderId, camera.cullingMask);

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
			if (renderPaths.ContainsKey(camera))
			{
				RenderPath path = renderPaths[camera];
				Plugin.EffekseerSetRenderSettings(path.renderId,
					(camera.activeTexture != null));
			}
		}
	}

}