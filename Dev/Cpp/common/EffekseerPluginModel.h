
#ifndef __EFFEKSEER_PLUGIN_MODEL_H__
#define __EFFEKSEER_PLUGIN_MODEL_H__

#include <map>
#include <memory>
#include <string>

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#include <Effekseer/Effekseer.h>
#else
#include <Effekseer.h>
#endif

#include "../unity/IUnityInterface.h"
#include "EffekseerPluginCommon.h"
#include "MemoryFile.h"

namespace EffekseerPlugin
{
using ModelLoaderLoad = void*(UNITY_INTERFACE_API*)(const char16_t* path, void* data, int dataSize, int& requiredDataSize);
using ModelLoaderUnload = void(UNITY_INTERFACE_API*)(int id, void* modelPointer);

class ModelLoader : public Effekseer::ModelLoader
{
	ModelLoaderLoad load;
	ModelLoaderUnload unload;
	GetUnityIDFromPath getUnityId_;

	IDtoResourceTable<Effekseer::ModelRef> id2Obj_;

	Effekseer::RefPtr<MemoryFile> memoryFile;

	Effekseer::RefPtr<Effekseer::ModelLoader> internalLoader;

public:
	static Effekseer::RefPtr<Effekseer::ModelLoader> Create(ModelLoaderLoad load, ModelLoaderUnload unload, GetUnityIDFromPath getUnityId);

	ModelLoader(ModelLoaderLoad load, ModelLoaderUnload unload, GetUnityIDFromPath getUnityId);

	virtual ~ModelLoader() = default;
	virtual Effekseer::ModelRef Load(const char16_t* path) override;
	virtual void Unload(Effekseer::ModelRef source) override;

	Effekseer::FileInterfaceRef GetFileInterface() { return memoryFile; }
	void SetInternalLoader(Effekseer::RefPtr<Effekseer::ModelLoader> loader) { internalLoader = loader; }
};

class ProceduralModelGenerator : public Effekseer::ProceduralModelGenerator
{
public:
	ProceduralModelGenerator() = default;
	~ProceduralModelGenerator() override = default;
	void Ungenerate(Effekseer::ModelRef model) override;
};
} // namespace EffekseerPlugin

#endif
