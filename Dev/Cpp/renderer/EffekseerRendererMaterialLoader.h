
#pragma once

#include "../common/EffekseerPluginMaterial.h"
#include "../unity/IUnityInterface.h"
#include <Effekseer.h>
#include <map>
#include <memory>
#include <string>
#include <vector>

namespace EffekseerRendererUnity
{


class MaterialLoader : public Effekseer::MaterialLoader
{
	EffekseerPlugin::MaterialLoaderLoad load_ = nullptr;
	EffekseerPlugin::MaterialLoaderUnload unload_ = nullptr;

	struct MaterialResource
	{
		int referenceCount = 1;
		Effekseer::MaterialRef internalData;
	};
	std::map<std::u16string, MaterialResource> resources;
	EffekseerPlugin::MemoryFile memoryFile_;
	EffekseerPlugin::MemoryFile memoryFileForCache_;

public:
	MaterialLoader(EffekseerPlugin::MaterialLoaderLoad load, EffekseerPlugin::MaterialLoaderUnload unload);

	virtual ~MaterialLoader();
	Effekseer::MaterialRef Load(const EFK_CHAR* path) override;
	void Unload(Effekseer::MaterialRef data) override;
};


} // namespace EffekseerRendererUnity
