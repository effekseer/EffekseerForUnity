#if UNITY_POST_PROCESSING_STACK_V2

using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(PostProcessingStackEffekseerRendererBeforeStack), PostProcessEvent.BeforeStack, "Effekseer/RenderingBeforeStack")]
public sealed class PostProcessingStackEffekseerBeforeStackSettings : PostProcessEffectSettings
{
}

#endif