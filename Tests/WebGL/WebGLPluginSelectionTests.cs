// Copy into a disposable project's Assets/Editor, then execute
// WebGLPluginSelectionTests.Run with Unity -batchmode -quit.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class WebGLPluginSelectionTests
{
    public static void Run()
    {
#if UNITY_6000_5_OR_NEWER
        const string expected = "4.0.19-64bit/libEffekseerUnity.a";
#elif UNITY_2023_2_OR_NEWER
        const string expected = "3.1.38-64bit/libEffekseerUnity.bc";
#else
        const string expected = "3.1.8-64bit/libEffekseerUnity.bc";
#endif
#if UNITY_6000_5_OR_NEWER
        // Simulate an upgrade that left the old .bc archive behind.
        var archive = PluginImporter.GetAllImporters().Single(p => p.assetPath.EndsWith(expected)).assetPath;
        var legacy = Path.ChangeExtension(archive, ".bc");
        if (!File.Exists(legacy) && !AssetDatabase.CopyAsset(archive, legacy))
            throw new Exception("Could not create legacy package fixture");
#endif
        var plugins = PluginImporter.GetAllImporters();
        var web = plugins.Where(p => p.assetPath.Contains("/Effekseer/Plugins/WebGL/")).ToArray();
        var unrelated = plugins.Where(p => !p.assetPath.Contains("/Effekseer/Plugins/WebGL/"))
            .ToDictionary(p => p.assetPath, p => p.GetCompatibleWithPlatform(BuildTarget.WebGL));
        // Simulate stale package settings: every WebGL library enabled.
        foreach (var p in web)
        {
            p.SetCompatibleWithPlatform(BuildTarget.WebGL, true);
            p.SaveAndReimport();
        }
        EffekseerPluginSwitcher.ValidateForBuild();
        var active = PluginImporter.GetAllImporters().Where(p =>
            p.assetPath.Contains("/Effekseer/Plugins/WebGL/") &&
            p.GetCompatibleWithPlatform(BuildTarget.WebGL)).ToArray();
        if (active.Length != 1 || !active[0].assetPath.EndsWith(expected))
            throw new Exception("Wrong WebGL library selection");
        var selected = active[0].assetPath;
        var metadata = File.ReadAllText(selected + ".meta");
        EffekseerPluginSwitcher.Run();
        if (metadata != File.ReadAllText(selected + ".meta"))
            throw new Exception("Repeated selection modified metadata");
        foreach (var p in PluginImporter.GetAllImporters())
            if (unrelated.ContainsKey(p.assetPath) &&
                unrelated[p.assetPath] != p.GetCompatibleWithPlatform(BuildTarget.WebGL))
                throw new Exception("Modified non-WebGL plugin: " + p.assetPath);

        // Missing required archive must stop a build rather than select an older SDK.
        var hidden = selected + ".disabled";
        if (File.Exists(hidden)) throw new Exception("Test destination already exists");
        var moveError = AssetDatabase.MoveAsset(selected, hidden);
        if (!string.IsNullOrEmpty(moveError)) throw new Exception(moveError);
        bool rejected = false;
        try
        {
            try { EffekseerPluginSwitcher.ValidateForBuild(); }
            catch (BuildFailedException) { rejected = true; }
        }
        finally
        {
            moveError = AssetDatabase.MoveAsset(hidden, selected);
            if (!string.IsNullOrEmpty(moveError)) throw new Exception(moveError);
            AssetDatabase.Refresh();
            EffekseerPluginSwitcher.ValidateForBuild();
        }
        if (!rejected) throw new Exception("Missing required archive was accepted");
        Debug.Log("WEBGL_SELECTION_TEST: PASS " + Application.unityVersion + " " + expected);
    }
}
