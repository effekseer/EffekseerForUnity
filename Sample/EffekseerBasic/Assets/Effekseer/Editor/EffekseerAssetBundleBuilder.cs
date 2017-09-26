using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class EffekseerAssetBundleBuilder
{
    // Bundles直下のディレクトリをAssetBundleとしてビルドする
    [MenuItem("AssetBundles/Build by Effekseer")]
    static void BuildAssetBundles()
    {
        string bundleRoot = Application.dataPath + "/" + "Bundles";
        string[] bundlePaths = Directory.GetDirectories(bundleRoot, "*", SearchOption.TopDirectoryOnly);

        var buildMap = new List<AssetBundleBuild>();
        foreach (var path in bundlePaths)
        {
            var build = new AssetBundleBuild();
            build.assetBundleName = Path.GetFileName(path);
            build.assetNames = (from asset in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                                where Path.GetExtension(asset) != ".meta"
                                select asset.Replace("\\", "/").Replace(Application.dataPath, "Assets")
                                ).ToArray();
            buildMap.Add(build);
        }
        //AssetBundleを出力
        BuildPipeline.BuildAssetBundles(Application.streamingAssetsPath, buildMap.ToArray(), BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
    }
}
