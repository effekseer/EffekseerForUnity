using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Effekseer
{
	using Internal;

	public class EffekseerSystem : MonoBehaviour
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
		
		#region Internal Implimentation
		
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		static void RuntimeInitializeOnLoad()
		{
			if (Instance != null) {
				return;
			}

			// Find instance if is not set static variable
			var instance = GameObject.FindObjectOfType<EffekseerSystem>();
			if (instance == null) {
				// Create instance if instance not found
				var go = new GameObject("Effekseer");
				go.AddComponent<EffekseerSystem>();
				DontDestroyOnLoad(go);
			}
		}
		
		// Singleton instance
		public static EffekseerSystem Instance { get; private set; }
		public static bool IsValid { get { return Instance != null; } }

		private new EffekseerRenderer renderer;

		// Loaded native effects
		private Dictionary<int, IntPtr> nativeEffects = new Dictionary<int, IntPtr>();
		// Loaded effect resources
		private List<EffekseerSoundInstance> soundInstances = new List<EffekseerSoundInstance>();

#if UNITY_EDITOR
		// For hot reloading
		[SerializeField] private List<int> nativeEffectsKeys = new List<int>();
		[SerializeField] private List<string> nativeEffectsValues = new List<string>();
#endif

		// A AssetBundle that current loading
		private EffekseerEffectAsset effectAssetInLoading;
		
		internal void LoadEffect(EffekseerEffectAsset effectAsset) {
			effectAssetInLoading = effectAsset;
			int id = effectAsset.GetInstanceID();
			IntPtr nativeEffect;
			if (!nativeEffects.TryGetValue(id, out nativeEffect)) {
				byte[] bytes = effectAsset.efkBytes;
				nativeEffect = Plugin.EffekseerLoadEffectOnMemory(bytes, bytes.Length);
				nativeEffects.Add(id, nativeEffect);
			} else {
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
			}
		}

		internal static void InitPlugin() {
			var settings = EffekseerSettings.Instance;

			// サポート外グラフィックスAPIのチェック
			switch (SystemInfo.graphicsDeviceType) {
			case GraphicsDeviceType.Metal:
	#if UNITY_5_4_OR_NEWER
			case GraphicsDeviceType.Direct3D12:
	#elif UNITY_5_5_OR_NEWER
			case GraphicsDeviceType.Vulkan:
	#endif
				Debug.LogError("[Effekseer] Graphics API \"" + SystemInfo.graphicsDeviceType + "\" is not supported.");
				return;
			}

			// Zのnearとfarの反転対応
			bool reversedDepth = false;
	#if UNITY_5_5_OR_NEWER
			switch (SystemInfo.graphicsDeviceType) {
			case GraphicsDeviceType.Direct3D11:
			case GraphicsDeviceType.Direct3D12:
			case GraphicsDeviceType.Metal:
				reversedDepth = true;
				break;
			}
	#endif

			// Initialize effekseer library
			Plugin.EffekseerInit(settings.effectInstances, settings.maxSquares, reversedDepth, settings.isRightEffekseerHandledCoordinateSystem);
		}

		internal static void TermPlugin() {
			// Finalize Effekseer library
			Plugin.EffekseerTerm();
			// For a platform that is releasing in render thread
			GL.IssuePluginEvent(Plugin.EffekseerGetRenderFunc(), 0);
		}

		private void EnableCallbacks() {
			Plugin.EffekseerSetTextureLoaderEvent(
				TextureLoaderLoad, 
				TextureLoaderUnload);
			Plugin.EffekseerSetModelLoaderEvent(
				ModelLoaderLoad, 
				ModelLoaderUnload);
			Plugin.EffekseerSetSoundLoaderEvent(
				SoundLoaderLoad, 
				SoundLoaderUnload);
			Plugin.EffekseerSetSoundPlayerEvent(
				SoundPlayerPlay,
				SoundPlayerStopTag, 
				SoundPlayerPauseTag, 
				SoundPlayerCheckPlayingTag, 
				SoundPlayerStopAll);
		}

		private void DisableCallbacks() {
			Plugin.EffekseerSetTextureLoaderEvent(null, null);
			Plugin.EffekseerSetModelLoaderEvent(null, null);
			Plugin.EffekseerSetSoundLoaderEvent(null, null);
			Plugin.EffekseerSetSoundPlayerEvent(null,null, null, null, null);
		}

		void Awake() {
			Instance = this;
			InitPlugin();
			
			if (Application.isPlaying) {
				var settings = EffekseerSettings.Instance;
				// サウンドインスタンスを作る
				for (int i = 0; i < settings.soundInstances; i++) {
					GameObject go = new GameObject();
					go.name = "Sound Instance";
					go.transform.parent = transform;
					soundInstances.Add(go.AddComponent<EffekseerSoundInstance>());
				}
			}
		}

		void OnDestroy() {
			foreach (var effectAsset in EffekseerEffectAsset.enabledAssets) {
				ReleaseEffect(effectAsset);
			}

			TermPlugin();
		}

		void OnEnable() {
			if (Instance == null) {
				Instance = this;
			}
			renderer = new EffekseerRenderer();
			renderer.SetVisible(true);

			// Enable all loading functions
			EnableCallbacks();
			
	#if UNITY_EDITOR
			Resume();
	#endif
			foreach (var effectAsset in EffekseerEffectAsset.enabledAssets) {
				LoadEffect(effectAsset);
			}
		}

		void OnDisable() {
	#if UNITY_EDITOR
			Suspend();
	#endif
			renderer.CleanUp();
			renderer.SetVisible(false);
			renderer = null;
			
			// Disable all loading functions
			DisableCallbacks();
		}
	
	#if UNITY_EDITOR
		void Suspend() {
			foreach (var pair in nativeEffects) {
				nativeEffectsKeys.Add(pair.Key);
				nativeEffectsValues.Add(pair.Value.ToString());
				Plugin.EffekseerUnloadResources(pair.Value);
			}
			nativeEffects.Clear();
		}
		void Resume() {
			for (int i = 0; i < nativeEffectsKeys.Count; i++) {
				IntPtr nativeEffect = new IntPtr((long)ulong.Parse(nativeEffectsValues[i]));
				nativeEffects.Add(nativeEffectsKeys[i], nativeEffect);
			}
			nativeEffectsKeys.Clear();
			nativeEffectsValues.Clear();
		}
	#endif
		
		void LateUpdate() {
			float deltaFrames = Time.deltaTime * 60.0f;
			int updateCount = Mathf.Max(1, Mathf.RoundToInt(deltaFrames));
			for (int i = 0; i < updateCount; i++) {
				Plugin.EffekseerUpdate(deltaFrames / updateCount);
			}
		}
		
		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerTextureLoaderLoad))]
		private static IntPtr TextureLoaderLoad(IntPtr path, out int width, out int height, out int format) {
			var pathstr = Marshal.PtrToStringUni(path);
			var asset = Instance.effectAssetInLoading;
			var res = asset.FindTexture(pathstr);
			var texture = (res != null) ? res.texture : null;

			if (texture != null) {
				width = texture.width;
				height = texture.height;
				switch (texture.format) {
				case TextureFormat.DXT1: format = 1; break;
				case TextureFormat.DXT5: format = 2; break;
				default: format = 0; break;
				}
			
				return texture.GetNativeTexturePtr();
			}
			width = 0;
			height = 0;
			format = 0;
			return IntPtr.Zero;
		}
		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerTextureLoaderUnload))]
		private static void TextureLoaderUnload(IntPtr path) {
		}
		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerModelLoaderLoad))]
		private static int ModelLoaderLoad(IntPtr path, IntPtr buffer, int bufferSize) {
			var pathstr = Marshal.PtrToStringUni(path);
			var asset = Instance.effectAssetInLoading;
			var res = asset.FindModel(pathstr);
			var model = (res != null) ? res.asset : null;

			if (model != null) {
				if (model.bytes.Length <= bufferSize) {
					Marshal.Copy(model.bytes, 0, buffer, model.bytes.Length);
					return model.bytes.Length;
				}
			}
			return -1;
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerModelLoaderUnload))]
		private static void ModelLoaderUnload(IntPtr path) {
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
	
		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerSoundPlayerPlay))]
		private static void SoundPlayerPlay(IntPtr tag, 
				IntPtr data, float volume, float pan, float pitch, 
				bool mode3D, float x, float y, float z, float distance) {
			Instance.PlaySound(tag, data, volume, pan, pitch, mode3D, x, y, z, distance);
		}
		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerSoundPlayerStopTag))]
		private static void SoundPlayerStopTag(IntPtr tag) {
			Instance.StopSound(tag);
		}
		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerSoundPlayerPauseTag))]
		private static void SoundPlayerPauseTag(IntPtr tag, bool pause) {
			Instance.PauseSound(tag, pause);
		}
		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerSoundPlayerCheckPlayingTag))]
		private static bool SoundPlayerCheckPlayingTag(IntPtr tag) {
			return Instance.CheckSound(tag);
		}
		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerSoundPlayerStopAll))]
		private static void SoundPlayerStopAll() {
			Instance.StopAllSounds();
		}

		private void PlaySound(IntPtr tag, 
			IntPtr data, float volume, float pan, float pitch, 
			bool mode3D, float x, float y, float z, float distance)
		{
			if (data == IntPtr.Zero) {
				return;
			}
			var resource = EffekseerSoundResource.FromIntPtr(data);
			if (resource == null) {
				return;
			}
			foreach (var instance in soundInstances) {
				if (!instance.CheckPlaying()) {
					instance.Play(tag.ToString(), resource, volume, pan, pitch, mode3D, x, y, z, distance);
					break;
				}
			}
		}
		private void StopSound(IntPtr tag) {
			foreach (var sound in soundInstances) {
				if (sound.AudioTag == tag.ToString()) {
					sound.Stop();
				}
			}
		}
		private void PauseSound(IntPtr tag, bool paused) {
			foreach (var sound in soundInstances) {
				if (sound.AudioTag == tag.ToString()) {
					sound.Pause(paused);
				}
			}
		}
		private bool CheckSound(IntPtr tag) {
			bool playing = false;
			foreach (var sound in soundInstances) {
				if (sound.AudioTag == tag.ToString()) {
					playing |= sound.CheckPlaying();
				}
			}
			return playing;
		}
		private void StopAllSounds() {
			foreach (var sound in soundInstances) {
				sound.Stop();
			}
		}

		#endregion
	}
}
