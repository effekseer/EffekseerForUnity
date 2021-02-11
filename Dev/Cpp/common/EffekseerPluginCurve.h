
#pragma once

#include <map>
#include <memory>
#include <string>

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#include <Effekseer/Effekseer.h>
#else
#include <Effekseer.h>
#endif

#include "../unity/IUnityInterface.h"
#include "EffekseerPluginCommon.h"
#include "MemoryFile.h"

namespace EffekseerPlugin
{
using CurveLoaderLoad = void*(UNITY_INTERFACE_API*)(const char16_t* path, void* data, int dataSize, int& requiredDataSize);
using CurveLoaderUnload = void(UNITY_INTERFACE_API*)(const char16_t* path, void* CurvePointer);

class CurveLoader : public Effekseer::CurveLoader
{
	CurveLoaderLoad load_;
	CurveLoaderUnload unload_;
	MemoryFile memoryFile_;

	Effekseer::RefPtr<Effekseer::CurveLoader> internalLoader_;

public:
	CurveLoader(CurveLoaderLoad load, CurveLoaderUnload unload);

	virtual ~CurveLoader() = default;
	virtual Effekseer::CurveRef Load(const char16_t* path) override;
	virtual void Unload(Effekseer::CurveRef source) override;
};

} // namespace EffekseerPlugin
