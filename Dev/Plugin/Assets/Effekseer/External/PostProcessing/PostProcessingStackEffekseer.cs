#if EFFEKSEER_POSTPROCESSING_SUPPORT

using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

class PostProcessingStackEffekseerRendererBeforeStack : PostProcessingStackEffekseerRenderer<PostProcessingStackEffekseerBeforeStackSettings>
{
}
class PostProcessingStackEffekseerRendererAfterStack : PostProcessingStackEffekseerRenderer<PostProcessingStackEffekseerAfterStackSettings>
{
}

class PostProcessingStackEffekseerRenderer<T> : PostProcessEffectRenderer<T> where T : PostProcessEffectSettings
{
	Effekseer.Internal.RenderTargetProperty prop = new Effekseer.Internal.RenderTargetProperty();
	Material grabDepthMat = null;

	public override void Init()
	{
		base.Init();
	}

	public override void Release()
	{
		base.Release();
	}

	int propertyId = Shader.PropertyToID("_PostProcessingStackEffekseerRendererDepth");

	public override DepthTextureMode GetCameraFlags()
	{
		return DepthTextureMode.Depth;
	}

	public override void Render(PostProcessRenderContext context)
	{

#if UNITY_EDITOR
		if (grabDepthMat == null)
		{
			Effekseer.EffekseerSettings.AssignAssets();
		}
#endif

		if(grabDepthMat == null && Effekseer.EffekseerSettings.Instance.grabDepthShader != null)
		{
			grabDepthMat = new Material(Effekseer.EffekseerSettings.Instance.grabDepthShader);
		}

		if (Effekseer.EffekseerSystem.Instance == null || grabDepthMat == null)
		{
			context.command.Blit(context.source, context.destination);
			return;
		}

		context.command.Blit(context.source, context.destination);

		var depthDescriptor = new RenderTextureDescriptor(context.width, context.height, RenderTextureFormat.Depth, 16);
		var depthIdentifer = new UnityEngine.Rendering.RenderTargetIdentifier(propertyId);
		context.command.GetTemporaryRT(propertyId, depthDescriptor);

		context.command.Blit(context.source, depthIdentifer, grabDepthMat);

		prop.colorTargetDescriptor = new RenderTextureDescriptor(context.width, context.height, context.sourceFormat);
		prop.colorTargetIdentifier = context.destination;
		prop.depthTargetIdentifier = depthIdentifer;
		prop.renderFeature = Effekseer.Internal.RenderFeature.PostProcess;
		prop.canGrabDepth = true;
		context.command.SetRenderTarget(context.destination, depthIdentifer);

		Effekseer.EffekseerSystem.Instance.renderer.Render(context.camera, prop, context.command);

		context.command.ReleaseTemporaryRT(propertyId);
	}
}

#endif
