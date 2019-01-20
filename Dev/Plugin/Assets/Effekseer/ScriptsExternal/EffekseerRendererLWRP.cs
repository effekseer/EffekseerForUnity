/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;

namespace Effekseer
{
	[AddComponentMenu("Effekseer/Effekseer RendererLWRP")]
	public class EffekseerRendererLWRP : MonoBehaviour, IAfterTransparentPass
	{
		public ScriptableRenderPass GetPassToEnqueue(
			 RenderTextureDescriptor baseDescriptor,
			RenderTargetHandle colorHandle,
			RenderTargetHandle depthHandle)
		{
			var cameraComponent = gameObject.GetComponent<Camera>();

			return new EffekseerRenderPassLWRP(cameraComponent, colorHandle.id);
		}
	}

	class EffekseerRenderPassLWRP : ScriptableRenderPass
	{
		Camera cameraComponent = null;
		int dstID = 0;

		public EffekseerRenderPassLWRP(Camera cameraComponent, int dstID)
		{
			this.cameraComponent = cameraComponent;
			this.dstID = dstID;

			RegisterShaderPassName("Effekseer");
		}

		public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (EffekseerSystem.Instance == null) return;

			EffekseerSystem.Instance.renderer.Render(cameraComponent, dstID);
			var commandBuffer = EffekseerSystem.Instance.renderer.GetCameraCommandBuffer(cameraComponent);

			if (commandBuffer != null)
			{
				context.ExecuteCommandBuffer(commandBuffer);
				context.Submit();
			}
		}
	}
}
*/