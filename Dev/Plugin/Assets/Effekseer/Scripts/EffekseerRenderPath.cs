using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace Effekseer.Internal
{
	internal class RenderPathBase : IDisposable
	{
		public bool isCommandBufferFromExternal = false;
		public CommandBuffer commandBuffer;
		public Camera camera;
		public int renderId;
		public int LifeTime = 5;
		public CameraEvent cameraEvent;
		public BackgroundRenderTexture renderTexture;
		public DepthRenderTexture depthTexture;
		protected bool isDistortionEnabled = false;
		protected bool isDepthEnabled = false;

		/// <summary>
		/// Distortion is disabled forcely because of VR
		/// </summary>
		protected bool isDistortionMakeDisabledForcely = false;

		public virtual void Init(Camera camera, CameraEvent cameraEvent, int renderId, bool isCommandBufferFromExternal) { }

		public virtual void ResetParameters(bool enableDistortion, bool enableDepth, RenderTargetProperty renderTargetProperty,
				IEffekseerBlitter blitter, StereoRendererUtil.StereoRenderingTypes stereoRenderingType = StereoRendererUtil.StereoRenderingTypes.None)
		{ }

		public virtual void Update() { }

		public virtual void Dispose()
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

			if (renderTexture != null)
			{
				renderTexture.Release();
				renderTexture = null;
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

				if (targetSize.x != this.depthTexture.width ||
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
	}

	internal class RenderPathContainer<T> where T : RenderPathBase, new()
	{
		Dictionary<Camera, T> renderPaths = new Dictionary<Camera, T>();
		int nextRenderID = 0;

		public void CleanUp()
		{
			// dispose all render pathes
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

		public void UpdateRenderPath(bool disableCullingMask, Camera camera, int additionalMask, RenderTargetProperty renderTargetProperty, CommandBuffer targetCommandBuffer, IEffekseerBlitter blitter, CameraEvent cameraEvent, out T path, out int allEffectMask, out int cameraMask)
		{
			path = null;

			// check a culling mask
			allEffectMask = Effekseer.Plugin.EffekseerGetCameraCullingMaskToShowAllEffects();
			cameraMask = camera.cullingMask;
			cameraMask = cameraMask & additionalMask;

#if UNITY_EDITOR
			var settings = EffekseerSettings.Instance;
			if (camera.cameraType == CameraType.SceneView)
			{
				// check a camera in the scene view
				if (settings.drawInSceneView == false)
				{
					return;
				}
			}
#endif

#if UNITY_EDITOR
			if (disableCullingMask)
			{
				cameraMask = allEffectMask;
			}
#endif

			// don't need to update because doesn't exists and need not to render
			if ((allEffectMask & cameraMask) == 0 && !renderPaths.ContainsKey(camera))
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
				foreach (var renderPath in renderPaths)
				{
					if (renderPath.Value.LifeTime >= 0) continue;

					removed.Add(renderPath.Key);
					Plugin.EffekseerAddRemovingRenderPath(renderPath.Value.renderId);
				}

				foreach (var r in removed)
				{
					renderPaths.Remove(r);
				}
			}

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

				path = new T();
				path.Init(camera, cameraEvent, nextRenderID, targetCommandBuffer != null);
				var stereoRenderingType = (camera.stereoEnabled) ? StereoRendererUtil.GetStereoRenderingType() : StereoRendererUtil.StereoRenderingTypes.None;
				path.ResetParameters(EffekseerRendererUtils.IsDistortionEnabled, EffekseerRendererUtils.IsDepthEnabled, renderTargetProperty, blitter, stereoRenderingType);
				renderPaths.Add(camera, path);
				nextRenderID = (nextRenderID + 1) % EffekseerRendererUtils.RenderIDCount;
			}

			if (!path.IsValid(renderTargetProperty))
			{
				path.Dispose();
				var stereoRenderingType = (camera.stereoEnabled) ? StereoRendererUtil.GetStereoRenderingType() : StereoRendererUtil.StereoRenderingTypes.None;
				path.ResetParameters(EffekseerRendererUtils.IsDistortionEnabled, EffekseerRendererUtils.IsDepthEnabled, renderTargetProperty, blitter, stereoRenderingType);
			}

			path.Update();
			path.LifeTime = 60;
			Plugin.EffekseerSetRenderingCameraCullingMask(path.renderId, cameraMask);
		}

		public void OnPostRender(Camera camera)
		{
			if (renderPaths.ContainsKey(camera))
			{
				var path = renderPaths[camera];
				Plugin.EffekseerSetRenderSettings(path.renderId,
					(camera.activeTexture != null));
			}
		}
	}
}
