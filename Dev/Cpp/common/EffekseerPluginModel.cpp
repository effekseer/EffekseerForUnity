#include "EffekseerPluginModel.h"
#include <algorithm>

namespace EffekseerPlugin
{
ModelLoader::ModelLoader(ModelLoaderLoad load, ModelLoaderUnload unload, GetUnityIDFromPath getUnityId)
	: load(load), unload(unload), getUnityId_(getUnityId), memoryFile(1 * 1024 * 1024)
{
}

Effekseer::ModelRef ModelLoader::Load(const EFK_CHAR* path)
{
	auto id = getUnityId_(path);

	Effekseer::ModelRef generated;

	if (id2Obj_.TryLoad(id, generated))
	{
		return generated;
	}

	// Load with unity
	int requiredDataSize = 0;
	auto modelPtr = load((const char16_t*)path, &memoryFile.LoadedBuffer[0], (int)memoryFile.LoadedBuffer.size(), requiredDataSize);

	if (requiredDataSize == 0)
	{
		// Failed to load
		return nullptr;
	}

	if (modelPtr == nullptr)
	{
		// Lack of memory
		memoryFile.Resize(requiredDataSize);

		// Load with unity
		modelPtr = load((const char16_t*)path, &memoryFile.LoadedBuffer[0], (int)memoryFile.LoadedBuffer.size(), requiredDataSize);

		if (modelPtr == nullptr)
		{
			// Failed to load
			return nullptr;
		}
	}

	memoryFile.LoadedSize = (size_t)requiredDataSize;
	auto modelDataPtr = internalLoader->Load(path);

	id2Obj_.Register(id, modelDataPtr, modelPtr);

	return modelDataPtr;
}

void ModelLoader::Unload(Effekseer::ModelRef source)
{
	if (source == nullptr)
	{
		return;
	}

	int32_t id{};
	void* nativePtr{};
	if (id2Obj_.Unload(source, id, nativePtr))
	{
		unload(id, nativePtr);

		// delay unload
		auto instance = RenderThreadEvent::GetInstance();
		if (instance != nullptr)
		{
			source->AddRef();
			instance->AddEvent([source]() {
				// a resource must be unload in a rendering thread
				source->Release();
			});
		}
	}
}

void ProceduralModelGenerator::Ungenerate(Effekseer::ModelRef model)
{
	if (model == nullptr)
	{
		return;
	}

	// delay unload
	auto instance = RenderThreadEvent::GetInstance();
	if (instance != nullptr)
	{
		model->AddRef();
		instance->AddEvent([model]() {
			// a resource must be unload in a rendering thread
			model->Release();
		});
	}
}

} // namespace EffekseerPlugin