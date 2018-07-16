#include "EffekseerPluginCommon.h"
#include "EffekseerPluginTexture.h"
#include "EffekseerPluginModel.h"
#include "EffekseerPluginSound.h"
#include "Effekseer.h"

using namespace Effekseer;
using namespace EffekseerPlugin;

namespace EffekseerPlugin
{
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
	// フレームの更新
	void UNITY_API EffekseerUpdate(float deltaFrame)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}
		
		g_EffekseerManager->Update(deltaFrame);

		for (auto& settings : renderSettings) {
			settings.stereoRenderCount = 0;
		}
	}
	
	// エフェクトのロード
	Effect* UNITY_API EffekseerLoadEffect(const EFK_CHAR* path)
	{
		if (g_EffekseerManager == NULL) {
			return NULL;
		}
		
		return Effect::Create(g_EffekseerManager, path);
	}
	
	// エフェクトのロード（メモリ指定）
	Effect* UNITY_API EffekseerLoadEffectOnMemory(void* data, int32_t size)
	{
		if (g_EffekseerManager == NULL) {
			return NULL;
		}
		
		return Effect::Create(g_EffekseerManager, data, size);
	}
	
	// エフェクトのアンロード
	void UNITY_API EffekseerReleaseEffect(Effect* effect)
	{
		if (effect != NULL) {
			effect->Release();
		}
	}

	// エフェクト再生
	int UNITY_API EffekseerPlayEffect(Effect* effect, float x, float y, float z)
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
	void UNITY_API EffekseerUpdateHandle(int handle, float deltaFrame)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}
		
		g_EffekseerManager->BeginUpdate();
		g_EffekseerManager->UpdateHandle(handle, deltaFrame);
		g_EffekseerManager->EndUpdate();
	}

	// エフェクト停止
	void UNITY_API EffekseerStopEffect(int handle)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->StopEffect(handle);
	}
	
	// エフェクトのルートだけを停止
	void UNITY_API EffekseerStopRoot(int handle)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->StopRoot(handle);
	}
	
	// 全てのエフェクト再生
	void UNITY_API EffekseerStopAllEffects()
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->StopAllEffects();
	}

	void UNITY_API EffekseerSetPausedToAllEffects(int paused)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->SetPausedToAllEffects(paused != 0);
	}
	
	int UNITY_API EffekseerGetShown(int handle)
	{
		if (g_EffekseerManager == NULL) {
			return 0;
		}

		return g_EffekseerManager->GetShown(handle);
	}

	// エフェクト可視設定
	void UNITY_API EffekseerSetShown(int handle, int shown)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->SetShown(handle, shown != 0);
	}

	int UNITY_API EffekseerGetPaused(int handle)
	{
		if (g_EffekseerManager == NULL) {
			return 0;
		}

		return g_EffekseerManager->GetPaused(handle);
	}

	// エフェクト一時停止
	void UNITY_API EffekseerSetPaused(int handle, int paused)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->SetPaused(handle, paused != 0);
	}

	float UNITY_API EffekseerGetSpeed(int handle)
	{
		if (g_EffekseerManager == NULL) {
			return 0;
		}

		return g_EffekseerManager->GetSpeed(handle);
	}

	void UNITY_API EffekseerSetSpeed(int handle, float speed)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->SetSpeed(handle, speed);
	}
	
	// エフェクト存在状態
	int UNITY_API EffekseerExists(int handle)
	{
		if (g_EffekseerManager == NULL) {
			return false;
		}

		return g_EffekseerManager->Exists(handle);
	}
	
	// エフェクト位置設定
	void UNITY_API EffekseerSetLocation(int handle, float x, float y, float z)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->SetLocation(handle, x, y, z);
	}
	
	// エフェクト回転設定
	void UNITY_API EffekseerSetRotation(int handle, float x, float y, float z, float angle)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		Vector3D axis(x, y, z);
		g_EffekseerManager->SetRotation(handle, axis, angle);
	}
	
	// エフェクト拡縮設定
	void UNITY_API EffekseerSetScale(int handle, float x, float y, float z)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->SetScale(handle, x, y, z);
	}

	// Specify the color of overall effect.
	void UNITY_API EffekseerSetAllColor(int handle, int r, int g, int b, int a)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->SetAllColor(handle, Effekseer::Color(r, g, b, a));
	}
	
	// エフェクトのターゲット位置設定
	void UNITY_API EffekseerSetTargetLocation(int handle, float x, float y, float z)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->SetTargetLocation(handle, x, y, z);
	}
	
	// プロジェクション行列設定
	void UNITY_API EffekseerSetProjectionMatrix(int renderId, float matrixArray[])
	{
		if (renderId >= 0 && renderId < MAX_RENDER_PATH) {
			Array2Matrix(renderSettings[renderId].projectionMatrix, matrixArray);
		}
	}
	
	// ビュー行列設定
	void UNITY_API EffekseerSetCameraMatrix(int renderId, float matrixArray[])
	{
		if (renderId >= 0 && renderId < MAX_RENDER_PATH) {
			Array2Matrix(renderSettings[renderId].cameraMatrix, matrixArray);
		}
	}

	// ステレオレンダリング(VR)用行列設定
	void UNITY_API EffekseerSetStereoRenderingMatrix(int renderId, float projMatL[], float projMatR[], float camMatL[], float camMatR[])
	{
		if (renderId >= 0 && renderId < MAX_RENDER_PATH) {
			auto& settings = renderSettings[renderId];
			settings.stereoEnabled = true;
			Array2Matrix(settings.leftProjectionMatrix, projMatL);
			Array2Matrix(settings.rightProjectionMatrix, projMatR);
			Array2Matrix(settings.leftCameraMatrix, camMatL);
			Array2Matrix(settings.rightCameraMatrix, camMatR);
		}
	}

	// 描画設定
	void UNITY_API EffekseerSetRenderSettings(int renderId, bool renderIntoTexture)
	{
		if (renderId >= 0 && renderId < MAX_RENDER_PATH) {
			renderSettings[renderId].renderIntoTexture = renderIntoTexture;
		}
	}

	void UNITY_API EffekseerSetTextureLoaderEvent(
		TextureLoaderLoad load,
		TextureLoaderUnload unload)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->SetTextureLoader(EffekseerPlugin::TextureLoader::Create(load, unload));
	}

	void UNITY_API EffekseerSetModelLoaderEvent(
		ModelLoaderLoad load,
		ModelLoaderUnload unload)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->SetModelLoader(EffekseerPlugin::ModelLoader::Create(load, unload));
	}

	void UNITY_API EffekseerSetSoundLoaderEvent(
		SoundLoaderLoad load,
		SoundLoaderUnload unload)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->SetSoundLoader(EffekseerPlugin::SoundLoader::Create(load, unload));
	}

	void UNITY_API EffekseerSetSoundPlayerEvent(
		SoundPlayerPlay play,
		SoundPlayerStopTag stopTag,
		SoundPlayerPauseTag pauseTag,
		SoundPlayerCheckPlayingTag checkPlayingTag,
		SoundPlayerStopAll stopAll)
	{
		if (g_EffekseerManager == NULL) {
			return;
		}

		g_EffekseerManager->SetSoundPlayer(EffekseerPlugin::SoundPlayer::Create(play, stopTag, pauseTag, checkPlayingTag, stopAll ));
	}
}
