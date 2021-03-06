
#pragma once

#include "../common/EffekseerPluginTexture.h"
#include "../unity/IUnityInterface.h"

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#include <Effekseer/Effekseer.h>
#else
#include <Effekseer.h>
#endif

#include <map>
#include <string>

namespace EffekseerRendererUnity
{

class Texture : public Effekseer::Backend::Texture
{
public:
	Texture(void* userData) : UserData(userData) {}
	virtual ~Texture() = default;

	void* UserData = nullptr;
};

class TextureLoader : public Effekseer::TextureLoader
{
protected:
	EffekseerPlugin::TextureLoaderLoad load;
	EffekseerPlugin::TextureLoaderUnload unload;
	EffekseerPlugin::GetUnityIDFromPath getUnityId_;

	std::map<Effekseer::TextureRef, void*> textureData2NativePtr;
	EffekseerPlugin::IDtoResourceTable<Effekseer::TextureRef> id2Texture_;

public:
	static Effekseer::RefPtr<Effekseer::TextureLoader> Create(EffekseerPlugin::TextureLoaderLoad load,
															  EffekseerPlugin::TextureLoaderUnload unload,
															  EffekseerPlugin::GetUnityIDFromPath getUnityId);
	TextureLoader(EffekseerPlugin::TextureLoaderLoad load,
				  EffekseerPlugin::TextureLoaderUnload unload,
				  EffekseerPlugin::GetUnityIDFromPath getUnityId)
		: load(load), unload(unload), getUnityId_(getUnityId)
	{
	}
	~TextureLoader() override = default;
	Effekseer::TextureRef Load(const EFK_CHAR* path, Effekseer::TextureType textureType) override;
	void Unload(Effekseer::TextureRef source) override;
};
}; // namespace EffekseerRendererUnity
