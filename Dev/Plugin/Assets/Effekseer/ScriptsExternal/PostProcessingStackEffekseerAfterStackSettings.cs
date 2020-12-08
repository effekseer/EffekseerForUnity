#if UNITY_POST_PROCESSING_STACK_V2

using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(PostProcessingStackEffekseerRendererAfterStack), PostProcessEvent.AfterStack, "Effekseer/RenderingAfterStack")]
public sealed class PostProcessingStackEffekseerAfterStackSettings : PostProcessEffectSettings
{
}

#endif