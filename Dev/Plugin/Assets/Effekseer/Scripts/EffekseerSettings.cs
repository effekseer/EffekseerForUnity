using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Effekseer
{
	public enum EffekseerRendererType
	{
		Native = 0,
		Unity = 1,
	}

	public class EffekseerSettings : ScriptableObject
	{
		/// <summary xml:lang="en">
		/// Whether it does draw in scene view for editor.
		/// </summary>
		/// <summary xml:lang="ja">
		/// エディタのシーンビューに描画するかどうか
		/// </summary>
		[SerializeField]
		public bool drawInSceneView = true;

		/// <summary xml:lang="en">
		/// Whether effects are rendered as a post processing in PostProcessingStack
		/// </summary>
		/// <summary xml:lang="ja">
		/// ポストプロセッシングスタックのポストプロセスとしてエフェクトを描画するかどうか?
		/// </summary>
		[SerializeField]
		public bool renderAsPostProcessingStack = false;

		/// <summary xml:lang="en">
		/// Whether to maintain gamma color in linear space.
		/// </summary>
		/// <summary xml:lang="ja">
		/// リニアスペースでガンマカラーを維持するかどうか
		/// </summary>
		public bool MaintainGammaColorInLinearSpace = true;

		/// <summary>
		/// 
		/// </summary>
		[SerializeField]
		public EffekseerRendererType RendererType = EffekseerRendererType.Native;

		/// <summary xml:lang="en">
		/// Maximum number of effect instances.
		/// </summary>
		/// <summary xml:lang="ja">
		/// エフェクトインスタンスの最大数
		/// </summary>
		[SerializeField]
		public int effectInstances = 8192;

		/// <summary xml:lang="en">
		/// Maximum number of quads that can be drawn.
		/// </summary>
		/// <summary xml:lang="ja">
		/// 描画できる四角形の最大数
		/// </summary>
		[SerializeField]
		public int maxSquares = 8192;

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
		[SerializeField]
		public bool isRightEffekseerHandledCoordinateSystem = false;

		/// <summary xml:lang="en">
		/// The number of thread to update effects
		/// </summary>
		/// <summary xml:lang="ja">
		/// エフェクトの更新に使用するスレッド数
		/// </summary>
		[SerializeField]
		public int threadCount = 2;

		/// <summary xml:lang="en">
		/// Maximum number of sound instances.
		/// </summary>
		/// <summary xml:lang="ja">
		/// サウンドインスタンスの最大数
		/// </summary>
		[SerializeField]
		public int soundInstances = 16;

		/// <summary xml:lang="en">
		/// Enables distortion effect.
		/// When It has set false, rendering will be faster.
		/// </summary>
		/// <summary xml:lang="ja">
		/// 歪みエフェクトを有効にします。
		/// falseにすると描画処理が軽くなります。
		/// </summary>
		[SerializeField]
		public bool enableDistortion = true;

		/// <summary xml:lang="en">
		/// Enables distortion effect on mobile environment (iOS,Android,WebGL,Switch).
		/// When It has set false, rendering will be faster.
		/// </summary>
		/// <summary xml:lang="ja">
		/// モバイル環境(iOS,Android,WebGL,Switch)で歪みエフェクトを有効にします。
		/// falseにすると描画処理が軽くなります。
		/// </summary>
		[SerializeField]
		public bool enableDistortionMobile = false;


		/// <summary xml:lang="en">
		/// Enables depth.
		/// When It has set false, rendering will be faster.
		/// </summary>
		/// <summary xml:lang="ja">
		/// 深度を有効にします。
		/// falseにすると描画処理が軽くなります。
		/// </summary>
		[SerializeField]
		public bool enableDepth = true;

		/// <summary xml:lang="en">
		/// Enables depth on mobile environment (iOS,Android,WebGL,Switch).
		/// When It has set false, rendering will be faster.
		/// </summary>
		/// <summary xml:lang="ja">
		/// モバイル環境(iOS,Android,WebGL,Switch)で深度を有効にします。
		/// falseにすると描画処理が軽くなります。
		/// </summary>
		[SerializeField]
		public bool enableDepthMobile = false;

		/*
		/// <summary xml:lang="en">
		/// The scale of buffer for distortion.
		/// If this values is small, run fastly
		/// </summary>
		/// <summary xml:lang="ja">
		/// 歪みのためのバッファの大きさです。
		/// この値が小さいと、速度が速くなります。
		/// </summary>
		[SerializeField]
		public float distortionBufferScale = 1.0f;
		*/

		#region Network
		/// <summary xml:lang="en">
		/// A network port to edit effects from remote
		/// </summary>
		/// <summary xml:lang="ja">
		/// リモートでエフェクトを編集するためのネットワークのポート
		/// </summary>
		[SerializeField]
		public uint NetworkPort = 60000;

		/// <summary xml:lang="en">
		/// Does run a server automatically to edit effects from remote?
		/// </summary>
		/// <summary xml:lang="ja">
		/// リモートでエフェクトを編集するためにサーバーを自動的に起動するか?
		/// </summary>
		[SerializeField]
		public bool DoStartNetworkAutomatically = false;
		#endregion

		private static EffekseerSettings instance;
		public static EffekseerSettings Instance
		{
			get
			{
				if (instance != null)
				{
					return instance;
				}

				instance = LoadAsset();

				if (instance == null)
				{
					Debug.LogWarning("Effekseer Settings is not found. Please Create Effekseer Settings with Create->Effekseer->Effekseer Settings.");
					instance = CreateInstance<EffekseerSettings>();
				}

				return instance;
			}
		}

		static EffekseerSettings LoadAsset()
		{
#if UNITY_EDITOR
			var asset = PlayerSettings.GetPreloadedAssets().OfType<EffekseerSettings>().FirstOrDefault();

			if (asset != null)
			{
				return asset;
			}
#endif
			return Resources.Load<EffekseerSettings>("EffekseerSettings");
		}

		void OnEnable()
		{
			instance = this;
		}

#if UNITY_EDITOR
#if UNITY_2018_3_OR_NEWER
#else
		[MenuItem("Edit/Project Settings/Effekseer")]
#endif
		public static void EditOrCreateAsset()
		{
			var asset = LoadAsset();

			if (asset == null)
			{
				asset = CreateAssetInternal();
			}

			instance = asset;

			if (asset == null)
			{
				return;
			}

			EditorGUIUtility.PingObject(asset);
			Selection.activeObject = asset;
		}

		[UnityEditor.MenuItem("Assets/Create/Effekseer/Effekseer Settings")]
		public static void CreateAsset()
		{
			CreateAssetInternal();
		}

		static EffekseerSettings CreateAssetInternal()
		{
			var path = EditorUtility.SaveFilePanelInProject(
				"Save EffekseerSettings",
				"EffekseerSettings",
				"asset",
				string.Empty);

			if (string.IsNullOrEmpty(path))
			{
				return null;
			}

			var asset = CreateInstance<EffekseerSettings>();
			AssetDatabase.CreateAsset(asset, path);

			var preloadedAssets = PlayerSettings.GetPreloadedAssets().ToList();
			preloadedAssets.RemoveAll(x => x is EffekseerSettings);
			preloadedAssets.Add(asset);
			PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());

			return asset;
		}
#endif
	}
}
