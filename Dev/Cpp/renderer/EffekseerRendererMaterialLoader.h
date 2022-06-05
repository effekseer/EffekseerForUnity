
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
	EffekseerPlugin::GetUnityIDFromPath getUnityId_;

	EffekseerPlugin::MemoryFile memoryFile_;
	EffekseerPlugin::MemoryFile memoryFileForCache_;
	EffekseerPlugin::IDtoResourceTable<Effekseer::MaterialRef> id2Obj_;

public:
	MaterialLoader(EffekseerPlugin::MaterialLoaderLoad load,
				   EffekseerPlugin::MaterialLoaderUnload unload,
				   EffekseerPlugin::GetUnityIDFromPath getUnityId);

	virtual ~MaterialLoader();
	Effekseer::MaterialRef Load(const EFK_CHAR* path) override;
	void Unload(Effekseer::MaterialRef data) override;
};

} // namespace EffekseerRendererUnity
