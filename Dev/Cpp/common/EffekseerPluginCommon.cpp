#include "EffekseerPluginCommon.h"
#include "../graphicsAPI/EffekseerPluginGraphics.h"
#include "EffekseerPluginCurve.h"
#include "EffekseerPluginModel.h"
#include "EffekseerPluginNetwork.h"
#include "EffekseerPluginSound.h"
#include "EffekseerPluginTexture.h"

using namespace Effekseer;
using namespace EffekseerPlugin;

namespace EffekseerPlugin
{

extern Effekseer::ManagerRef g_EffekseerManager;
extern Graphics* g_graphics;
extern float g_time;
extern Effekseer::Vector3D g_lightDirection;
extern Effekseer::Color g_lightColor;
extern Effekseer::Color g_lightAmbientColor;

RenderSettings renderSettings[MAX_RENDER_PATH] = {{}};

void Array2Matrix(Matrix44& matrix, float matrixArray[])
{
	matrix.Values[0][0] = matrixArray[0];
	matrix.Values[1][0] = matrixArray[1];
	matrix.Values[2][0] = matrixArray[2];
	matrix.Values[3][0] = matrixArray[3];
	matrix.Values[0][1] = matrixArray[4];
	matrix.Values[1][1] = matrixArray[5];
	matrix.Values[2][1] = matrixArray[6];
	matrix.Values[3][1] = matrixArray[7];
	matrix.Values[0][2] = matrixArray[8];
	matrix.Values[1][2] = matrixArray[9];
	matrix.Values[2][2] = matrixArray[10];
	matrix.Values[3][2] = matrixArray[11];
	matrix.Values[0][3] = matrixArray[12];
	matrix.Values[1][3] = matrixArray[13];
	matrix.Values[2][3] = matrixArray[14];
	matrix.Values[3][3] = matrixArray[15];
}

void CalculateCameraDirectionAndPosition(const Effekseer::Matrix44& matrix, Effekseer::Vector3D& direction, Effekseer::Vector3D& position)
{
	const auto& mat = matrix;

	direction = -::Effekseer::Vector3D(matrix.Values[0][2], matrix.Values[1][2], matrix.Values[2][2]);

	{
		auto localPos = ::Effekseer::Vector3D(-mat.Values[3][0], -mat.Values[3][1], -mat.Values[3][2]);
		auto f = ::Effekseer::Vector3D(mat.Values[0][2], mat.Values[1][2], mat.Values[2][2]);
		auto r = ::Effekseer::Vector3D(mat.Values[0][0], mat.Values[1][0], mat.Values[2][0]);
		auto u = ::Effekseer::Vector3D(mat.Values[0][1], mat.Values[1][1], mat.Values[2][1]);

		position = r * localPos.X + u * localPos.Y + f * localPos.Z;
	}
}
} // namespace EffekseerPlugin

