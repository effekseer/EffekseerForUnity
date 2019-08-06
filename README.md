# EffekseerForUnity

This is the Effekseer's runtime plugin for Unity.  
You will be able to show the effects that was created with Effekseer.  

Unity向けEffekseer実行プラグインです。  
Effekseerで作成したエフェクトをUnityで表示することができます。

- [Official website](http://effekseer.github.io)

- [Effekseer main repository](https://github.com/effekseer/Effekseer)

- [EffekseerForUnity(Legacy)](https://github.com/effekseer/EffekseerForUnity/tree/legacy)

## Help

- [Help](https://effekseer.github.io/Help_Unity/index.html)

## How to develop

### Clone the source code

Needs to clone main repository in the same place of this repository, to develop this plugin.

このプラグインを開発するには、このリポジトリと同じ場所に本体リポジトリもクローンする必要があります。

```
git clone https://github.com/effekseer/Effekseer
git clone https://github.com/effekseer/EffekseerForUnity
```

### Build binaries

You need to build native binaries.

#### Windows

Visual Studio 2015 or later is required.

Execute ``` Dev/Cpp/windows/Build.bat ```

#### macOS(iOS)

Execute ``` Dev/Cpp/macosx/Build.sh ```

#### Android

NDK is required.

Execute ``` Dev/Cpp/android/Build.bat ```

#### WebGL

emscripten, MinGW and Visual Studio 2015 or late are required.

Execute ``` Dev/Cpp/webgl/Build.bat ```

### Edit native codes

#### Windows

Uses Visual Studio 2015 or later, to open and build the following solution file.

- Dev/Cpp/windows/EffekseerUnity.sln

#### macOS

Uses Xcode, to open the following project file.

- Dev/Cpp/macosx/EffekseerUnity.xcodeproj

