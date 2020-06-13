#include "EffekseerPluginCommon.h"
#include "EffekseerPluginTexture.h"
#include "EffekseerPluginModel.h"
#include "EffekseerPluginSound.h"
#include "EffekseerPluginNetwork.h"
#include "../graphicsAPI/EffekseerPluginGraphics.h"

using namespace Effekseer;
using namespace EffekseerPlugin;

namespace EffekseerPlugin
{

extern Effekseer::Manager* g_EffekseerManager;
extern Graphics* g_graphics;
extern float g_time;
extern Effekseer::Vector3D g_lightDirection;
extern Effekseer::Color g_lightColor;
extern Effekseer::Color g_lightAmbientColor;

	RenderSettings renderSettings[MAX_RENDER_PATH] = {{}};

	void Array2Matrix(Matrix44& matrix, float matrixArray[])
	{
		matrix.Values[0][0] = matrixArray[ 0];
		matrix.Values[1][0] = matrixArray[ 1];
		matrix.Values[2][0] = matrixArray[ 2];
		matrix.Values[3][0] = matrixArray[ 3];
		matrix.Values[0][1] = matrixArray[ 4];
		matrix.Values[1][1] = matrixArray[ 5];
		matrix.Values[2][1] = matrixArray[ 6];
		matrix.Values[3][1] = matrixArray[ 7];
		matrix.Values[0][2] = matrixArray[ 8];
		matrix.Values[1][2] = matrixArray[ 9];
		matrix.Values[2][2] = matrixArray[10];
		matrix.Values[3][2] = matrixArray[11];
		matrix.Values[0][3] = matrixArray[12];
		matrix.Values[1][3] = matrixArray[13];
		matrix.Values[2][3] = matrixArray[14];
		matrix.Values[3][3] = matrixArray[15];
	}


	void CalculateCameraDirectionAndPosition(const Effekseer::Matrix44& matrix, Effekseer::Vector3D& direction, Effekseer::Vector3D& position)
	{
		auto mat = matrix;

		direction = ::Effekseer::Vector3D(matrix.Values[0][2], matrix.Values[1][2], matrix.Values[2][2]);
		
		{
			auto localPos = ::Effekseer::Vector3D(-mat.Values[3][0], -mat.Values[3][1], -mat.Values[3][2]);
			auto f = ::Effekseer::Vector3D(mat.Values[0][2], mat.Values[1][2], mat.Values[2][2]);
			auto r = ::Effekseer::Vector3D(mat.Values[0][0], mat.Values[1][0], mat.Values[2][0]);
			auto u = ::Effekseer::Vector3D(mat.Values[0][1], mat.Values[1][1], mat.Values[2][1]);

			position = r * localPos.X + u * localPos.Y + f * localPos.Z;
		}
	}
}

