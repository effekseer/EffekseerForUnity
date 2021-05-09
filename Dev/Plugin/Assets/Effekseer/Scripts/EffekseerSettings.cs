using System.Collections;
using System.Collections.Generic;
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

		[SerializeField]
		public Shader texture2DArrayBlitMaterial = null;

		[SerializeField]
		public Shader texture2DBlitMaterial = null;

		[SerializeField]
		public Shader grabDepthShader = null;

		[SerializeField]
		public Shader fixedShader = null;

		/// <summary>
		/// A shader to avoid a unity bug
		/// </summary>
		[SerializeField]
		public Shader fakeMaterial = null;


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
				instance = Resources.Load<EffekseerSettings>("EffekseerSettings");
				if (instance == null)
				{
					instance = CreateInstance<EffekseerSettings>();
				}
				return instance;
			}
		}

#if UNITY_EDITOR
#if UNITY_2018_3_OR_NEWER
#else
		[MenuItem("Edit/Project Settings/Effekseer")]
#endif
		public static void EditOrCreateAsset()
		{
			var asset = AssignAssets();
			EditorGUIUtility.PingObject(asset);
			Selection.activeObject = asset;
		}

		public static EffekseerSettings AssignAssets()
		{
			var asset = Resources.Load<EffekseerSettings>("EffekseerSettings");

			if (asset == null)
			{
				const string baseDir = "Assets";
				const string assetDir = baseDir + "/Effekseer";
				const string materialDir = assetDir + "/Materials";
				const string resourcesDir = assetDir + "/Resources";
				const string assetPath = resourcesDir + "/EffekseerSettings.asset";

				if (!AssetDatabase.IsValidFolder(assetDir))
				{
					AssetDatabase.CreateFolder(baseDir, "Effekseer");
				}

				if (!AssetDatabase.IsValidFolder(resourcesDir))
				{
					AssetDatabase.CreateFolder(assetDir, "Resources");
				}

				asset = CreateInstance<EffekseerSettings>();
				asset.texture2DArrayBlitMaterial = AssetDatabase.LoadAssetAtPath<Shader>(materialDir + "/Texture2DArrayBlitShader.shader");
				asset.texture2DBlitMaterial = AssetDatabase.LoadAssetAtPath<Shader>(materialDir + "/Texture2DBlitShader.shader");
				asset.fakeMaterial = AssetDatabase.LoadAssetAtPath<Shader>(materialDir + "/FakeShader.shader");
				asset.grabDepthShader = AssetDatabase.LoadAssetAtPath<Shader>(materialDir + "/GrabDepthShader.shader");
				asset.fixedShader = AssetDatabase.LoadAssetAtPath<Shader>(materialDir + "/EffekseerFixedShader.shader");

				AssetDatabase.CreateAsset(asset, assetPath);
				AssetDatabase.Refresh();
			}
			else
			{
				string assetDir = EffekseerEffectAsset.NormalizeAssetPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(asset)), ".."));
				string materialDir = assetDir + "/Materials";

				bool dirtied = false;
				if (asset.fakeMaterial == null)
				{
					asset.fakeMaterial = AssetDatabase.LoadAssetAtPath<Shader>(materialDir + "/FakeShader.shader");

					if (asset.fakeMaterial != null)
					{
						dirtied = true;
					}
				}

				if (asset.grabDepthShader == null)
				{
					asset.grabDepthShader = AssetDatabase.LoadAssetAtPath<Shader>(materialDir + "/GrabDepthShader.shader");

					if (asset.grabDepthShader != null)
					{
						dirtied = true;
					}
				}

				if (asset.fixedShader == null)
				{
					asset.fixedShader = AssetDatabase.LoadAssetAtPath<Shader>(materialDir + "/EffekseerFixedShader.shader");

					if (asset.fixedShader != null)
					{
						dirtied = true;
					}
				}

				if (dirtied)
				{
					EditorUtility.SetDirty(asset);
					AssetDatabase.Refresh();
				}
			}

			return asset;
		}
#endif
	}
}
