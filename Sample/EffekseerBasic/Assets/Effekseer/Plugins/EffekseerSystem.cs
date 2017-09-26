using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Effekseer;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EffekseerSystem : MonoBehaviour
{
	/// <summary xml:lang="en">
	/// Whether it does draw in scene view for editor.
	/// </summary>
	/// <summary xml:lang="ja">
	/// エディタのシーンビューに描画するかどうか
	/// </summary>
	public bool drawInSceneView = true;

	/// <summary xml:lang="en">
	/// Maximum number of effect instances.
	/// </summary>
	/// <summary xml:lang="ja">
	/// エフェクトインスタンスの最大数
	/// </summary>
	public int effectInstances	= 1600;

	/// <summary xml:lang="en">
	/// Maximum number of quads that can be drawn.
	/// </summary>
	/// <summary xml:lang="ja">
	/// 描画できる四角形の最大数
	/// </summary>
	public int maxSquares		= 8192;

	/// <summary xml:lang="en">
	/// The coordinate system of effects.
	/// if it is true, effects is loaded as same as before version 1.3.
	/// if it is false, effects is shown as same as the editor.
	/// </summary>
	/// <summary xml:lang="ja">
	/// エフェクトの座標系
	/// trueならば、version1.3以前と同じように読み込まれる。
	/// falseならば、エディタと同じように表示される。
	/// </summary>
	public bool isRightHandledCoordinateSystem = false;

	/// <summary xml:lang="en">
	/// Maximum number of sound instances.
	/// </summary>
	/// <summary xml:lang="ja">
	/// サウンドインスタンスの最大数
	/// </summary>
	public int soundInstances	= 16;

	/// <summary xml:lang="en">
	/// Enables distortion effect.
	/// When It has set false, rendering will be faster.
	/// </summary>
	/// <summary xml:lang="ja">
	/// 歪みエフェクトを有効にする。
	/// falseにすると描画処理が軽くなります。
	/// </summary>
	public bool enableDistortion	= true;

	/// <summary xml:lang="en">
	/// A CameraEvent to draw all effects.
	/// </summary>
	/// <summary xml:lang="ja">
	/// エフェクトの描画するタイミング
	/// </summary>
	const CameraEvent cameraEvent	= CameraEvent.BeforeImageEffects;

	/// <summary xml:lang="en">
	/// Plays the effect.
	/// </summary>
	/// <param name="name" xml:lang="en">Effect name</param>
	/// <param name="location" xml:lang="en">Location in world space</param>
	/// <returns>Played effect instance</returns>
	/// <summary xml:lang="ja">
	/// エフェクトの再生
	/// </summary>
	/// <param name="name" xml:lang="ja">エフェクト名</param>
	/// <param name="location" xml:lang="ja">再生開始する位置</param>
	/// <returns>再生したエフェクトインスタンス</returns>
	public static EffekseerHandle PlayEffect(string name, Vector3 location)
	{
		IntPtr effect = Instance._GetEffect(name);
		if (effect != IntPtr.Zero) {
			int handle = Plugin.EffekseerPlayEffect(effect, location.x, location.y, location.z);
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
	/// Loads the effect from "Resources/Effekseer/"
	/// </summary>
	/// <param name="name" xml:lang="en">Effect name (that resolved extensions from efk file name)</param>
	/// <summary xml:lang="ja">
	/// エフェクトのロード (Resources/Effekseer/から)
	/// </summary>
	/// <param name="name" xml:lang="ja">エフェクト名 (efkファイルの名前から".efk"を取り除いたもの)</param>
	public static void LoadEffect(string name)
	{
		Instance._LoadEffect(name, null);
	}
	
	/// <summary xml:lang="en">
	/// Loads the effect from AssetBundle
	/// </summary>
	/// <param name="name" xml:lang="en">Effect name (that resolved extensions from efk file name)</param>
	/// <param name="assetBundle" xml:lang="en">Source AssetBundle</param>
	/// <summary xml:lang="ja">
	/// エフェクトのロード (AssetBundleから)
	/// </summary>
	/// <param name="name" xml:lang="ja">エフェクト名 (efkファイルの名前から".efk"を取り除いたもの)</param>
	/// <param name="assetBundle" xml:lang="ja">ロード元のAssetBundle</param>
	public static void LoadEffect(string name, AssetBundle assetBundle)
	{
		Instance._LoadEffect(name, assetBundle);
	}

	/// <summary xml:lang="en">
	/// Releases the effect
	/// </summary>
	/// <param name="name" xml:lang="en">Effect name (that resolved extensions from efk file name)</param>
	/// <summary xml:lang="ja">
	/// エフェクトの解放
	/// </summary>
	/// <param name="name" xml:lang="ja">エフェクト名 (efkファイルの名前から".efk"を取り除いたもの)</param>
	public static void ReleaseEffect(string name)
	{
		Instance._ReleaseEffect(name);
	}

	#region Internal Implimentation
	
	// Singleton instance
	private static EffekseerSystem instance = null;
	public static EffekseerSystem Instance
	{
		get {
			if (instance == null) {
				// Find instance when is not set static variable
				var system = GameObject.FindObjectOfType<EffekseerSystem>();
				if (system != null) {
					// Sets static variable when instance is found
					instance = system;
				} else {
					// Create instance when instance is not found
					var go = GameObject.Find("Effekseer");
					if (go == null) {
						go = new GameObject("Effekseer");
					}
					instance = go.AddComponent<EffekseerSystem>();
				}
			}
			return instance;
		}
	}

	private int initedCount = 0;

	// Loaded effects
	private Dictionary<string, IntPtr> effectList;
	// Loaded effect resources
	private List<TextureResource> textureList;
	private List<ModelResource> modelList;
	private List<SoundResource> soundList;
	private List<SoundInstance> soundInstanceList;
	
	// A AssetBundle that current loading
	private AssetBundle assetBundle;

#if UNITY_EDITOR
	// ホットリロードの退避用
	private List<string> savedEffectList = new List<string>();
#endif

	// カメラごとのレンダーパス
	class RenderPath : IDisposable
	{
		public Camera camera;
		public CommandBuffer commandBuffer;
		public CameraEvent cameraEvent;
		public int renderId;
		public RenderTexture renderTexture;

		public RenderPath(Camera camera, CameraEvent cameraEvent, int renderId) {
			this.camera = camera;
			this.renderId = renderId;
			this.cameraEvent = cameraEvent;
		}
		
		public void Init(bool enableDistortion) {
			// プラグイン描画するコマンドバッファを作成
			this.commandBuffer = new CommandBuffer();
			this.commandBuffer.name = "Effekseer Rendering";

#if UNITY_5_6_OR_NEWER
			if (enableDistortion) {
				RenderTextureFormat format = (this.camera.allowHDR) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
#else
			if (enableDistortion && camera.cameraType == CameraType.Game) {
				RenderTextureFormat format = (camera.hdr) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
#endif
				// 歪みテクスチャを作成
				this.renderTexture = new RenderTexture(this.camera.pixelWidth, this.camera.pixelHeight, 0, format);
				this.renderTexture.Create();
				// 歪みテクスチャへのコピーコマンドを追加
				this.commandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, this.renderTexture);
				this.commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
			}

			// プラグイン描画コマンドを追加
			this.commandBuffer.IssuePluginEvent(Plugin.EffekseerGetRenderFunc(), this.renderId);
			// コマンドバッファをカメラに登録
			this.camera.AddCommandBuffer(this.cameraEvent, this.commandBuffer);
		}

		public void Dispose() {
			if (this.commandBuffer != null) {
				if (this.camera != null) {
					this.camera.RemoveCommandBuffer(this.cameraEvent, this.commandBuffer);
				}
				this.commandBuffer.Dispose();
				this.commandBuffer = null;
			}
		}

		public bool IsValid() {
			if (this.renderTexture != null) {
				return this.camera.pixelWidth == this.renderTexture.width &&
					this.camera.pixelHeight == this.renderTexture.height;
			}
			return true;
		}
	};
	private Dictionary<Camera, RenderPath> renderPaths = new Dictionary<Camera, RenderPath>();

	private IntPtr _GetEffect(string name) {
		if (effectList.ContainsKey(name)) {
			return effectList[name];
		}
		
		// 存在しなかったらロード
		return _LoadEffect(name, null);
	}

	private IntPtr _LoadEffect(string name, AssetBundle assetBundle) {
		if (effectList.ContainsKey(name)) {
			return effectList[name];
		}

		byte[] bytes;
		if (assetBundle != null) {
			var asset = assetBundle.LoadAsset<TextAsset>(name);
			bytes = asset.bytes;
		} else {
			// Resourcesから読み込む
			var asset = Resources.Load<TextAsset>(Utility.ResourcePath(name, true));
			if (asset == null) {
				Debug.LogError("[Effekseer] Failed to load effect: " + name);
				return IntPtr.Zero;
			}
			bytes = asset.bytes;
		}

		this.assetBundle = assetBundle;
		GCHandle ghc = GCHandle.Alloc(bytes, GCHandleType.Pinned);
		IntPtr effect = Plugin.EffekseerLoadEffectOnMemory(ghc.AddrOfPinnedObject(), bytes.Length);
		ghc.Free();
		this.assetBundle = null;
		
		effectList.Add(name, effect);

		return effect;
	}
	
	private void _ReleaseEffect(string name) {
		if (effectList.ContainsKey(name) == false) {
			var effect = effectList[name];
			Plugin.EffekseerReleaseEffect(effect);
			effectList.Remove(name);
		}
	}
	
	internal void Init() {
		if (this.initedCount++ > 0) {
			return;
		}

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
		// Init effekseer library
		Plugin.EffekseerInit(effectInstances, maxSquares, reversedDepth, isRightHandledCoordinateSystem);

		this.effectList = new Dictionary<string, IntPtr>();
		this.textureList = new List<TextureResource>();
		this.modelList = new List<ModelResource>();
		this.soundList = new List<SoundResource>();
		this.soundInstanceList = new List<SoundInstance>();
		
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

		if (Application.isPlaying) {
			// サウンドインスタンスを作る
			for (int i = 0; i < soundInstances; i++) {
				GameObject go = new GameObject();
				go.name = "Sound Instance";
				go.transform.parent = transform;
				soundInstanceList.Add(go.AddComponent<SoundInstance>());
			}
		}
		
		Camera.onPreCull += OnPreCullEvent;
	}

	internal void Term() {
		if (--this.initedCount > 0) {
			return;
		}
		
		Camera.onPreCull -= OnPreCullEvent;

		if (this.effectList != null) {
			foreach (var pair in this.effectList) {
				Plugin.EffekseerReleaseEffect(pair.Value);
			}
			this.effectList = null;
		}
		// Effekseerライブラリの終了処理
		Plugin.EffekseerTerm();
		// レンダリングスレッドで解放する環境向けにレンダリング命令を投げる
		GL.IssuePluginEvent(Plugin.EffekseerGetRenderFunc(), 0);
	}
	
	void Awake() {
		this.Init();
	}
	
	void OnDestroy() {
		this.Term();
	}

	void OnEnable() {
#if UNITY_EDITOR
		Resume();
#endif
		CleanUp();
	}

	void OnDisable() {
#if UNITY_EDITOR
		Suspend();
#endif
		CleanUp();
	}
	
#if UNITY_EDITOR
	void Suspend() {
		// Dictionaryは消えるので文字列にして退避
		foreach (var pair in effectList) {
			savedEffectList.Add(pair.Key + "," + pair.Value.ToString());
		}
		effectList.Clear();
	}
	void Resume() {
		// ホットリロード時はリジューム処理
		foreach (var effect in savedEffectList) {
			string[] tokens = effect.Split(',');
			if (tokens.Length == 2) {
				effectList.Add(tokens[0], (IntPtr)ulong.Parse(tokens[1]));
			}
		}
		savedEffectList.Clear();
	}
#endif

	void CleanUp() {
		// レンダーパスの破棄
		foreach (var pair in renderPaths) {
			var camera = pair.Key;
			var path = pair.Value;
			path.Dispose();
		}
		renderPaths.Clear();
	}
	
	void LateUpdate() {
		float deltaFrames = Time.deltaTime * 60.0f;
		int updateCount = Mathf.Max(1, Mathf.RoundToInt(deltaFrames));
		for (int i = 0; i < updateCount; i++) {
			Plugin.EffekseerUpdate(deltaFrames / updateCount);
		}
	}
	
	void OnPreCullEvent(Camera camera) {
#if UNITY_EDITOR
		if (camera.cameraType == CameraType.SceneView) {
			// シーンビューのカメラはチェック
			if (this.drawInSceneView == false) {
				return;
			}
		}
#endif
		RenderPath path;
		
		// カリングマスクをチェック
		if ((Camera.current.cullingMask & (1 << gameObject.layer)) == 0) {
			if (renderPaths.ContainsKey(camera)) {
				// レンダーパスが存在すればコマンドバッファを解除
				path = renderPaths[camera];
				path.Dispose();
				renderPaths.Remove(camera);
			}
			return;
		}

		if (renderPaths.ContainsKey(camera)) {
			// レンダーパスが有れば使う
			path = renderPaths[camera];
		} else {
			// 無ければレンダーパスを作成
			path = new RenderPath(camera, cameraEvent, renderPaths.Count);
			path.Init(this.enableDistortion);
			renderPaths.Add(camera, path);
		}

		if (!path.IsValid()) {
			path.Dispose();
			path.Init(this.enableDistortion);
		}

		// 歪みテクスチャをセット
		if (path.renderTexture) {
			Plugin.EffekseerSetBackGroundTexture(path.renderId, path.renderTexture.GetNativeTexturePtr());
		}

		// ビュー関連の行列を更新
		Plugin.EffekseerSetProjectionMatrix(path.renderId, Utility.Matrix2Array(
			GL.GetGPUProjectionMatrix(camera.projectionMatrix, false)));
		Plugin.EffekseerSetCameraMatrix(path.renderId, Utility.Matrix2Array(
			camera.worldToCameraMatrix));
	}
	
	void OnRenderObject() {
		if (renderPaths.ContainsKey(Camera.current)) {
			RenderPath path = renderPaths[Camera.current];
			Plugin.EffekseerSetRenderSettings(path.renderId, 
				(RenderTexture.active != null));
		}
	}

	[AOT.MonoPInvokeCallbackAttribute(typeof(Plugin.EffekseerTextureLoaderLoad))]
	private static IntPtr TextureLoaderLoad(IntPtr path, out int width, out int height, out int format) {
		var pathstr = Marshal.PtrToStringUni(path);
		var res = new TextureResource();
		if (res.Load(pathstr, EffekseerSystem.Instance.assetBundle)) {
			EffekseerSystem.Instance.textureList.Add(res);
			width = res.texture.width;
			height = res.texture.height;
			switch (res.texture.format) {
			case TextureFormat.DXT1: format = 1; break;
			case TextureFormat.DXT5: format = 2; break;
			default: format = 0; break;
			}
			
			return res.GetNativePtr();
		}
		width = 0;
		height = 0;
		format = 0;
		return IntPtr.Zero;
	}
	[AOT.MonoPInvokeCallbackAttribute(typeof(Plugin.EffekseerTextureLoaderUnload))]
	private static void TextureLoaderUnload(IntPtr path) {
		var pathstr = Marshal.PtrToStringUni(path);
		foreach (var res in EffekseerSystem.Instance.textureList) {
			if (res.path == pathstr) {
				EffekseerSystem.Instance.textureList.Remove(res);
				return;
			}
		}
	}
	[AOT.MonoPInvokeCallbackAttribute(typeof(Plugin.EffekseerModelLoaderLoad))]
	private static int ModelLoaderLoad(IntPtr path, IntPtr buffer, int bufferSize) {
		var pathstr = Marshal.PtrToStringUni(path);
		var res = new ModelResource();
		if (res.Load(pathstr, EffekseerSystem.Instance.assetBundle) && res.Copy(buffer, bufferSize)) {
			EffekseerSystem.Instance.modelList.Add(res);
			return res.modelData.bytes.Length;
		}
		return 0;
	}
	[AOT.MonoPInvokeCallbackAttribute(typeof(Plugin.EffekseerModelLoaderUnload))]
	private static void ModelLoaderUnload(IntPtr path) {
		var pathstr = Marshal.PtrToStringUni(path);
		foreach (var res in EffekseerSystem.Instance.modelList) {
			if (res.path == pathstr) {
				EffekseerSystem.Instance.modelList.Remove(res);
				return;
			}
		}
	}
	[AOT.MonoPInvokeCallbackAttribute(typeof(Plugin.EffekseerSoundLoaderLoad))]
	private static int SoundLoaderLoad(IntPtr path) {
		var pathstr = Marshal.PtrToStringUni(path);
		var res = new SoundResource();
		if (res.Load(pathstr, EffekseerSystem.Instance.assetBundle)) {
			EffekseerSystem.Instance.soundList.Add(res);
			return EffekseerSystem.Instance.soundList.Count;
		}
		return 0;
	}
	[AOT.MonoPInvokeCallbackAttribute(typeof(Plugin.EffekseerSoundLoaderUnload))]
	private static void SoundLoaderUnload(IntPtr path) {
		var pathstr = Marshal.PtrToStringUni(path);
		foreach (var res in EffekseerSystem.Instance.soundList) {
			if (res.path == pathstr) {
				EffekseerSystem.Instance.soundList.Remove(res);
				return;
			}
		}
	}
	
	[AOT.MonoPInvokeCallbackAttribute(typeof(Plugin.EffekseerSoundPlayerPlay))]
	private static void SoundPlayerPlay(IntPtr tag, 
			int data, float volume, float pan, float pitch, 
			bool mode3D, float x, float y, float z, float distance) {
		EffekseerSystem.Instance.PlaySound(tag, data, volume, pan, pitch, mode3D, x, y, z, distance);
	}
	[AOT.MonoPInvokeCallbackAttribute(typeof(Plugin.EffekseerSoundPlayerStopTag))]
	private static void SoundPlayerStopTag(IntPtr tag) {
		EffekseerSystem.Instance.StopSound(tag);
	}
	[AOT.MonoPInvokeCallbackAttribute(typeof(Plugin.EffekseerSoundPlayerPauseTag))]
	private static void SoundPlayerPauseTag(IntPtr tag, bool pause) {
		EffekseerSystem.Instance.PauseSound(tag, pause);
	}
	[AOT.MonoPInvokeCallbackAttribute(typeof(Plugin.EffekseerSoundPlayerCheckPlayingTag))]
	private static bool SoundPlayerCheckPlayingTag(IntPtr tag) {
		return EffekseerSystem.Instance.CheckSound(tag);
	}
	[AOT.MonoPInvokeCallbackAttribute(typeof(Plugin.EffekseerSoundPlayerStopAll))]
	private static void SoundPlayerStopAll() {
		EffekseerSystem.Instance.StopAllSounds();
	}

	private void PlaySound(IntPtr tag, 
		int data, float volume, float pan, float pitch, 
		bool mode3D, float x, float y, float z, float distance)
	{
		if (data <= 0) {
			return;
		}
		SoundResource resource = soundList[data - 1];
		if (resource == null) {
			return;
		}
		foreach (var instance in soundInstanceList) {
			if (!instance.CheckPlaying()) {
				instance.Play(tag.ToString(), resource.audio, volume, pan, pitch, mode3D, x, y, z, distance);
				break;
			}
		}
	}
	private void StopSound(IntPtr tag) {
		foreach (var sound in soundInstanceList) {
			if (sound.AudioTag == tag.ToString()) {
				sound.Stop();
			}
		}
	}
	private void PauseSound(IntPtr tag, bool paused) {
		foreach (var sound in soundInstanceList) {
			if (sound.AudioTag == tag.ToString()) {
				sound.Pause(paused);
			}
		}
	}
	private bool CheckSound(IntPtr tag) {
		bool playing = false;
		foreach (var sound in soundInstanceList) {
			if (sound.AudioTag == tag.ToString()) {
				playing |= sound.CheckPlaying();
			}
		}
		return playing;
	}
	private void StopAllSounds() {
		foreach (var sound in soundInstanceList) {
			sound.Stop();
		}
	}

	#endregion
}

/// <summary xml:lang="ja">
/// A instance handle of played effect
/// </summary>
/// <summary xml:lang="ja">
/// 再生したエフェクトのインスタンスハンドル
/// </summary>
public struct EffekseerHandle
{
	private int m_handle;

	public EffekseerHandle(int handle = -1)
	{
		m_handle = handle;
	}

	internal void UpdateHandle(float deltaFrame)
	{
		Plugin.EffekseerUpdateHandle(m_handle, deltaFrame);
	}
	
	/// <summary xml:lang="en">
	/// Stops the played effect.
	/// All nodes will be destroyed.
	/// </summary>
	/// <summary xml:lang="ja">
	/// エフェクトを停止する
	/// 全てのエフェクトが瞬時に消える
	/// </summary>
	public void Stop()
	{
		Plugin.EffekseerStopEffect(m_handle);
	}
	
	/// <summary xml:lang="en">
	/// Stops the root node of the played effect.
	/// The root node will be destroyed. Then children also will be destroyed by their lifetime.
	/// </summary>
	/// <summary xml:lang="ja">
	/// 再生中のエフェクトのルートノードだけを停止
	/// ルートノードを削除したことで子ノード生成が停止され寿命で徐々に消える
	/// </summary>
	public void StopRoot()
	{
		Plugin.EffekseerStopRoot(m_handle);
	}
	
	/// <summary xml:lang="en">
	/// Sets the effect location
	/// </summary>
	/// <summary xml:lang="ja">
	/// エフェクトの位置を設定
	/// </summary>
	/// <param name="location">位置</param>
	public void SetLocation(Vector3 location)
	{
		Plugin.EffekseerSetLocation(m_handle, location.x, location.y, location.z);
	}
	
	/// <summary xml:lang="en">
	/// Sets the effect rotation
	/// </summary>
	/// <summary xml:lang="ja">
	/// エフェクトの回転を設定
	/// </summary>
	/// <param name="rotation">回転</param>
	public void SetRotation(Quaternion rotation)
	{
		Vector3 axis;
		float angle;
		rotation.ToAngleAxis(out angle, out axis);
		Plugin.EffekseerSetRotation(m_handle, axis.x, axis.y, axis.z, angle * Mathf.Deg2Rad);
	}
	
	/// <summary xml:lang="en">
	/// Sets the effect scale
	/// </summary>
	/// <summary xml:lang="ja">
	/// エフェクトの拡縮を設定
	/// </summary>
	/// <param name="scale">拡縮</param>
	public void SetScale(Vector3 scale)
	{
		Plugin.EffekseerSetScale(m_handle, scale.x, scale.y, scale.z);
	}
	
	/// <summary xml:lang="en">
	/// Sets the effect target location
	/// </summary>
	/// <summary xml:lang="ja">
	/// エフェクトのターゲット位置を設定
	/// </summary>
	/// <param name="targetLocation">ターゲット位置</param>
	public void SetTargetLocation(Vector3 targetLocation)
	{
		Plugin.EffekseerSetTargetLocation(m_handle, targetLocation.x, targetLocation.y, targetLocation.z);
	}

	/// <summary xml:lang="en">
	/// Pausing the effect
	/// <para>true:  It will update on Update()</para>
	/// <para>false: It will not update on Update()</para>
	/// </summary>
	/// <summary xml:lang="ja">
	/// ポーズ設定
	/// <para>true:  停止中。Updateで更新しない</para>
	/// <para>false: 再生中。Updateで更新する</para>
	/// </summary>
	public bool paused
	{
		set {
			Plugin.EffekseerSetPaused(m_handle, value);
		}
		get {
			return Plugin.EffekseerGetPaused(m_handle);
		}
	}
	
	/// <summary xml:lang="en">
	/// Showing the effect
	/// <para>true:  It will be rendering.</para>
	/// <para>false: It will not be rendering.</para>
	/// </summary>
	/// <summary xml:lang="ja">
	/// 表示設定
	/// <para>true:  表示ON。Drawで描画する</para>
	/// <para>false: 表示OFF。Drawで描画しない</para>
	/// </summary>
	public bool shown
	{
		set {
			Plugin.EffekseerSetShown(m_handle, value);
		}
		get {
			return Plugin.EffekseerGetShown(m_handle);
		}
	}
	
	/// <summary xml:lang="ja">
	/// Whether the effect instance is enabled<br/>
	/// <para>true:  enabled</para>
	/// <para>false: disabled</para>
	/// </summary>
	/// <summary xml:lang="ja">
	/// インスタンスハンドルが有効かどうか<br/>
	/// <para>true:  有効</para>
	/// <para>false: 無効</para>
	/// </summary>
	public bool enabled
	{
		get {
			return m_handle >= 0;
		}
	}
	
	/// <summary xml:lang="en">
	/// Existing state
	/// <para>true:  It's existed.</para>
	/// <para>false: It isn't existed or stopped.</para>
	/// </summary>
	/// <summary xml:lang="ja">
	/// エフェクトのインスタンスが存在しているかどうか
	/// <para>true:  存在している</para>
	/// <para>false: 再生終了で破棄。もしくはStopで停止された</para>
	/// </summary>
	public bool exists
	{
		get {
			return Plugin.EffekseerExists(m_handle);
		}
	}
}