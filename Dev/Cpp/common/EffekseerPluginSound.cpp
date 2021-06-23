#include "EffekseerPluginSound.h"
#include <algorithm>

namespace EffekseerPlugin
{

Effekseer::SoundDataRef SoundLoader::Load(const EFK_CHAR* path)
{
	auto id = getUnityId_(path);

	Effekseer::SoundDataRef generated;

	if (id2Obj_.TryLoad(id, generated))
	{
		return generated;
	}

	auto nativePtr = load((const char16_t*)path);
	auto data = Effekseer::MakeRefPtr<SoundData>(nativePtr);

	id2Obj_.Register(id, data, (void*)nativePtr);

	return data;
}

void SoundLoader::Unload(Effekseer::SoundDataRef source)
{
	if (source == nullptr)
	{
		return;
	}

	int32_t id{};
	void* nativePtr{};
	if (id2Obj_.Unload(source, id, nativePtr))
	{
		unload(source.DownCast<SoundData>()->UserData);
	}
}

Effekseer::SoundLoaderRef SoundLoader::Create(SoundLoaderLoad load, SoundLoaderUnload unload, GetUnityIDFromPath getUnityId)
{
	return Effekseer::MakeRefPtr<SoundLoader>(load, unload, getUnityId);
}

Effekseer::SoundPlayerRef SoundPlayer::Create(SoundPlayerPlay play,
											  SoundPlayerStopTag stopTag,
											  SoundPlayerPauseTag pauseTag,
											  SoundPlayerCheckPlayingTag checkPlayingTag,
											  SoundPlayerStopAll stopAll)
{
	return Effekseer::MakeRefPtr<SoundPlayer>(play, stopTag, pauseTag, checkPlayingTag, stopAll);
}
} // namespace EffekseerPlugin