extern "C"
{
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerUpdateTime(float deltaFrame) 
	{
		g_time += deltaFrame * (1.0f / 60.0f);
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerResetTime()
	{
		g_time = 0.0f; 
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerUpdate(float deltaFrame)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}
		
		Effekseer::Manager::UpdateParameter param;
		param.DeltaFrame = deltaFrame;
		param.UpdateInterval = 1.0f;

		g_EffekseerManager->Update(param);
		
	}
	
	UNITY_INTERFACE_EXPORT Effect* UNITY_INTERFACE_API EffekseerLoadEffect(const EFK_CHAR* path, float magnification)
	{
		if (g_EffekseerManager == NULL) {
			return NULL;
		}
		
		auto effect = Effect::Create(g_EffekseerManager, path, magnification);

#ifndef _SWITCH
		if (Network::GetInstance()->IsRunning())
		{
			Network::GetInstance()->Register(effect->GetName(), effect);
		}
#endif

		return effect;
	}
	
	// エフェクトのロード（メモリ指定）
	UNITY_INTERFACE_EXPORT Effect* UNITY_INTERFACE_API EffekseerLoadEffectOnMemory(void* data,
																				   int32_t size,
																				   const EFK_CHAR* path,
																				   float magnification)
	{
		if (g_EffekseerManager == NULL) {
			return NULL;
		}
		
		auto effect = Effect::Create(g_EffekseerManager, data, size, magnification);
		
		if (effect != nullptr)
		{
			effect->SetName(path);

#ifndef _SWITCH
			if (Network::GetInstance()->IsRunning())
			{
				Network::GetInstance()->Register(effect->GetName(), effect);
			}
#endif
		}
		
		return effect;
	}
	
	// エフェクトのアンロード
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerReleaseEffect(Effect* effect)
	{
		if (effect != NULL) {
#ifndef _SWITCH
			if (Network::GetInstance()->IsRunning())
			{
				Network::GetInstance()->Unregister(effect);
			}
#endif

			effect->Release();
		}
	}

	// エフェクトのリソースのリロード
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerReloadResources(Effect* effect)
	{
		if (effect != NULL) {
			effect->ReloadResources();
		}
	}

	// エフェクトのリソースのリロード
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerUnloadResources(Effect* effect)
	{
		if (effect != NULL) {
			effect->UnloadResources();
		}
	}

	UNITY_INTERFACE_EXPORT float UNITY_INTERFACE_API EffekseerGetEffectMagnification(Effect* effect)
	{ 
		if (effect != nullptr)
		{
			return effect->GetMaginification();
		}
		return 0.0f;
	}

	// エフェクト再生
	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API EffekseerPlayEffect(Effect* effect, float x, float y, float z)
	{
		if (g_EffekseerManager == NULL) {
			return -1;
		}

		if (effect != NULL) {
			return g_EffekseerManager->Play(effect, x, y, z);
		}
		return -1;
	}
	
	// フレームの更新(ハンドル単位)
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerUpdateHandle(int handle, float deltaFrame)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}
		
		g_EffekseerManager->BeginUpdate();
		g_EffekseerManager->UpdateHandle(handle, deltaFrame);
		g_EffekseerManager->EndUpdate();
	}

	// エフェクト停止
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerStopEffect(int handle)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->StopEffect(handle);
	}
	
	// エフェクトのルートだけを停止
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerStopRoot(int handle)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->StopRoot(handle);
	}
	
	// 全てのエフェクト再生
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerStopAllEffects()
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->StopAllEffects();
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetPausedToAllEffects(int paused)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->SetPausedToAllEffects(paused != 0);
	}
	
	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API EffekseerGetCameraCullingMaskToShowAllEffects()
	{
		if (g_EffekseerManager == NULL) {
			return 0;
		}

		return g_EffekseerManager->GetCameraCullingMaskToShowAllEffects();
	}

	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API EffekseerGetShown(int handle)
	{
		if (g_EffekseerManager == NULL) {
			return 0;
		}

		return g_EffekseerManager->GetShown(handle);
	}

	// エフェクト可視設定
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetShown(int handle, int shown)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->SetShown(handle, shown != 0);
	}

	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API EffekseerGetPaused(int handle)
	{
		if (g_EffekseerManager == NULL) {
			return 0;
		}

		return g_EffekseerManager->GetPaused(handle);
	}

	// エフェクト一時停止
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetPaused(int handle, int paused)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->SetPaused(handle, paused != 0);
	}

	UNITY_INTERFACE_EXPORT float UNITY_INTERFACE_API EffekseerGetSpeed(int handle)
	{
		if (g_EffekseerManager == NULL) {
			return 0;
		}

		return g_EffekseerManager->GetSpeed(handle);
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetSpeed(int handle, float speed)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->SetSpeed(handle, speed);
	}
	
	// エフェクト存在状態
	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API EffekseerExists(int handle)
	{
		if (g_EffekseerManager == NULL) {
			return false;
		}

		return g_EffekseerManager->Exists(handle);
	}
	
	// エフェクト位置設定
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetLocation(int handle, float x, float y, float z)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->SetLocation(handle, x, y, z);
	}
	
	// エフェクト回転設定
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetRotation(int handle, float x, float y, float z, float angle)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		Vector3D axis(x, y, z);
		g_EffekseerManager->SetRotation(handle, axis, angle);
	}
	
	// エフェクト拡縮設定
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetScale(int handle, float x, float y, float z)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->SetScale(handle, x, y, z);
	}

	// Specify the color of overall effect.
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetAllColor(int handle, int r, int g, int b, int a)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->SetAllColor(handle, Effekseer::Color(r, g, b, a));
	}
	
	// エフェクトのターゲット位置設定
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetTargetLocation(int handle, float x, float y, float z)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->SetTargetLocation(handle, x, y, z);
	}

	UNITY_INTERFACE_EXPORT float UNITY_INTERFACE_API EffekseerGetDynamicInput(int handle, int index)
	{
		if (g_EffekseerManager == NULL)
		{
			return 0.0f;
		}

		return g_EffekseerManager->GetDynamicInput(handle, index);
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetDynamicInput(int handle, int index, float value)
	{
		if (g_EffekseerManager == NULL)
		{
			return;
		}

		g_EffekseerManager->SetDynamicInput(handle, index, value);
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetLayer(int handle, int layer)
	{
		if (g_EffekseerManager == NULL)
		{
			return;
		}

		g_EffekseerManager->SetLayer(handle, layer);
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetGroupMask(int handle, int64_t groupMask)
	{
		if (g_EffekseerManager == nullptr)
		{
			return;
		}

		g_EffekseerManager->SetGroupMask(handle, groupMask);
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetTimeScaleByGroup(int64_t groupMask, float timeScale)
	{
		if (g_EffekseerManager == nullptr)
		{
			return;
		}

		g_EffekseerManager->SetTimeScaleByGroup(groupMask, timeScale);
	}

	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API EffekseerGetInstanceCount(int handle)
	{
		if (g_EffekseerManager == nullptr)
		{
			return 0;
		}

		return g_EffekseerManager->GetInstanceCount(handle);
	}

	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API EffekseerGetRestInstancesCount()
	{
		if (g_EffekseerManager == nullptr)
		{
			return 0;
		}

		return g_EffekseerManager->GetRestInstancesCount();
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetLightDirection(float x, float y, float z)
	{
		g_lightDirection.X = x;
		g_lightDirection.Y = y;
		g_lightDirection.Z = z;
	}


	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetLightColor(int32_t r, int32_t g, int32_t b)
	{ 
		g_lightColor.R = r;
		g_lightColor.G = g;
		g_lightColor.B = b;
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetLightAmbientColor(int32_t r, int32_t g, int32_t b)
	{
		g_lightAmbientColor.R = r;
		g_lightAmbientColor.G = g;
		g_lightAmbientColor.B = b;
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetProjectionMatrix(int renderId, float matrixArray[])
	{
		if (renderId >= 0 && renderId < MAX_RENDER_PATH) {
			Array2Matrix(renderSettings[renderId].projectionMatrix, matrixArray);
			renderSettings[renderId].stereoEnabled = false;
		}
	}
	
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetCameraMatrix(int renderId, float matrixArray[])
	{
		if (renderId >= 0 && renderId < MAX_RENDER_PATH) {
			Array2Matrix(renderSettings[renderId].cameraMatrix, matrixArray);
			renderSettings[renderId].stereoEnabled = false;
		}
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetStereoRenderingMatrix(
		int renderId, int renderType, 
		float camCenterMat[],
		float projMatL[], float projMatR[],
		float camMatL[], float camMatR[])
	{
		if (renderId >= 0 && renderId < MAX_RENDER_PATH) {
			auto& settings = renderSettings[renderId];
			settings.stereoEnabled = true;
			settings.stereoRenderCount = 0;
			settings.stereoRenderingType = (StereoRenderingType)renderType;
			Array2Matrix(settings.cameraMatrix, camCenterMat);
			Array2Matrix(settings.leftProjectionMatrix, projMatL);
			Array2Matrix(settings.rightProjectionMatrix, projMatR);
			Array2Matrix(settings.leftCameraMatrix, camMatL);
			Array2Matrix(settings.rightCameraMatrix, camMatR);
		}
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetRenderSettings(int renderId, bool renderIntoTexture)
	{
		if (renderId >= 0 && renderId < MAX_RENDER_PATH) {
			renderSettings[renderId].renderIntoTexture = renderIntoTexture;
		}
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetRenderingCameraCullingMask(int renderId, int32_t cameraCullingMask)
	{
		if (renderId >= 0 && renderId < MAX_RENDER_PATH)
		{
			renderSettings[renderId].cameraCullingMask = cameraCullingMask;
		}
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetTextureLoaderEvent(
		TextureLoaderLoad load,
		TextureLoaderUnload unload)
	{
		if (g_EffekseerManager == nullptr)
		{
			return;
		}

		if (g_graphics == nullptr)
		{
			return;
		}

		g_EffekseerManager->SetTextureLoader(g_graphics->Create(load, unload));
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetModelLoaderEvent(
		ModelLoaderLoad load,
		ModelLoaderUnload unload)
	{
		if (g_EffekseerManager == nullptr)
		{
			return;
		}

		if (g_graphics == nullptr)
		{
			return;
		}

		g_EffekseerManager->SetModelLoader(g_graphics->Create(load, unload));
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetMaterialLoaderEvent(MaterialLoaderLoad load, MaterialLoaderUnload unload)
	{
		if (g_EffekseerManager == nullptr)
		{
			return;
		}

		if (g_graphics == nullptr)
		{
			return;
		}

		g_EffekseerManager->SetMaterialLoader(g_graphics->Create(load, unload));
	}


	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetSoundLoaderEvent(
		SoundLoaderLoad load,
		SoundLoaderUnload unload)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->SetSoundLoader(EffekseerPlugin::SoundLoader::Create(load, unload));
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetSoundPlayerEvent(
		SoundPlayerPlay play,
		SoundPlayerStopTag stopTag,
		SoundPlayerPauseTag pauseTag,
		SoundPlayerCheckPlayingTag checkPlayingTag,
		SoundPlayerStopAll stopAll)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		if (play && stopTag && pauseTag && checkPlayingTag && stopAll) {
			g_EffekseerManager->SetSoundPlayer(EffekseerPlugin::SoundPlayer::Create(play, stopTag, pauseTag, checkPlayingTag, stopAll ));
		} else {
			g_EffekseerManager->SetSoundPlayer(nullptr);
		}
	}
}
