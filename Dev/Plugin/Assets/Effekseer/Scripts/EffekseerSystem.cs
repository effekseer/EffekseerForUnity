using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Effekseer
{
	using Internal;

	[Serializable]
	public class EffekseerSystem
	{
		/// <summary xml:lang="en">
		/// Plays the effect.
		/// </summary>
		/// <param name="effectAsset" xml:lang="en">Effect asset</param>
		/// <param name="location" xml:lang="en">Location in world space</param>
		/// <returns>Played effect instance</returns>
		/// <summary xml:lang="ja">
		/// エフェクトの再生
		/// </summary>
		/// <param name="effectAsset" xml:lang="ja">エフェクトアセット</param>
		/// <param name="location" xml:lang="ja">再生開始する位置</param>
		/// <returns>再生したエフェクトインスタンス</returns>
		public static EffekseerHandle PlayEffect(EffekseerEffectAsset effectAsset, Vector3 location)
		{
			if (Instance == null) {
				Debug.LogError("[Effekseer] System is not initialized.");
				return new EffekseerHandle(-1);
			}
			if (effectAsset == null) {
				Debug.LogError("[Effekseer] Specified effect is null.");
				return new EffekseerHandle(-1);
			}

			IntPtr nativeEffect;
			if (Instance.nativeEffects.TryGetValue(effectAsset.GetInstanceID(), out nativeEffect)) {
				int handle = Plugin.EffekseerPlayEffect(nativeEffect, location.x, location.y, location.z);
				return new EffekseerHandle(handle);
			}
			return new EffekseerHandle(-1);
		}

		/// <summary xml:lang="en">
		/// Stops all effects
		/// </summary>
		/// <summary xml:lang="ja">
		/// 全エフェクトの再生停止
		/// </summary>
		public static void StopAllEffects()
		{
			Plugin.EffekseerStopAllEffects();
		}

		/// <summary xml:lang="en">
		/// Pause or resume all effects
		/// </summary>
		/// <summary xml:lang="ja">
		/// 全エフェクトの一時停止、もしくは再開
		/// </summary>
		public static void SetPausedToAllEffects(bool paused)
		{
			Plugin.EffekseerSetPausedToAllEffects(paused);
		}

		#region Network
		/// <summary xml:lang="en">
		/// start a server to edit effects from remote
		/// </summary>
		/// <summary xml:lang="ja">
		/// リモートでエフェクトを編集するためにサーバーを起動する。
		/// </summary>
		public static bool StartNetwork()
		{
			return Plugin.StartNetwork((int)EffekseerSettings.Instance.NetworkPort) > 0;
		}

		/// <summary xml:lang="en">
		/// stop a server to edit effects from remote
		/// </summary>
		/// <summary xml:lang="ja">
		/// リモートでエフェクトを編集するためにサーバーを停止する。
		/// </summary>
		public static void StopNetwork()
		{
			Plugin.StopNetwork();
		}
		#endregion

		#region Internal Implimentation


		// Singleton instance
		public static EffekseerSystem Instance { get; private set; }
		public static bool IsValid { get { return Instance != null && Instance.enabled; } }

		/// <summary>
		/// Don't touch it!!
		/// </summary>
		public bool enabled;

		public IEffekseerRenderer renderer { get; private set; }

		// Loaded native effects
		//[SerializeField] List<EffekseerEffectAsset> loadedEffects = new List<EffekseerEffectAsset>();
		private Dictionary<int, IntPtr> nativeEffects = new Dictionary<int, IntPtr>();

#if UNITY_EDITOR
		// For hot reloading
		[SerializeField] private List<int> nativeEffectsKeys = new List<int>();
		[SerializeField] private List<string> nativeEffectsValues = new List<string>();
#endif

		// A AssetBundle that current loading
		private EffekseerEffectAsset effectAssetInLoading;

		internal Effekseer.EffekseerRendererType RendererType { get; private set; }

		private static Dictionary<IntPtr, Texture> cachedTextures = new Dictionary<IntPtr, Texture>();

		private static Dictionary<Texture, IntPtr> cachedTextureIDs = new Dictionary<Texture, IntPtr>();

		private static int textureIDCounter = 0;

		private static Dictionary<IntPtr, UnityRendererModel> cachedModels = new Dictionary<IntPtr, UnityRendererModel>();

		private void ReloadEffects()
		{
			foreach (var weakEffectAsset in EffekseerEffectAsset.enabledAssets)
			{
                EffekseerEffectAsset effectAsset = weakEffectAsset.Value.Target as EffekseerEffectAsset;

                if (effectAsset != null)
				{
					effectAssetInLoading = effectAsset;
					int id = effectAsset.GetInstanceID();
					IntPtr nativeEffect;
					if (nativeEffects.TryGetValue(id, out nativeEffect))
					{
						Plugin.EffekseerReloadResources(nativeEffect);
					}
					effectAssetInLoading = null;
				}
			}

			/*
			foreach (var effectAsset in loadedEffects) {
				effectAssetInLoading = effectAsset;
				int id = effectAsset.GetInstanceID();
				IntPtr nativeEffect;
				if (nativeEffects.TryGetValue(id, out nativeEffect)) {
					Plugin.EffekseerReloadResources(nativeEffect);
				}
				effectAssetInLoading = null;
			}
			*/
		}

		/// <summary>
		/// Don't touch it!!
		/// </summary>
		public void LoadEffect(EffekseerEffectAsset effectAsset) {
			effectAssetInLoading = effectAsset;
			int id = effectAsset.GetInstanceID();
			IntPtr nativeEffect;
			if (!nativeEffects.TryGetValue(id, out nativeEffect)) {
				byte[] bytes = effectAsset.efkBytes;
				var namePtr = Marshal.StringToCoTaskMemUni(effectAsset.name);
				nativeEffect = Plugin.EffekseerLoadEffectOnMemory(bytes, bytes.Length, namePtr, effectAsset.Scale);
				nativeEffects.Add(id, nativeEffect);
				//loadedEffects.Add(effectAsset);
				//effectAsset.GetInstanceID
				Marshal.FreeCoTaskMem(namePtr);
			}
			else
			{
				// For reloading
				Plugin.EffekseerReloadResources(nativeEffect);
			}

			effectAssetInLoading = null;
		}

		internal void ReleaseEffect(EffekseerEffectAsset effectAsset) {
			int id = effectAsset.GetInstanceID();
			IntPtr nativeEffect;
			if (nativeEffects.TryGetValue(id, out nativeEffect)) {
				Plugin.EffekseerReleaseEffect(nativeEffect);
				nativeEffects.Remove(id);
				//loadedEffects.Remove(effectAsset);
			}
		}

		/// <summary>
		/// Don't touch it!!
		/// </summary>
		public void InitPlugin() {
			//Debug.Log("EffekseerSystem.InitPlugin");
			if (Instance != null) {
				Debug.LogError("[Effekseer] EffekseerSystem instance is already found.");
			}

#if (UNITY_WEBGL || UNITY_IOS || UNITY_SWITCH) && !UNITY_EDITOR
			Plugin.RegisterPlugin();
#endif

			Instance = this;
			
			var settings = EffekseerSettings.Instance;

			RendererType = settings.RendererType;

			if(RendererType == EffekseerRendererType.Unity)
			{
				bool isWebGL = false;
#if (UNITY_WEBGL)
				isWebGL = true;
#endif

				if (SystemInfo.supportsComputeShaders)
				{
					if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore ||
						SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3)
					{
						Debug.LogWarning("[Effekseer] Graphics API \"" + SystemInfo.graphicsDeviceType + "\" has many limitations with ComputeShader. Renderer is changed into Native.");
						RendererType = EffekseerRendererType.Native;
					}
					else if(isWebGL)
					{
						Debug.LogWarning("[Effekseer] Graphics API WebGL has many limitations with ComputeShader. Renderer is changed into Native.");
						RendererType = EffekseerRendererType.Native;
					}
					else
					{
						// OK
					}
				}
				else
				{
					Debug.LogWarning("[Effekseer] Graphics API \"" + SystemInfo.graphicsDeviceType + "\" is not supported. Renderer is changed into Native.");
					RendererType = EffekseerRendererType.Native;
				}
			}
			else
			{
				// Check whether this api is supported
				switch (SystemInfo.graphicsDeviceType)
				{
					case GraphicsDeviceType.Metal:
					case GraphicsDeviceType.Direct3D12:
					case GraphicsDeviceType.Vulkan:
					case GraphicsDeviceType.XboxOne:
					case GraphicsDeviceType.Switch:
					case GraphicsDeviceType.PlayStation4:
						if (RendererType == EffekseerRendererType.Native)
						{
							RendererType = EffekseerRendererType.Unity;
						}
						Debug.LogWarning("[Effekseer] Graphics API \"" + SystemInfo.graphicsDeviceType + "\" is not supported. Renderer is changed into Unity.");
						break;
				}
			}

			// Zのnearとfarの反転対応
			bool reversedDepth = false;
			switch (SystemInfo.graphicsDeviceType) {
			case GraphicsDeviceType.Direct3D11:
			case GraphicsDeviceType.Direct3D12:
			case GraphicsDeviceType.Metal:
			case GraphicsDeviceType.PlayStation4:
#if UNITY_2017_4_OR_NEWER
			case GraphicsDeviceType.Switch:
#endif
				reversedDepth = true;
				break;
			}

			// Initialize effekseer library
			Plugin.EffekseerInit(settings.effectInstances, settings.maxSquares, reversedDepth ? 1 : 0, settings.isRightEffekseerHandledCoordinateSystem ? 1 : 0, (int)RendererType);

            // Flip
            if(RendererType == EffekseerRendererType.Native)
            {
                Plugin.EffekseerSetIsTextureFlipped(1);
                Plugin.EffekseerSetIsBackgroundTextureFlipped(1);
            }
            if (RendererType == EffekseerRendererType.Unity)
            {
                Plugin.EffekseerSetIsTextureFlipped(0);
                Plugin.EffekseerSetIsBackgroundTextureFlipped(0);
            }

            if (EffekseerSettings.Instance.DoStartNetworkAutomatically)
			{
				StartNetwork();
			}
		}

		/// <summary>
		/// Don't touch it!!
		/// </summary>
		public void TermPlugin() {

			StopNetwork();

			//Debug.Log("EffekseerSystem.TermPlugin");
			foreach (var effectAsset in EffekseerEffectAsset.enabledAssets) {
                EffekseerEffectAsset target = effectAsset.Value.Target as EffekseerEffectAsset;

                if (target != null)
                {
                    ReleaseEffect(target);
				}
			}
			nativeEffects.Clear();
			
#if UNITY_EDITOR
			nativeEffectsKeys.Clear();
			nativeEffectsValues.Clear();
#endif

			// Finalize Effekseer library
			Plugin.EffekseerTerm();
			// For a platform that is releasing in render thread
			GL.IssuePluginEvent(Plugin.EffekseerGetRenderFunc(), 0);
			
			Instance = null;
		}

		/// <summary>
		/// Don't touch it!!
		/// </summary>
		public void OnEnable() {
			if (Instance == null) {
				Instance = this;
			}

			if (Instance.RendererType == EffekseerRendererType.Native)
			{
				renderer = new EffekseerRendererNative();
			}
			else
			{
				renderer = new EffekseerRendererUnity();
			}

			renderer.SetVisible(true);

			// Enable all loading functions
			Plugin.EffekseerSetTextureLoaderEvent(
				TextureLoaderLoad, 
				TextureLoaderUnload);
			Plugin.EffekseerSetModelLoaderEvent(
				ModelLoaderLoad, 
				ModelLoaderUnload);
			Plugin.EffekseerSetSoundLoaderEvent(
				SoundLoaderLoad, 
				SoundLoaderUnload);
			
#if UNITY_EDITOR
			for (int i = 0; i < nativeEffectsKeys.Count; i++) {
				IntPtr nativeEffect = new IntPtr((long)ulong.Parse(nativeEffectsValues[i]));
				nativeEffects.Add(nativeEffectsKeys[i], nativeEffect);
			}
			nativeEffectsKeys.Clear();
			nativeEffectsValues.Clear();
#endif

			ReloadEffects();

			enabled = true;
		}

		/// <summary>
		/// Don't touch it!!
		/// </summary>
		public void OnDisable() {
			enabled = false;

#if UNITY_EDITOR
			foreach (var pair in nativeEffects) {
				nativeEffectsKeys.Add(pair.Key);
				nativeEffectsValues.Add(pair.Value.ToString());
				Plugin.EffekseerUnloadResources(pair.Value);
			}
			nativeEffects.Clear();
#endif
			renderer.CleanUp();
			renderer.SetVisible(false);
			renderer = null;
			
			// Disable all loading functions
			Plugin.EffekseerSetTextureLoaderEvent(null, null);
			Plugin.EffekseerSetModelLoaderEvent(null, null);
			Plugin.EffekseerSetSoundLoaderEvent(null, null);
		}

		float restFrames = 0;

		internal void Update(float deltaTime) {
			float deltaFrames = Utility.TimeToFrames(deltaTime);
			restFrames += deltaFrames;
			int updateCount = Mathf.RoundToInt(restFrames);
			for (int i = 0; i < updateCount; i++) {
				Plugin.EffekseerUpdate(1);
			}
			restFrames -= updateCount;
		}

		internal static Texture GetCachedTexture(IntPtr key)
		{
			if(cachedTextures.ContainsKey(key))
			{
				return cachedTextures[key];
			}
			return Texture2D.whiteTexture;
		}

		internal static UnityRendererModel GetCachedModel(IntPtr key)
		{
			if (cachedModels.ContainsKey(key))
			{
				return cachedModels[key];
			}
			return null;
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerTextureLoaderLoad))]
		private static IntPtr TextureLoaderLoad(IntPtr path, out int width, out int height, out int format)
		{
			var pathstr = Marshal.PtrToStringUni(path);
			var asset = Instance.effectAssetInLoading;
			var res = asset.FindTexture(pathstr);
			var texture = (res != null) ? res.texture : null;

			if (texture != null)
			{
				width = texture.width;
				height = texture.height;
				switch (texture.format)
				{
					case TextureFormat.DXT1: format = 1; break;
					case TextureFormat.DXT5: format = 2; break;
					default: format = 0; break;
				}

				if (Instance.RendererType == EffekseerRendererType.Unity)
				{
					// metal has a bug in GetNativeTexturePtr
					// to avoid to call GetNativeTexturePtr, use textureID
					if (cachedTextureIDs.ContainsKey(texture))
					{
						//Debug.Log("LoadCache(Unity) " + pathstr.ToString());
						return cachedTextureIDs[texture];
					}
					else
					{
						var ptr = new IntPtr();
						do
						{
							textureIDCounter++;
							if (textureIDCounter > int.MaxValue / 2)
							{
								textureIDCounter = 0;
							}
							ptr = new IntPtr(textureIDCounter);
						}
						while (cachedTextures.ContainsKey(ptr));

						cachedTextures.Add(ptr, texture);
						cachedTextureIDs.Add(texture, ptr);
						//Debug.Log("Load(Unity) " + pathstr.ToString());
						return ptr;
					}
				}
				else
				{
					var ptr = texture.GetNativeTexturePtr();
					return ptr;
				}
			}
			width = 0;
			height = 0;
			format = 0;
			return IntPtr.Zero;
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerTextureLoaderUnload))]
		private static void TextureLoaderUnload(IntPtr path, IntPtr nativePtr)
		{
			var pathstr = Marshal.PtrToStringUni(path);

			if (Instance.RendererType == EffekseerRendererType.Unity)
			{
				var texture = cachedTextures[nativePtr];
				cachedTextureIDs.Remove(texture);
				cachedTextures.Remove(nativePtr);
				//Debug.Log("Unload(Unity) " + pathstr.ToString());
			}
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerModelLoaderLoad))]
		private static IntPtr ModelLoaderLoad(IntPtr path, IntPtr buffer, int bufferSize, ref int requiredBufferSize) {
			var pathstr = Marshal.PtrToStringUni(path);
			pathstr = Path.ChangeExtension(pathstr, ".asset");
			var asset = Instance.effectAssetInLoading;
			var res = asset.FindModel(pathstr);
			var model = (res != null) ? res.asset : null;

			if (model != null) {
				requiredBufferSize = model.bytes.Length;

				if (model.bytes.Length <= bufferSize) {
					Marshal.Copy(model.bytes, 0, buffer, model.bytes.Length);

					if(Instance.RendererType == EffekseerRendererType.Unity)
					{
						var unityRendererModel = new UnityRendererModel();
						unityRendererModel.Initialize(model.bytes);

						IntPtr ptr = unityRendererModel.VertexBuffer.GetNativeBufferPtr();
						if (!cachedModels.ContainsKey(ptr)) {
							cachedModels.Add(ptr, unityRendererModel);
						}
						return ptr;
					}

					return new IntPtr(1);
				}
			}

			return IntPtr.Zero;
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerModelLoaderUnload))]
		private static void ModelLoaderUnload(IntPtr path, IntPtr modelPtr) {
			if (Instance.RendererType == EffekseerRendererType.Unity)
			{
				if(cachedModels.ContainsKey(modelPtr))
				{
					var model = cachedModels[modelPtr];
					model.Dispose();
					cachedModels.Remove(modelPtr);
				}
			}
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerSoundLoaderLoad))]
		private static IntPtr SoundLoaderLoad(IntPtr path) {
			var pathstr = Marshal.PtrToStringUni(path);
			var asset = Instance.effectAssetInLoading;
			
			var res = asset.FindSound(pathstr);
			if (res != null) {
				return res.ToIntPtr();
			}
			return IntPtr.Zero;
		}
		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerSoundLoaderUnload))]
		private static void SoundLoaderUnload(IntPtr path) {
		}

#endregion
	}
}
