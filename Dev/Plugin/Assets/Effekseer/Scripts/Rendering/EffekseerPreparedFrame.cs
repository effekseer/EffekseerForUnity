using UnityEngine;
using UnityEngine.Rendering;

namespace Effekseer.Internal
{
	public sealed class EffekseerPreparedFrame
	{
		internal object BackendPath { get; set; }
		internal CommandBuffer CommandBuffer { get; set; }
		internal BackgroundRenderTexture Background { get; set; }
		internal DepthRenderTexture Depth { get; set; }
		internal Material TargetCommitMaterial { get; set; }
		internal bool IsNativeRenderer { get; set; }

		public Camera Camera { get; internal set; }
		public bool HasVisibleEffects { get; internal set; }
	}
}
