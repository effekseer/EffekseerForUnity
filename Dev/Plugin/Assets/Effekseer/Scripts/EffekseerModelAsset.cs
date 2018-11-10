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
	public class EffekseerModelResource : ScriptableObject
	{
		[SerializeField]
		public string path;
		[SerializeField]
		public EffekseerModelAsset asset;
			
#if UNITY_EDITOR
		public static EffekseerModelResource LoadAsset(string dirPath, string resPath) {
			EffekseerModelAsset asset = AssetDatabase.LoadAssetAtPath<EffekseerModelAsset>(dirPath + "/" + resPath);

			var res = new EffekseerModelResource();
			res.path = resPath;
			res.asset = asset;
			return res;
		}
		public static bool InspectorField(EffekseerModelResource res) {
			EditorGUILayout.LabelField(res.path);
			var result = EditorGUILayout.ObjectField(res.asset, typeof(EffekseerModelAsset), false) as EffekseerModelAsset;
			if (result != res.asset) {
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
			if (data == null) {
				return;
			}
			
			string assetPath = path + ".asset";
			
			var asset = CreateInstance<EffekseerModelAsset>();
			asset.bytes = data;
			
			AssetDatabase.CreateAsset(asset, assetPath);
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
