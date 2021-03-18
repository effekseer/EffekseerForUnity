using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace Effekseer.Editor
{
	using Internal;

	[CustomEditor(typeof(EffekseerModelAsset))]
	public class EffekseerModelAssetEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			var asset = target as EffekseerModelAsset;

			EditorGUILayout.LabelField("Data Size", asset.bytes.Length.ToString() + " bytes");
		}
	}

	[CustomEditor(typeof(EffekseerMaterialAsset))]
	public class EffekseerMaterialAssetEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			var asset = target as EffekseerMaterialAsset;

			if (asset.materialBuffers == null)
			{
				EditorGUILayout.LabelField("MaterialBuffer : None");
			}
			else
			{
				EditorGUILayout.LabelField("MaterialBuffer: ", asset.materialBuffers.Length.ToString() + " bytes");
			}

			if (asset.cachedMaterialBuffers == null)
			{
				EditorGUILayout.LabelField("CachedMaterialBuffer : None");
			}
			else
			{
				EditorGUILayout.LabelField("CachedMaterialBuffer: ", asset.cachedMaterialBuffers.Length.ToString() + " bytes");
			}

			EditorGUILayout.ObjectField(asset.shader, typeof(Shader), false);
		}
	}

	[CustomEditor(typeof(EffekseerEffectAsset))]
	public class EffekseerEffectAssetEditor : UnityEditor.Editor
	{
		bool textureVisible = true;
		bool soundVisible = true;
		bool modelVisible = true;
		bool materialVisible = true;
		bool curveVisible = true;
		public override void OnInspectorGUI()
		{
			var asset = target as EffekseerEffectAsset;

			if (asset == null)
			{
				return;
			}

			EditorGUILayout.LabelField("Data Size", asset.efkBytes.Length.ToString() + " bytes");

			var scale = EditorGUILayout.FloatField("Scale", asset.Scale);
			scale = Math.Max(0, scale);
			if (asset.Scale != scale)
			{
				asset.Scale = scale;
				EditorUtility.SetDirty(asset);
			}

			textureVisible = EditorGUILayout.Foldout(textureVisible, "Texture Resources: " + asset.textureResources.Length);
			if (textureVisible)
			{
				EditorGUI.indentLevel++;
				foreach (var res in asset.textureResources)
				{
					if (EffekseerTextureResource.InspectorField(res))
					{
						EditorUtility.SetDirty(asset);
					}
				}
				EditorGUI.indentLevel--;
			}

			soundVisible = EditorGUILayout.Foldout(soundVisible, "Sound Resources: " + asset.soundResources.Length);
			if (soundVisible)
			{
				EditorGUI.indentLevel++;
				foreach (var res in asset.soundResources)
				{
					if (EffekseerSoundResource.InspectorField(res))
					{
						EditorUtility.SetDirty(asset);
					}
				}
				EditorGUI.indentLevel--;
			}

			modelVisible = EditorGUILayout.Foldout(modelVisible, "Model Resources: " + asset.modelResources.Length);
			if (modelVisible)
			{
				EditorGUI.indentLevel++;
				foreach (var res in asset.modelResources)
				{
					if (EffekseerModelResource.InspectorField(res))
					{
						EditorUtility.SetDirty(asset);
					}
				}
				EditorGUI.indentLevel--;
			}

			if (asset.materialResources != null)
			{
				materialVisible = EditorGUILayout.Foldout(materialVisible, "Material Resources: " + asset.materialResources.Length);
				if (materialVisible)
				{
					EditorGUI.indentLevel++;
					foreach (var res in asset.materialResources)
					{
						if (EffekseerMaterialResource.InspectorField(res))
						{
							EditorUtility.SetDirty(asset);
						}
					}
					EditorGUI.indentLevel--;
				}
			}

			if (asset.curveResources != null)
			{
				curveVisible = EditorGUILayout.Foldout(curveVisible, "Curve Resources: " + asset.curveResources.Length);
				if (curveVisible)
				{
					EditorGUI.indentLevel++;
					foreach (var res in asset.curveResources)
					{
						if (EffekseerCurveResource.InspectorField(res))
						{
							EditorUtility.SetDirty(asset);
						}
					}
					EditorGUI.indentLevel--;
				}
			}
		}
	}
}

#endif