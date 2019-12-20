/*
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Effekseer
{
	class EffekseerRenderPassHDRP : UnityEngine.Rendering.HighDefinition.CustomPass
	{
		Effekseer.Internal.RenderTargetProperty prop = new Internal.RenderTargetProperty();

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

			prop.Viewport = hdCamera.finalViewport;

			// TODO : improve it
			prop.colorTargetDescriptor = new UnityEngine.RenderTextureDescriptor(colorBuffer.rt.width, colorBuffer.rt.height, colorBuffer.rt.format, 0, colorBuffer.rt.mipmapCount);
			prop.colorTargetDescriptor.msaaSamples = hdCamera.msaaSamples == MSAASamples.None ? 1 : 2;

			EffekseerSystem.Instance.renderer.Render(hdCamera.camera, prop, cmd);
		}

		protected override void Cleanup()
		{
			base.Cleanup();
		}
	}
}
*/