# Effekseer UnityPlugin Manual

![](../img/plugin_logo.png)

## 概要 {#overview}
ゲームエンジンUnityとの連携について説明します。  
なお、Unity Technologies社とこのツールは特に提携しているというわけではないため、  
バージョンや状況によっては上手く動作しない可能性があります。

Effekseerの再生プログラムはC++で書かれているため、Unity上ではネイティブプラグイン扱いになります。<br>

## 動作環境 {#environment}

### Unityバージョン
Unity 5.2 以降。 Personal, Plus, Pro

### プラットフォーム

<table>
<thead>
<tr class="header">
<th>プラットフォーム</th>
<th style="text-align: center;">Graphics API</th>
<th style="text-align: center;">対応状況</th>
<th width="350px">備考</th>
</tr>
</thead>
<tbody>
<tr>
<td rowspan="4">Windows</td>
<td style="text-align: center;">DirectX9</td>
<td style="text-align: center;">○</td>
<td rowspan="4">
Unity 5.5.0 時点で DirectX12 は標準APIに含まれていないので Player Settings の変更は不要。
</td>
</tr>
<tr>
<td style="text-align: center;">DirectX11</td>
<td style="text-align: center;">○</td>
</tr>
<tr>
<td style="text-align: center;">DirectX12</td>
<td style="text-align: center;">×</td>
</tr>
<tr>
<td style="text-align: center;">OpenGLCore</td>
<td style="text-align: center;">×</td>
</tr>
<tr>
<td rowspan="3">macOS</td>
<td style="text-align: center;">OpenGLCore</td>
<td style="text-align: center;">○</td>
<td rowspan="3">
Unity 5.5.0 時点で Metal は標準APIに含まれていないので Player Settings の変更は不要。	
</td>
</tr>
<tr>
<td style="text-align: center;">OpenGL2</td>
<td style="text-align: center;">○</td>
</tr>
<tr>
<td style="text-align: center;">Metal</td>
<td style="text-align: center;">×</td>
</tr>
<tr>
<td rowspan="3">Android</td>
<td style="text-align: center;">OpenGL ES 2.0</td>
<td style="text-align: center;">○</td>
<td rowspan="3">
Unity 5.5.0 時点で Vulkan は標準APIに含まれていないので Player Settings の変更は不要。
</td>
</tr>
<tr>
<td style="text-align: center;">OpenGL ES 3.0</td>
<td style="text-align: center;">○</td>
</tr>
<tr>
<td style="text-align: center;">Vulkan</td>
<td style="text-align: center;">×</td>
</tr>
<tr>
<td rowspan="3">iOS</td>
<td style="text-align: center;">OpenGL ES 2.0</td>
<td style="text-align: center;">○</td>
<td rowspan="3">
Build Settings -&gt; Player Settings -&gt; Other Settings -&gt; Auto Graphics API のチェックを外し、Metalを削除する必要があります。
</td>
</tr>
<tr>
<td style="text-align: center;">OpenGL ES 3.0</td>
<td style="text-align: center;">○</td>
</tr>
<tr>
<td style="text-align: center;">Metal</td>
<td style="text-align: center;">×</td>
</tr>
<tr>
<td rowspan="2">WebGL</td>
<td style="text-align: center;">OpenGL ES 2.0 (WebGL 1.0)</td>
<td style="text-align: center;">○</td>
<td rowspan="2"></td>
</tr>
<td style="text-align: center;">OpenGL ES 3.0 (WebGL 2.0)</td>
<td style="text-align: center;">？</td>
</tr>
</tbody>
</table>

## 導入方法 {#how-to-import}
Effekseer.unitypackage を開いてUnityプロジェクトにインポートします。

![](../img/unity_import.png)

## 仕様変更

### 1.3

エフェクトの前後が入れ替わっています。
1.2以前の表示を行う場合、EffekseerSystemコンポーネントの ```isRightHandledCoordinateSystem``` をtrueにしてください。

歪み方法が変更されました。Effekseerのエフェクトが歪みにより歪まなくなっています。
1.4以降に、歪みにより歪むエフェクトを追加する予定です。


## 既知の問題 {#issues}
- 対応プラットフォームでも非対応の Graphics API では、正しくエフェクトの描画が行われません。<br>上記の"対応プラットフォーム"の表を確認をしてください。

## Todo {#todo}
- 未対応の Graphics API (Metal, Vulkan) の対応
- ポイントライトのコントロール
- インスタンスのコリジョン判定
