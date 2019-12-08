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
	public class EffekseerMaterialResource
	{
		[SerializeField]
		public string path;
		[SerializeField]
		public EffekseerMaterialAsset asset;
			
#if UNITY_EDITOR
		public static EffekseerMaterialResource LoadAsset(string dirPath, string resPath) {
			resPath = Path.ChangeExtension(resPath, ".asset");

			EffekseerMaterialAsset asset = AssetDatabase.LoadAssetAtPath<EffekseerMaterialAsset>(EffekseerEffectAsset.NormalizeAssetPath(dirPath + "/" + resPath));

			var res = new EffekseerMaterialResource();
			res.path = resPath;
			res.asset = asset;
			return res;
		}
		public static bool InspectorField(EffekseerMaterialResource res) {
			EditorGUILayout.LabelField(res.path);
			var result = EditorGUILayout.ObjectField(res.asset, typeof(EffekseerMaterialAsset), false) as EffekseerMaterialAsset;
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
	public class EffekseerMaterialAsset : ScriptableObject
	{
		[SerializeField]
		public byte[] materialBuffers;

		[SerializeField]
		public byte[] cachedMaterialBuffers;

#if UNITY_EDITOR
		public static void CreateAsset(string path, bool isCachedFile)
		{
			byte[] data = File.ReadAllBytes(path);
			if (data == null) {
				return;
			}

			string assetPath = Path.ChangeExtension(path, ".asset");


			var asset = AssetDatabase.LoadAssetAtPath<EffekseerMaterialAsset>(assetPath);
			if (asset != null)
			{
			}

			string assetDir = assetPath.Substring(0, assetPath.LastIndexOf('/'));

			bool isNewAsset = false;
			if (asset == null)
			{
				isNewAsset = true;
				asset = CreateInstance<EffekseerMaterialAsset>();
			}

			if(isCachedFile)
			{
				asset.cachedMaterialBuffers = data;
			}
			else
			{
				asset.materialBuffers = data;
			}

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
