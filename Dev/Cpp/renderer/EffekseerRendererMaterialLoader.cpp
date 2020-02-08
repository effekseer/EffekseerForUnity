#include "EffekseerRendererMaterialLoader.h"
#include "EffekseerRendererImplemented.h"
#include "EffekseerRendererShader.h"
#include <algorithm>

namespace EffekseerRendererUnity
{

MaterialLoader::MaterialLoader(EffekseerPlugin::MaterialLoaderLoad load, EffekseerPlugin::MaterialLoaderUnload unload)
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
							 memoryFile_.LoadedBuffer.data(),
							 (int)memoryFile_.LoadedBuffer.size(),
							 requiredDataSize,
							 memoryFileForCache_.LoadedBuffer.data(),
							 (int)memoryFileForCache_.LoadedBuffer.size(),
							 requiredCachedDataSize);

	if (requiredDataSize == 0 && requiredCachedDataSize == 0)
	{
		// Failed to load
		return nullptr;
	}

	{
		// Lack of memory
		memoryFileForCache_.Resize(requiredCachedDataSize);
		memoryFile_.Resize(requiredDataSize);

		// Load with unity
		materialPtr = load_((const char16_t*)path,
							memoryFile_.LoadedBuffer.data(),
							(int)memoryFile_.LoadedBuffer.size(),
							requiredDataSize,
							memoryFileForCache_.LoadedBuffer.data(),
							(int)memoryFileForCache_.LoadedBuffer.size(),
							requiredCachedDataSize);

		if (materialPtr == nullptr)
		{
			// Failed to load
			return nullptr;
		}
	}

	// try to load from code
	if (requiredDataSize > 0)
	{
		memoryFile_.LoadedSize = static_cast<size_t>(requiredDataSize);

		std::shared_ptr<Effekseer::Material> material = std::make_shared<Effekseer::Material>();
		material->Load((const uint8_t*)memoryFile_.LoadedBuffer.data(), memoryFile_.LoadedBuffer.size());

		res.internalData = new ::Effekseer::MaterialData();

		auto materialData = res.internalData;

		materialData->IsSimpleVertex = material->GetIsSimpleVertex();
		materialData->IsRefractionRequired = material->GetHasRefraction();
		materialData->CustomData1 = material->GetCustomData1Count();
		materialData->CustomData2 = material->GetCustomData2Count();
		materialData->TextureCount = std::min(material->GetTextureCount(), Effekseer::UserTextureSlotMax);
		materialData->UniformCount = material->GetUniformCount();
		materialData->ShadingModel = material->GetShadingModel();

		for (int32_t i = 0; i < materialData->TextureCount; i++)
		{
			materialData->TextureWrapTypes.at(i) = material->GetTextureWrap(i);
		}

		materialData->UserPtr = new Shader(materialPtr, material, false, false);
		materialData->ModelUserPtr = new Shader(materialPtr, material, true, false);

		if (material->GetHasRefraction())
		{
			materialData->RefractionUserPtr = new Shader(materialPtr, material, false, true);
			materialData->RefractionModelUserPtr = new Shader(materialPtr, material, true, true);		
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
		auto ss = static_cast<Shader*>(it->second.internalData->UserPtr);
		auto sm = static_cast<Shader*>(it->second.internalData->ModelUserPtr);
		auto srs = static_cast<Shader*>(it->second.internalData->RefractionUserPtr);
		auto srm = static_cast<Shader*>(it->second.internalData->RefractionModelUserPtr);

		ES_SAFE_DELETE(ss);
		ES_SAFE_DELETE(sm);
		ES_SAFE_DELETE(srs);
		ES_SAFE_DELETE(srm);
		ES_SAFE_DELETE(it->second.internalData);

		unload_(it->first.c_str(), nullptr);
		resources.erase(it);
	}
}

} // namespace EffekseerRendererUnity