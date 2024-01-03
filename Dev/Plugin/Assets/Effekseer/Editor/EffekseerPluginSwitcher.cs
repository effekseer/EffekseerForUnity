#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

[InitializeOnLoad]
public class EffekseerPluginSwitcher
{
	static readonly string tempFilePath = "Temp/EffekseerPluginSwitcher";

#if UNITY_2023_2_OR_NEWER
	static readonly string bcPath = "WebGL/3.1.38-64bit/libEffekseerUnity.bc";
#elif UNITY_2022_2_OR_NEWER
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
		if (!System.IO.File.Exists(tempFilePath))
		{
			var importers = PluginImporter.GetAllImporters().Where(importer => importer.assetPath.Contains("libEffekseerUnity.bc"));

			if (importers.Count() > 0)
			{
				var processed = new List<string>();
				foreach (var importer in importers)
				{
					var enabled = importer.assetPath.Contains(bcPath);
					importer.SetCompatibleWithPlatform(BuildTarget.WebGL, enabled);
					processed.Add(importer.assetPath + " : " + enabled.ToString());
				}
				System.IO.File.WriteAllLines(tempFilePath, processed.ToArray());
			}
		}
	}
}

#endif
