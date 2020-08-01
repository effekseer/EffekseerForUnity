/*

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class EffekseerURPRenderPassFeature : ScriptableRendererFeature
{
	class EffekseerRenderPassURP : UnityEngine.Rendering.Universal.ScriptableRenderPass
	{
		RenderTargetIdentifier cameraColorTarget;
		RenderTargetIdentifier cameraDepthTarget;

		Effekseer.Internal.RenderTargetProperty prop = new Effekseer.Internal.RenderTargetProperty();

		public EffekseerRenderPassURP(RenderTargetIdentifier cameraColorTarget, RenderTargetIdentifier cameraDepthTarget)
		{
			// HACK
			bool isValidDepth = !cameraDepthTarget.ToString().Contains("-1");

			this.cameraColorTarget = cameraColorTarget;
			prop.colorTargetIdentifier = cameraColorTarget;
			this.renderPassEvent = UnityEngine.Rendering.Universal.RenderPassEvent.AfterRenderingTransparents;

			if(isValidDepth)
			{
				this.cameraDepthTarget = cameraDepthTarget;
				prop.depthTargetIdentifier = cameraDepthTarget;
			}
		}

		public override void Execute(ScriptableRenderContext context, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
		{
			if (Effekseer.EffekseerSystem.Instance == null) return;
			prop.colorTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
			prop.isRequiredToCopyBackground = true;
			Effekseer.EffekseerSystem.Instance.renderer.Render(renderingData.cameraData.camera, prop, null);
			var commandBuffer = Effekseer.EffekseerSystem.Instance.renderer.GetCameraCommandBuffer(renderingData.cameraData.camera);

			if (commandBuffer != null)
			{
				context.ExecuteCommandBuffer(commandBuffer);
				context.Submit();
			}
		}
	}

	EffekseerRenderPassURP m_ScriptablePass;

	public override void Create()
	{
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		m_ScriptablePass = new EffekseerRenderPassURP(renderer.cameraColorTarget, renderer.cameraDepth);
		m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
		renderer.EnqueuePass(m_ScriptablePass);

	}
}

*/
