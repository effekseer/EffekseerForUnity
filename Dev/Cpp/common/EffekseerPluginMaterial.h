
#pragma once

#include "../unity/IUnityInterface.h"
#include "EffekseerPluginCommon.h"
#include "MemoryFile.h"
#include <Effekseer.h>
#include <map>
#include <memory>
#include <string>

namespace EffekseerPlugin
{
using MaterialLoaderLoad = void*(UNITY_INTERFACE_API*)(const char16_t* path,
													   void* materialBuffer,
													   int materialBufferSize,
													   int& requiredMaterialBufferSize,
													   void* cachedMaterialBuffer,
													   int cachedMaterialBufferSize,
													   int& requiredCachedMaterialBufferSize);

using MaterialLoaderUnload = void(UNITY_INTERFACE_API*)(const char16_t* path, void* materialPointer);

class MaterialLoader : public Effekseer::MaterialLoader
{
	MaterialLoaderLoad load_ = nullptr;
	MaterialLoaderUnload unload_ = nullptr;

	struct MaterialResource
	{
		int referenceCount = 1;
		Effekseer::MaterialData* internalData;
	};
	std::map<std::u16string, MaterialResource> resources;
	MemoryFile memoryFile_;
	MemoryFile memoryFileForCache_;
	std::unique_ptr<Effekseer::MaterialLoader> internalLoader;

public:
	MaterialLoader(MaterialLoaderLoad load, MaterialLoaderUnload unload);

	virtual ~MaterialLoader() = default;
	Effekseer::MaterialData* Load(const EFK_CHAR* path) override;
	void Unload(Effekseer::MaterialData* data) override;
	void SetInternalLoader(Effekseer::MaterialLoader* loader) { internalLoader.reset(loader); }
};

} // namespace EffekseerPlugin
