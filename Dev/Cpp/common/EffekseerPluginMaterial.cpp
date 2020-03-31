#include "EffekseerPluginMaterial.h"
#include <algorithm>
#include <assert.h>

namespace EffekseerPlugin
{

/**
@brief	load a material with delay
*/
class LazyMaterialData : public Effekseer::MaterialData
{
private:
	Effekseer::MaterialData* internalData_ = nullptr;

	Effekseer::CustomVector<uint8_t> data_;
	Effekseer::CustomVector<uint8_t> compiledData_;
	std::shared_ptr<MaterialLoaderHolder> internalLoader_ = nullptr;

public:
	LazyMaterialData(const std::shared_ptr<MaterialLoaderHolder>& loader,
					 const Effekseer::CustomVector<uint8_t>& data,
					 const Effekseer::CustomVector<uint8_t>& compiledData)
		: internalLoader_(loader), data_(data), compiledData_(compiledData)
	{
	}

	void Load()
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

	void Unload()
	{
		if (internalData_ != nullptr)
		{
			internalLoader_->Get()->Unload(internalData_);
			internalData_ = nullptr;
		}
	}
};

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

void MaterialEvent::Load(LazyMaterialData* data)
{
	std::lock_guard<std::mutex> lock(mtx_);

	Command c;
	c.type = CommandType::Load;
	c.data = data;
	commands_.emplace_back(c);
}

void MaterialEvent::UnloadAndDelete(LazyMaterialData* data)
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
			ES_SAFE_DELETE(c.data);
		}
		else
		{
			assert(0);
		}
	}

	commands_.clear();
}

MaterialLoader::MaterialLoader(MaterialLoaderLoad load, MaterialLoaderUnload unload)
	: load_(load), unload_(unload), memoryFile_(1 * 1024 * 1024), memoryFileForCache_(1 * 1024 * 1024)
{
}

Effekseer::MaterialData* MaterialLoader::Load(const EFK_CHAR* path)
{
	auto it = resources.find((const char16_t*)path);
	if (it != resources.end())
	{
		it->second.referenceCount++;
		return it->second.internalData;
	}

	// Load with unity
	MaterialResource res;
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
		auto data = new LazyMaterialData(internalLoader_, memoryFile_.LoadedBuffer, memoryFileForCache_.LoadedBuffer);
		res.internalData = data;

		auto eventInstance = MaterialEvent::GetInstance();

		if (eventInstance != nullptr)
		{
			eventInstance->Load(data);
		}
		else
		{
			data->Load();
		}

		resources.insert(std::make_pair((const char16_t*)path, res));
		return res.internalData;
	}

	return nullptr;
}
void MaterialLoader::Unload(Effekseer::MaterialData* data)
{
	if (data == nullptr)
	{
		return;
	}

	auto it = std::find_if(resources.begin(), resources.end(), [data](const std::pair<std::u16string, MaterialResource>& pair) {
		return pair.second.internalData == data;
	});
	if (it == resources.end())
	{
		return;
	}

	it->second.referenceCount--;
	if (it->second.referenceCount <= 0)
	{
		auto eventInstance = MaterialEvent::GetInstance();

		if (eventInstance != nullptr)
		{
			eventInstance->UnloadAndDelete(it->second.internalData);
		}
		else
		{
			it->second.internalData->Unload();
			ES_SAFE_DELETE(it->second.internalData);
		}

		unload_(it->first.c_str(), nullptr);
		resources.erase(it);
	}
}
} // namespace EffekseerPlugin