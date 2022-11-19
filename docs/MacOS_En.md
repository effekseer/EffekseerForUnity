# Regarding the warning on MacOS

Currently, native notarization is not available when using Unity on MacOS.
Therefore, if you want to use EffekseerForUnity, you need to install or handle it in the following way.

# Use the Unity PackageManager

Install Effekseer with PackageManager. This is the easiest way.

https://docs.unity3d.com/2019.4/Documentation/Manual/upm-ui-giturl.html

```
https://github.com/effekseer/EffekseerForUnity_Release.git?path=Assets/Effekseer
```

# Share data with your team using git.

Even if you don't use Unity PackageManager, as long as you synchronize your data with other PCs via git, you won't have any problems.

# Use xattr -rc.

If you want to share data with multiple PCs by compressing them into a zip or other format without using PackageManager or git, you need to enter the following command before starting Unity.

```
xattr -rc UnityProject
```
