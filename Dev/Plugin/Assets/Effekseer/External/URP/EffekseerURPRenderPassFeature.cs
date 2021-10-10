#if EFFEKSEER_URP_SUPPORT

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class EffekseerURPRenderPassFeature : ScriptableRendererFeature
{
	class EffekseerRenderPassURP : UnityEngine.Rendering.Universal.ScriptableRenderPass
	{
#if !EFFEKSEER_URP_DEPTHTARGET_FIX
		RenderTargetIdentifier cameraColorTarget;
		RenderTargetIdentifier cameraDepthTarget;
#else
		ScriptableRenderer renderer;
#endif
		Effekseer.Internal.RenderTargetProperty prop = new Effekseer.Internal.RenderTargetProperty();

#if !EFFEKSEER_URP_DEPTHTARGET_FIX
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
#else
		public EffekseerRenderPassURP(ScriptableRenderer renderer)
		{
			this.renderer = renderer;
			this.renderPassEvent = UnityEngine.Rendering.Universal.RenderPassEvent.AfterRenderingTransparents;
		}
#endif

		public override void Execute(ScriptableRenderContext context, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
		{
			if (Effekseer.EffekseerSystem.Instance == null) return;
#if EFFEKSEER_URP_DEPTHTARGET_FIX
			prop.colorTargetIdentifier = this.renderer.cameraColorTarget;

			bool isValidDepth = !this.renderer.cameraDepthTarget.ToString().Contains("-1");

			if (isValidDepth)
			{
				prop.depthTargetIdentifier = this.renderer.cameraDepthTarget;
				Debug.Log("Valid depth");
			}
			else
			{
				prop.depthTargetIdentifier = null;
			}
#endif
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
#if !EFFEKSEER_URP_DEPTHTARGET_FIX
		m_ScriptablePass = new EffekseerRenderPassURP(renderer.cameraColorTarget, renderer.cameraDepth);
		m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
		renderer.EnqueuePass(m_ScriptablePass);
#else
		m_ScriptablePass = new EffekseerRenderPassURP(renderer);
		m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
		renderer.EnqueuePass(m_ScriptablePass);
#endif
	}
}

#endif
