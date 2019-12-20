using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace Effekseer.Internal
{
	public class RenderTargetProperty
	{
		public int? colorBufferID = null;
		public RenderTargetIdentifier colorTargetIdentifier;
		public RenderTargetIdentifier? depthTargetIdentifier;
		public RenderTextureDescriptor colorTargetDescriptor;
		public Rect Viewport;

		internal void ApplyToCommandBuffer(CommandBuffer cb, BackgroundRenderTexture backgroundRenderTexture)
		{
			cb.Blit(colorTargetIdentifier, backgroundRenderTexture.renderTexture);

			if (depthTargetIdentifier.HasValue)
			{
				cb.SetRenderTarget(colorTargetIdentifier, depthTargetIdentifier.Value);
			}
			else
			{
				cb.SetRenderTarget(colorTargetIdentifier);
			}
		}
	}

	public interface IEffekseerRenderer
	{
		int layer { get; set; }

		void SetVisible(bool visible);

		void CleanUp();

		CommandBuffer GetCameraCommandBuffer(Camera camera);
		
		void Render(Camera camera, RenderTargetProperty renderTargetProperty, CommandBuffer targetCommandBuffer);

		void OnPostRender(Camera camera);
	}

	internal class EffekseerRendererUtils
	{
		public const int RenderIDCount = 128;

		internal static int ScaledClamp(int value, float scale)
		{
			var v = (int)(value * scale);
			v = Math.Max(v, 1);
			v = Math.Min(v, value);
			return v;
		}

		internal static float DistortionBufferScale
		{
			get
			{
				return 1.0f;
			}
		}

		internal static bool IsDistortionEnabled
		{
			get
			{
#if UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL || UNITY_SWITCH
				return EffekseerSettings.Instance.enableDistortionMobile;
#else
				return EffekseerSettings.Instance.enableDistortion;
#endif
			}
		}
	}

	internal class BackgroundRenderTexture
	{
		public RenderTexture renderTexture = null;
		public RenderTexture renderTextureMSAA = null;

		public IntPtr ptr = IntPtr.Zero;

		public BackgroundRenderTexture(int width, int height, int depth, RenderTextureFormat format, RenderTargetProperty renderTargetProperty)
		{
#if UNITY_STANDALONE_WIN
			if(renderTargetProperty != null)
			{
				renderTexture = new RenderTexture(width, height, 0, format, RenderTextureReadWrite.Linear);
			}
			else
			{
				renderTexture = new RenderTexture(width, height, 0, format);
			}
#else
			renderTexture = new RenderTexture(width, height, 0, format);
#endif
		}

		public bool Create()
		{
			// HACK for ZenPhone (cannot understand)
			if (this.renderTexture == null || !this.renderTexture.Create())
			{
				this.renderTexture = null;
				return false;
			}

			this.ptr = this.renderTexture.GetNativeTexturePtr();
			return true;
		}

		public int width
		{
			get
			{
				if (renderTexture != null) return renderTexture.width;
				return 0;
			}
		}

		public int height
		{
			get
			{
				if (renderTexture != null) return renderTexture.height;
				return 0;
			}
		}

		public void Release()
		{
			if (renderTexture != null)
			{
				renderTexture.Release();
				renderTexture = null;
				ptr = IntPtr.Zero;
			}
		}
	}
}