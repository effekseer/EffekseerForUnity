using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

namespace Effekseer
{
	public static class Utility
	{
		public static float[] Matrix2Array(Matrix4x4 mat) {
			float[] res = new float[16];
			res[ 0] = mat.m00; res[ 1] = mat.m01; res[ 2] = mat.m02; res[ 3] = mat.m03;
			res[ 4] = mat.m10; res[ 5] = mat.m11; res[ 6] = mat.m12; res[ 7] = mat.m13;
			res[ 8] = mat.m20; res[ 9] = mat.m21; res[10] = mat.m22; res[11] = mat.m23;
			res[12] = mat.m30; res[13] = mat.m31; res[14] = mat.m32; res[15] = mat.m33;
			return res;
		}

		public static float TimeToFrames(float time) {
			return time * 60.0f;
		}
	}
	
	internal static class Plugin
	{
#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_WEBGL || UNITY_SWITCH)
			public const string pluginName = "__Internal";
#else
		public const string pluginName = "EffekseerUnity";
#endif

#if (UNITY_WEBGL || UNITY_IOS || UNITY_SWITCH) && !UNITY_EDITOR
		[DllImport (pluginName)]
		public static extern void RegisterPlugin();
#endif

		[DllImport(pluginName)]
		public static extern void EffekseerInit(int maxInstances, int maxSquares, int isRightHandedCoordinate, int reversedDepth, int rendererType);

		[DllImport(pluginName)]
		public static extern void EffekseerTerm();

		[DllImport(pluginName)]
		public static extern void EffekseerResetTime();

		[DllImport(pluginName)]
		public static extern void EffekseerUpdateTime(float deltaTime);

		[DllImport(pluginName)]
		public static extern void EffekseerUpdate(float deltaTime);

		[DllImport(pluginName)]
		public static extern IntPtr EffekseerGetRenderFunc(int renderId = 0);

		[DllImport(pluginName)]
		public static extern IntPtr EffekseerGetRenderFrontFunc(int renderId = 0);

		[DllImport(pluginName)]
		public static extern IntPtr EffekseerGetRenderBackFunc(int renderId = 0);

		[DllImport(pluginName)]
		public static extern void EffekseerRender(int renderId = 0);

		[DllImport(pluginName)]
		public static extern void EffekseerRenderFront(int renderId = 0);

		[DllImport(pluginName)]
		public static extern void EffekseerRenderBack(int renderId = 0);

		[DllImport(pluginName)]
		public static extern void EffekseerSetProjectionMatrix(int renderId, float[] matrix);

		[DllImport(pluginName)]
		public static extern void EffekseerSetCameraMatrix(int renderId, float[] matrix);

		[DllImport(pluginName)]
		public static extern void EffekseerSetStereoRenderingMatrix(int renderId, int renderType, float[] camCenterMat, float[] projMatL, float[] projMatR, float[] camMatL, float[] camMatR);

		[DllImport(pluginName)]
		public static extern void EffekseerSetBackGroundTexture(int renderId, IntPtr background);

		[DllImport(pluginName)]
		public static extern void EffekseerSetRenderSettings(int renderId, bool renderIntoTexture);

		[DllImport(pluginName)]
		public static extern void EffekseerSetRenderingCameraCullingMask(int renderId, int cullingMask);

		[DllImport(pluginName)]
        public static extern void EffekseerSetIsTextureFlipped(int isFlipped);

        [DllImport(pluginName)]
        public static extern void EffekseerSetIsBackgroundTextureFlipped(int isFlipped);

		[DllImport(pluginName)]
		public static extern void EffekseerAddRemovingRenderPath(int renderID);

		[DllImport(pluginName)]
		public static extern IntPtr EffekseerLoadEffect(IntPtr path, float magnification);

		[DllImport(pluginName)]
		public static extern IntPtr EffekseerLoadEffectOnMemory(byte[] data, int size, IntPtr path, float magnification);

		[DllImport(pluginName)]
		public static extern void EffekseerReleaseEffect(IntPtr effect);

		[DllImport(pluginName)]
		public static extern void EffekseerReloadResources(IntPtr effect);

		[DllImport(pluginName)]
		public static extern void EffekseerUnloadResources(IntPtr effect);

