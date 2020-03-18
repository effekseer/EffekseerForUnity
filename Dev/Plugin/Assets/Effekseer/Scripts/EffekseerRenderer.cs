using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace Effekseer.Internal
{
	public class RenderTargetProperty
	{
		/// <summary>
		/// Ring buffer (it should be better implement)
		/// </summary>
		const int MaterialRingCount = 6;

		public int? colorBufferID = null;
		public RenderTargetIdentifier colorTargetIdentifier;
		public RenderTargetIdentifier? depthTargetIdentifier;
		public RenderTextureDescriptor colorTargetDescriptor;
		public Rect Viewport;
		public bool isRequiredToChangeViewport = false;
		public RenderTexture colorTargetRenderTexture = null;

		public bool isRequiredToCopyBackground = false;

		int blitArrayMaterialOffset = 0;
		int blitMaterialOffset = 0;
		List<Material> blitMaterials = new List<Material>();
		List<Material> blitArrayMaterials = new List<Material>();

		public RenderTargetProperty()
		{
		}

		internal void ApplyToCommandBuffer(CommandBuffer cb, BackgroundRenderTexture backgroundRenderTexture)
		{
			if(isRequiredToChangeViewport)
			{
				if(colorTargetRenderTexture.dimension == TextureDimension.Tex2DArray)
				{
					var m = AllocateBlitArrayMaterial();
					m.SetTexture("_BackgroundTex", colorTargetRenderTexture);
					m.SetVector("textureArea", new Vector4(
						Viewport.width / colorTargetRenderTexture.width,
						Viewport.height / colorTargetRenderTexture.height,
						Viewport.x / colorTargetRenderTexture.width,
						Viewport.y / colorTargetRenderTexture.height));
					cb.SetRenderTarget(backgroundRenderTexture.renderTexture);
					cb.ClearRenderTarget(true, true, new Color(0, 0, 0));
					cb.Blit(colorTargetIdentifier, backgroundRenderTexture.renderTexture, m);
				}
				else
				{
					var m = AllocateBlitMaterial();
					m.SetTexture("_BackgroundTex", colorTargetRenderTexture);
					m.SetVector("textureArea", new Vector4(
						Viewport.width / colorTargetRenderTexture.width,
						Viewport.height / colorTargetRenderTexture.height,
						Viewport.x / colorTargetRenderTexture.width,
						Viewport.y / colorTargetRenderTexture.height));
					cb.SetRenderTarget(backgroundRenderTexture.renderTexture);
					cb.ClearRenderTarget(true, true, new Color(0, 0, 0));
					cb.Blit(colorTargetIdentifier, backgroundRenderTexture.renderTexture, m);
				}
			}
			else if(isRequiredToCopyBackground)
			{
				cb.CopyTexture(colorTargetIdentifier, new RenderTargetIdentifier(backgroundRenderTexture.renderTexture));
			}
			else
			{
				cb.Blit(colorTargetIdentifier, backgroundRenderTexture.renderTexture);
			}
			
			if(!isRequiredToCopyBackground)
			{
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

		Material AllocateBlitArrayMaterial()
		{
			if(blitArrayMaterials.Count == 0)
			{
				for(int i = 0; i < MaterialRingCount; i++)
				{
					blitArrayMaterials.Add(new Material(Effekseer.EffekseerSettings.Instance.texture2DArrayBlitMaterial));
				}
			}

			blitArrayMaterialOffset++;
			blitArrayMaterialOffset %= MaterialRingCount;
			return blitArrayMaterials[blitArrayMaterialOffset];
		}

		Material AllocateBlitMaterial()
		{
			if (blitMaterials.Count == 0)
			{
				for (int i = 0; i < MaterialRingCount; i++)
				{
					blitMaterials.Add(new Material(Effekseer.EffekseerSettings.Instance.texture2DBlitMaterial));
				}
			}

			blitMaterialOffset++;
			blitMaterialOffset %= MaterialRingCount;
			return blitMaterials[blitMaterialOffset];
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

		public static Vector2Int GetRequiredSize(Camera camera, RenderTargetProperty renderTargetProperty)
		{
			if (renderTargetProperty != null)
			{
				var width = renderTargetProperty.colorTargetDescriptor.width;
				var height = renderTargetProperty.colorTargetDescriptor.height;
				return new Vector2Int(width, height);
			}

			if(camera != null)
			{
				var width = EffekseerRendererUtils.ScaledClamp(camera.scaledPixelWidth, EffekseerRendererUtils.DistortionBufferScale);
				var height = EffekseerRendererUtils.ScaledClamp(camera.scaledPixelHeight, EffekseerRendererUtils.DistortionBufferScale);
				return new Vector2Int(width, height);
			}

			return new Vector2Int();
		}

		public BackgroundRenderTexture(int width, int height, int depth, RenderTextureFormat format, RenderTargetProperty renderTargetProperty)
		{
			if (renderTargetProperty != null)
			{
				width = renderTargetProperty.colorTargetDescriptor.width;
				height = renderTargetProperty.colorTargetDescriptor.height;
			}

#if UNITY_STANDALONE_WIN
			if (renderTargetProperty != null)
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
			if(renderTexture != null)
			{
				renderTexture.name = "EffekseerBackground";
			}
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