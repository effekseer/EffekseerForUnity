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
	public class EffekseerModelResource
	{
		[SerializeField]
		public string path;
		[SerializeField]
		public EffekseerModelAsset asset;

#if UNITY_EDITOR
		public static EffekseerModelResource LoadAsset(string dirPath, string resPath)
		{
			resPath = Path.ChangeExtension(resPath, ".asset");

			EffekseerModelAsset asset = AssetDatabase.LoadAssetAtPath<EffekseerModelAsset>(EffekseerEffectAsset.NormalizeAssetPath(dirPath + "/" + resPath));

			var res = new EffekseerModelResource();
			res.path = resPath;
			res.asset = asset;
			return res;
		}
		public static bool InspectorField(EffekseerModelResource res)
		{
			EditorGUILayout.LabelField(res.path);
			var result = EditorGUILayout.ObjectField(res.asset, typeof(EffekseerModelAsset), false) as EffekseerModelAsset;
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
	public class EffekseerModelAsset : ScriptableObject
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

			var asset = AssetDatabase.LoadAssetAtPath<EffekseerModelAsset>(assetPath);
			if (asset != null)
			{
			}

			bool isNewAsset = false;

			if (asset == null)
			{
				asset = CreateInstance<EffekseerModelAsset>();
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