		[DllImport(pluginName)]
		public static extern float EffekseerGetEffectMagnification(IntPtr effect);

		[DllImport(pluginName)]
		public static extern int EffekseerPlayEffect(IntPtr effect, float x, float y, float z);

		[DllImport(pluginName)]
		public static extern void EffekseerUpdateHandle(int handle, float deltaDrame);

		[DllImport(pluginName)]
		public static extern void EffekseerStopEffect(int handle);

		[DllImport(pluginName)]
		public static extern void EffekseerStopRoot(int handle);

		[DllImport(pluginName)]
		public static extern void EffekseerStopAllEffects();

		[DllImport(pluginName)]
		public static extern void EffekseerSetPausedToAllEffects(bool paused);

		[DllImport(pluginName)]
		public static extern int EffekseerGetCameraCullingMaskToShowAllEffects();

		[DllImport(pluginName)]
		public static extern bool EffekseerGetShown(int handle);

		[DllImport(pluginName)]
		public static extern void EffekseerSetShown(int handle, bool shown);

		[DllImport(pluginName)]
		public static extern bool EffekseerGetPaused(int handle);

		[DllImport(pluginName)]
		public static extern void EffekseerSetPaused(int handle, bool paused);

		[DllImport(pluginName)]
		public static extern float EffekseerGetSpeed(int handle);

		[DllImport(pluginName)]
		public static extern void EffekseerSetSpeed(int handle, float speed);

		[DllImport(pluginName)]
		public static extern bool EffekseerExists(int handle);

		[DllImport(pluginName)]
		public static extern void EffekseerSetLocation(int handle, float x, float y, float z);

		[DllImport(pluginName)]
		public static extern void EffekseerSetRotation(int handle, float x, float y, float z, float angle);

		[DllImport(pluginName)]
		public static extern void EffekseerSetScale(int handle, float x, float y, float z);

		[DllImport(pluginName)]
		public static extern void EffekseerSetAllColor(int handle, int r, int g, int b, int a);

		[DllImport(pluginName)]
		public static extern void EffekseerSetTargetLocation(int handle, float x, float y, float z);

		[DllImport(pluginName)]
		public static extern void EffekseerSetLightDirection(float x, float y, float z);

		[DllImport(pluginName)]
		public static extern void EffekseerSetLightColor(int r, int g, int b);

		[DllImport(pluginName)]
		public static extern void EffekseerSetLightAmbientColor(int r, int g, int b);

		[DllImport(pluginName)]
		public static extern void EffekseerSetLayer(int handle, int layer);

		[DllImport(pluginName)]
		public static extern float EffekseerGetDynamicInput(int handle, int index);

		[DllImport(pluginName)]
		public static extern void EffekseerSetDynamicInput(int handle, int index, float value);

		[DllImport(pluginName)]
		public static extern void EffekseerSetTextureLoaderEvent(
			EffekseerTextureLoaderLoad load,
			EffekseerTextureLoaderUnload unload);
		public delegate IntPtr EffekseerTextureLoaderLoad(IntPtr path, out int width, out int height, out int format);
		public delegate void EffekseerTextureLoaderUnload(IntPtr path, IntPtr nativePtr);

		[DllImport(pluginName)]
		public static extern void EffekseerSetModelLoaderEvent(
			EffekseerModelLoaderLoad load,
			EffekseerModelLoaderUnload unload);
		public delegate IntPtr EffekseerModelLoaderLoad(IntPtr path, IntPtr buffer, int bufferSize, ref int requiredBufferSize);
		public delegate void EffekseerModelLoaderUnload(IntPtr path, IntPtr modelPtr);

		[DllImport(pluginName)]
		public static extern void EffekseerSetMaterialLoaderEvent(
	EffekseerMaterialLoaderLoad load,
	EffekseerMaterialLoaderUnload unload);
		public delegate IntPtr EffekseerMaterialLoaderLoad(IntPtr path,
			IntPtr materialBuffer, int materialBufferSize, ref int requiredMaterialBufferSize,
			IntPtr cachedMaterialBuffer, int cachedMaterialBufferSize, ref int requiredCachedMaterialBufferSize);
		public delegate void EffekseerMaterialLoaderUnload(IntPtr path, IntPtr modelPtr);

