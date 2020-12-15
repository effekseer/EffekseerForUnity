
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
class TextureLoader : public Effekseer::TextureLoader
{
protected:
	EffekseerPlugin::TextureLoaderLoad load;
	EffekseerPlugin::TextureLoaderUnload unload;

	struct TextureResource
	{
		int referenceCount = 1;
		Effekseer::TextureRef textureDataPtr = nullptr;
	};
	std::map<std::u16string, TextureResource> resources;
	std::map<void*, void*> textureData2NativePtr;

public:
	static Effekseer::RefPtr<Effekseer::TextureLoader> Create(EffekseerPlugin::TextureLoaderLoad load, EffekseerPlugin::TextureLoaderUnload unload);
	TextureLoader(EffekseerPlugin::TextureLoaderLoad load, EffekseerPlugin::TextureLoaderUnload unload) : load(load), unload(unload) {}
	~TextureLoader() override = default;
	Effekseer::TextureRef Load(const EFK_CHAR* path, Effekseer::TextureType textureType) override;
	void Unload(Effekseer::TextureRef source) override;
};
}; // namespace EffekseerRendererUnity
