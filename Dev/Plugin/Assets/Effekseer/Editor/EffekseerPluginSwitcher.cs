#if UNITY_EDITOR

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

[InitializeOnLoad]
public class EffekseerPluginSwitcher
{
#if UNITY_6000_5_OR_NEWER
	const string libraryPath = "4.0.19-64bit/libEffekseerUnity.a";
#elif UNITY_2023_2_OR_NEWER
	const string libraryPath = "3.1.38-64bit/libEffekseerUnity.bc";
#else
	const string libraryPath = "3.1.8-64bit/libEffekseerUnity.bc";
#endif

	static EffekseerPluginSwitcher()
	{
		// Wait until the initial asset import has finished. No session cache:
		// importing a newer package must also refresh the selection.
		EditorApplication.delayCall += Run;
	}

	static PluginImporter[] GetLibraries()
	{
		return PluginImporter.GetAllImporters().Where(importer =>
			importer.assetPath.Contains("/Effekseer/Plugins/WebGL/") &&
			(Path.GetFileName(importer.assetPath) == "libEffekseerUnity.bc" ||
			 Path.GetFileName(importer.assetPath) == "libEffekseerUnity.a")).ToArray();
	}

	static bool IsSelected(PluginImporter importer)
	{
		return importer.assetPath.EndsWith("/WebGL/" + libraryPath, StringComparison.Ordinal);
	}

	public static void Run()
	{
		foreach (var importer in GetLibraries())
		{
			var enabled = IsSelected(importer);
			if (importer.GetCompatibleWithPlatform(BuildTarget.WebGL) != enabled)
			{
				importer.SetCompatibleWithPlatform(BuildTarget.WebGL, enabled);
				importer.SaveAndReimport();
			}
		}
	}

	public static void ValidateForBuild()
	{
		var libraries = GetLibraries();
		if (libraries.Count(IsSelected) != 1)
		{
			throw new BuildFailedException("Effekseer WebGL requires exactly one " + libraryPath +
				". Reimport the complete Effekseer package and remove duplicate installations.");
		}

		Run();
		var enabled = GetLibraries().Where(importer =>
			importer.GetCompatibleWithPlatform(BuildTarget.WebGL)).ToArray();
		if (enabled.Length != 1 || !IsSelected(enabled[0]))
		{
			throw new BuildFailedException("Effekseer WebGL library selection is invalid.");
		}
	}
}

class EffekseerWebGLBuildValidator : IPreprocessBuildWithReport
{
	public int callbackOrder { get { return 0; } }

	public void OnPreprocessBuild(BuildReport report)
	{
		if (report.summary.platform == BuildTarget.WebGL)
		{
			EffekseerPluginSwitcher.ValidateForBuild();
		}
	}
}

#endif