		[DllImport(pluginName)]
		public static extern void EffekseerSetSoundLoaderEvent(
			EffekseerSoundLoaderLoad load,
			EffekseerSoundLoaderUnload unload);
		public delegate IntPtr EffekseerSoundLoaderLoad(IntPtr path);
		public delegate void EffekseerSoundLoaderUnload(IntPtr path);

		[DllImport(pluginName)]
		public static extern void EffekseerSetSoundPlayerEvent(
			EffekseerSoundPlayerPlay play, 
			EffekseerSoundPlayerStopTag stopTag, 
			EffekseerSoundPlayerPauseTag pauseTag, 
			EffekseerSoundPlayerCheckPlayingTag checkPlayingTag, 
			EffekseerSoundPlayerStopAll atopAll);
		public delegate void EffekseerSoundPlayerPlay(IntPtr tag, 
			IntPtr data, float volume, float pan, float pitch, 
			bool mode3D, float x, float y, float z, float distance);
		public delegate void EffekseerSoundPlayerStopTag(IntPtr tag);
		public delegate void EffekseerSoundPlayerPauseTag(IntPtr tag, bool pause);
		public delegate bool EffekseerSoundPlayerCheckPlayingTag(IntPtr tag);
		public delegate void EffekseerSoundPlayerStopAll();

		#region UnityRenderer

		public enum RendererMaterialType : int
		{
			Default = 0,
			BackDistortion = 6,
			Lighting = 7,
			File = 128,
		}

		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct UnityRenderParameter
		{
			//! 0 - procedual, 1 - model
			public int RenderMode;

			public RendererMaterialType MaterialType;

			//! VertexBuffer 
			public int VertexBufferOffset;

			//! VertexBuffer 
			public int VertexBufferStride;

			//! For model
			public int CustomData1BufferOffset;

			//! For model
			public int CustomData2BufferOffset;

			public int UniformBufferOffset;

			//! Element count (Triangle) or instance
			public int ElementCount;

			public int ZTest;

			public int ZWrite;

			public int Blend;

			public int Culling;

			public float DistortionIntensity;

			//! for material
			public int IsRefraction;

			//! Texture ptr
			public IntPtr TexturePtrs0;
			public IntPtr TexturePtrs1;
			public IntPtr TexturePtrs2;
			public IntPtr TexturePtrs3;
			public IntPtr TexturePtrs4;
			public IntPtr TexturePtrs5;
			public IntPtr TexturePtrs6;
			public IntPtr TexturePtrs7;

			public IntPtr GetTexturePtr(int i)
			{
				if (i == 0) return TexturePtrs0;
				if (i == 1) return TexturePtrs1;
				if (i == 2) return TexturePtrs2;
				if (i == 3) return TexturePtrs3;
				if (i == 4) return TexturePtrs4;
				if (i == 5) return TexturePtrs5;
				if (i == 6) return TexturePtrs6;
				if (i == 7) return TexturePtrs7;
				return IntPtr.Zero;
			}

			public fixed int TextureFilterTypes[8];

			public fixed int TextureWrapTypes[8];

			public int TextureCount;

			//! Material ptr
			public IntPtr MaterialPtr;

			//! Model ptri
			public IntPtr ModelPtr;
		};

		[StructLayout(LayoutKind.Sequential)]
		public struct UnityRenderModelParameter
		{
			public Matrix4x4 Matrix;
			public Vector4 UV;
			public Color VColor;
			public int Time;
		};

		[DllImport(pluginName)]
		public static extern void GetUnityRenderParameter(ref UnityRenderParameter dst, int index);

		[DllImport(pluginName)]
		public static extern int GetUnityRenderParameterCount();

		[DllImport(pluginName)]
		public static extern IntPtr GetUnityRenderVertexBuffer();

		[DllImport(pluginName)]
		public static extern int GetUnityRenderVertexBufferCount();

		[DllImport(pluginName)]
		public static extern IntPtr GetUnityRenderInfoBuffer();

		#endregion

		#region Network
		[DllImport(pluginName)]
		public static extern int StartNetwork(int port);

		[DllImport(pluginName)]
		public static extern void StopNetwork();

		[DllImport(pluginName)]
		public static extern void UpdateNetwork();

		#endregion
	}
}