# EffekseerForUnity

This is the Effekseer's runtime plugin for Unity5.  
You will be able to show the effects that was created with Effekseer.  

Unity5向けEffekseer実行プラグインです。  
Effekseerで作成したエフェクトをUnityで表示することができます。

- [Official website](http://effekseer.github.io)
- [Effekseer main repository](https://github.com/effekseer/Effekseer)

# Supported Platforms

<table>
<thead>
<tr class="header">
<th>Platforms</th>
<th style="text-align: center;">Graphics API</th>
<th style="text-align: center;">Support</th>
<th width="300px">Note</th>
</tr>
</thead>
<tbody>
<tr>
<td rowspan="4">Windows</td>
<td style="text-align: center;">DirectX9</td>
<td style="text-align: center;">○</td>
<td rowspan="4">
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

# Clone the source code

Needs to clone main repository in the same place of this repository, to develop this plugin.

このプラグインを開発するには、このリポジトリと同じ場所に本体リポジトリもクローンする必要があります。

```
git clone https://github.com/effekseer/Effekseer
git clone https://github.com/effekseer/EffekseerForUnity
```

# Building

## Windows

Uses Visual Studio 2013 or later, to open and build the following solution file.

- Dev/Cpp/windows/EffekseerUnity.sln

## macOS

Uses Xcode, to open the following project file.

- Dev/Cpp/macosx/EffekseerUnity.xcodeproj

# Todo
- iOS Metal support
- Android Vulkan support