extern "C"
{
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerUpdateTime(float deltaFrame) { g_time += deltaFrame * (1.0f / 60.0f); }

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerResetTime() { g_time = 0.0f; }

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerUpdate(float deltaFrame)
	{
		if (g_EffekseerManager == NULL)
		{
			return;
		}

		Effekseer::Manager::UpdateParameter param;
		param.DeltaFrame = deltaFrame;
		param.UpdateInterval = 1.0f;

		g_EffekseerManager->Update(param);
	}

	UNITY_INTERFACE_EXPORT void* UNITY_INTERFACE_API EffekseerLoadEffect(const char16_t* path, float magnification)
	{
		if (g_EffekseerManager == NULL)
		{
			return NULL;
		}

		auto effect = Effect::Create(g_EffekseerManager, path, magnification);

#ifndef _SWITCH
		if (Network::GetInstance()->IsRunning())
		{
			Network::GetInstance()->Register(effect->GetName(), effect);
		}
#endif

		return effect.Pin();
	}

	// エフェクトのロード（メモリ指定）
	UNITY_INTERFACE_EXPORT void* UNITY_INTERFACE_API EffekseerLoadEffectOnMemory(void* data,
																				 int32_t size,
																				 const EFK_CHAR* path,
																				 float magnification)
	{
		if (g_EffekseerManager == NULL)
		{
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

		return effect.Pin();
	}

	// エフェクトのアンロード
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerReleaseEffect(void* effect)
	{
		auto pinned = Effekseer::EffectRef::FromPinned(effect);

		if (effect != NULL)
		{
#ifndef _SWITCH
			if (Network::GetInstance()->IsRunning())
			{
				Network::GetInstance()->Unregister(pinned);
			}
#endif

			Effekseer::EffectRef::Unpin(effect);
		}
	}

	// エフェクトのリソースのリロード
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerReloadResources(Effect* effect)
	{
		if (g_EffekseerManager != nullptr)
		{
			g_EffekseerManager->LockRendering();
		}

		if (effect != NULL)
		{
			effect->ReloadResources();
		}

		if (g_EffekseerManager != nullptr)
		{
			g_EffekseerManager->UnlockRendering();
		}
	}

	// エフェクトのリソースのリロード
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerUnloadResources(Effect* effect)
	{
		if (g_EffekseerManager != nullptr)
		{
			g_EffekseerManager->LockRendering();
		}

		if (effect != NULL)
		{
			effect->UnloadResources();
		}

		if (g_EffekseerManager != nullptr)
		{
			g_EffekseerManager->UnlockRendering();
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
		auto pinned = Effekseer::EffectRef::FromPinned(effect);

		if (g_EffekseerManager == NULL)
		{
			return -1;
		}

		if (effect != NULL)
		{
			return g_EffekseerManager->Play(pinned, x, y, z);
		}
		return -1;
	}

	// フレームの更新(ハンドル単位)
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerUpdateHandle(int handle, float deltaFrame)
	{
		if (g_EffekseerManager == NULL)
		{
			return;
		}

		g_EffekseerManager->BeginUpdate();
		g_EffekseerManager->UpdateHandle(handle, deltaFrame);
		g_EffekseerManager->EndUpdate();
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerUpdateHandleToMoveToFrame(int handle, float frame)
	{
		if (g_EffekseerManager == NULL)
		{
			return;
		}

		g_EffekseerManager->BeginUpdate();
		g_EffekseerManager->UpdateHandleToMoveToFrame(handle, frame);
		g_EffekseerManager->EndUpdate();
	}

	// エフェクト停止
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerStopEffect(int handle)
	{
		if (g_EffekseerManager == NULL)
		{
			return;
		}

		g_EffekseerManager->StopEffect(handle);
	}

	// エフェクトのルートだけを停止
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerStopRoot(int handle)
	{
		if (g_EffekseerManager == NULL)
		{
			return;
		}

		g_EffekseerManager->StopRoot(handle);
	}

	// 全てのエフェクト再生
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerStopAllEffects()
	{
		if (g_EffekseerManager == NULL)
		{
			return;
		}

		g_EffekseerManager->StopAllEffects();
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetPausedToAllEffects(int paused)
	{
		if (g_EffekseerManager == NULL)
		{
			return;
		}

		g_EffekseerManager->SetPausedToAllEffects(paused != 0);
	}

	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API EffekseerGetCameraCullingMaskToShowAllEffects()
	{
		if (g_EffekseerManager == NULL)
		{
			return 0;
		}

		return g_EffekseerManager->GetCameraCullingMaskToShowAllEffects();
	}

	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API EffekseerGetShown(int handle)
	{
		if (g_EffekseerManager == NULL)
		{
			return 0;
		}

		return g_EffekseerManager->GetShown(handle);
	}

	// エフェクト可視設定
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetShown(int handle, int shown)
	{
		if (g_EffekseerManager == NULL)
		{
			return;
		}

		g_EffekseerManager->SetShown(handle, shown != 0);
	}

	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API EffekseerGetPaused(int handle)
	{
		if (g_EffekseerManager == NULL)
		{
			return 0;
		}

		return g_EffekseerManager->GetPaused(handle);
	}

	// エフェクト一時停止
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetPaused(int handle, int paused)
	{
		if (g_EffekseerManager == NULL)
		{
			return;
		}

		g_EffekseerManager->SetPaused(handle, paused != 0);
	}

	UNITY_INTERFACE_EXPORT float UNITY_INTERFACE_API EffekseerGetSpeed(int handle)
	{
		if (g_EffekseerManager == NULL)
		{
			return 0;
		}

		return g_EffekseerManager->GetSpeed(handle);
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetSpeed(int handle, float speed)
	{
		if (g_EffekseerManager == NULL)
		{
			return;
		}

		g_EffekseerManager->SetSpeed(handle, speed);
	}

	// エフェクト存在状態
	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API EffekseerExists(int handle)
	{
		if (g_EffekseerManager == NULL)
		{
			return false;
		}

		return g_EffekseerManager->Exists(handle);
	}

	// エフェクト位置設定
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetLocation(int handle, float x, float y, float z)
	{
		if (g_EffekseerManager == NULL)
		{
			return;
		}

		g_EffekseerManager->SetLocation(handle, x, y, z);
	}

	// エフェクト回転設定
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetRotation(int handle, float x, float y, float z, float angle)
	{
		if (g_EffekseerManager == NULL)
		{
			return;
		}

		Vector3D axis(x, y, z);
		g_EffekseerManager->SetRotation(handle, axis, angle);
	}

	// エフェクト拡縮設定
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetScale(int handle, float x, float y, float z)
	{
		if (g_EffekseerManager == NULL)
		{
			return;
		}

		g_EffekseerManager->SetScale(handle, x, y, z);
	}

	// Specify the color of overall effect.
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetAllColor(int handle, int r, int g, int b, int a)
	{
		if (g_EffekseerManager == NULL)
		{
			return;
		}

		g_EffekseerManager->SetAllColor(handle, Effekseer::Color(r, g, b, a));
	}

	// エフェクトのターゲット位置設定
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetTargetLocation(int handle, float x, float y, float z)
	{
		if (g_EffekseerManager == NULL)
		{
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

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSendTrigger(int handle, int32_t index)
	{
		if (g_EffekseerManager == nullptr)
		{
			return;
		}

		g_EffekseerManager->SendTrigger(handle, index);
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
		if (renderId >= 0 && renderId < MAX_RENDER_PATH)
		{
			Array2Matrix(renderSettings[renderId].projectionMatrix, matrixArray);
			renderSettings[renderId].stereoEnabled = false;
		}
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetCameraMatrix(int renderId, float matrixArray[])
	{
		if (renderId >= 0 && renderId < MAX_RENDER_PATH)
		{
			Array2Matrix(renderSettings[renderId].cameraMatrix, matrixArray);
			renderSettings[renderId].stereoEnabled = false;
		}
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetStereoRenderingMatrix(
		int renderId, int renderType, float camCenterMat[], float projMatL[], float projMatR[], float camMatL[], float camMatR[])
	{
		if (renderId >= 0 && renderId < MAX_RENDER_PATH)
		{
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
		if (renderId >= 0 && renderId < MAX_RENDER_PATH)
		{
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

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetRenderTargetProperty(int renderId,
																					 Effekseer::Backend::TextureFormatType renderTarget,
																					 Effekseer::Backend::TextureFormatType depthTarget,
																					 int width,
																					 int height)
	{
		if (renderId >= 0 && renderId < MAX_RENDER_PATH)
		{
			renderSettings[renderId].renderTargetType = renderTarget;
			renderSettings[renderId].depthTargetType = depthTarget;
			renderSettings[renderId].screenWidth = width;
			renderSettings[renderId].screenHeight = height;
		}
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetTextureLoaderEvent(TextureLoaderLoad load,
																				   TextureLoaderUnload unload,
																				   GetUnityIDFromPath getUnityId)
	{
		if (g_EffekseerManager == nullptr)
		{
			return;
		}

		if (g_graphics == nullptr)
		{
			return;
		}

		g_EffekseerManager->SetTextureLoader(g_graphics->Create(load, unload, getUnityId));
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetModelLoaderEvent(ModelLoaderLoad load,
																				 ModelLoaderUnload unload,
																				 GetUnityIDFromPath getUnityId)
	{
		if (g_EffekseerManager == nullptr)
		{
			return;
		}

		if (g_graphics == nullptr)
		{
			return;
		}

		g_EffekseerManager->SetModelLoader(g_graphics->Create(load, unload, getUnityId));
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetMaterialLoaderEvent(MaterialLoaderLoad load,
																					MaterialLoaderUnload unload,
																					GetUnityIDFromPath getUnityId)
	{
		if (g_EffekseerManager == nullptr)
		{
			return;
		}

		if (g_graphics == nullptr)
		{
			return;
		}

		g_EffekseerManager->SetMaterialLoader(g_graphics->Create(load, unload, getUnityId));
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetProceduralModelGeneratorEvent(ProceduralModelGeneratorGenerate load,
																							  ProceduralModelGeneratorUngenerate unload)
	{
		if (g_EffekseerManager == nullptr)
		{
			return;
		}

		if (g_graphics == nullptr)
		{
			return;
		}

		auto generator = g_graphics->Create(load, unload);
		if (generator != nullptr)
		{
			g_EffekseerManager->GetSetting()->SetProceduralMeshGenerator(generator);
		}
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetSoundLoaderEvent(SoundLoaderLoad load,
																				 SoundLoaderUnload unload,
																				 GetUnityIDFromPath getUnityId)
	{
		if (g_EffekseerManager == NULL)
		{
			return;
		}

		g_EffekseerManager->SetSoundLoader(EffekseerPlugin::SoundLoader::Create(load, unload, getUnityId));
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetSoundPlayerEvent(SoundPlayerPlay play,
																				 SoundPlayerStopTag stopTag,
																				 SoundPlayerPauseTag pauseTag,
																				 SoundPlayerCheckPlayingTag checkPlayingTag,
																				 SoundPlayerStopAll stopAll)
	{
		if (g_EffekseerManager == NULL)
		{
			return;
		}

		if (play && stopTag && pauseTag && checkPlayingTag && stopAll)
		{
			g_EffekseerManager->SetSoundPlayer(EffekseerPlugin::SoundPlayer::Create(play, stopTag, pauseTag, checkPlayingTag, stopAll));
		}
		else
		{
			g_EffekseerManager->SetSoundPlayer(nullptr);
		}
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetCurveLoaderEvent(CurveLoaderLoad load,
																				 CurveLoaderUnload unload,
																				 GetUnityIDFromPath getUnityId)
	{
		if (g_EffekseerManager == nullptr)
		{
			return;
		}

		g_EffekseerManager->SetCurveLoader(Effekseer::MakeRefPtr<EffekseerPlugin::CurveLoader>(load, unload, getUnityId));
	}

	std::shared_ptr<RenderThreadEvent> RenderThreadEvent::instance_;

	RenderThreadEvent::~RenderThreadEvent() { Execute(); }

	void RenderThreadEvent::Initialize() { instance_ = std::make_shared<RenderThreadEvent>(); }

	void RenderThreadEvent::Terminate() { instance_ = nullptr; }

	std::shared_ptr<RenderThreadEvent> RenderThreadEvent::GetInstance() { return instance_; }

	void RenderThreadEvent::AddEvent(const std::function<void()>& e)
	{
		std::lock_guard<std::mutex> lock(mtx_);
		events_.emplace_back(e);
	}

	void RenderThreadEvent::Execute()
	{
		std::lock_guard<std::mutex> lock(mtx_);

		for (const auto& e : events_)
		{
			e();
		}
		events_.clear();
	}

	std::shared_ptr<MultiThreadedEffekseerManager> MultiThreadedEffekseerManager::instance_;

	void MultiThreadedEffekseerManager::PushCommand(const MultiThreadedEffekseerManager::Command& cmd)
	{
		std::lock_guard<std::mutex> lock(mtx_);
		commands_.emplace_back(cmd);
	}

	MultiThreadedEffekseerManager::MultiThreadedEffekseerManager(int maxInstances) { manager_ = Effekseer::Manager::Create(maxInstances); }

	MultiThreadedEffekseerManager::~MultiThreadedEffekseerManager()
	{
		for (const auto& cmd : commands_)
		{
			if (cmd.Type == CommandType::Play)
			{
				::Effekseer::RefPtr<::Effekseer::Effect>::Unpin(cmd.Play.EffectPtr);
			}
		}
		commands_.clear();
	}

	void MultiThreadedEffekseerManager::Apply()
	{
		{
			std::lock_guard<std::mutex> lock(mtx_);
			threadCommands_ = commands_;
			commands_.clear();
		}

		for (const auto& cmd : threadCommands_)
		{
			if (cmd.Type == CommandType::Update)
			{
				Effekseer::Manager::UpdateParameter param;
				param.DeltaFrame = cmd.Update.DeltaFrame;
				param.UpdateInterval = 1.0f;
				manager_->Update(param);
			}
			else if (cmd.Type == CommandType::Play)
			{
				auto effect = ::Effekseer::RefPtr<::Effekseer::Effect>::FromPinned(cmd.Play.EffectPtr);
				auto eid = manager_->Play(effect, cmd.Play.Position[0], cmd.Play.Position[1], cmd.Play.Position[2]);
				internalHandleToHandleInternal_[cmd.Handle] = eid;

				::Effekseer::RefPtr<::Effekseer::Effect>::Unpin(cmd.Play.EffectPtr);
			}
			else if (cmd.Type == CommandType::SetTimeScaleByGroup)
			{
				manager_->SetTimeScaleByGroup(cmd.SetTimeScaleByGroup.GroupMask, cmd.SetTimeScaleByGroup.TimeScale);
			}
			else if (cmd.Type == CommandType::StopAllEffects)
			{
				manager_->StopAllEffects();
			}
			else if (cmd.Type == CommandType::SetPausedToAllEffects)
			{
				manager_->SetPausedToAllEffects(cmd.BoolValue.Value);
			}
			else
			{
				auto it = internalHandleToHandleInternal_.find(cmd.Handle);
				if (it != internalHandleToHandleInternal_.end())
				{
					if (cmd.Type == CommandType::UpdateHandle)
					{
						manager_->UpdateHandle(it->second, cmd.FloatValue.Value);
					}
					else if (cmd.Type == CommandType::UpdateHandleToMoveToFrame)
					{
						manager_->UpdateHandleToMoveToFrame(it->second, cmd.FloatValue.Value);
					}
					else if (cmd.Type == CommandType::Stop)
					{
						manager_->StopEffect(it->second);
					}
					else if (cmd.Type == CommandType::StopRoot)
					{
						manager_->StopRoot(it->second);
					}
					else if (cmd.Type == CommandType::SendTrigger)
					{
						manager_->SendTrigger(it->second, cmd.IntValue.Value);
					}
					else if (cmd.Type == CommandType::SetVisibility)
					{
						manager_->SetShown(it->second, cmd.BoolValue.Value);
					}
					else if (cmd.Type == CommandType::SetPause)
					{
						manager_->SetPaused(it->second, cmd.BoolValue.Value);
					}
					else if (cmd.Type == CommandType::SetSpeed)
					{
						manager_->SetSpeed(it->second, cmd.FloatValue.Value);
					}
					else if (cmd.Type == CommandType::SetPosition)
					{
						manager_->SetLocation(
							it->second, {cmd.FloatArrayValue.Values[0], cmd.FloatArrayValue.Values[1], cmd.FloatArrayValue.Values[2]});
					}
					else if (cmd.Type == CommandType::SetRotation)
					{
						manager_->SetRotation(
							it->second, cmd.FloatArrayValue.Values[0], cmd.FloatArrayValue.Values[1], cmd.FloatArrayValue.Values[2]);
					}
					else if (cmd.Type == CommandType::SetScale)
					{
						manager_->SetScale(
							it->second, cmd.FloatArrayValue.Values[0], cmd.FloatArrayValue.Values[1], cmd.FloatArrayValue.Values[2]);
					}
					else if (cmd.Type == CommandType::SetTargetLocation)
					{
						manager_->SetTargetLocation(
							it->second, cmd.FloatArrayValue.Values[0], cmd.FloatArrayValue.Values[1], cmd.FloatArrayValue.Values[2]);
					}
					else if (cmd.Type == CommandType::SetColor)
					{
						manager_->SetAllColor(it->second,
											  {static_cast<uint8_t>(cmd.IntArrayValue.Values[0]),
											   static_cast<uint8_t>(cmd.IntArrayValue.Values[1]),
											   static_cast<uint8_t>(cmd.IntArrayValue.Values[2]),
											   static_cast<uint8_t>(cmd.IntArrayValue.Values[3])});
					}
					else if (cmd.Type == CommandType::SetDynamicInput)
					{
						manager_->SetDynamicInput(it->second, cmd.FloatValueIndex.Index, cmd.FloatValueIndex.Value);
					}
					else if (cmd.Type == CommandType::SetLayer)
					{
						manager_->SetLayer(it->second, cmd.IntValue.Value);
					}
					else if (cmd.Type == CommandType::SetGroupMask)
					{
						manager_->SetGroupMask(it->second, cmd.Int64Value.Value);
					}
				}
			}
		}

		{
			std::lock_guard<std::mutex> lock(mtx_);

			restInstanceCount_ = manager_->GetRestInstancesCount();
			cameraCullingMaskToShowAllEffects_ = manager_->GetCameraCullingMaskToShowAllEffects();

			for (const auto& kv : internalHandleToHandleInternal_)
			{
				if (!manager_->Exists(kv.second))
				{
					removingIds_.emplace_back(kv.first);
				}

				internalHandleStates_[kv.first].InstanceCount = manager_->GetInstanceCount(kv.second);
			}

			for (const auto& id : removingIds_)
			{
				internalHandleStates_.erase(id);
				internalHandleToHandleInternal_.erase(id);
			}

			removingIds_.clear();
		}
	}

	void MultiThreadedEffekseerManager::Update(float deltaFrame)
	{
		Command cmd;
		cmd.Type = CommandType::Update;
		cmd.Update.DeltaFrame = deltaFrame;
		PushCommand(cmd);
	}

	void MultiThreadedEffekseerManager::StopAllEffects()
	{
		Command cmd;
		cmd.Type = CommandType::StopAllEffects;
		PushCommand(cmd);
	}

	void MultiThreadedEffekseerManager::SetPausedToAllEffects(bool paused)
	{
		Command cmd;
		cmd.Type = CommandType::SetPausedToAllEffects;
		cmd.BoolValue.Value = paused;
		PushCommand(cmd);

		{
			std::lock_guard<std::mutex> lock(mtx_);
			for (auto& it : internalHandleStates_)
			{
				it.second.Paused = paused;
			}
		}
	}

	void MultiThreadedEffekseerManager::UpdateHandle(int handle, float deltaFrame)
	{
		Command cmd;
		cmd.Type = CommandType::UpdateHandle;
		cmd.FloatValue.Value = deltaFrame;
		PushCommand(cmd);
	}

	void MultiThreadedEffekseerManager::UpdateHandleToMoveToFrame(int handle, float frame)
	{
		Command cmd;
		cmd.Type = CommandType::UpdateHandleToMoveToFrame;
		cmd.FloatValue.Value = frame;
		PushCommand(cmd);
	}

	int32_t MultiThreadedEffekseerManager::PlayEffect(void* effectPtr, float x, float y, float z)
	{
		if (effectPtr == nullptr)
		{
			return -1;
		}

		auto pinned = Effekseer::EffectRef::FromPinned(effectPtr);
		auto p = pinned.Pin();

		auto handle = nextInternalHandle_;

		{
			std::lock_guard<std::mutex> lock(mtx_);
			auto state = EffectState();
			state.DynamicInputs = pinned->GetDefaultDynamicInputs();
			internalHandleStates_[handle] = state;
		}

		nextInternalHandle_++;

		Command cmd;
		cmd.Type = CommandType::Play;
		cmd.Handle = handle;
		cmd.Play.EffectPtr = p;
		cmd.Play.Position[0] = x;
		cmd.Play.Position[1] = y;
		cmd.Play.Position[2] = z;
		PushCommand(cmd);
		return handle;
	}

	void MultiThreadedEffekseerManager::StopEffect(int32_t handle)
	{
		Command cmd;
		cmd.Type = CommandType::Stop;
		cmd.Handle = handle;
		PushCommand(cmd);
	}

	void MultiThreadedEffekseerManager::StopRootEffect(int32_t handle)
	{
		Command cmd;
		cmd.Type = CommandType::StopRoot;
		cmd.Handle = handle;
		PushCommand(cmd);
	}

	void MultiThreadedEffekseerManager::SendTrigger(int32_t handle, int32_t index)
	{
		Command cmd;
		cmd.Type = CommandType::SendTrigger;
		cmd.Handle = handle;
		cmd.IntValue.Value = index;
		PushCommand(cmd);
	}

	void MultiThreadedEffekseerManager::SetVisibility(int32_t handle, bool visible)
	{
		Command cmd;
		cmd.Type = CommandType::SetVisibility;
		cmd.Handle = handle;
		cmd.BoolValue.Value = visible;
		PushCommand(cmd);

		{
			std::lock_guard<std::mutex> lock(mtx_);
			auto it = internalHandleStates_.find(handle);
			if (it != internalHandleStates_.end())
			{
				it->second.Visible = visible;
			}
		}
	}

	void MultiThreadedEffekseerManager::SetPaused(int32_t handle, bool paused)
	{
		Command cmd;
		cmd.Type = CommandType::SetPause;
		cmd.Handle = handle;
		cmd.BoolValue.Value = paused;
		PushCommand(cmd);

		{
			std::lock_guard<std::mutex> lock(mtx_);
			auto it = internalHandleStates_.find(handle);
			if (it != internalHandleStates_.end())
			{
				it->second.Paused = paused;
			}
		}
	}

	void MultiThreadedEffekseerManager::SetSpeed(int32_t handle, float speed)
	{
		Command cmd;
		cmd.Type = CommandType::SetSpeed;
		cmd.Handle = handle;
		cmd.FloatValue.Value = speed;
		PushCommand(cmd);

		{
			std::lock_guard<std::mutex> lock(mtx_);
			auto it = internalHandleStates_.find(handle);
			if (it != internalHandleStates_.end())
			{
				it->second.Speed = speed;
			}
		}
	}

	void MultiThreadedEffekseerManager::SetPosition(int32_t handle, float x, float y, float z)
	{
		Command cmd;
		cmd.Type = CommandType::SetPosition;
		cmd.Handle = handle;
		cmd.FloatArrayValue.Values[0] = x;
		cmd.FloatArrayValue.Values[1] = y;
		cmd.FloatArrayValue.Values[2] = z;
		PushCommand(cmd);
	}

	void MultiThreadedEffekseerManager::SetRotation(int32_t handle, float x, float y, float z)
	{
		Command cmd;
		cmd.Type = CommandType::SetRotation;
		cmd.Handle = handle;
		cmd.FloatArrayValue.Values[0] = x;
		cmd.FloatArrayValue.Values[1] = y;
		cmd.FloatArrayValue.Values[2] = z;
		PushCommand(cmd);
	}

	void MultiThreadedEffekseerManager::SetScale(int32_t handle, float x, float y, float z)
	{
		Command cmd;
		cmd.Type = CommandType::SetScale;
		cmd.Handle = handle;
		cmd.FloatArrayValue.Values[0] = x;
		cmd.FloatArrayValue.Values[1] = y;
		cmd.FloatArrayValue.Values[2] = z;
		PushCommand(cmd);
	}

	void MultiThreadedEffekseerManager::SetTargetLocation(int32_t handle, float x, float y, float z)
	{
		Command cmd;
		cmd.Type = CommandType::SetTargetLocation;
		cmd.Handle = handle;
		cmd.FloatArrayValue.Values[0] = x;
		cmd.FloatArrayValue.Values[1] = y;
		cmd.FloatArrayValue.Values[2] = z;
		PushCommand(cmd);
	}

	void MultiThreadedEffekseerManager::SetColor(int32_t handle, int r, int g, int b, int a)
	{
		Command cmd;
		cmd.Type = CommandType::SetColor;
		cmd.Handle = handle;
		cmd.IntArrayValue.Values[0] = r;
		cmd.IntArrayValue.Values[1] = g;
		cmd.IntArrayValue.Values[2] = b;
		cmd.IntArrayValue.Values[3] = a;
		PushCommand(cmd);
	}

	void MultiThreadedEffekseerManager::SetDynamicInput(int32_t handle, int index, float value)
	{
		Command cmd;
		cmd.Type = CommandType::SetDynamicInput;
		cmd.Handle = handle;
		cmd.FloatValueIndex.Index = index;
		cmd.FloatValueIndex.Value = value;
		PushCommand(cmd);

		{
			std::lock_guard<std::mutex> lock(mtx_);
			auto it = internalHandleStates_.find(handle);
			if (it != internalHandleStates_.end())
			{
				it->second.DynamicInputs[index] = value;
			}
		}
	}

	void MultiThreadedEffekseerManager::SetLayer(int32_t handle, int32_t layer)
	{
		Command cmd;
		cmd.Type = CommandType::SetLayer;
		cmd.Handle = handle;
		cmd.IntValue.Value = layer;
		PushCommand(cmd);
	}

	void MultiThreadedEffekseerManager::SetGroupMask(int32_t handle, int64_t groupMask)
	{
		Command cmd;
		cmd.Type = CommandType::SetGroupMask;
		cmd.Handle = handle;
		cmd.Int64Value.Value = groupMask;
		PushCommand(cmd);
	}

	void MultiThreadedEffekseerManager::SetTimeScaleByGroup(int64_t groupMask, float timeScale)
	{
		Command cmd;
		cmd.Type = CommandType::SetTimeScaleByGroup;
		cmd.SetTimeScaleByGroup.GroupMask = groupMask;
		cmd.SetTimeScaleByGroup.TimeScale = timeScale;
		PushCommand(cmd);
	}

	bool MultiThreadedEffekseerManager::Exists(int32_t handle)
	{
		std::lock_guard<std::mutex> lock(mtx_);
		return internalHandleStates_.find(handle) != internalHandleStates_.end();
	}

	bool MultiThreadedEffekseerManager::GetVisibility(int32_t handle)
	{
		std::lock_guard<std::mutex> lock(mtx_);
		auto it = internalHandleStates_.find(handle);
		if (it != internalHandleStates_.end())
		{
			return it->second.Visible;
		}
		return false;
	}

	bool MultiThreadedEffekseerManager::GetPaused(int32_t handle)
	{
		std::lock_guard<std::mutex> lock(mtx_);
		auto it = internalHandleStates_.find(handle);
		if (it != internalHandleStates_.end())
		{
			return it->second.Paused;
		}
		return false;
	}

	float MultiThreadedEffekseerManager::GetSpeed(int32_t handle)
	{
		std::lock_guard<std::mutex> lock(mtx_);
		auto it = internalHandleStates_.find(handle);
		if (it != internalHandleStates_.end())
		{
			return it->second.Speed;
		}
		return 0.0f;
	}

	int MultiThreadedEffekseerManager::GetInstanceCount(int32_t handle)
	{
		std::lock_guard<std::mutex> lock(mtx_);
		auto it = internalHandleStates_.find(handle);
		if (it != internalHandleStates_.end())
		{
			return it->second.InstanceCount;
		}
		return 0;
	}

	int MultiThreadedEffekseerManager::GetRestInstanceCount()
	{
		std::lock_guard<std::mutex> lock(mtx_);
		return restInstanceCount_;
	}

	int MultiThreadedEffekseerManager::GetCameraCullingMaskToShowAllEffects()
	{
		std::lock_guard<std::mutex> lock(mtx_);
		return cameraCullingMaskToShowAllEffects_;
	}

	float MultiThreadedEffekseerManager::GetDynamicInput(int32_t handle, int index)
	{
		std::lock_guard<std::mutex> lock(mtx_);
		auto it = internalHandleStates_.find(handle);
		if (it != internalHandleStates_.end())
		{
			return it->second.DynamicInputs[index];
		}
		return 0;
	}

	Effekseer::ManagerRef& MultiThreadedEffekseerManager::GetManager() { return manager_; }

	void MultiThreadedEffekseerManager::Initialize(int maxInstances)
	{
		instance_ = std::make_shared<MultiThreadedEffekseerManager>(maxInstances);
	}

	void MultiThreadedEffekseerManager::Terminate() { instance_ = nullptr; }

	std::shared_ptr<MultiThreadedEffekseerManager> MultiThreadedEffekseerManager::GetInstance() { return instance_; }
}
