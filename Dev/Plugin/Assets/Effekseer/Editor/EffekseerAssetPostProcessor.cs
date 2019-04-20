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
				if (Path.GetExtension(assetPath) == ".efk")
				{
					EffekseerEffectAsset.CreateAsset(assetPath);
				}
				if (Path.GetExtension(assetPath) == ".efkmodel")
				{
					EffekseerModelAsset.CreateAsset(assetPath);
				}
                if (Path.GetExtension(assetPath) == ".efkproj")
                {
                    EffekseerTool.Core.LoadFrom(assetPath);
                    var exporter = new EffekseerTool.Binary.Exporter();
                    var data = exporter.Export(1);
                    EffekseerEffectAsset.CreateAsset(assetPath, data);
                }
            }
		}
	}
}
#endif