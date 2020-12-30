
#pragma once

#include "../unity/IUnityInterface.h"
#include "EffekseerPluginCommon.h"
#include "MemoryFile.h"

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#include <Effekseer/Effekseer.h>
#else
#include <Effekseer.h>
#endif

#include <map>
#include <memory>
#include <mutex>
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

class LazyMaterialData;

/**
	@vrief	an event queue of material
	@note
	be careful
*/
class MaterialEvent
{
private:
	enum class CommandType
	{
		Load,
		UnloadAndDelete,
	};

	struct Command
	{
		CommandType type = CommandType::Load;
		Effekseer::RefPtr<LazyMaterialData> data = nullptr;
	};

	std::mutex mtx_;
	Effekseer::CustomVector<Command> commands_;

	static std::shared_ptr<MaterialEvent> instance_;

public:
	static void Initialize();

	static void Terminate();

	static std::shared_ptr<MaterialEvent> GetInstance();

	void Load(Effekseer::RefPtr<LazyMaterialData> data);

	void UnloadAndDelete(Effekseer::RefPtr<LazyMaterialData> data);

	void Execute();
};

/**
	@brief a holder class to make loader shared_ptr
*/
class MaterialLoaderHolder
{
private:
	Effekseer::MaterialLoaderRef internalLoader_;

public:
	MaterialLoaderHolder(Effekseer::MaterialLoaderRef loader) { internalLoader_ = loader; }

	Effekseer::MaterialLoaderRef Get() const { return internalLoader_; }
};

class MaterialLoader : public Effekseer::MaterialLoader
{
	MaterialLoaderLoad load_ = nullptr;
	MaterialLoaderUnload unload_ = nullptr;

	struct MaterialResource
	{
		int referenceCount = 1;
		Effekseer::RefPtr<LazyMaterialData> internalData;
	};
	std::map<std::u16string, MaterialResource> resources;
	MemoryFile memoryFile_;
	MemoryFile memoryFileForCache_;
	std::shared_ptr<MaterialLoaderHolder> internalLoader_;

public:
	MaterialLoader(MaterialLoaderLoad load, MaterialLoaderUnload unload);

	virtual ~MaterialLoader() = default;
	Effekseer::MaterialRef Load(const EFK_CHAR* path) override;
	void Unload(Effekseer::MaterialRef data) override;
	void SetInternalLoader(const std::shared_ptr<MaterialLoaderHolder>& loader) { internalLoader_ = loader; }
};

class LazyMaterialData : public Effekseer::Material
{
private:
    Effekseer::MaterialRef internalData_ = nullptr;

    Effekseer::CustomVector<uint8_t> data_;
    Effekseer::CustomVector<uint8_t> compiledData_;
    std::shared_ptr<MaterialLoaderHolder> internalLoader_ = nullptr;

public:
    LazyMaterialData(const std::shared_ptr<MaterialLoaderHolder>& loader,
                     const Effekseer::CustomVector<uint8_t>& data,
                     int32_t dataSize,
                     const Effekseer::CustomVector<uint8_t>& compiledData,
                     int32_t compiledDataSize);

    void Load();

    void Unload();
};

} // namespace EffekseerPlugin
