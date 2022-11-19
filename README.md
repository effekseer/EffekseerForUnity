# EffekseerForUnity

This is the Effekseer's runtime plugin for Unity.  
You will be able to show the effects that was created with Effekseer.  

Unity向けEffekseer実行プラグインです。  
Effekseerで作成したエフェクトをUnityで表示することができます。

## UnityPackage

If you are compiling on a Mac, we recommend getting Effekseer via PackageManager.

もし、Macでコンパイルする場合、PackageManager経由でEffekseerを取得することを勧めます。

```
https://github.com/effekseer/EffekseerForUnity_Release.git?path=Assets/Effekseer
```

## Links

- [Official website](http://effekseer.github.io)

- [Effekseer main repository](https://github.com/effekseer/Effekseer)

## Help

- [Help](https://effekseer.github.io/Help_Unity/index.html)

## How to develop

### Clone the source code

```
git clone https://github.com/effekseer/EffekseerForUnity
cd EffekseerForUnity
git submodule update --init
```

### Build binaries

You need to build native binaries.

#### Windows

Visual Studio 2017 or later is required.

Execute ``` Dev/Cpp/windows/Build.bat ```

#### macOS(iOS)

Execute ``` Dev/Cpp/macosx/Build.sh ```

#### Android

NDK is required.

Execute ``` Dev/Cpp/android/Build.bat ```

#### WebGL

emscripten, MinGW and Visual Studio 2017 or late are required.

Execute ``` Dev/Cpp/webgl/Build.bat ```

### Edit native codes

#### Windows

Install cmake and call ``` Dev/Cpp/windows/Build.bat ```

#### macOS

Uses Xcode, to open the following project file.

- Dev/Cpp/macosx/EffekseerUnity.xcodeproj

