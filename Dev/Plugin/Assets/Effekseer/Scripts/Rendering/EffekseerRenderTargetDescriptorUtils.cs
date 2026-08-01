using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace Effekseer.Internal
{
	public static class EffekseerRenderTargetDescriptorUtils
	{
		public static bool IsStereoRendering(Camera camera)
		{
			return camera != null && camera.stereoEnabled;
		}

		public static bool CanUseXREyeTextureDescriptor(Camera camera)
		{
			// XR-WA-005 (English): XRSettings.enabled is application-wide and can be true for a
			// non-stereo camera. Use eyeTextureDesc only when the current camera is rendering stereo.
			// Remove this condition when Unity exposes a camera-specific Built-in XR descriptor.
			// XR-WA-005 (日本語): XRSettings.enabled はアプリ全体の状態であり、非Stereo Cameraでも
			// trueになり得ます。現在のCameraがStereo描画する場合のみeyeTextureDescを使用します。
			// Built-in向けのカメラ単位XR Descriptorが提供された場合のみ削除してください。
			return IsStereoRendering(camera) && XRSettings.enabled;
		}

		public static RenderTextureDescriptor CreateTemporaryColorDescriptor(
			RenderTexture source, Camera camera)
		{
			var sourceDescriptor = source.descriptor;
			var descriptor = new RenderTextureDescriptor(
				source.width, source.height, source.format, 0, source.mipmapCount);
			descriptor.useDynamicScale = sourceDescriptor.useDynamicScale;

			if (IsStereoRendering(camera))
			{
				// XR-WA-005 (English): Inherit only the XR layout from the source. Copying HDRP's full
				// camera descriptor also imports attachment-specific flags and packed GraphicsFormat
				// details that are not part of Effekseer's temporary distortion texture contract.
				// Keep source.format as Unity's logical channel layout. Remove this filtering when HDRP
				// exposes a descriptor intended for temporary sampled color textures.
				// XR-WA-005 (日本語): SourceからXR Layoutだけを継承します。HDRPのCamera Descriptor全体を
				// コピーすると、Effekseerの一時Distortion Textureには不要なAttachment固有Flagや
				// Packed GraphicsFormatの詳細まで引き継がれます。Unityの論理Channel Layoutである
				// source.formatを使用します。HDRPがSample用一時Color Texture向けDescriptorを
				// 提供した場合のみ、このFilteringを削除してください。
				descriptor.dimension = sourceDescriptor.dimension;
				descriptor.volumeDepth = sourceDescriptor.volumeDepth;
				descriptor.vrUsage = sourceDescriptor.vrUsage;
			}

			return descriptor;
		}

		public static RenderTextureDescriptor CreatePostProcessingDescriptor(
			Camera camera, int width, int height, RenderTextureFormat format)
		{
			if (CanUseXREyeTextureDescriptor(camera))
			{
				// XR-WA-005 (English): PostProcessing does not expose its source descriptor, so inherit
				// eyeTextureDesc for an XR camera. Remove this when the source descriptor becomes public.
				// XR-WA-005 (日本語): PostProcessingはSource Descriptorを公開しないため、XR Cameraでは
				// eyeTextureDescを継承します。Source Descriptorが公開された場合のみ削除してください。
				return XRSettings.eyeTextureDesc;
			}

			return new RenderTextureDescriptor(width, height, format);
		}

	}
}
