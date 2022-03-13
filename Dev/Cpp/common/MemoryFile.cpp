#include "MemoryFile.h"

namespace EffekseerPlugin
{

MemoryFileReader::MemoryFileReader(uint8_t* data, size_t length) : data_(data), length_(length), position_(0) {}

size_t MemoryFileReader::Read(void* buffer, size_t size)
{
	if (size >= length_ - position_)
	{
		size = length_ - position_;
	}
	memcpy(buffer, &data_[position_], size);
	position_ += static_cast<int32_t>(size);
	return size;
}

void MemoryFileReader::Seek(int position) { this->position_ = position; }

int MemoryFileReader::GetPosition() const { return position_; }

size_t MemoryFileReader::GetLength() const { return length_; }

MemoryFile::MemoryFile(size_t bufferSize) { LoadedBuffer.resize(bufferSize); }

void MemoryFile::Resize(size_t bufferSize) { LoadedBuffer.resize(bufferSize); }

Effekseer::FileReaderRef MemoryFile::OpenRead(const EFK_CHAR* path)
{
	return Effekseer::MakeRefPtr<MemoryFileReader>(&LoadedBuffer[0], LoadedSize);
}

Effekseer::FileWriterRef MemoryFile::OpenWrite(const EFK_CHAR* path) { return nullptr; }

} // namespace EffekseerPlugin