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
#endif
		ScriptableRenderer renderer;
		Effekseer.Internal.RenderTargetProperty prop = new Effekseer.Internal.RenderTargetProperty();

		public EffekseerRenderPassURP(ScriptableRenderer renderer)
		{
			this.renderer = renderer;
			this.renderPassEvent = UnityEngine.Rendering.Universal.RenderPassEvent.AfterRenderingTransparents;
		}

#if !EFFEKSEER_URP_DEPTHTARGET_FIX
		public void Setup(RenderTargetIdentifier cameraColorTarget, RenderTargetIdentifier cameraDepthTarget)
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
#endif

		public override void Execute(ScriptableRenderContext context, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
		{
			if (Effekseer.EffekseerSystem.Instance == null) return;
#if EFFEKSEER_URP_DEPTHTARGET_FIX
			prop.colorTargetIdentifier = this.renderer.cameraColorTarget;

			var isValidDepth = renderingData.cameraData.cameraType != CameraType.SceneView;

			if (isValidDepth)
			{
				prop.depthTargetIdentifier = this.renderer.cameraDepthTarget;
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
		if (m_ScriptablePass == null)
		{
			m_ScriptablePass = new EffekseerRenderPassURP(renderer);
		}

		m_ScriptablePass.Setup(renderer.cameraColorTarget, renderer.cameraDepth);

		renderer.EnqueuePass(m_ScriptablePass);
#else
		m_ScriptablePass = m_ScriptablePass ?? new EffekseerRenderPassURP(renderer);
		renderer.EnqueuePass(m_ScriptablePass);
#endif
	}
}

#endif
