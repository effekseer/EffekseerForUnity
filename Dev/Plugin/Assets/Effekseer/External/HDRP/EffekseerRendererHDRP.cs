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

			// HDRP-WA-DRS-001 (English): HDRP 14-17 keep camera RTHandles at their maximum allocation
			// while Dynamic Resolution changes only the active viewport. camera.pixelRect is the
			// unscaled GameView rectangle and copying that area also samples unused RTHandle pixels.
			// Use the camera color RTHandle's current viewport for target binding and source sampling.
			// Remove this only when HDRP exposes a camera color descriptor whose size is the active
			// viewport rather than the backing allocation.
			// HDRP-WA-DRS-001 (日本語): HDRP 14-17 は Camera RTHandle を最大サイズのまま確保し、
			// Dynamic Resolution では有効 Viewport のみを変更します。camera.pixelRect は縮小前の
			// GameView 領域なので、そのままコピーすると RTHandle の未使用領域まで参照します。
			// Target の設定とコピー元の Sampling には Camera Color RTHandle の現在の Viewport を
			// 使用します。HDRP が確保サイズではなく有効 Viewport サイズの Camera Color Descriptor を
			// 提供するようになった場合のみ削除してください。
			var activeViewportSize = colorBuffer.GetScaledSize(colorBuffer.rtHandleProperties.currentViewportSize);
			activeViewportSize.x = Mathf.Clamp(activeViewportSize.x, 1, colorRT.width);
			activeViewportSize.y = Mathf.Clamp(activeViewportSize.y, 1, colorRT.height);
			prop.ActualScreenSize = activeViewportSize;
			prop.SourceViewport = new Rect(0, 0, activeViewportSize.x, activeViewportSize.y);
			prop.Viewport = new Rect(0, 0, activeViewportSize.x, activeViewportSize.y);

			prop.colorTargetDescriptor = EffekseerRenderTargetDescriptorUtils.CreateTemporaryColorDescriptor(
				colorRT, hdCamera.camera);
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
