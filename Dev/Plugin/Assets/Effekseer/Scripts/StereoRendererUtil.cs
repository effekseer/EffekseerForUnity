namespace Effekseer.Internal
{
	public static class StereoRendererUtil
	{
		public enum StereoRenderingTypes
		{
			// Multiple pass VR rendering.
			MultiPass = 0,
			// Single pass VR rendering ( via double-wide render texture ).
			SinglePass = 1,
			// Single pass VR rendering ( via instanced rendering ).
			SinglePassInstanced = 2,
			// Unknown
			None,
		}

		public static StereoRenderingTypes GetStereoRenderingType()
		{
#if UNITY_2018_3_OR_NEWER
			if (!UnityEngine.XR.XRSettings.enabled)
			{
				return StereoRenderingTypes.None;
			}
			else if (UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.MultiPass)
			{
				return StereoRenderingTypes.MultiPass;
			}
			else if (UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePass)
			{
				return StereoRenderingTypes.SinglePass;
			}
			else if (UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassInstanced)
			{
				return StereoRenderingTypes.SinglePassInstanced;
			}
			else
			{
				return StereoRenderingTypes.None;
			}
#else
			return StereoRenderingTypes.None;
#endif
		}
	}
}