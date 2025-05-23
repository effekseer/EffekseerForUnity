using UnityEngine;
using UnityEngine.Rendering;

namespace Effekseer.Internal
{
	public interface IEffekseerBlitter
	{
		void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier dest, bool xrRendering);
		void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier dest, Material material, bool xrRendering);
		void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier color, bool xrRendering);
		void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier color, RenderTargetIdentifier depth, bool xrRendering);
	}

	public class StandardBlitter : IEffekseerBlitter
	{	
		public void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier dest, bool xrRendering)
		{
			cmd.Blit(source, dest);
		}

		public void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier dest, Material material, bool xrRendering)
		{
			cmd.Blit(source, dest, material);
		}

		public void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier color, bool xrRendering)
		{
			if (xrRendering)
			{
				cmd.SetRenderTarget(color, 0, CubemapFace.Unknown, -1);
			}
			else
			{
				cmd.SetRenderTarget(color);
			}
		}

		public void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier color, RenderTargetIdentifier depth, bool xrRendering)
		{
			if (xrRendering)
			{
				cmd.SetRenderTarget(color, depth, 0, CubemapFace.Unknown, -1);
			}
			else
			{
				cmd.SetRenderTarget(color, depth);
			}
		}
	}
}