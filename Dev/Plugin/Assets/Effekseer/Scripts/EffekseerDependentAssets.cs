using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Effekseer
{
	public class EffekseerDependentAssets : ScriptableObject
	{
		public Shader texture2DArrayBlitMaterial = null;

		public Shader texture2DBlitMaterial = null;

		public Shader grabDepthShader = null;

		public Shader fixedShader = null;

		/// <summary>
		/// A shader to avoid a unity bug
		/// </summary>
		public Shader fakeMaterial = null;

		private static EffekseerDependentAssets instance;
		public static EffekseerDependentAssets Instance
		{
			get
			{
				if (instance != null)
				{
					return instance;
				}
				instance = Resources.Load<EffekseerDependentAssets>("EffekseerDependentAssets");
				if (instance == null)
				{
					instance = CreateInstance<EffekseerDependentAssets>();
				}
				return instance;
			}
		}

#if UNITY_EDITOR
		public static EffekseerDependentAssets AssignAssets()
		{
			var asset = Resources.Load<EffekseerDependentAssets>("EffekseerDependentAssets");

			if (asset == null)
			{
				const string baseDir = "Assets";
				const string assetDir = baseDir + "/Effekseer";
				const string materialDir = assetDir + "/Materials";
				const string resourcesDir = assetDir + "/Resources";
				const string assetPath = resourcesDir + "/EffekseerDependentAssets.asset";

				if (!AssetDatabase.IsValidFolder(assetDir))
				{
					AssetDatabase.CreateFolder(baseDir, "Effekseer");
				}

				if (!AssetDatabase.IsValidFolder(resourcesDir))
				{
					AssetDatabase.CreateFolder(assetDir, "Resources");
				}

				asset = CreateInstance<EffekseerDependentAssets>();
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
