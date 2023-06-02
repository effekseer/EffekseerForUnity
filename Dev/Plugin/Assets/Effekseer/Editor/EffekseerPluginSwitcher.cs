#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

[InitializeOnLoad]
public class EffekseerPluginSwitcher
{
	static readonly string tempFilePath = "Temp/EffekseerPluginSwitcher";

#if UNITY_2022_2_OR_NEWER
	static readonly string bcPath = "WebGL/3.1.8-64bit/libEffekseerUnity.bc";
#elif UNITY_2021_2_OR_NEWER
	static readonly string bcPath = "WebGL/2.0.19-64bit/libEffekseerUnity.bc";
#else
	static readonly string bcPath = "WebGL/libEffekseerUnity.bc";
#endif

	static EffekseerPluginSwitcher()
	{
		Run();
	}

	static public void Run()
	{
		// called only once
		if (!File.Exists(tempFilePath))
		{
			var importers = PluginImporter.GetAllImporters().Where(importer => importer.assetPath.Contains("libEffekseerUnity.bc"));

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

#endif
