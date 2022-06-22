#if EFFEKSEER_HDRP_SUPPORT

using Effekseer.Internal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Effekseer
{
	class EffekseerRenderPassHDRP : UnityEngine.Rendering.HighDefinition.CustomPass
	{
		Effekseer.Internal.RenderTargetProperty prop = new Internal.RenderTargetProperty();
		private IEffekseerBlitter blitter = new StandardBlitter();

		public EffekseerRenderPassHDRP()
		{

		}

		protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
		{
			prop = new Internal.RenderTargetProperty();
			base.Setup(renderContext, cmd);
		}

		protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
		{
			if (EffekseerSystem.Instance == null) return;

			RTHandle colorBuffer;
			RTHandle depthBuffer;
			GetCameraBuffers(out colorBuffer, out depthBuffer);
			
			prop.colorTargetIdentifier = new RenderTargetIdentifier(colorBuffer);
			prop.depthTargetIdentifier = new RenderTargetIdentifier(depthBuffer);
			prop.colorTargetRenderTexture = (UnityEngine.RenderTexture)colorBuffer;
			prop.depthTargetRenderTexture = depthBuffer;
			prop.renderFeature = Effekseer.Internal.RenderFeature.HDRP;

			// TODO : It needs to support VR and override
			prop.Viewport = hdCamera.camera.pixelRect;

			prop.colorTargetDescriptor = new UnityEngine.RenderTextureDescriptor(hdCamera.actualWidth, hdCamera.actualHeight, colorBuffer.rt.format, 0, colorBuffer.rt.mipmapCount);
			prop.colorTargetDescriptor.msaaSamples = hdCamera.msaaSamples == MSAASamples.None ? 1 : 2;
			prop.isRequiredToChangeViewport = true;
			EffekseerSystem.Instance.renderer.Render(hdCamera.camera, prop, cmd, blitter);
		}

		protected override void Cleanup()
		{
			base.Cleanup();
		}
	}
}

#endif