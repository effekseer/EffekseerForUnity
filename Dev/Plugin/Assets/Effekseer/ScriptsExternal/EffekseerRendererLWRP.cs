/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;


namespace Effekseer
{
	public class EffekseerRendererLWRP : ScriptableRendererFeature
	{
		static RenderTargetHandle afterTransparent;

		public EffekseerRendererLWRP()
		{
			afterTransparent.Init("_AfterTransparent");
		}

		public override void Create()
		{
			Debug.LogWarning("Plese use URP, LWRP for Effekseer is deprecated.");
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			var pass = new EffekseerRenderPassLWRP(renderer.cameraColorTarget, renderer.cameraDepth);
			renderer.EnqueuePass(pass);
		}
	}

	class EffekseerRenderPassLWRP : ScriptableRenderPass
	{
		RenderTargetIdentifier cameraColorTarget;
		RenderTargetIdentifier cameraDepthTarget;

		Effekseer.Internal.RenderTargetProperty prop = new Internal.RenderTargetProperty();

		public EffekseerRenderPassLWRP(RenderTargetIdentifier cameraColorTarget, RenderTargetIdentifier cameraDepthTarget)
		{
			// HACK
			bool isValidDepth = !cameraDepthTarget.ToString().Contains("-1");

			this.cameraColorTarget = cameraColorTarget;
			prop.colorTargetIdentifier = cameraColorTarget;
			this.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

			if(isValidDepth)
			{
				this.cameraDepthTarget = cameraDepthTarget;
				prop.depthTargetIdentifier = cameraDepthTarget;
			}
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (EffekseerSystem.Instance == null) return;
			prop.colorTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
			prop.isRequiredToCopyBackground = true;

			EffekseerSystem.Instance.renderer.Render(renderingData.cameraData.camera, prop, null);
			var commandBuffer = EffekseerSystem.Instance.renderer.GetCameraCommandBuffer(renderingData.cameraData.camera);


			if (commandBuffer != null)
			{
				context.ExecuteCommandBuffer(commandBuffer);
				context.Submit();
			}
		}
	}
}
*/
