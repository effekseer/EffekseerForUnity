#include "EffekseerPluginCurve.h"
#include <algorithm>

namespace EffekseerPlugin
{
CurveLoader::CurveLoader(CurveLoaderLoad load, CurveLoaderUnload unload) : load_(load), unload_(unload), memoryFile_(1 * 1024 * 1024)
{
	internalLoader_ = Effekseer::MakeRefPtr<Effekseer::CurveLoader>(&memoryFile_);
}

Effekseer::CurveRef CurveLoader::Load(const EFK_CHAR* path)
{
	// Load with unity
	int requiredDataSize = 0;
	auto CurvePtr = load_((const char16_t*)path, &memoryFile_.LoadedBuffer[0], (int)memoryFile_.LoadedBuffer.size(), requiredDataSize);

	if (requiredDataSize == 0)
	{
		// Failed to load
		return nullptr;
	}

	if (CurvePtr == nullptr)
	{
		// Lack of memory
		memoryFile_.Resize(requiredDataSize);

		// Load with unity
		CurvePtr = load_((const char16_t*)path, &memoryFile_.LoadedBuffer[0], (int)memoryFile_.LoadedBuffer.size(), requiredDataSize);

		if (CurvePtr == nullptr)
		{
			// Failed to load
			return nullptr;
		}
	}

	memoryFile_.LoadedSize = (size_t)requiredDataSize;
	return internalLoader_->Load(path);
}

void CurveLoader::Unload(Effekseer::CurveRef source) {}
} // namespace EffekseerPlugin