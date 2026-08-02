using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Mock;

internal static class URPVRProjectSetup
{
    private const string TestScenePath = "Assets/Tests/Basic.unity";
    private const string OpenXRLoaderTypeName = "UnityEngine.XR.OpenXR.OpenXRLoader";

    [InitializeOnLoadMethod]
    private static void ScheduleAutomaticSetup()
    {
        EditorApplication.delayCall -= ConfigureWhenEditorIsReady;
        EditorApplication.delayCall += ConfigureWhenEditorIsReady;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall -= ConfigureWhenEditorIsReady;
            EditorApplication.delayCall += ConfigureWhenEditorIsReady;
        }
    }

    private static void ConfigureWhenEditorIsReady()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Configure();
    }

    [MenuItem("Effekseer Tests/Configure URP VR Project")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("URP VR project setup is only available in Edit Mode.");
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall -= ConfigureWhenEditorIsReady;
            EditorApplication.delayCall += ConfigureWhenEditorIsReady;
            return;
        }

        ConfigureOpenXR();
        ConfigureBuildScene();
        ConfigureXRCamera();
        AssetDatabase.SaveAssets();
        Debug.Log("URP VR test project configuration is complete.");
    }

    public static void ConfigureFromCommandLine()
    {
        Configure();
    }

    private static void ConfigureOpenXR()
    {
        const BuildTargetGroup targetGroup = BuildTargetGroup.Standalone;
        var perBuildTarget = GetOrCreateXRSettingsStore();

        if (!perBuildTarget.HasSettingsForBuildTarget(targetGroup))
        {
            perBuildTarget.CreateDefaultSettingsForBuildTarget(targetGroup);
        }

        if (!perBuildTarget.HasManagerSettingsForBuildTarget(targetGroup))
        {
            perBuildTarget.CreateDefaultManagerSettingsForBuildTarget(targetGroup);
        }

        var generalSettings = perBuildTarget.SettingsForBuildTarget(targetGroup);
        if (generalSettings == null || generalSettings.Manager == null)
        {
            throw new InvalidOperationException("Failed to create Standalone XR management settings.");
        }

        var openXRLoaderAssigned = generalSettings.Manager.activeLoaders.Any(
            loader => loader != null && loader.GetType().FullName == OpenXRLoaderTypeName);
        if (!openXRLoaderAssigned &&
            !XRPackageMetadataStore.AssignLoader(generalSettings.Manager, OpenXRLoaderTypeName, targetGroup))
        {
            throw new InvalidOperationException("Failed to assign the OpenXR loader for Standalone.");
        }

        FeatureHelpers.RefreshFeatures(targetGroup);
        var openXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(targetGroup);
        if (openXRSettings == null)
        {
            throw new InvalidOperationException("Failed to create Standalone OpenXR settings.");
        }

        openXRSettings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
        var mockRuntime = openXRSettings.GetFeature<MockRuntime>();
        if (mockRuntime == null)
        {
            throw new InvalidOperationException("The OpenXR Mock Runtime feature is unavailable.");
        }

        mockRuntime.enabled = true;
        EditorUtility.SetDirty(mockRuntime);
        EditorUtility.SetDirty(openXRSettings);
        EditorUtility.SetDirty(generalSettings);
        EditorUtility.SetDirty(generalSettings.Manager);
    }

    private static XRGeneralSettingsPerBuildTarget GetOrCreateXRSettingsStore()
    {
        if (EditorBuildSettings.TryGetConfigObject(
                XRGeneralSettings.k_SettingsKey,
                out XRGeneralSettingsPerBuildTarget perBuildTarget) &&
            perBuildTarget != null)
        {
            return perBuildTarget;
        }

        var existingGuid = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget").FirstOrDefault();
        if (!string.IsNullOrEmpty(existingGuid))
        {
            perBuildTarget = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(
                AssetDatabase.GUIDToAssetPath(existingGuid));
        }

        if (perBuildTarget == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/XR"))
            {
                AssetDatabase.CreateFolder("Assets", "XR");
            }

            if (!AssetDatabase.IsValidFolder("Assets/XR/Settings"))
            {
                AssetDatabase.CreateFolder("Assets/XR", "Settings");
            }

            perBuildTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(
                perBuildTarget,
                "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset");
            AssetDatabase.SaveAssets();
        }

        EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perBuildTarget, true);
        return perBuildTarget;
    }

    private static void ConfigureBuildScene()
    {
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(TestScenePath, true) };
    }

    private static void ConfigureXRCamera()
    {
        if (EditorSceneManager.GetSceneManagerSetup().Any(setup => setup.isLoaded && SceneManager.GetSceneByPath(setup.path).isDirty))
        {
            Debug.LogWarning("Skipped XR camera setup because an open scene has unsaved changes.");
            return;
        }

        var previousSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
            var camera = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .FirstOrDefault(candidate => candidate.CompareTag("MainCamera"))
                ?? scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true)).FirstOrDefault();

            if (camera == null)
            {
                throw new InvalidOperationException($"No camera was found in {TestScenePath}.");
            }

            var changed = false;
            var xrOrigin = camera.transform.parent;
            if (xrOrigin == null || xrOrigin.name != "XR Origin")
            {
                var originObject = new GameObject("XR Origin");
                xrOrigin = originObject.transform;
                xrOrigin.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);
                camera.transform.SetParent(xrOrigin, true);
                camera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                changed = true;
            }

            var trackedPoseDriver = camera.GetComponent<TrackedPoseDriver>();
            if (trackedPoseDriver == null)
            {
                trackedPoseDriver = camera.gameObject.AddComponent<TrackedPoseDriver>();
                changed = true;
            }

            if (trackedPoseDriver.trackingType != TrackedPoseDriver.TrackingType.RotationAndPosition)
            {
                trackedPoseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
                changed = true;
            }

            if (trackedPoseDriver.updateType != TrackedPoseDriver.UpdateType.UpdateAndBeforeRender)
            {
                trackedPoseDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
                changed = true;
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
        finally
        {
            if (!Application.isBatchMode)
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }
    }
}
