
#pragma once

#include "../unity/IUnityInterface.h"
#include "EffekseerPluginCommon.h"
#include <Effekseer.h>
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
	size_t Read(void* buffer, size_t size);
	void Seek(int position);
	int GetPosition();
	size_t GetLength();
};

class MemoryFile : public Effekseer::FileInterface
{
public:
	std::vector<uint8_t> LoadedBuffer;
	size_t LoadedSize = 0;
	explicit MemoryFile(size_t bufferSize);
	void Resize(size_t bufferSize);
	Effekseer::FileReader* OpenRead(const EFK_CHAR* path);
	Effekseer::FileWriter* OpenWrite(const EFK_CHAR* path);
};

} // namespace EffekseerPlugin
