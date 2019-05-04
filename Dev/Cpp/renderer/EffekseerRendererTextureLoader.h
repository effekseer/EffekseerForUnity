
#pragma once

#include "../common/EffekseerPluginTexture.h"
#include "../unity/IUnityInterface.h"
#include <Effekseer.h>
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
		Effekseer::TextureData* textureDataPtr = nullptr;
	};
	std::map<std::u16string, TextureResource> resources;
	std::map<void*, void*> textureData2NativePtr;

public:
	static TextureLoader* Create(EffekseerPlugin::TextureLoaderLoad load, EffekseerPlugin::TextureLoaderUnload unload);
	TextureLoader(EffekseerPlugin::TextureLoaderLoad load, EffekseerPlugin::TextureLoaderUnload unload) : load(load), unload(unload) {}
	virtual ~TextureLoader() {}
	Effekseer::TextureData* Load(const EFK_CHAR* path, Effekseer::TextureType textureType) override;
	void Unload(Effekseer::TextureData* source) override;
};
}; // namespace EffekseerRendererUnity
