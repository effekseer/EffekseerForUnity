using UnityEngine.Rendering;

namespace Effekseer.Internal
{
	public interface IEffekseerBlitter
	{
		void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier dest, bool xrRendering);
	}

	public class StandardBlitter : IEffekseerBlitter
	{	
		public void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier dest, bool xrRendering)
		{
			cmd.Blit(source, dest);
		}
	}
}