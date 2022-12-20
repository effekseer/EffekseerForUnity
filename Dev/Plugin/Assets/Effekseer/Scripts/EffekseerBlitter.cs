using UnityEngine;
using UnityEngine.Rendering;

namespace Effekseer.Internal
{
	public interface IEffekseerBlitter
	{
		void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier dest, bool xrRendering);
		void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier dest, Material material);
	}

	public class StandardBlitter : IEffekseerBlitter
	{	
		public void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier dest, bool xrRendering)
		{
			cmd.Blit(source, dest);
		}
	}
}