#include "EffekseerPluginSound.h"
#include <algorithm>

namespace EffekseerPlugin
{
Effekseer::SoundDataRef SoundLoader::Load(const EFK_CHAR* path)
{
	// リソーステーブルを検索して存在したらそれを使う
	auto it = resources.find((const char16_t*)path);
	if (it != resources.end())
	{
		it->second.referenceCount++;
		return it->second.soundID;
	}

	// Unityでロード
	SoundResource res;
	res.soundID = Effekseer::MakeRefPtr<SoundData>(load((const char16_t*)path));
	if (res.soundID == 0)
	{
		return 0;
	}

	// リソーステーブルに追加
	resources.insert(std::make_pair((const char16_t*)path, res));

	return res.soundID;
}

void SoundLoader::Unload(Effekseer::SoundDataRef source)
{
	if (source == nullptr)
	{
		return;
	}

	// アンロードするモデルを検索
	auto it = std::find_if(resources.begin(), resources.end(), [source](const std::pair<std::u16string, SoundResource>& pair) {
		return pair.second.soundID == source;
	});
	if (it == resources.end())
	{
		return;
	}

	// 参照カウンタが0になったら実際にアンロード
	it->second.referenceCount--;
	if (it->second.referenceCount <= 0)
	{
		unload(it->first.c_str());
		resources.erase(it);
	}
}

Effekseer::SoundLoaderRef SoundLoader::Create(SoundLoaderLoad load, SoundLoaderUnload unload)
{
	return Effekseer::MakeRefPtr<SoundLoader>(load, unload);
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