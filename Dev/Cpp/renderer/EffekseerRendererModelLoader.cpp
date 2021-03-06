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

Effekseer::ModelRef ProceduralModelGenerator::Generate(const Effekseer::ProceduralModelParameter* parameter)
{
	auto original = Effekseer::ProceduralModelGenerator::Generate(parameter);

	vertecies_.resize(original->GetVertexCount());
	memcpy(vertecies_.data(), original->GetVertexes(), sizeof(Effekseer::Model::Vertex) * vertecies_.size());

	faces_.resize(original->GetFaceCount());
	memcpy(faces_.data(), original->GetFaces(), sizeof(Effekseer::Model::Face) * faces_.size());

	auto ptr = generate_(vertecies_.data(), static_cast<int32_t>(vertecies_.size()), faces_.data(), static_cast<int32_t>(faces_.size()));

	auto newmodel = Effekseer::MakeRefPtr<Model>(vertecies_, faces_);
	newmodel->InternalPtr = ptr;
	return newmodel;
}

void ProceduralModelGenerator::Ungenerate(Effekseer::ModelRef model)
{
	if (model == nullptr)
	{
		return;
	}

	ungenerate_(model.DownCast<Model>()->InternalPtr);
}

} // namespace EffekseerRendererUnity