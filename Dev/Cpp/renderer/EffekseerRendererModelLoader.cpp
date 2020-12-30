#include "EffekseerRendererModelLoader.h"
#include "EffekseerRendererImplemented.h"

#include <algorithm>

namespace EffekseerRendererUnity
{
ModelLoader::ModelLoader(EffekseerPlugin::ModelLoaderLoad load, EffekseerPlugin::ModelLoaderUnload unload) : load(load), unload(unload)
{
	internalBuffer.resize(1024 * 1024);
}

Effekseer::ModelRef ModelLoader::Load(const EFK_CHAR* path)
{
	// Load with unity
	int requiredDataSize = 0;
	void* modelPtr = nullptr;

	modelPtr = load(path, internalBuffer.data(), static_cast<int32_t>(internalBuffer.size()), requiredDataSize);

	if (requiredDataSize == 0)
	{
		// Failed to load
		return nullptr;
	}

	if (modelPtr == nullptr)
	{
		// reallocate a buffer
		internalBuffer.resize(requiredDataSize);

		modelPtr = load(path, internalBuffer.data(), static_cast<int32_t>(internalBuffer.size()), requiredDataSize);

		if (modelPtr == nullptr)
		{
			return nullptr;
		}
	}

	internalBuffer.resize(requiredDataSize);

	auto model = Effekseer::MakeRefPtr<Model>(internalBuffer.data(), static_cast<int32_t>(internalBuffer.size()));
	model->InternalPtr = modelPtr;
	return model;
}

void ModelLoader::Unload(Effekseer::ModelRef source)
{
	if (source == nullptr)
	{
		return;
	}

	// find a model
	auto model = source.DownCast<Model>().Get();
	unload(source->GetPath().c_str(), model->InternalPtr);
}
} // namespace EffekseerRendererUnity