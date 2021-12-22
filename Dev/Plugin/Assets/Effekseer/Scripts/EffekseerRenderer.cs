using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace Effekseer.Internal
{
	class RendererUtils
	{
		public static void SetupDepthBuffer(ref DepthRenderTexture depthRenderTexure, bool enabled, Camera camera, RenderTargetProperty renderTargetProperty)
		{
			if (depthRenderTexure != null)
			{
				depthRenderTexure.Release();
				depthRenderTexure = null;
			}

			if (enabled)
			{
				var targetSize = BackgroundRenderTexture.GetRequiredSize(camera, renderTargetProperty);

				depthRenderTexure = new DepthRenderTexture(targetSize.x, targetSize.y, renderTargetProperty);

				// HACK for some smart phone
				if (depthRenderTexure == null || !depthRenderTexure.Create())
				{
					depthRenderTexure = null;
				}
			}
		}

		public static void SetupBackgroundBuffer(ref BackgroundRenderTexture renderTexture, bool enableDistortion, Camera camera, RenderTargetProperty renderTargetProperty)
		{
			if (renderTexture != null)
			{
				renderTexture.Release();
				renderTexture = null;
			}

			if (enableDistortion)
			{
				var targetSize = BackgroundRenderTexture.GetRequiredSize(camera, renderTargetProperty);

#if UNITY_IOS || UNITY_ANDROID
					RenderTextureFormat format = RenderTextureFormat.ARGB32;
#else
				RenderTextureFormat format = (camera.allowHDR) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
#endif
				renderTexture = new BackgroundRenderTexture(targetSize.x, targetSize.y, 0, format, renderTargetProperty);

				// HACK for ZenPhone
				if (renderTexture == null || !renderTexture.Create())
				{
					renderTexture = null;
				}
			}
		}

	}

	public enum RenderFeature
	{
		HDRP,
		URP,
		PostProcess,
	}

	public class RenderTargetProperty
	{
		public RenderFeature renderFeature = RenderFeature.PostProcess;

		/// <summary>
		/// Ring buffer (it should be better implement)
		/// </summary>
		const int MaterialRingCount = 12;

		public int? colorBufferID = null;
		public RenderTargetIdentifier colorTargetIdentifier;
		public RenderTargetIdentifier? depthTargetIdentifier;
		public RenderTextureDescriptor colorTargetDescriptor;
		public Rect Viewport;
		public bool isRequiredToChangeViewport = false;
		public RenderTexture colorTargetRenderTexture = null;
		public RenderTexture depthTargetRenderTexture = null;
		public bool canGrabDepth = false;

		public bool isRequiredToCopyBackground = false;

		int blitArrayMaterialOffset = 0;
		int blitMaterialOffset = 0;
		List<Material> blitMaterials = new List<Material>();
		List<Material> blitArrayMaterials = new List<Material>();

		Material grabDepthMat;

		public RenderTargetProperty()
		{
		}

		internal void ApplyToCommandBuffer(CommandBuffer cb, DepthRenderTexture depthRenderTexture)
		{
			if (depthRenderTexture != null)
			{
#if UNITY_EDITOR
				if (grabDepthMat == null)
				{
					EffekseerDependentAssets.AssignAssets();
				}
#endif

				if (grabDepthMat == null && EffekseerDependentAssets.Instance.grabDepthShader != null)
				{
					grabDepthMat = new Material(EffekseerDependentAssets.Instance.grabDepthShader);
					grabDepthMat.EnableKeyword("_AS_COLOR_");
				}

				if (renderFeature == RenderFeature.PostProcess)
				{
					cb.Blit(null, depthRenderTexture.renderTexture, grabDepthMat);
				}
				else if (renderFeature == RenderFeature.URP)
				{
					if (canGrabDepth)
					{
						cb.Blit(null, depthRenderTexture.renderTexture, grabDepthMat);
					}
					else
					{
						cb.SetRenderTarget(depthRenderTexture.renderTexture);
						cb.ClearRenderTarget(true, true, new Color(0, 0, 0));
					}
				}
				else if (renderFeature == RenderFeature.HDRP)
				{
					var m = AllocateBlitArrayMaterial();
					m.SetTexture("_BackgroundTex", depthTargetRenderTexture);
					m.SetVector("textureArea", new Vector4(
						Viewport.width / depthTargetRenderTexture.width,
						Viewport.height / depthTargetRenderTexture.height,
						Viewport.x / depthTargetRenderTexture.width,
						Viewport.y / depthTargetRenderTexture.height));
					cb.SetRenderTarget(depthRenderTexture.renderTexture);
					cb.ClearRenderTarget(true, true, new Color(0, 0, 0));
					cb.Blit(null, depthRenderTexture.renderTexture, m);
				}
				else
				{
					throw new Exception();
				}

				// restore
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

		internal void ApplyToCommandBuffer(CommandBuffer cb, BackgroundRenderTexture backgroundRenderTexture)
		{
			if (isRequiredToChangeViewport)
			{
				if (colorTargetRenderTexture.dimension == TextureDimension.Tex2DArray)
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
			else if (isRequiredToCopyBackground)
			{
				cb.Blit(colorTargetIdentifier, backgroundRenderTexture.renderTexture);
			}
			else
			{
				cb.Blit(colorTargetIdentifier, backgroundRenderTexture.renderTexture);
			}

			// restore
			if (depthTargetIdentifier.HasValue)
			{
				cb.SetRenderTarget(colorTargetIdentifier, depthTargetIdentifier.Value);
			}
			else
			{
				cb.SetRenderTarget(colorTargetIdentifier);
			}
		}

		Material AllocateBlitArrayMaterial()
		{
			if (blitArrayMaterials.Count == 0)
			{
				for (int i = 0; i < MaterialRingCount; i++)
				{
					blitArrayMaterials.Add(new Material(EffekseerDependentAssets.Instance.texture2DArrayBlitMaterial));
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
					blitMaterials.Add(new Material(EffekseerDependentAssets.Instance.texture2DBlitMaterial));
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

#if UNITY_EDITOR
		bool disableCullingMask { get; set; }
#endif
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

		internal static bool IsDepthEnabled
		{
			get
			{
#if UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL || UNITY_SWITCH
				return EffekseerSettings.Instance.enableDepthMobile;
#else
				return EffekseerSettings.Instance.enableDepth;
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

			if (camera != null)
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

			if (renderTargetProperty != null)
			{
				renderTexture = new RenderTexture(renderTargetProperty.colorTargetDescriptor);
				renderTexture.antiAliasing = 1;
			}
			else
			{
				if (XRSettings.enabled)
				{
					renderTexture = new RenderTexture(XRSettings.eyeTextureDesc);
				}
				else
				{
					if(QualitySettings.activeColorSpace == ColorSpace.Linear)
					{
						renderTexture = new RenderTexture(width, height, 0, format, RenderTextureReadWrite.Linear);
					}
					else
					{
						renderTexture = new RenderTexture(width, height, 0, format);
					}
				}
			}

			if (renderTexture != null)
			{
				renderTexture.name = "EffekseerBackground";
			}
		}

		public bool Create()
		{
			// HACK for ZenPhone
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

	internal class DepthRenderTexture
	{
		internal RenderTexture renderTexture;
		public IntPtr ptr = IntPtr.Zero;

		public DepthRenderTexture(int width, int height, RenderTargetProperty renderTargetProperty)
		{
			if (renderTargetProperty != null)
			{
				width = renderTargetProperty.colorTargetDescriptor.width;
				height = renderTargetProperty.colorTargetDescriptor.height;
			}

			RenderTextureDescriptor desc = new RenderTextureDescriptor(width, height, RenderTextureFormat.RHalf);

			renderTexture = new RenderTexture(desc);

			if (renderTexture != null)
			{
				renderTexture.name = "EffekseerDepth";
			}
		}

		public bool Create()
		{
			// HACK for ZenPhone
			if (this.renderTexture == null || !this.renderTexture.Create())
			{
				this.renderTexture = null;
				return false;
			}

			this.ptr = this.renderTexture.GetNativeTexturePtr();
			return true;
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
	}
}