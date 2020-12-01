
#pragma once

#include "../common/EffekseerPluginModel.h"
#include "../unity/IUnityInterface.h"

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#include <Effekseer/Effekseer.h>
#else
#include <Effekseer.h>
#endif

#include <map>
#include <memory>
#include <string>
#include <vector>

namespace EffekseerRendererUnity
{
class ModelLoader : public Effekseer::ModelLoader
{
	EffekseerPlugin::ModelLoaderLoad load;
	EffekseerPlugin::ModelLoaderUnload unload;

	struct ModelResource
	{
		int referenceCount = 1;
		Effekseer::Model* internalData;
	};
	std::map<std::u16string, ModelResource> resources;
	std::vector<uint8_t> internalBuffer;

public:
	ModelLoader(EffekseerPlugin::ModelLoaderLoad load, EffekseerPlugin::ModelLoaderUnload unload);

	virtual ~ModelLoader() = default;
	virtual Effekseer::Model* Load(const EFK_CHAR* path);
	virtual void Unload(Effekseer::Model* source);
};

} // namespace EffekseerRendererUnity
