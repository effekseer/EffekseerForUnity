// After 5.7
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
            this.cameraColorTarget = cameraColorTarget;
			this.cameraDepthTarget = cameraDepthTarget;
            this.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

			prop.colorTargetIdentifier = cameraColorTarget;
			prop.depthTargetIdentifier = cameraDepthTarget;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (EffekseerSystem.Instance == null) return;
			prop.colorTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            EffekseerSystem.Instance.renderer.Render(renderingData.cameraData.camera, null, prop);
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


// Before 5.7
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

			EffekseerSystem.Instance.renderer.Render(cameraComponent, dstID, null);
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