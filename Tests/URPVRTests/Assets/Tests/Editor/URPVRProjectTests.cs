using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Mock;

namespace Effekseer.URPVR.EditorTests
{
    public sealed class URPVRProjectTests
    {
        private const string TestScenePath = "Assets/Tests/Basic.unity";

        [Test]
        public void UsesUnity6000_5AndURP()
        {
            StringAssert.StartsWith("6000.5.", Application.unityVersion);
            Assert.That(UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline, Is.TypeOf<UniversalRenderPipelineAsset>());
        }

        [Test]
        public void StandaloneUsesOpenXRMockRuntimeAndSinglePassInstanced()
        {
            var generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            Assert.That(generalSettings, Is.Not.Null);
            Assert.That(generalSettings.Manager, Is.Not.Null);
            Assert.That(generalSettings.Manager.activeLoaders.Any(loader => loader != null && loader.GetType().FullName == "UnityEngine.XR.OpenXR.OpenXRLoader"), Is.True);

            var openXRSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
            Assert.That(openXRSettings, Is.Not.Null);
            Assert.That(openXRSettings.renderMode, Is.EqualTo(OpenXRSettings.RenderMode.SinglePassInstanced));
            Assert.That(openXRSettings.GetFeature<MockRuntime>(), Is.Not.Null);
            Assert.That(openXRSettings.GetFeature<MockRuntime>().enabled, Is.True);
        }

        [Test]
        public void TestSceneContainsTrackedXRCameraAndIsInBuild()
        {
            Assert.That(EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == TestScenePath), Is.True);

            var previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
                var camera = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                    .FirstOrDefault(candidate => candidate.CompareTag("MainCamera"))
                    ?? scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true)).FirstOrDefault();

                Assert.That(camera, Is.Not.Null);
                Assert.That(camera.transform.parent, Is.Not.Null);
                Assert.That(camera.transform.parent.name, Is.EqualTo("XR Origin"));
                Assert.That(camera.GetComponent<TrackedPoseDriver>(), Is.Not.Null);
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
}
