#if EFFEKSEER_URP_SUPPORT

using Effekseer.Internal;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

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
		if (xrRendering)
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

#if UNITY_6000_0_OR_NEWER
		[Obsolete]
#endif
		public override void Execute(ScriptableRenderContext context, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
		{
			if (Effekseer.EffekseerSystem.Instance == null) return;
#if EFFEKSEER_URP_DEPTHTARGET_FIX
			var renderer = renderingData.cameraData.renderer;
#if UNITY_2022_3_OR_NEWER
			prop.colorTargetIdentifier = renderer.cameraColorTargetHandle;
#else
			prop.colorTargetIdentifier = renderer.cameraColorTarget;
#endif

			// NOTE: We need to know whether the depth in cameraDepthTarget is valid or not since if it is valid,
			//       we need to pass cameraDepthTarget to SetRenderTarget() later on. If it isn't valid, the depth in
			//       cameraColorTarget is used instead.
#if UNITY_2022_3_OR_NEWER
			var cameraDepthTarget = renderer.cameraDepthTargetHandle;
#else
			var cameraDepthTarget = renderer.cameraDepthTarget;
#endif
			var isValidDepth = IsValidCameraDepthTarget(cameraDepthTarget);

			if (isValidDepth)
			{
				prop.depthTargetIdentifier = cameraDepthTarget;
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
			Effekseer.EffekseerSystem.Instance.renderer.Render(renderingData.cameraData.camera, prop, null, true, blitter);
			var commandBuffer = Effekseer.EffekseerSystem.Instance.renderer.GetCameraCommandBuffer(renderingData.cameraData.camera);

			if (commandBuffer != null)
			{
				context.ExecuteCommandBuffer(commandBuffer);
			}
		}

#if UNITY_6000_0_OR_NEWER
		class PassData
		{
			public TextureHandle colorTexture;
			public TextureHandle depthTexture;

			public Effekseer.Internal.RenderTargetProperty prop = new();
			public IEffekseerBlitter blitter = new UrpBlitter();
		}

		class DummyPassData
		{
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			if (Effekseer.EffekseerSystem.Instance == null) return;

			string profilerTag = "EffekseerPath";
			string profilerDummyTag = "EffekseerDummyPath";

			UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

			using (var builder = renderGraph.AddUnsafePass<PassData>(profilerTag, out var passData))
			{
				UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

				passData.blitter = this.blitter;

				//passData.prop.colorTargetIdentifier = resourceData.activeColorTexture;
				//passData.prop.depthTargetIdentifier = resourceData.activeDepthTexture;
				passData.colorTexture = resourceData.activeColorTexture;
				passData.depthTexture = resourceData.activeDepthTexture;
				passData.prop.colorTargetDescriptor = cameraData.cameraTargetDescriptor;

				// Linear and native renderer makes a result white.
				passData.prop.colorTargetDescriptor.sRGB = false;

				passData.prop.isRequiredToCopyBackground = true;
				passData.prop.renderFeature = Effekseer.Internal.RenderFeature.URP;
#if EFFEKSEER_URP_XRRENDERING
				passData.prop.xrRendering = cameraData.xrRendering;
#endif
				passData.prop.canGrabDepth = cameraData.requiresDepthTexture;

				builder.AllowPassCulling(false);
				builder.AllowGlobalStateModification(true);

				builder.SetRenderFunc((PassData passData, UnsafeGraphContext context) =>
				{
					using (new ProfilingScope(context.cmd, profilingSampler))
					{
						var commandBuffer = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
						passData.prop.colorTargetIdentifier = passData.colorTexture;
						passData.prop.depthTargetIdentifier = passData.depthTexture;

						Effekseer.EffekseerSystem.Instance.renderer.Render(cameraData.camera, passData.prop, commandBuffer, true, passData.blitter);
					}
				});
			}

			// HACK : Dummy pass to ensure that the render graph is executed
			using (var builder = renderGraph.AddRasterRenderPass<DummyPassData>(profilerDummyTag, out var passData))
			{
				UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
				builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
				builder.AllowPassCulling(false);
				builder.AllowGlobalStateModification(true);

				builder.SetRenderFunc((DummyPassData passData, RasterGraphContext context) =>
				{
					using (new ProfilingScope(context.cmd, profilingSampler))
					{
						context.cmd.BeginSample(profilerDummyTag);
						context.cmd.EndSample(profilerDummyTag);
					}
				});
			}
		}
#endif
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
