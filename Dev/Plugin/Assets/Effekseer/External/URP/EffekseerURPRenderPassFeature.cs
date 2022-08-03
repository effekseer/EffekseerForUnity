#if EFFEKSEER_URP_SUPPORT

using Effekseer.Internal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class UrpBlitter : IEffekseerBlitter
{	
	public static readonly int sourceTex = Shader.PropertyToID("_SourceTex");
	private Material blitMaterial;

	public UrpBlitter()
	{
		this.blitMaterial = CoreUtils.CreateEngineMaterial("Hidden/Universal Render Pipeline/Blit");
	}

	public void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier dest)
	{
		CoreUtils.SetRenderTarget(
			cmd,
			dest,
			RenderBufferLoadAction.Load,
			RenderBufferStoreAction.Store,
			ClearFlag.None,
			Color.black);
		cmd.SetGlobalTexture(sourceTex, source);
		cmd.DrawProcedural(Matrix4x4.identity, blitMaterial, 0, MeshTopology.Quads, 4);
	}
}

public class EffekseerURPRenderPassFeature : ScriptableRendererFeature
{
	class EffekseerRenderPassURP : UnityEngine.Rendering.Universal.ScriptableRenderPass
	{
#if !EFFEKSEER_URP_DEPTHTARGET_FIX
		RenderTargetIdentifier cameraColorTarget;
		RenderTargetIdentifier cameraDepthTarget;
#endif
		Effekseer.Internal.RenderTargetProperty prop = new Effekseer.Internal.RenderTargetProperty();
		private IEffekseerBlitter blitter = new UrpBlitter();

		public EffekseerRenderPassURP(ScriptableRenderer renderer)
		{
			this.renderPassEvent = UnityEngine.Rendering.Universal.RenderPassEvent.AfterRenderingTransparents;
		}

		bool IsValid(RenderTargetIdentifier identifer)
		{
			// HACK
			return !identifer.ToString().Contains("NameID -1");
		}

#if !EFFEKSEER_URP_DEPTHTARGET_FIX
		public void Setup(RenderTargetIdentifier cameraColorTarget, RenderTargetIdentifier cameraDepthTarget)
		{
			bool isValidDepth = IsValid(cameraDepthTarget);

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
			var renderer = renderingData.cameraData.renderer;
			prop.colorTargetIdentifier = renderer.cameraColorTarget;

			var isValidDepth = IsValid(renderer.cameraDepthTarget);

			if (isValidDepth)
			{
				prop.depthTargetIdentifier = renderer.cameraDepthTarget;
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
			Effekseer.EffekseerSystem.Instance.renderer.Render(renderingData.cameraData.camera, prop, null, blitter);
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
