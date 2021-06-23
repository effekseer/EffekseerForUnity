#include "EffekseerPluginMaterial.h"
#include <algorithm>
#include <assert.h>

namespace EffekseerPlugin
{

LazyMaterial::LazyMaterial(const std::shared_ptr<MaterialLoaderHolder>& loader,
								   const Effekseer::CustomVector<uint8_t>& data,
								   int32_t dataSize,
								   const Effekseer::CustomVector<uint8_t>& compiledData,
								   int32_t compiledDataSize)
	: internalLoader_(loader)
{
	if (dataSize > 0)
	{
		data_.assign(data.begin(), data.begin() + dataSize);
	}

	if (compiledDataSize > 0)
	{
		compiledData_.assign(compiledData.begin(), compiledData.begin() + compiledDataSize);
	}
}

void LazyMaterial::Load()
{
	if (compiledData_.size() > 0)
	{
		internalData_ = internalLoader_->Get()->Load(compiledData_.data(), compiledData_.size(), Effekseer::MaterialFileType::Compiled);
	}

	if (internalData_ == nullptr && data_.size() > 0)
	{
		internalData_ = internalLoader_->Get()->Load(data_.data(), data_.size(), Effekseer::MaterialFileType::Code);
	}

	data_.clear();
	data_.shrink_to_fit();

	compiledData_.clear();
	compiledData_.shrink_to_fit();

	if (internalData_ != nullptr)
	{
		this->ShadingModel = internalData_->ShadingModel;
		this->IsSimpleVertex = internalData_->IsSimpleVertex;
		this->IsRefractionRequired = internalData_->IsRefractionRequired;
		this->CustomData1 = internalData_->CustomData1;
		this->CustomData2 = internalData_->CustomData2;
		this->TextureCount = internalData_->TextureCount;
		this->UniformCount = internalData_->UniformCount;
		this->TextureWrapTypes = internalData_->TextureWrapTypes;
		this->UserPtr = internalData_->UserPtr;
		this->ModelUserPtr = internalData_->ModelUserPtr;
		this->RefractionUserPtr = internalData_->RefractionUserPtr;
		this->RefractionModelUserPtr = internalData_->RefractionModelUserPtr;
	}
}

void LazyMaterial::Unload()
{
	if (internalData_ != nullptr)
	{
		internalLoader_->Get()->Unload(internalData_);
		internalData_ = nullptr;
	}
}

std::shared_ptr<MaterialEvent> MaterialEvent::instance_;

void MaterialEvent::Initialize() { instance_ = std::make_shared<MaterialEvent>(); }

void MaterialEvent::Terminate()
{
	if (instance_ != nullptr)
	{
		instance_->Execute();
	}
	instance_ = nullptr;
}

std::shared_ptr<MaterialEvent> MaterialEvent::GetInstance() { return instance_; }

void MaterialEvent::Load(Effekseer::RefPtr<LazyMaterial> data)
{
	std::lock_guard<std::mutex> lock(mtx_);

	Command c;
	c.type = CommandType::Load;
	c.data = data;
	commands_.emplace_back(c);
}

void MaterialEvent::UnloadAndDelete(Effekseer::RefPtr<LazyMaterial> data)
{
	std::lock_guard<std::mutex> lock(mtx_);

	Command c;
	c.type = CommandType::UnloadAndDelete;
	c.data = data;
	commands_.emplace_back(c);
}

void MaterialEvent::Execute()
{
	std::lock_guard<std::mutex> lock(mtx_);

	for (auto& c : commands_)
	{
		if (c.type == CommandType::Load)
		{
			c.data->Load();
		}
		else if (c.type == CommandType::UnloadAndDelete)
		{
			c.data->Unload();
		}
		else
		{
			assert(0);
		}
	}

	commands_.clear();
}

MaterialLoader::MaterialLoader(MaterialLoaderLoad load, MaterialLoaderUnload unload, GetUnityIDFromPath getUnityId)
	: load_(load), unload_(unload), getUnityId_(getUnityId), memoryFile_(1 * 1024 * 1024), memoryFileForCache_(1 * 1024 * 1024)
{
}

Effekseer::MaterialRef MaterialLoader::Load(const EFK_CHAR* path)
{
	auto id = getUnityId_(path);

	Effekseer::RefPtr<LazyMaterial> generated;

	if (id2Obj_.TryLoad(id, generated))
	{
		return generated;
	}

	// Load with unity
	int requiredDataSize = 0;
	int requiredCachedDataSize = 0;

	auto materialPtr = load_((const char16_t*)path,
							 &memoryFile_.LoadedBuffer[0],
							 (int)memoryFile_.LoadedBuffer.size(),
							 requiredDataSize,
							 &memoryFileForCache_.LoadedBuffer[0],
							 (int)memoryFileForCache_.LoadedBuffer.size(),
							 requiredCachedDataSize);

	if (requiredDataSize == 0 && requiredCachedDataSize == 0)
	{
		// Failed to load
		return nullptr;
	}

	if (materialPtr == nullptr)
	{
		// Lack of memory
		memoryFileForCache_.Resize(requiredCachedDataSize);
		memoryFile_.Resize(requiredDataSize);

		// Load with unity
		materialPtr = load_((const char16_t*)path,
							&memoryFile_.LoadedBuffer[0],
							(int)memoryFile_.LoadedBuffer.size(),
							requiredDataSize,
							&memoryFileForCache_.LoadedBuffer[0],
							(int)memoryFileForCache_.LoadedBuffer.size(),
							requiredCachedDataSize);

		if (materialPtr == nullptr)
		{
			// Failed to load
			return nullptr;
		}
	}

	// try to load from caches
	if (requiredCachedDataSize > 0)
	{
		memoryFileForCache_.LoadedSize = static_cast<size_t>(requiredCachedDataSize);
	}

	// try to load from code
	if (requiredDataSize > 0)
	{
		memoryFile_.LoadedSize = static_cast<size_t>(requiredDataSize);
	}

	if (memoryFileForCache_.LoadedSize > 0 || memoryFile_.LoadedSize > 0)
	{
		auto data = Effekseer::MakeRefPtr<LazyMaterial>(internalLoader_,
															memoryFile_.LoadedBuffer,
															memoryFile_.LoadedSize,
															memoryFileForCache_.LoadedBuffer,
															memoryFileForCache_.LoadedSize);
		auto internalData = data;

		auto eventInstance = MaterialEvent::GetInstance();

		if (eventInstance != nullptr)
		{
			eventInstance->Load(data);
		}
		else
		{
			data->Load();
		}

		id2Obj_.Register(id, data, materialPtr);

		return internalData;
	}

	return nullptr;
}
void MaterialLoader::Unload(Effekseer::MaterialRef data)
{
	if (data == nullptr)
	{
		return;
	}

	auto ldata = data.DownCast<LazyMaterial>();
	int32_t id{};
	void* nativePtr{};
	if (id2Obj_.Unload(ldata, id, nativePtr))
	{
		auto eventInstance = MaterialEvent::GetInstance();

		if (eventInstance != nullptr)
		{
			eventInstance->UnloadAndDelete(data.DownCast<LazyMaterial>());
		}
		else
		{
			data.DownCast<LazyMaterial>()->Unload();
		}

		unload_(id, nullptr);
	}
}
} // namespace EffekseerPlugin
