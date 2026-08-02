# URP VR Tests (Unity 6000.5)

This project verifies Effekseer rendering with URP 17.5, OpenXR, and Input System
1.19 on Unity 6000.5.
It follows `docs/Development/Verification.md`: OpenXR is enabled for Standalone,
Mock Runtime is enabled, the camera is placed under an XR origin with a tracked
pose driver, and stereo rendering uses Single Pass Instanced.

## Open and configure

1. Open this directory with Unity 6000.5.0f1.
2. Wait for package restore and script compilation. The editor setup script runs
   automatically and configures OpenXR and `Assets/Tests/Basic.unity`.
3. If setup was interrupted, run **Effekseer Tests > Configure URP VR Project**.
4. Open `Assets/Tests/Basic.unity` and enter Play Mode.
5. In the Game view, select **Both Eyes** and verify Effekseer effects appear in
   both views.

The Mock Runtime takes over the system OpenXR runtime while it is enabled. Disable
it in **Project Settings > XR Plug-in Management > OpenXR > Features** before
testing with a physical headset.

## Automated validation

Run the EditMode tests from **Window > General > Test Runner**, or in batch mode:

```powershell
Unity.exe -batchmode -projectPath Tests/URPVRTests -runTests -testPlatform EditMode -testResults TestResults.xml
```

The tests check the Unity version, URP assignment, OpenXR loader, Mock Runtime,
Single Pass Instanced mode, build scene, and XR camera hierarchy.
