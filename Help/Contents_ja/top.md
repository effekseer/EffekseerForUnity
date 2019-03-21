# Effekseer UnityPlugin Manual

![](../img/plugin_logo.png)

## 概要 {#overview}

ゲームエンジンUnityとの連携について説明します。  
なお、Unity Technologies社とこのツールは特に提携しているというわけではないため、  
バージョンや状況によっては上手く動作しない可能性があります。

Effekseerの再生プログラムはC++で書かれているため、Unity上ではネイティブプラグイン扱いになります。<br>

### ベータバージョン

このバージョンはベータバージョンです。いくつかの機能が不足しています。

- Metalでの歪み機能
- Vulkanでの動作
- ライティング
- ドキュメント

開発に協力してくれると助かります。

[github](https://github.com/effekseer/EffekseerForUnity/tree/renewal)

## 動作環境 {#environment}

### Unityバージョン
Unity 2017 以降

### プラットフォーム

EffekseerForUnity has two renderers. First renderer is drawn with Compute Shader(UnityRenderer). Second renderer is drawn with native API(NativeRenderer). 
UnityRenderer runs on everywhere where compute shader is enabled. On the other hand, NativeRenderer runs on limited platforms. But NativeRenderer is drawn with multithread.
If unsupported renderer is selected, renderer is changed automatically.

EffekseerForUnityには2種類のレンダラーがあります。1つ目はComputeShaderで描画するUnityRendererです。2つ目はネイティブのAPIで描画するNativeRendererです。
UnityRendererはComputeShaderが有効な全ての環境で動きます。一方、NativeRendererは限られたプラットフォームでしか動きません。しかし、マルチスレッドで描画することができます。
レンダラーは ``` Edit -> ProjectSettings -> Effekseer ``` から選択できます.
もし、サポートされていないレンダラらーが選択されていた場合、自動的にレンダラーが変更されます。

<table>
<thead>
<tr class="header">
<th>Platforms</th>
<th style="text-align: center;">Graphics API</th>
<th style="text-align: center;">UnityRenderer</th>
<th style="text-align: center;">NativeRenderer</th>
<th width="350px">Notes</th>
</tr>
</thead>
<tbody>

<tr>
<td rowspan="5">Windows</td>
<td style="text-align: center;">DirectX9</td>
<td style="text-align: center;"></td>
<td style="text-align: center;">OK</td>
<td rowspan="5">
</td>
</tr>

<tr>
<td style="text-align: center;">DirectX11</td>
<td style="text-align: center;">OK</td>
<td style="text-align: center;">OK</td>
</tr>

<tr>
<td style="text-align: center;">DirectX12</td>
<td style="text-align: center;">OK</td>
<td style="text-align: center;"></td>
</tr>

<tr>
<td style="text-align: center;">OpenGLCore</td>
<td style="text-align: center;">Theoretically</td>
<td style="text-align: center;"></td>
</tr>

<tr>
<td rowspan="3">macOS</td>
<td style="text-align: center;">OpenGLCore</td>
<td style="text-align: center;">Theoretically</td>
<td style="text-align: center;">OK</td>
<td rowspan="3">
</td>
</tr>

<tr>
<td style="text-align: center;">OpenGL2</td>
<td style="text-align: center;"></td>
<td style="text-align: center;">OK</td>
</tr>

<tr>
<td style="text-align: center;">Metal</td>
<td style="text-align: center;">OK</td>
<td style="text-align: center;"></td>
</tr>

<tr>
<td rowspan="3">Android</td>
<td style="text-align: center;">OpenGL ES 2.0</td>
<td style="text-align: center;"></td>
<td style="text-align: center;">OK</td>
<td rowspan="3">
もしVulkanがデフォルトの場合、Player Settingsを変更してください。
</td>
</tr>

<tr>
<td style="text-align: center;">OpenGL ES 3.0</td>
<td style="text-align: center;"></td>
<td style="text-align: center;">OK</td>
</tr>

<tr>
<td style="text-align: center;">Vulkan</td>
<td style="text-align: center;">Debugging</td>
<td style="text-align: center;"></td>
</tr>

<tr>
<td rowspan="3">iOS</td>
<td style="text-align: center;">OpenGL ES 2.0</td>
<td style="text-align: center;"></td>
<td style="text-align: center;">OK</td>
<td rowspan="3">
</td>
</tr>

<tr>
<td style="text-align: center;">OpenGL ES 3.0</td>
<td style="text-align: center;"></td>
<td style="text-align: center;">OK</td>
</tr>

<tr>
<td style="text-align: center;">Metal</td>
<td style="text-align: center;">OK</td>
<td style="text-align: center;"></td>
</tr>

<tr>
<td rowspan="2">WebGL</td>
<td style="text-align: center;">OpenGL ES 2.0 (WebGL 1.0)</td>
<td style="text-align: center;"></td>
<td style="text-align: center;">OK</td>
<td rowspan="2"></td>
</tr>

<tr>
<td style="text-align: center;">OpenGL ES 3.0 (WebGL 2.0)</td>
<td style="text-align: center;"></td>
<td style="text-align: center;">Debugging</td>
</tr>
<tr>
<td>Console Game</td>
<td style="text-align: center;"></td>
<td style="text-align: center;">Theoretically</td>
<td style="text-align: center;"></td>
<td>開発者がC++をコンパイルする必要があります。</td>
</tr>

</tbody>
</table>

Theoretically - テストはしていないですが、理論的には動作します。

Debugging -　テストはしましたが、何らかの不具合により動きません。

## 導入方法 {#how-to-import}
Effekseer.unitypackage を開いてUnityプロジェクトにインポートします。

![](../img/unity_import.png)


## 既知の問題 {#issues}
- DirectX11のForwardレンダラーで、Editor上のGameViewのみ、3Dモデルの表裏が逆になります。Effekseer上でカリングの設定を変更してください。

## Todo {#todo}
- 未対応の Graphics API (Metal, Vulkan) の対応
- ポイントライトのコントロール
- インスタンスのコリジョン判定
