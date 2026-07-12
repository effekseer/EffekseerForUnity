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
#if UNITY_6000_0_OR_NEWER
			// Effekseer rebinds the camera depth explicitly during execution, so the
			// HDRP custom pass itself doesn't need to declare Camera depth here.
			targetColorBuffer = TargetBuffer.Camera;
			targetDepthBuffer = TargetBuffer.None;
#endif
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

			prop.ActualScreenSize = new Vector2Int(hdCamera.actualWidth, hdCamera.actualHeight);
			prop.SourceViewport = hdCamera.camera.pixelRect;
#if UNITY_6000_0_OR_NEWER
			// HDRP keeps the full-resolution RT allocated and shrinks the active viewport when
			// dynamic resolution is enabled, so keep the render viewport and the source
			// sampling viewport separate.
			prop.Viewport = new Rect(0, 0, hdCamera.actualWidth, hdCamera.actualHeight);
#else
			prop.Viewport = new Rect(0, 0, hdCamera.camera.pixelRect.width, hdCamera.camera.pixelRect.height);
#endif

			// XR-WA-005 (English): Preserve HDRP's authoritative camera descriptor so temporary
			// Effekseer resources keep the XR dimension, view count and dynamic-resolution flags.
			// Remove this only if HDRP exposes an equivalent descriptor-independent allocation API.
			// XR-WA-005 (日本語): Effekseer の一時リソースへ XR dimension、view 数、動的解像度フラグを
			// 引き継ぐため、HDRP のカメラ Descriptor を維持します。同等の Descriptor 非依存 Allocation API が
			// HDRP に追加された場合のみ削除してください。
			prop.colorTargetDescriptor = colorRT.descriptor;
			prop.colorTargetDescriptor.depthBufferBits = 0;
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

			EffekseerRenderCoordinator.Render(EffekseerSystem.Instance.renderer,
				new EffekseerRenderFrameInput(hdCamera.camera, LayerMask.value, prop, cmd, true, blitter));
		}

		protected override void Execute(CustomPassContext ctx)
		{
			// CustomPassContext is available from HDRP 14 (Unity 2022.3) onward and
			// remains the RenderGraph-compatible entry point in Unity 6.
			Execute(ctx.cameraColorBuffer, ctx.cameraDepthBuffer, ctx.cmd, ctx.hdCamera);
		}

		protected override void Cleanup()
		{
			base.Cleanup();
		}
	}
}

#endif
