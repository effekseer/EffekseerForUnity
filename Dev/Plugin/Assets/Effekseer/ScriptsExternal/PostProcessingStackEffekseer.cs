#if UNITY_POST_PROCESSING_STACK_V2

using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

class PostProcessingStackEffekseerRendererBeforeStack : PostProcessingStackEffekseerRenderer<PostProcessStackEffekseerBeforeStackSettings>
{
}
class PostProcessingStackEffekseerRendererAfterStack : PostProcessingStackEffekseerRenderer<PostProcessingStackEffekseerAfterStackSettings>
{
}

class PostProcessingStackEffekseerRenderer<T> : PostProcessEffectRenderer<T> where T : PostProcessEffectSettings
{
	Effekseer.Internal.RenderTargetProperty prop = new Effekseer.Internal.RenderTargetProperty();

	public override void Init()
	{
		base.Init();
	}

	public override void Release()
	{
		base.Release();
	}

	public override DepthTextureMode GetCameraFlags()
	{
		return DepthTextureMode.Depth;
	}

	public override void Render(PostProcessRenderContext context)
	{
		if (Effekseer.EffekseerSystem.Instance == null)
		{
			context.command.Blit(context.source, context.destination);
			return;
		}

		context.command.Blit(context.source, context.destination);

		prop.colorTargetDescriptor = new RenderTextureDescriptor(context.width, context.height, context.sourceFormat, 0, 1);
		prop.colorTargetIdentifier = context.destination;
		prop.depthTargetIdentifier = UnityEngine.Rendering.BuiltinRenderTextureType.Depth;
		Effekseer.EffekseerSystem.Instance.renderer.Render(context.camera, prop, context.command);
	}
}

#endif
