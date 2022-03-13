
#pragma once

#include "../unity/IUnityInterface.h"
#include "EffekseerPluginCommon.h"

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#include <Effekseer/Effekseer.h>
#else
#include <Effekseer.h>
#endif

#include <map>
#include <memory>
#include <string>

namespace EffekseerPlugin
{

class MemoryFileReader : public Effekseer::FileReader
{
	uint8_t* data_ = nullptr;
	size_t length_ = 0;
	int position_ = 0;

public:
	MemoryFileReader(uint8_t* data, size_t length);
	size_t Read(void* buffer, size_t size) override;
	void Seek(int position) override;
	int GetPosition() const override;
	size_t GetLength() const override;
};

class MemoryFile : public Effekseer::FileInterface
{
public:
	Effekseer::CustomVector<uint8_t> LoadedBuffer;
	size_t LoadedSize = 0;
	explicit MemoryFile(size_t bufferSize);
	void Resize(size_t bufferSize);
	Effekseer::FileReaderRef OpenRead(const EFK_CHAR* path);
	Effekseer::FileWriterRef OpenWrite(const EFK_CHAR* path);
};

} // namespace EffekseerPlugin
