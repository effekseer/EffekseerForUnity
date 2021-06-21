
#ifndef __EFFEKSEER_PLUGIN_SOUND_H__
#define __EFFEKSEER_PLUGIN_SOUND_H__

#include <map>
#include <string>

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#include <Effekseer/Effekseer.h>
#else
#include <Effekseer.h>
#endif

#include "../unity/IUnityInterface.h"
#include "EffekseerPluginCommon.h"

namespace EffekseerPlugin
{
using SoundTag = Effekseer::SoundTag;
using SoundHandle = Effekseer::SoundHandle;

using SoundLoaderLoad = uintptr_t(UNITY_INTERFACE_API*)(const char16_t* path);
using SoundLoaderUnload = void(UNITY_INTERFACE_API*)(const uintptr_t nativePtr);

class SoundData : public Effekseer::SoundData
{
private:
public:
	SoundData(uintptr_t userData) : UserData(userData) {}
	virtual ~SoundData() = default;

	uintptr_t UserData = 0;
};

class SoundLoader : public Effekseer::SoundLoader
{
	SoundLoaderLoad load;
	SoundLoaderUnload unload;
	GetUnityIDFromPath getUnityId_;

	IDtoResourceTable<Effekseer::SoundDataRef> id2Obj_;

public:
	static Effekseer::SoundLoaderRef Create(SoundLoaderLoad load, SoundLoaderUnload unload, GetUnityIDFromPath getUnityId);
	SoundLoader(SoundLoaderLoad load, SoundLoaderUnload unload, GetUnityIDFromPath getUnityId) : load(load), unload(unload), getUnityId_(getUnityId) {}
	virtual ~SoundLoader() override = default;
	virtual Effekseer::SoundDataRef Load(const EFK_CHAR* path) override;
	virtual void Unload(Effekseer::SoundDataRef source) override;
};

using SoundPlayerPlay = void(UNITY_INTERFACE_API*)(
	SoundTag tag, uintptr_t Data, float Volume, float Pan, float Pitch, bool Mode3D, float x, float y, float z, float Distance);
using SoundPlayerStopTag = void(UNITY_INTERFACE_API*)(SoundTag tag);
using SoundPlayerPauseTag = void(UNITY_INTERFACE_API*)(SoundTag tag, bool pause);
using SoundPlayerCheckPlayingTag = bool(UNITY_INTERFACE_API*)(SoundTag tag);
using SoundPlayerStopAll = void(UNITY_INTERFACE_API*)();

class SoundPlayer : public Effekseer::SoundPlayer
{
	SoundPlayerPlay play;
	SoundPlayerStopTag stopTag;
	SoundPlayerPauseTag pauseTag;
	SoundPlayerCheckPlayingTag checkPlayingTag;
	SoundPlayerStopAll stopAll;

public:
	static Effekseer::SoundPlayerRef Create(SoundPlayerPlay play,
											SoundPlayerStopTag stopTag,
											SoundPlayerPauseTag pauseTag,
											SoundPlayerCheckPlayingTag checkPlayingTag,
											SoundPlayerStopAll stopAll);
	SoundPlayer(SoundPlayerPlay play,
				SoundPlayerStopTag stopTag,
				SoundPlayerPauseTag pauseTag,
				SoundPlayerCheckPlayingTag checkPlayingTag,
				SoundPlayerStopAll stopAll)
		: play(play), stopTag(stopTag), pauseTag(pauseTag), checkPlayingTag(checkPlayingTag), stopAll(stopAll)
	{
	}
	virtual ~SoundPlayer() {}
	virtual SoundHandle Play(SoundTag tag, const InstanceParameter& parameter)
	{
		auto sd = reinterpret_cast<SoundData*>(parameter.Data.Get());

		play(tag,
			 (uintptr_t)sd->UserData,
			 parameter.Volume,
			 parameter.Pan,
			 parameter.Pitch,
			 parameter.Mode3D,
			 parameter.Position.X,
			 parameter.Position.Y,
			 parameter.Position.Z,
			 parameter.Distance);
		return 0;
	}
	virtual void Stop(SoundHandle handle, SoundTag tag) {}
	virtual void Pause(SoundHandle handle, SoundTag tag, bool pause) {}
	virtual bool CheckPlaying(SoundHandle handle, SoundTag tag) { return false; }
	virtual void StopTag(SoundTag tag) { stopTag(tag); }
	virtual void PauseTag(SoundTag tag, bool pause) { pauseTag(tag, pause); }
	virtual bool CheckPlayingTag(SoundTag tag) { return checkPlayingTag(tag); }
	virtual void StopAll() { stopAll(); }
};
} // namespace EffekseerPlugin

#endif
