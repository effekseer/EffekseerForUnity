#if EFFEKSEER_URP_SUPPORT

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class EffekseerURPRenderPassFeature : ScriptableRendererFeature
{
	class EffekseerRenderPassURP : UnityEngine.Rendering.Universal.ScriptableRenderPass
	{
		Effekseer.Internal.RenderTargetProperty prop = new Effekseer.Internal.RenderTargetProperty();

		public EffekseerRenderPassURP()
		{
			this.renderPassEvent = UnityEngine.Rendering.Universal.RenderPassEvent.AfterRenderingTransparents;
			prop.colorTargetIdentifier = colorAttachment;
			prop.depthTargetIdentifier = depthAttachment;
		}

		public override void Execute(ScriptableRenderContext context, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
		{
			if (Effekseer.EffekseerSystem.Instance == null) return;
			prop.colorTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
			prop.isRequiredToCopyBackground = true;
			prop.renderFeature = Effekseer.Internal.RenderFeature.URP;
			prop.canGrabDepth = renderingData.cameraData.requiresDepthTexture;
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
		m_ScriptablePass = new EffekseerRenderPassURP();
		m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
		renderer.EnqueuePass(m_ScriptablePass);

	}
}

#endif
