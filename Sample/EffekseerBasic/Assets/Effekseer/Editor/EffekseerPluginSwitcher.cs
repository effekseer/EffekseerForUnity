using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

[InitializeOnLoad]
public class EffekseerPluginSwitcher
{
    static readonly string tempFilePath = "Temp/EffekseerPluginSwitcher";

#if UNITY_5_5_OR_NEWER
    static readonly string bcPath = "WebGL/1.36.7-64bit/libEffekseerUnity.bc";
#else
    static readonly string bcPath = "WebGL/libEffekseerUnity.bc";
#endif

    static EffekseerPluginSwitcher()
    {
        Run();
    }

    static public void Run()
    {
        // １回だけ処理に制限します
        if (!File.Exists(tempFilePath))
        {
            var importers = PluginImporter.GetAllImporters().Where(importer => importer.assetPath.Contains("libEffekseerUnity.bc"));

            // 初回起動時にはまだ準備完了してなかったため、
            // libEffekseerUnity.bc がヒットしなかった場合処理しない
            if (importers.Count() > 0)
            {
                foreach (var importer in importers)
                {
                    importer.SetCompatibleWithPlatform(BuildTarget.WebGL, importer.assetPath.Contains(bcPath));
                }
                File.Create(tempFilePath);
            }
        }
    }
}
