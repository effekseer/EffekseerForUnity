#include "EffekseerRendererMaterialLoader.h"
#include "EffekseerRendererImplemented.h"
#include "EffekseerRendererShader.h"
#include <algorithm>

namespace EffekseerRendererUnity
{

MaterialLoader::MaterialLoader(EffekseerPlugin::MaterialLoaderLoad load,
							   EffekseerPlugin::MaterialLoaderUnload unload,
							   EffekseerPlugin::GetUnityIDFromPath getUnityId)
	: load_(load), unload_(unload), getUnityId_(getUnityId), memoryFile_(1 * 1024 * 1024), memoryFileForCache_(1 * 1024 * 1024)
{
}

MaterialLoader::~MaterialLoader() {}

Effekseer::MaterialRef MaterialLoader::Load(const EFK_CHAR* path)
{
	auto id = getUnityId_(path);

	Effekseer::MaterialRef generated;

	if (id2Obj_.TryLoad(id, generated))
	{
		return generated;
	}

	// Load with unity
	int requiredDataSize = 0;
	int requiredCachedDataSize = 0;

	auto materialPtr = load_((const char16_t*)path, nullptr, 0, requiredDataSize, nullptr, 0, requiredCachedDataSize);

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

		std::shared_ptr<Effekseer::MaterialFile> material = std::make_shared<Effekseer::MaterialFile>();
		material->Load((const uint8_t*)memoryFile_.LoadedBuffer.data(), static_cast<int32_t>(memoryFile_.LoadedBuffer.size()));

		auto internalData = Effekseer::MakeRefPtr<::Effekseer::Material>();

		auto materialData = internalData;

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

		id2Obj_.Register(id, internalData, materialPtr);

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

	int32_t id{};
	void* nativePtr{};
	if (id2Obj_.Unload(data, id, nativePtr))
	{
		auto ss = static_cast<Shader*>(data->UserPtr);
		auto sm = static_cast<Shader*>(data->ModelUserPtr);
		auto srs = static_cast<Shader*>(data->RefractionUserPtr);
		auto srm = static_cast<Shader*>(data->RefractionModelUserPtr);

		void* ptr = nullptr;
		if (ss != nullptr)
		{
			ptr = ss->GetUnityMaterial();
		}

		ES_SAFE_DELETE(ss);
		ES_SAFE_DELETE(sm);
		ES_SAFE_DELETE(srs);
		ES_SAFE_DELETE(srm);

		unload_(id, ptr);
	}
}

} // namespace EffekseerRendererUnity