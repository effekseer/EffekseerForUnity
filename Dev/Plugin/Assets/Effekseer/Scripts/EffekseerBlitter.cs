using UnityEngine.Rendering;

namespace Effekseer.Internal
{
	public interface IEffekseerBlitter
	{
		void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier dest);
	}

	public class StandardBlitter : IEffekseerBlitter
	{	
		public void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier dest)
		{
			cmd.Blit(source, dest);
		}
	}
}