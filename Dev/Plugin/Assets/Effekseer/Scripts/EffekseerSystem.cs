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

	enum DummyTextureType
	{
		White,
		Normal,
	}

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
			if (Instance == null)
			{
#if UNITY_EDITOR
				if (Application.isPlaying)
				{
					Debug.LogError("[Effekseer] System is not initialized.");
				}
				else
				{
					Debug.LogError("[Effekseer] System is not initialized. Please call EffekseerEditor.instance.InitSystem");
				}
#else
				Debug.LogError("[Effekseer] System is not initialized.");
#endif
				return new EffekseerHandle(-1);
			}
			if (effectAsset == null)
			{
				Debug.LogError("[Effekseer] Specified effect is null.");
				return new EffekseerHandle(-1);
			}

			IntPtr nativeEffect;
			if (Instance.nativeEffects.TryGetValue(effectAsset.GetInstanceID(), out nativeEffect))
			{
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

		/// <summary xml:lang="en">
		/// Gets the number of remaining allocated instances.
		/// </summary>
		/// <summary xml:lang="ja">
		/// 残りの確保したインスタンス数を取得する。
		/// </summary>
		public static int restInstanceCount
		{
			get { return Plugin.EffekseerGetRestInstancesCount(); }
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
		private Dictionary<int, IntPtr> nativeResourceLoadedEffects = new Dictionary<int, IntPtr>();

#if UNITY_EDITOR
		// For hot reloading
		[SerializeField] private List<int> nativeEffectsKeys = new List<int>();
		[SerializeField] private List<string> nativeEffectsValues = new List<string>();
#endif

		// A AssetBundle that current loading
		private EffekseerEffectAsset effectAssetInLoading;

		internal Effekseer.EffekseerRendererType RendererType { get; private set; }

		private static CachedTextureContainer cachedTextures = new CachedTextureContainer();

		private static CachedModelContainer cachedModels = new CachedModelContainer();

		private static CachedMaterialContainer cachedMaterials = new CachedMaterialContainer();

		private static CachedSoundContainer cachedSounds = new CachedSoundContainer();

		static Vector3 lightDirection = new Vector3(1, 1, -1);

		static Color lightColor = new Color(255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f);

		static Color lightAmbientColor = new Color(40.0f / 255.0f, 40.0f / 255.0f, 40.0f / 255.0f);

		static Texture2D normalTexture = null;

		public static Vector3 LightDirection
		{
			get { return lightDirection; }
			set
			{
				lightDirection = value;
			}
		}

		public static Color LightColor
		{
			get { return lightColor; }
			set
			{
				lightColor = value;
			}
		}

		public static Color LightAmbientColor
		{
			get { return lightAmbientColor; }
			set
			{
				lightAmbientColor = value;
			}
		}

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
						if (!nativeResourceLoadedEffects.ContainsKey(id))
						{
							Plugin.EffekseerReloadResources(nativeEffect);
							nativeResourceLoadedEffects.Add(id, nativeEffect);
						}
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
		public void LoadEffect(EffekseerEffectAsset effectAsset)
		{
			effectAssetInLoading = effectAsset;
			int id = effectAsset.GetInstanceID();
			IntPtr nativeEffect;
			if (!nativeEffects.TryGetValue(id, out nativeEffect))
			{
				byte[] bytes = effectAsset.efkBytes;
				var namePtr = Marshal.StringToCoTaskMemUni(effectAsset.name);
				nativeEffect = Plugin.EffekseerLoadEffectOnMemory(bytes, bytes.Length, namePtr, effectAsset.Scale);
				nativeEffects.Add(id, nativeEffect);
				nativeResourceLoadedEffects.Add(id, nativeEffect);
				//loadedEffects.Add(effectAsset);
				//effectAsset.GetInstanceID
				Marshal.FreeCoTaskMem(namePtr);
			}
			else
			{
				// For reloading
				if (!nativeResourceLoadedEffects.ContainsKey(id))
				{
					nativeResourceLoadedEffects.Add(id, nativeEffect);
					Plugin.EffekseerReloadResources(nativeEffect);
				}
			}

			effectAssetInLoading = null;
		}

		internal void ReleaseEffect(EffekseerEffectAsset effectAsset)
		{
			int id = effectAsset.GetInstanceID();
			IntPtr nativeEffect;
			if (nativeEffects.TryGetValue(id, out nativeEffect))
			{
				Plugin.EffekseerUnloadResources(nativeEffect);
				Plugin.EffekseerReleaseEffect(nativeEffect);
				nativeResourceLoadedEffects.Remove(id);
				nativeEffects.Remove(id);
				//loadedEffects.Remove(effectAsset);
			}
		}

		internal float GetEffectMagnification(EffekseerEffectAsset effectAsset)
		{
			int id = effectAsset.GetInstanceID();
			IntPtr nativeEffect;
			if (nativeEffects.TryGetValue(id, out nativeEffect))
			{
				return Plugin.EffekseerGetEffectMagnification(nativeEffect);
			}
			return 0.0f;
		}

		/// <summary>
		/// Don't touch it!!
		/// </summary>
		public void InitPlugin()
		{
			//Debug.Log("EffekseerSystem.InitPlugin");
			if (Instance != null)
			{
				Debug.LogError("[Effekseer] EffekseerSystem instance is already found.");
			}

#if (UNITY_WEBGL || UNITY_IOS || UNITY_SWITCH) && !UNITY_EDITOR
			Plugin.RegisterPlugin();
#endif

#if UNITY_SWITCH && UNITY_2017 && NET_4_6
#error cannot compile with ilcpp
#endif

			Instance = this;

			var settings = EffekseerSettings.Instance;

			RendererType = settings.RendererType;

			if (RendererType == EffekseerRendererType.Unity)
			{
				bool isWebGL = false;
#if (UNITY_WEBGL)
				isWebGL = true;
#endif

				if (SystemInfo.supportsComputeShaders)
				{
					int maxComputeBufferInputsVertex = 0;
#if UNITY_2019_3_OR_NEWER
					maxComputeBufferInputsVertex = SystemInfo.maxComputeBufferInputsVertex;
#endif

					if ((SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore ||
						SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3) && maxComputeBufferInputsVertex < 4

					)
					{
						Debug.LogWarning("[Effekseer] Graphics API \"" + SystemInfo.graphicsDeviceType + "\" has many limitations with ComputeShader. Renderer is changed into Native.");
						RendererType = EffekseerRendererType.Native;
					}
					else if (isWebGL)
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
#if UNITY_IOS
				switch (SystemInfo.graphicsDeviceType)
				{
					case GraphicsDeviceType.OpenGLES2:
					case GraphicsDeviceType.OpenGLES3:
						Debug.LogError("OpenGL is not suppoedted on Mac and iOS.");
						break;
				}
#endif

				// Check whether this api is supported
				switch (SystemInfo.graphicsDeviceType)
				{
					case GraphicsDeviceType.Metal:
					case GraphicsDeviceType.Direct3D12:
					case GraphicsDeviceType.Vulkan:
					case GraphicsDeviceType.XboxOne:
					case GraphicsDeviceType.XboxOneD3D12:
#if UNITY_2021_1_OR_NEWER
					case GraphicsDeviceType.PlayStation5NGGC:
#endif
						if (RendererType == EffekseerRendererType.Native)
						{
							RendererType = EffekseerRendererType.Unity;
						}
						Debug.LogWarning("[Effekseer] Graphics API \"" + SystemInfo.graphicsDeviceType + "\" is not supported. Renderer is changed into Unity.");
						break;
				}
			}

			// reverse Znear and Zfar
			bool reversedDepth = false;
			switch (SystemInfo.graphicsDeviceType)
			{
				case GraphicsDeviceType.Direct3D11:
				case GraphicsDeviceType.Direct3D12:
				case GraphicsDeviceType.Metal:
				case GraphicsDeviceType.PlayStation4:
				case GraphicsDeviceType.PlayStation5:
#if UNITY_2021_1_OR_NEWER
				case GraphicsDeviceType.PlayStation5NGGC:
#endif
#if UNITY_2017_4_OR_NEWER
				case GraphicsDeviceType.Switch:
#endif
					reversedDepth = true;
					break;
			}

			// Initialize effekseer library

			var maintainGammaColor = QualitySettings.activeColorSpace == ColorSpace.Linear && settings.MaintainGammaColorInLinearSpace;

			Plugin.EffekseerInit(settings.effectInstances, settings.maxSquares, reversedDepth ? 1 : 0, maintainGammaColor ? 1 : 0, settings.isRightEffekseerHandledCoordinateSystem ? 1 : 0, settings.threadCount, (int)RendererType);

			// Flip
			if (RendererType == EffekseerRendererType.Native)
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

			// Create dummy texture
			normalTexture = new Texture2D(16, 16);
			normalTexture.name = "EffekseerNormalTexture";
			Color[] normalColor = new Color[16 * 16];
			for (int i = 0; i < normalColor.Length; i++)
			{
				normalColor[i] = new Color(0.5f, 0.5f, 1.0f);
			}
			normalTexture.SetPixels(normalColor);
			normalTexture.Apply();
		}

		/// <summary>
		/// Don't touch it!!
		/// </summary>
		public void TermPlugin()
		{

			StopNetwork();

			StopAllEffects();

#if UNITY_EDITOR
			RestoreNativeEffects();
#endif

			foreach (var effectAsset in EffekseerEffectAsset.enabledAssets)
			{
				EffekseerEffectAsset target = effectAsset.Value.Target as EffekseerEffectAsset;

				if (target != null)
				{
					ReleaseEffect(target);
				}
			}
			nativeEffects.Clear();
			nativeResourceLoadedEffects.Clear();

			// Finalize Effekseer library
			Plugin.EffekseerTerm();
			// For a platform that is releasing in render thread
			GL.IssuePluginEvent(Plugin.EffekseerGetRenderFunc(), 0);

			Instance = null;
		}

		/// <summary>
		/// Don't touch it!!
		/// </summary>
		public unsafe void OnEnable()
		{
			if (Instance == null)
			{
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
				TextureLoaderUnload,
				TextureLoaderGetUnityId);
			Plugin.EffekseerSetModelLoaderEvent(
				ModelLoaderLoad,
				ModelLoaderUnload,
				ModelLoaderGetUnityId);
			Plugin.EffekseerSetSoundLoaderEvent(
				SoundLoaderLoad,
				SoundLoaderUnload,
				SoundLoaderGetUnityId);
			Plugin.EffekseerSetMaterialLoaderEvent(
				MaterialLoaderLoad,
				MaterialLoaderUnload,
				MaterialLoaderGetUnityId);
			Plugin.EffekseerSetProceduralModelGeneratorEvent(
				ProceduralMaterialGeneratorGenerate,
				ProceduralMaterialGeneratorUngenerate);
			Plugin.EffekseerSetCurveLoaderEvent(
				CurveLoaderLoad, CurveLoaderUnload, CurveLoaderGetUnityId
				);

#if UNITY_EDITOR
			RestoreNativeEffects();
#endif

			ReloadEffects();

			enabled = true;
		}

		/// <summary>
		/// Don't touch it!!
		/// </summary>
		public void OnDisable()
		{
			enabled = false;

#if UNITY_EDITOR
			EscapeNativeEffects();
#endif
			foreach (var pair in nativeResourceLoadedEffects)
			{
				Plugin.EffekseerUnloadResources(pair.Value);
			}
			nativeResourceLoadedEffects.Clear();

			renderer.CleanUp();
			renderer.SetVisible(false);
			renderer = null;

			// Disable all loading functions
			Plugin.EffekseerSetTextureLoaderEvent(null, null, null);
			Plugin.EffekseerSetModelLoaderEvent(null, null, null);
			Plugin.EffekseerSetSoundLoaderEvent(null, null, null);
			Plugin.EffekseerSetMaterialLoaderEvent(null, null, null);
			Plugin.EffekseerSetProceduralModelGeneratorEvent(null, null);
			Plugin.EffekseerSetCurveLoaderEvent(null, null, null);
		}

#if UNITY_EDITOR
		private unsafe void RestoreNativeEffects()
		{
			for (int i = 0; i < nativeEffectsKeys.Count; i++)
			{
				IntPtr nativeEffect = new IntPtr((long)ulong.Parse(nativeEffectsValues[i]));
				nativeEffects.Add(nativeEffectsKeys[i], nativeEffect);
			}
			nativeEffectsKeys.Clear();
			nativeEffectsValues.Clear();
		}

		private void EscapeNativeEffects()
		{
			foreach (var pair in nativeEffects)
			{
				nativeEffectsKeys.Add(pair.Key);
				nativeEffectsValues.Add(pair.Value.ToString());
			}
			nativeEffects.Clear();
		}

		public void UpdateTime(float deltaTime)
		{
			Plugin.EffekseerUpdateTime(deltaTime);
		}

		public void ResetRestTime()
		{
			restFrames = 0;
		}
#endif

		float restFrames = 0;

		internal void Update(float deltaTime, float unsacaledDeltaTime)
		{
			float deltaFrames = Utility.TimeToFrames(deltaTime);
			float unsacaledDeltaFrames = Utility.TimeToFrames(unsacaledDeltaTime);

			Plugin.EffekseerSetTimeScaleByGroup(1, deltaFrames);
			Plugin.EffekseerSetTimeScaleByGroup(2, unsacaledDeltaFrames);
			Plugin.EffekseerUpdate(1);

			restFrames += deltaFrames;
			int updateCount = Mathf.RoundToInt(restFrames);
			for (int i = 0; i < updateCount; i++)
			{
				Plugin.EffekseerUpdateTime(1);
				//Plugin.EffekseerUpdate(1);
			}
			restFrames -= updateCount;

			ApplyLightingToNative();
		}

		/// <summary>
		/// Don't touch it!!
		/// </summary>
		public void ApplyLightingToNative()
		{
			var lr = (int)Math.Max(Math.Min(255, lightColor.r * 255), 0);
			var lg = (int)Math.Max(Math.Min(255, lightColor.g * 255), 0);
			var lb = (int)Math.Max(Math.Min(255, lightColor.b * 255), 0);
			var lar = (int)Math.Max(Math.Min(255, lightAmbientColor.r * 255), 0);
			var lag = (int)Math.Max(Math.Min(255, lightAmbientColor.g * 255), 0);
			var lab = (int)Math.Max(Math.Min(255, lightAmbientColor.b * 255), 0);
			var direction = LightDirection.normalized;

			Plugin.EffekseerSetLightDirection(direction.x, direction.y, direction.z);
			Plugin.EffekseerSetLightColor(lr, lg, lb);
			Plugin.EffekseerSetLightAmbientColor(lar, lab, lag);
		}

		internal static Texture GetCachedTexture(IntPtr key, DummyTextureType type)
		{
			var cache = cachedTextures.GetResource(key);

			if (cache != null)
			{
				return cache;
			}

			if (type == DummyTextureType.White)
			{
				return Texture2D.whiteTexture;
			}
			else
			{
				return normalTexture;
			}
		}

		internal static UnityRendererModel GetCachedModel(IntPtr key)
		{
			return cachedModels.GetResource(key);
		}

		internal static UnityRendererMaterial GetCachedMaterial(IntPtr key)
		{
			return cachedMaterials.GetResource(key);
		}

		internal static IntPtr GetCachedSound(IntPtr key)
		{
			return cachedSounds.GetResource(key);
		}

		private static Texture2D GetTextureFromPath(string path)
		{
			var asset = Instance.effectAssetInLoading;
			var res = asset.FindTexture(path);
			return (res != null) ? res.texture : null;
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerTextureLoaderLoad))]
		private static IntPtr TextureLoaderLoad(IntPtr path, out int width, out int height, out int format, out int mipmapCount)
		{
			var pathstr = Marshal.PtrToStringUni(path);
			var texture = GetTextureFromPath(pathstr);

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
				mipmapCount = texture.mipmapCount;

				if (Instance.RendererType == EffekseerRendererType.Unity)
				{
					// metal has a bug in GetNativeTexturePtr
					// to avoid to call GetNativeTexturePtr, use textureID
					return cachedTextures.Load(texture, pathstr);
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
			mipmapCount = 0;
			return IntPtr.Zero;
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerTextureLoaderUnload))]
		private static void TextureLoaderUnload(int id, IntPtr nativePtr)
		{
			if (Instance.RendererType == EffekseerRendererType.Unity)
			{
				cachedTextures.Unload(nativePtr);
			}
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerGetUnityIdFromPath))]
		private static int TextureLoaderGetUnityId(IntPtr path)
		{
			var pathstr = Marshal.PtrToStringUni(path);
			var texture = GetTextureFromPath(pathstr);

			if (texture != null)
			{
				return texture.GetInstanceID();
			}

			return 0;
		}

		private static EffekseerModelAsset GetModelFromPath(string path)
		{
			var asset = Instance.effectAssetInLoading;
			var res = asset.FindModel(path);
			return (res != null) ? res.asset : null;
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerModelLoaderLoad))]
		private static IntPtr ModelLoaderLoad(IntPtr path, IntPtr buffer, int bufferSize, ref int requiredBufferSize)
		{
			var pathstr = Marshal.PtrToStringUni(path);
			pathstr = Path.ChangeExtension(pathstr, ".asset");
			var model = GetModelFromPath(pathstr);

			if (model != null)
			{
				requiredBufferSize = model.bytes.Length;

				if (model.bytes.Length <= bufferSize)
				{
					Marshal.Copy(model.bytes, 0, buffer, model.bytes.Length);

					if (Instance.RendererType == EffekseerRendererType.Unity)
					{
						return cachedModels.Load(model, pathstr);
					}

					return new IntPtr(1);
				}
			}

			return IntPtr.Zero;
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerModelLoaderUnload))]
		private static void ModelLoaderUnload(int id, IntPtr modelPtr)
		{
			if (Instance.RendererType == EffekseerRendererType.Unity)
			{
				cachedModels.Unload(modelPtr);
			}
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerGetUnityIdFromPath))]
		private static int ModelLoaderGetUnityId(IntPtr path)
		{
			var pathstr = Marshal.PtrToStringUni(path);
			pathstr = Path.ChangeExtension(pathstr, ".asset");
			var model = GetModelFromPath(pathstr);

			if (model != null)
			{
				return model.GetInstanceID();
			}

			return 0;
		}

		private static EffekseerMaterialAsset GetMaterialFromPath(string path)
		{
			var asset = Instance.effectAssetInLoading;
			var res = asset.FindMaterial(path);
			return (res != null) ? res.asset : null;
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerMaterialLoaderLoad))]
		private static IntPtr MaterialLoaderLoad(IntPtr path,
			IntPtr materialBuffer, int materialBufferSize, ref int requiredMaterialBufferSize,
			IntPtr cachedMaterialBuffer, int cachedMaterialBufferSize, ref int requiredCachedMaterialBufferSize)
		{
			var pathstr = Marshal.PtrToStringUni(path);
			pathstr = Path.ChangeExtension(pathstr, ".asset");
			var material = GetMaterialFromPath(pathstr);

			if (material != null)
			{
				if (Instance.RendererType == EffekseerRendererType.Unity)
				{
					int status = 0;

					if (requiredMaterialBufferSize == 0 && material.materialBuffers != null)
					{
						requiredMaterialBufferSize = material.materialBuffers.Length;

						status += 1;
						return new IntPtr(status);
					}

					if (material.materialBuffers.Length <= materialBufferSize)
					{
						Marshal.Copy(material.materialBuffers, 0, materialBuffer, material.materialBuffers.Length);
					}

					return cachedMaterials.Load(material, pathstr);
				}
				else
				{
					int status = 0;

					if (material.cachedMaterialBuffers != null)
					{
						requiredCachedMaterialBufferSize = material.cachedMaterialBuffers.Length;

						if (material.cachedMaterialBuffers.Length <= cachedMaterialBufferSize)
						{
							Marshal.Copy(material.cachedMaterialBuffers, 0, cachedMaterialBuffer, material.cachedMaterialBuffers.Length);
						}

						status += 2;
					}

					if (material.materialBuffers != null)
					{
						requiredMaterialBufferSize = material.materialBuffers.Length;

						if (material.materialBuffers.Length <= materialBufferSize)
						{
							Marshal.Copy(material.materialBuffers, 0, materialBuffer, material.materialBuffers.Length);
						}

						status += 1;
					}

					return new IntPtr(status);
				}
			}

			return IntPtr.Zero;
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerMaterialLoaderUnload))]
		private static void MaterialLoaderUnload(int id, IntPtr materialPtr)
		{
			if (Instance.RendererType == EffekseerRendererType.Unity)
			{
				cachedMaterials.Unload(materialPtr);
			}
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerGetUnityIdFromPath))]
		private static int MaterialLoaderGetUnityId(IntPtr path)
		{
			var pathstr = Marshal.PtrToStringUni(path);
			pathstr = Path.ChangeExtension(pathstr, ".asset");
			var material = GetMaterialFromPath(pathstr);

			if (material != null)
			{
				return material.GetInstanceID();
			}

			return 0;
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerProceduralMaterialGeneratorGenerate))]
		private static unsafe IntPtr ProceduralMaterialGeneratorGenerate(Plugin.ModelVertex* vertecies,
															int verteciesCount,
															Plugin.ModelFace* faces,
															int facesCount)
		{
			if (Instance.RendererType != EffekseerRendererType.Unity)
			{
				return IntPtr.Zero;
			}

			var unityRendererModel = new UnityRendererModel();
			unityRendererModel.Initialize(vertecies, verteciesCount, faces, facesCount);
			return cachedModels.Load(unityRendererModel, "ProceduralModel");
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerProceduralMaterialGeneratorUngenerate))]
		private static void ProceduralMaterialGeneratorUngenerate(IntPtr modelPtr)
		{
			if (Instance.RendererType == EffekseerRendererType.Unity)
			{
				cachedModels.Unload(modelPtr);
			}
		}

		private static EffekseerSoundResource GetSoundFromPath(string path)
		{
			var asset = Instance.effectAssetInLoading;
			return asset.FindSound(path);
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerSoundLoaderLoad))]
		private static IntPtr SoundLoaderLoad(IntPtr path)
		{
			var pathstr = Marshal.PtrToStringUni(path);
			var res = GetSoundFromPath(pathstr);
			if (res != null)
			{
				return cachedSounds.Load(res, pathstr);
			}
			return IntPtr.Zero;
		}
		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerSoundLoaderUnload))]
		private static void SoundLoaderUnload(IntPtr soundPtr)
		{
			cachedSounds.Unload(soundPtr);
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerGetUnityIdFromPath))]
		private static int SoundLoaderGetUnityId(IntPtr path)
		{
			var pathstr = Marshal.PtrToStringUni(path);
			var res = GetSoundFromPath(pathstr);

			if (res != null)
			{
				return res.clip.GetInstanceID();
			}

			return 0;
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerCurveLoaderLoad))]
		private static IntPtr CurveLoaderLoad(IntPtr path, IntPtr buffer, int bufferSize, ref int requiredBufferSize)
		{
			var pathstr = Marshal.PtrToStringUni(path);
			pathstr = Path.ChangeExtension(pathstr, ".asset");
			var asset = Instance.effectAssetInLoading;
			var res = asset.FindCurve(pathstr);
			var curve = (res != null) ? res.asset : null;

			if (curve != null)
			{
				requiredBufferSize = curve.bytes.Length;

				if (curve.bytes.Length <= bufferSize)
				{
					Marshal.Copy(curve.bytes, 0, buffer, curve.bytes.Length);
					return new IntPtr(1);
				}
			}

			return IntPtr.Zero;
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerModelLoaderUnload))]
		private static void CurveLoaderUnload(int id, IntPtr modelPtr)
		{
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerGetUnityIdFromPath))]
		private static int CurveLoaderGetUnityId(IntPtr path)
		{
			var pathstr = Marshal.PtrToStringUni(path);
			pathstr = Path.ChangeExtension(pathstr, ".asset");
			var asset = Instance.effectAssetInLoading;
			var res = asset.FindCurve(pathstr);

			if (res != null)
			{
				return res.asset.GetInstanceID();
			}

			return 0;
		}


		abstract class CachedResourceContainer<Resource, GeneratedResource> where Resource : class
		{
			class ResourceContainer
			{
				public int Reference;
				public Resource Resource;
				public GeneratedResource GeneratedResource;
				public string Info;
			}

			Dictionary<IntPtr, ResourceContainer> idToResource = new Dictionary<IntPtr, ResourceContainer>();

			Dictionary<Resource, IntPtr> resourceToIDs = new Dictionary<Resource, IntPtr>();

			protected abstract GeneratedResource GenerateResource(Resource resource);

			protected virtual void DisposeGeneratedResource(GeneratedResource resource) { }

			public GeneratedResource GetResource(IntPtr id)
			{
				ResourceContainer r;
				idToResource.TryGetValue(id, out r);
				if (r == null)
					return default(GeneratedResource);

				return r.GeneratedResource;
			}

			int currentId = 0;
			public IntPtr Load(Resource key, string info)
			{
				if (resourceToIDs.ContainsKey(key))
				{
					var id = resourceToIDs[key];
					idToResource[id].Reference++;
					return id;
				}

				IntPtr ptr;
				var generated = GenerateResource(key);
				if (generated == null)
				{
					return IntPtr.Zero;
				}

				do
				{
					currentId++;
					if (currentId > int.MaxValue / 2)
					{
						currentId = 1;
					}
					ptr = new IntPtr(currentId);
				}
				while (idToResource.ContainsKey(ptr));

				idToResource.Add(ptr, new ResourceContainer { Resource = key, GeneratedResource = generated, Reference = 1, Info = info });
				resourceToIDs.Add(key, ptr);

				// Debug.Log("Load(Unity) " + info);
				return ptr;
			}

			public void Unload(IntPtr id)
			{
				if (id == IntPtr.Zero)
				{
					return;
				}

				ResourceContainer resource;
				if (idToResource.TryGetValue(id, out resource))
				{
					resource.Reference--;
					if (resource.Reference == 0)
					{
						var texture = resource.Resource;
						resourceToIDs.Remove(texture);
						idToResource.Remove(id);
						DisposeGeneratedResource(resource.GeneratedResource);
						// Debug.Log("Unload(Unity) " + resource.Info);
					}
				}
				else
				{
					Debug.LogAssertion("Unload(Unity) Unload invalid resource " + id.ToString());
				}
			}
		}

		class CachedTextureContainer : CachedResourceContainer<Texture, Texture>
		{
			protected override Texture GenerateResource(Texture resource)
			{
				return resource;
			}
		}

		class CachedModelContainer : CachedResourceContainer<object, UnityRendererModel>
		{
			protected override UnityRendererModel GenerateResource(object resource)
			{
				var asset = resource as EffekseerModelAsset;

				if (asset != null)
				{
					var unityRendererModel = new UnityRendererModel();
					unityRendererModel.Initialize(asset.bytes);
					return unityRendererModel;
				}

				var model = resource as UnityRendererModel;
				if (model != null)
				{
					return model;
				}

				throw new InvalidDataException();
			}

			protected override void DisposeGeneratedResource(UnityRendererModel resource)
			{
				resource.Dispose();
			}
		}

		class CachedMaterialContainer : CachedResourceContainer<EffekseerMaterialAsset, UnityRendererMaterial>
		{
			protected override UnityRendererMaterial GenerateResource(EffekseerMaterialAsset resource)
			{
				return new UnityRendererMaterial(resource);
			}
		}

		class CachedSoundContainer : CachedResourceContainer<EffekseerSoundResource, IntPtr>
		{
			protected override IntPtr GenerateResource(EffekseerSoundResource resource)
			{
				return GCHandle.ToIntPtr(GCHandle.Alloc(resource));
			}

			protected override void DisposeGeneratedResource(IntPtr resource)
			{
				GCHandle.FromIntPtr(resource).Free();
			}
		}

		#endregion
	}
}
