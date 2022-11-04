using System;
using System.IO;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Effekseer.Internal
{
	[Serializable]
	public class EffekseerCurveResource
	{
		[SerializeField]
		public string path;
		[SerializeField]
		public EffekseerCurveAsset asset;

#if UNITY_EDITOR
		public static EffekseerCurveResource LoadAsset(string dirPath, string resPath)
		{
			resPath = Path.ChangeExtension(resPath, ".asset");

			EffekseerCurveAsset asset = AssetDatabase.LoadAssetAtPath<EffekseerCurveAsset>(EffekseerEffectAsset.NormalizeAssetPath(dirPath + "/" + resPath));

			var res = new EffekseerCurveResource();
			res.path = resPath;
			res.asset = asset;
			return res;
		}
		public static bool InspectorField(EffekseerCurveResource res)
		{
			EditorGUILayout.LabelField(res.path);
			var result = EditorGUILayout.ObjectField(res.asset, typeof(EffekseerCurveAsset), false) as EffekseerCurveAsset;
			if (result != res.asset)
			{
				res.asset = result;
				return true;
			}
			return false;
		}
#endif
	};
}

namespace Effekseer
{
	public class EffekseerCurveAsset : ScriptableObject
	{
		[SerializeField]
		public byte[] bytes;

#if UNITY_EDITOR
		public static void CreateAsset(string path)
		{
			byte[] data = File.ReadAllBytes(path);
			if (data == null)
			{
				return;
			}

			string assetPath = Path.ChangeExtension(path, ".asset");

			var asset = AssetDatabase.LoadAssetAtPath<EffekseerCurveAsset>(assetPath);
			if (asset != null)
			{
			}

			bool isNewAsset = false;

			if (asset == null)
			{
				asset = CreateInstance<EffekseerCurveAsset>();
				isNewAsset = true;
			}

			asset.bytes = data;

			if (isNewAsset)
			{
				AssetDatabase.CreateAsset(asset, assetPath);
			}
			else
			{
				EditorUtility.SetDirty(asset);
			}

			AssetDatabase.Refresh();
		}

		private static string ReadString(byte[] data, ref int filepos)
		{
			int length = BitConverter.ToInt32(data, filepos);
			filepos += 4;
			string str = Encoding.Unicode.GetString(data, filepos, (length - 1) * 2);
			filepos += length * 2;
			return str;
		}
#endif
	}
}
