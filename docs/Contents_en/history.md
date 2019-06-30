# Release notes

## 1.43h

- Fixed a fatal memory leak on MacOS and distortion

## 1.43g

- Support Android 64bit

## 1.43f

- Changed to not display when the EffekseerEmitter component is unchecked

## 1.43e

- Separate distortion settings on PC and mobile
- Fixed a bug that sometimes crashes in Metal environment
- Fixed a bug that a model with lighting enabled can not be drawn
- Fixed a bug that speeding up when the frame rate is over 60
- Fixed a bug that multiplication is not drawn correctly in UnityRenderer
- Fixed a memory leak about a model

## 1.43d

- Fixed a bug hotreloading of effects somtimes fails.

## 1.43c

- Fixed a bug about memories (This bug fix is very important)

## 1.43b

- Fixed a bug where much particles causes a crash in iOS

## 1.43
- Renewal!!

## 1.40
- Distortion specification is changed.
- Added functions

## 1.30
- Distortion specification is changed.
- Coordinate system specification is changed.

## 1.23
- [Windows] Fixed to correctly draw with Unity 5.5β
- [macOS] Supported to OpenGLCore environment
- Increased default number of instances and quads

## 1.22
- [Android] Fixed problem that not occasionally displayed effects
- Supported to culling mask

## 1.21
- Supported loading from AssetBundle
- [WebGL] Supported WebGL target build
- Fixed bug that track type effects are not rendered
- Added SetTargetLocation to EffekseerEmitter and EffekseerHandle
- Added paused and shown to EffekseerEmitter
- Added help contents
- Added reference manual

## 1.20
- Supports distortion effects
- Added StopRoot() to EffekseerEmitter and EffekseerHandle
- Changed to update processing with LateUpdate
- Fixed bug when releasing texture
- [iOS] Changed to output errors when executed in Metal environment

## 1.10b
- [Windows] Fixed to be able to draw correctly with Deferred Rendering
- [Mac] Fixed to be able to draw correctly with Deferred Rendering
- [Android] Added beta version
- [iOS] Added beta version

## 1.10a
- [Windows] Fixed crash of x86 build app

## 1.10
- Change resource files place to Resources/Effekseer
- Change resource loading to Resources.Load()
- Change audio playback to Unity standard Audio
- Change texture loading to use Unity standard Texture2D

## 1.01
- Support new specification of Native Plugin of Unity 5.2
