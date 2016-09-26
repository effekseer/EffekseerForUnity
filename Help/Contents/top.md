# Effekseer UnityPlugin Manual

![](img/plugin_logo.png)

## 概要 {#overview}
ゲームエンジンUnityとの連携について説明します。  
なお、Unity Technologies社とこのツールは特に提携しているというわけではないため、  
バージョンや状況によっては上手く動作しない可能性があります。

Effekseerの再生プログラムはC++で書かれているため、Unity上ではネイティブプラグイン扱いになります。<br>

## 動作環境 {#environment}

### Unityバージョン
Unity 5.2以降必須。  
PersonalでもProでも使えます。  

### プラットフォーム

| プラットフォーム | 対応状況  |    CPU    | 備考                        |
|--------------|:-------:|:---------:|----------------------------|
|  Windows     |    ○   |  x86/x64  | DX9,DX11で動作可能。DX12は不可。 |
|  Mac OS X    |    ○   |  x86/x64  | OpenGLで動作可能。Metalは不可。  |
|  Android     |    β   |ARMv7/x86   | OpenGLで動作可能。Vulkanは不可。 |
|  iOS         |    β   |ARMv7/ARM64 | OpenGLで動作可能。Metalは不可。  |
|  WebGL       |    ○  |      -     |                             |

## インストール方法 {#how-to-install}
Effekseer.unitypackage を開いてUnityプロジェクトにインポートします。

![](img/unity_import.png)

## 既知の問題 {#issues}
- [iOS] Metalグラフィックス環境では使用できません。
    EffekseerをiOS環境で使用するには  
    Build Settings -> Player Settings -> Other Settings -> Auto Graphics API のチェックを外し、  
    Metalを削除する必要があります。  

## Todo {#todo}
- Metal? Vulkan?
