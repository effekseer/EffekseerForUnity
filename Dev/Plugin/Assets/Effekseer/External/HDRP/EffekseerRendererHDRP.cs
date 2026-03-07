#if EFFEKSEER_HDRP_SUPPORT

using System;
using Effekseer.Internal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Effekseer
{
	[Serializable]
	class EffekseerRenderPassHDRP : UnityEngine.Rendering.HighDefinition.CustomPass
	{
		Effekseer.Internal.RenderTargetProperty prop = new Internal.RenderTargetProperty();
		private IEffekseerBlitter blitter = new StandardBlitter();

		public UnityEngine.LayerMask LayerMask = ~0;

		public EffekseerRenderPassHDRP()
		{

		}

		protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
		{
			prop = new Internal.RenderTargetProperty();
			base.Setup(renderContext, cmd);
		}

		bool TryPrepareRender(RTHandle colorBuffer, RTHandle depthBuffer, HDCamera hdCamera)
		{
			if (hdCamera == null || hdCamera.camera == null || colorBuffer == null || depthBuffer == null)
			{
				return false;
			}

			var colorRT = colorBuffer.rt;
			if (colorRT == null)
			{
				return false;
			}

			prop.colorTargetIdentifier = new RenderTargetIdentifier(colorBuffer);
			prop.depthTargetIdentifier = new RenderTargetIdentifier(depthBuffer);
			prop.colorTargetRenderTexture = (UnityEngine.RenderTexture)colorBuffer;
			prop.depthTargetRenderTexture = depthBuffer;
			prop.renderFeature = Effekseer.Internal.RenderFeature.HDRP;

			// TODO : It needs to support VR and override
			prop.ActualScreenSize = new Vector2Int(hdCamera.actualWidth, hdCamera.actualHeight);
			prop.Viewport = new Rect(0, 0, hdCamera.camera.pixelRect.width, hdCamera.camera.pixelRect.height);

			prop.colorTargetDescriptor = new UnityEngine.RenderTextureDescriptor(colorRT.width, colorRT.height, colorRT.format, 0, colorRT.mipmapCount);
			prop.colorTargetDescriptor.msaaSamples = hdCamera.msaaSamples == MSAASamples.None ? 1 : 2;
			prop.isRequiredToChangeViewport = true;
			return true;
		}

		void Execute(RTHandle colorBuffer, RTHandle depthBuffer, CommandBuffer cmd, HDCamera hdCamera)
		{
			if (EffekseerSystem.Instance == null || cmd == null)
			{
				return;
			}

			if (!TryPrepareRender(colorBuffer, depthBuffer, hdCamera))
			{
				return;
			}

			EffekseerSystem.Instance.renderer.Render(hdCamera.camera, LayerMask.value, prop, cmd, true, blitter);
		}

#if UNITY_6000_0_OR_NEWER
		protected override void Execute(CustomPassContext ctx)
		{
			// Unity 6 HDRP executes custom passes through RenderGraph internally.
			// Keep the RenderGraph entry point isolated from the legacy path so future
			// HDRP changes stay localized in this file.
			Execute(ctx.cameraColorBuffer, ctx.cameraDepthBuffer, ctx.cmd, ctx.hdCamera);
		}
#else
		protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
		{
			RTHandle colorBuffer;
			RTHandle depthBuffer;
			GetCameraBuffers(out colorBuffer, out depthBuffer);
			Execute(colorBuffer, depthBuffer, cmd, hdCamera);
		}
#endif

		protected override void Cleanup()
		{
			base.Cleanup();
		}
	}
}

#endif
