#include "EffekseerRendererModelLoader.h"
#include "EffekseerRendererImplemented.h"

#include <algorithm>

namespace EffekseerRendererUnity
{
	ModelLoader::ModelLoader(
		EffekseerPlugin::ModelLoaderLoad load,
		EffekseerPlugin::ModelLoaderUnload unload)
		: load(load)
		, unload(unload)
	{
		internalBuffer.resize(1024 * 1024);
	}

	void* ModelLoader::Load(const EFK_CHAR* path) {
		// find it from resource table and if it exists, it is reused.
		auto it = resources.find((const char16_t*)path);
		if (it != resources.end()) {
			it->second.referenceCount++;
			return it->second.internalData;
		}

		// Load with unity
		ModelResource res;
		int requiredDataSize = 0;
		void* modelPtr = nullptr;

		modelPtr = load((const char16_t*)path, internalBuffer.data(), internalBuffer.size(), requiredDataSize);

		if (requiredDataSize == 0)
		{
			// Failed to load
			return nullptr;
		}

		if (modelPtr == nullptr)
		{
			// reallocate a buffer
			internalBuffer.resize(requiredDataSize);
		}

		modelPtr = load((const char16_t*)path, internalBuffer.data(), internalBuffer.size(), requiredDataSize);

		if (modelPtr == nullptr)
		{
			return nullptr;
		}

		internalBuffer.resize(requiredDataSize);

		auto model = new Model(internalBuffer.data(), internalBuffer.size());
		model->InternalPtr = modelPtr;
		res.internalData = model;

		resources.insert(std::make_pair(
			(const char16_t*)path, res));
		return res.internalData;
	}

	void ModelLoader::Unload(void* source) {
		if (source == nullptr) {
			return;
		}

		// find a model
		auto it = std::find_if(resources.begin(), resources.end(),
			[source](const std::pair<std::u16string, ModelResource>& pair) {
			return pair.second.internalData == source;
		});
		if (it == resources.end()) {
			return;
		}

		// if refrercen count is zero, it is released
		it->second.referenceCount--;
		if (it->second.referenceCount <= 0)
		{
			auto model = (Model*)it->second.internalData;
			unload(it->first.c_str(), model->InternalPtr);
			ES_SAFE_DELETE(it->second.internalData);
			resources.erase(it);
		}
	}
}