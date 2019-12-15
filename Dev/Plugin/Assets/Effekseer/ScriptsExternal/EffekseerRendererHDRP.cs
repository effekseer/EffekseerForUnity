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
			base.Setup(renderContext, cmd);
		}

		protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
		{
			if (EffekseerSystem.Instance == null) return;

			RTHandle colorBuffer;
			RTHandle depthBuffer;
			GetCameraBuffers(out colorBuffer, out depthBuffer);

			EffekseerSystem.Instance.renderer.Render(hdCamera.camera, prop, cmd);
		}

		protected override void Cleanup()
		{
			base.Cleanup();
		}
	}
}
*/