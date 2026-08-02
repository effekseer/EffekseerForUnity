using UnityEngine;
using UnityEngine.Rendering;

namespace Effekseer.Internal
{
	public sealed class EffekseerRenderFrameInput
	{
		public Camera Camera { get; }
		public int AdditionalMask { get; }
		public RenderTargetProperty RenderTargetProperty { get; }
		public CommandBuffer TargetCommandBuffer { get; }
		public bool IsScriptable { get; }
		public IEffekseerBlitter Blitter { get; }
		public bool SetDefaultRenderTarget { get; }
		public bool UsesExternalCommands { get; }

		public EffekseerRenderFrameInput(Camera camera, int additionalMask, RenderTargetProperty renderTargetProperty,
			CommandBuffer targetCommandBuffer, bool isScriptable, IEffekseerBlitter blitter,
			bool setDefaultRenderTarget = true, bool usesExternalCommands = false)
		{
			Camera = camera;
			AdditionalMask = additionalMask;
			RenderTargetProperty = renderTargetProperty;
			TargetCommandBuffer = targetCommandBuffer;
			IsScriptable = isScriptable;
			Blitter = blitter;
			SetDefaultRenderTarget = setDefaultRenderTarget;
			UsesExternalCommands = usesExternalCommands;
		}
	}
}
