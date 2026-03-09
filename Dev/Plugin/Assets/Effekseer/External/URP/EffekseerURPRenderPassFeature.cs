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
	public void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier dest, bool xrRendering)
	{
		if (xrRendering)
		{
			CoreUtils.SetRenderTarget(cmd, dest);
			// FIXME: Scaling is ignored.
			//        The interface should take RTHandle instead of RenderTargetIdentifier and use Blitter.BlitCameraTexture.
			//        However, this will cause issues in terms of compatibility with Built-in RP support.
			Blitter.BlitTexture(cmd, source, Vector2.one, Blitter.GetBlitMaterial(TextureXR.dimension), 0);
		}
		else
		{
			cmd.Blit(source, dest);
		}
	}

	public void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier dest, Material material, bool xrRendering)
	{
		cmd.Blit(source, dest, material);
	}

	public void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier color, bool xrRendering)
	{
		CoreUtils.SetRenderTarget(cmd, color);
	}

	public void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier color, RenderTargetIdentifier depth, Vector2? actualScreenSize, bool xrRendering)
	{
		CoreUtils.SetRenderTarget(cmd, color, depth);
	}
}

public class EffekseerURPRenderPassFeature : ScriptableRendererFeature
{
	public UnityEngine.LayerMask LayerMask = ~0;

	class EffekseerRenderPassURP : UnityEngine.Rendering.Universal.ScriptableRenderPass
	{
		Effekseer.Internal.RenderTargetProperty prop = new Effekseer.Internal.RenderTargetProperty();
		private readonly IEffekseerBlitter blitter = new UrpBlitter();
		UnityEngine.LayerMask layerMask;
		private const string RenderPassName = nameof(EffekseerRenderPassURP);

		public EffekseerRenderPassURP(UnityEngine.LayerMask layerMask)
		{
			this.renderPassEvent = UnityEngine.Rendering.Universal.RenderPassEvent.AfterRenderingTransparents;
			this.layerMask = layerMask;
		}

		public void SetLayerMask(UnityEngine.LayerMask layerMask)
		{
			this.layerMask = layerMask;
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

		void PrepareRenderTargetProperty(RenderTargetProperty renderTargetProperty, RenderTextureDescriptor colorTargetDescriptor, bool requiresDepthTexture, bool xrRendering)
		{
			renderTargetProperty.colorBufferID = null;
			renderTargetProperty.depthTargetIdentifier = null;
			renderTargetProperty.colorTargetRenderTexture = null;
			renderTargetProperty.depthTargetRenderTexture = null;
			renderTargetProperty.ActualScreenSize = null;
			renderTargetProperty.Viewport = null;
			renderTargetProperty.SourceViewport = null;
			renderTargetProperty.isRequiredToChangeViewport = false;
			renderTargetProperty.colorTargetDescriptor = colorTargetDescriptor;

			// Linear and native renderer makes a result white.
			renderTargetProperty.colorTargetDescriptor.sRGB = false;
			renderTargetProperty.isRequiredToCopyBackground = true;
			renderTargetProperty.renderFeature = Effekseer.Internal.RenderFeature.URP;
			renderTargetProperty.canGrabDepth = requiresDepthTexture;
			renderTargetProperty.xrRendering = xrRendering;
		}

#if UNITY_6000_0_OR_NEWER
		[Obsolete]
#endif
		public override void Execute(ScriptableRenderContext context, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
		{
			if (Effekseer.EffekseerSystem.Instance == null) return;
			var xrRendering = renderingData.cameraData.xrRendering;
			PrepareRenderTargetProperty(prop, renderingData.cameraData.cameraTargetDescriptor, renderingData.cameraData.requiresDepthTexture, xrRendering);
			var renderer = renderingData.cameraData.renderer;
			prop.colorTargetIdentifier = renderer.cameraColorTargetHandle;

			// NOTE: We need to know whether the depth in cameraDepthTarget is valid or not since if it is valid,
			//       we need to pass cameraDepthTarget to SetRenderTarget() later on. If it isn't valid, the depth in
			//       cameraColorTarget is used instead.
			var cameraDepthTarget = renderer.cameraDepthTargetHandle;
			var isValidDepth = IsValidCameraDepthTarget(cameraDepthTarget);

			if (isValidDepth)
			{
				prop.depthTargetIdentifier = cameraDepthTarget;
			}
			else
			{
				prop.depthTargetIdentifier = null;
			}

			var cmd = CommandBufferPool.Get(RenderPassName);
			Effekseer.EffekseerSystem.Instance.renderer.Render(renderingData.cameraData.camera, layerMask.value, prop, cmd, true, blitter);
			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}

#if UNITY_6000_0_OR_NEWER
		class PassData
		{
			public Camera camera;
			public int layerMask;
			public TextureHandle colorTexture;
			public TextureHandle depthTexture;

			public Effekseer.Internal.RenderTargetProperty prop = new();
			public IEffekseerBlitter blitter = new UrpBlitter();
		}

		static void ExecuteRenderGraphPass(PassData passData, UnsafeGraphContext context)
		{
			var system = Effekseer.EffekseerSystem.Instance;
			if (system == null || passData.camera == null)
			{
				return;
			}

			var commandBuffer = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
			passData.prop.colorTargetIdentifier = (RenderTargetIdentifier)passData.colorTexture;
			passData.prop.depthTargetIdentifier = passData.depthTexture.IsValid() ? (RenderTargetIdentifier)passData.depthTexture : (RenderTargetIdentifier?)null;
			system.renderer.Render(passData.camera, passData.layerMask, passData.prop, commandBuffer, true, passData.blitter);
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			if (Effekseer.EffekseerSystem.Instance == null) return;

			UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
			UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
			var colorTexture = resourceData.activeColorTexture;
			if (!colorTexture.IsValid())
			{
				return;
			}

			var xrRendering = cameraData.xrRendering;

			using (var builder = renderGraph.AddUnsafePass<PassData>("EffekseerPass", out var passData, profilingSampler))
			{
				passData.camera = cameraData.camera;
				passData.layerMask = layerMask.value;
				passData.blitter = this.blitter;
				passData.colorTexture = colorTexture;
				builder.UseTexture(passData.colorTexture, AccessFlags.ReadWrite);
				passData.depthTexture = resourceData.activeDepthTexture;
				if (passData.depthTexture.IsValid())
				{
					builder.UseTexture(passData.depthTexture, AccessFlags.Write);
				}
				PrepareRenderTargetProperty(passData.prop, cameraData.cameraTargetDescriptor, cameraData.requiresDepthTexture, xrRendering);

				builder.AllowPassCulling(false);
				builder.AllowGlobalStateModification(true);
				builder.SetRenderFunc(static (PassData passData, UnsafeGraphContext context) => ExecuteRenderGraphPass(passData, context));
			}
		}
#endif
	}

	EffekseerRenderPassURP m_ScriptablePass;

	public override void Create()
	{
		m_ScriptablePass = new EffekseerRenderPassURP(LayerMask);
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		m_ScriptablePass = m_ScriptablePass ?? new EffekseerRenderPassURP(LayerMask);
		m_ScriptablePass.SetLayerMask(LayerMask);
		renderer.EnqueuePass(m_ScriptablePass);
	}
}

#endif
