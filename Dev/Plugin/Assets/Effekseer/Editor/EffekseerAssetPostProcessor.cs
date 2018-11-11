using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace Effekseer.Editor
{
	public class EffekseerAssetPostProcessor : AssetPostprocessor
	{
		static void OnPostprocessAllAssets(
			string[] importedAssets,
			string[] deletedAssets,
			string[] movedAssets,
			string[] movedFromPaths)
		{
			foreach(string assetPath in importedAssets) {
				if (Path.GetExtension(assetPath) == ".efk") {
					EffekseerEffectAsset.CreateAsset(assetPath);
				}
				/*if (Path.GetExtension(assetPath) == ".efkmodel") {
					File.Move(assetPath, assetPath + ".bytes");
				}*/
			}
		}
	}
}
#endif