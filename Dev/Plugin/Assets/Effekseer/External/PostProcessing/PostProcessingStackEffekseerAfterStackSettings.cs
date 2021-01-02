#if EFFEKSEER_POSTPROCESSING_SUPPORT

using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(PostProcessingStackEffekseerRendererAfterStack), PostProcessEvent.AfterStack, "Effekseer/RenderingAfterStack")]
public sealed class PostProcessingStackEffekseerAfterStackSettings : PostProcessEffectSettings
{
}

#endif