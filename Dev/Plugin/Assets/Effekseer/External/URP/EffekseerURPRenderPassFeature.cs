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

	public void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier dest, bool xrRendering)
	{
		if(xrRendering)
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
		else
		{
			cmd.Blit(source, dest);
		}
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

		bool IsValidCameraDepthTarget(RenderTargetIdentifier cameraDepthTarget)
		{
			// HACK: When using URP, the depth might be either written to a DepthBuffer attached to
			//       - cameraColorTarget
			//       OR
			//       - cameraDepthTarget
			//
			//       Which one contains the depth is dependent on many variables including but not limited to:
			//       - Unity Editor version
			//       - whether camera stacking is used
			//       - whether MSAA is enabled
			//       - whether Depth Texture is enabled
			//
			//       Effekseer needs to know where it can access the depth buffer. This hack checks if the depth is
			//       written to cameraDepthTarget based on the observation, that whenever Unity is writing depth to
			//       cameraDepthTarget, cameraDepthTarget's RenderTargetIdentifier contains either
			//       - NameId xxx (where xxx is an integer other than -1)
			//       OR
			//       - InstanceID yyy (where yyy is an integer other than 0)
			//
			//       A RenderTargetIdentifier might point to a valid RenderTexture in many different ways
			//       (including NameID or InstanceID), whether NameID or InstanceID is used to identify a valid
			//       RenderTexture depends on the Unity Editor / URP package version.
			var identifierString = cameraDepthTarget.ToString();
			return !identifierString.Contains("NameID -1") || !identifierString.Contains("InstanceID 0");
		}

#if !EFFEKSEER_URP_DEPTHTARGET_FIX
		public void Setup(RenderTargetIdentifier cameraColorTarget, RenderTargetIdentifier cameraDepthTarget)
		{
			bool isValidDepth = IsValidCameraDepthTarget(cameraDepthTarget);

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

			// NOTE: We need to know whether the depth in cameraDepthTarget is valid or not since if it is valid,
			//       we need to pass cameraDepthTarget to SetRenderTarget() later on. If it isn't valid, the depth in
			//       cameraColorTarget is used instead.
			var isValidDepth = IsValidCameraDepthTarget(renderer.cameraDepthTarget);

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
			
			// Linear and native renderer makes a result white.
			prop.colorTargetDescriptor.sRGB = false;

			prop.isRequiredToCopyBackground = true;
			prop.renderFeature = Effekseer.Internal.RenderFeature.URP;
#if EFFEKSEER_URP_XRRENDERING
			prop.xrRendering = renderingData.cameraData.xrRendering;
#endif
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
