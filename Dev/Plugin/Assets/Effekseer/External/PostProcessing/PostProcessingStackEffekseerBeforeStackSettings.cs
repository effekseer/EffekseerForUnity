#if EFFEKSEER_POSTPROCESSING_SUPPORT

using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(PostProcessingStackEffekseerRendererBeforeStack), PostProcessEvent.BeforeStack, "Effekseer/RenderingBeforeStack")]
public sealed class PostProcessingStackEffekseerBeforeStackSettings : PostProcessEffectSettings
{
}

#endif