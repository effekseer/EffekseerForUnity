#include "EffekseerPluginCurve.h"
#include <algorithm>

namespace EffekseerPlugin
{
CurveLoader::CurveLoader(CurveLoaderLoad load, CurveLoaderUnload unload, GetUnityIDFromPath getUnityId)
	: load_(load), unload_(unload), getUnityId_(getUnityId)
{
	memoryFile_ = Effekseer::MakeRefPtr<MemoryFile>(1 * 1024 * 1024);
	internalLoader_ = Effekseer::MakeRefPtr<Effekseer::CurveLoader>(memoryFile_);
}

Effekseer::CurveRef CurveLoader::Load(const EFK_CHAR* path)
{
	auto id = getUnityId_(path);

	Effekseer::CurveRef generated;

	if (id2Obj_.TryLoad(id, generated))
	{
		return generated;
	}

	// Load with unity
	int requiredDataSize = 0;
	auto CurvePtr = load_((const char16_t*)path, memoryFile_->LoadedBuffer.data(), (int)memoryFile_->LoadedBuffer.size(), requiredDataSize);

	if (requiredDataSize == 0)
	{
		// Failed to load
		return nullptr;
	}

	if (CurvePtr == nullptr)
	{
		// Lack of memory
		memoryFile_->Resize(requiredDataSize);

		// Load with unity
		CurvePtr = load_((const char16_t*)path, memoryFile_->LoadedBuffer.data(), (int)memoryFile_->LoadedBuffer.size(), requiredDataSize);

		if (CurvePtr == nullptr)
		{
			// Failed to load
			return nullptr;
		}
	}

	memoryFile_->LoadedSize = (size_t)requiredDataSize;
	auto data = internalLoader_->Load(path);

	id2Obj_.Register(id, data, CurvePtr);

	return data;
}

void CurveLoader::Unload(Effekseer::CurveRef source)
{
	int32_t id{};
	void* nativePtr{};
	if (id2Obj_.Unload(source, id, nativePtr))
	{
		unload_(id, nativePtr);
	}
}
} // namespace EffekseerPlugin