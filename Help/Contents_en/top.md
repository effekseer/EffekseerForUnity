# Effekseer UnityPlugin Manual

![](../img/plugin_logo.png)

## Overview {#overview}
It is explanation about cooperation with game engine Unity.
As this tool with Unity Technologies is not particularly tied up,
Depending on the version and circumstances it may not work well.

Because Effekseer's playback program is written in C ++, it is handled as a native plugin on Unity.

## Environment {#environment}

### Unity version
Unity 5.2 or later.  
Personal, Plus and Pro.

### Supported Platform

<table>
<thead>
<tr class="header">
<th>Platforms</th>
<th style="text-align: center;">Graphics API</th>
<th style="text-align: center;">Condition</th>
<th width="350px">Notes</th>
</tr>
</thead>
<tbody>
<tr>
<td rowspan="4">Windows</td>
<td style="text-align: center;">DirectX9</td>
<td style="text-align: center;">○</td>
<td rowspan="4">
If DirectX 12 is used by default, it must be checked off from Player Settings.
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
If Metal is used by default, it must be checked off from Player Settings.
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
If Vulkan is used by default, it must be checked off from Player Settings.
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
If Metal is used by default, it must be checked off from Player Settings.
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

## How to import {#how-to-import}
Open Effekseer.unitypackage and import it into the your Unity project.

![](../img/unity_import.png)

## Specification change

### 1.3

Before and after the effect are swapped.
When doing display before 1.2, set ```isRightHandledCoordinateSystem``` of EffekseerSystem component to true.

Distortion method has been changed. Effekseer's effect is not distorted due to distortion.
From 1.4 onwards, we plan to add effects distorted by distortion.

## Known issues {#issues}
- Even on supported platforms, effect graphics are not rendered correctly in Graphics API which is not supported.<br>Please check the table of "Supported Platform".

## Todo {#todo}
- Support some new Graphics API (Metal, Vulkan)
- Controll point lights
- Collision to particles
