#include <algorithm>
#include "EffekseerPluginModel.h"

#ifdef _WIN32
#include "../windows/RenderThreadEventQueue.h"
#include "../windows/LasyModelDX11.h"
#endif

namespace EffekseerPlugin
{
	ModelLoader::MemoryFileReader::MemoryFileReader(uint8_t* data, size_t length)
		: data(data), length(length), position(0)
	{
	}

	size_t ModelLoader::MemoryFileReader::Read( void* buffer, size_t size )
	{
		if (size >= length - position) {
			size = length - position;
		}
		memcpy(buffer, &data[position], size);
		position += (int)size;
		return size;
	}

	void ModelLoader::MemoryFileReader::Seek( int position )
	{
		this->position = position;
	}
	int ModelLoader::MemoryFileReader::GetPosition()
	{
		return position;
	}
	size_t ModelLoader::MemoryFileReader::GetLength()
	{
		return length;
	}

	ModelLoader::MemoryFile::MemoryFile( size_t bufferSize ) {
		loadbuffer.resize(bufferSize);
	}

	void ModelLoader::MemoryFile::Resize(size_t bufferSize) {
		loadbuffer.resize(bufferSize);
	}

	Effekseer::FileReader* ModelLoader::MemoryFile::OpenRead( const EFK_CHAR* path ) {
		return new MemoryFileReader(&loadbuffer[0], loadsize);
	}
	Effekseer::FileWriter* ModelLoader::MemoryFile::OpenWrite( const EFK_CHAR* path ) {
		return nullptr;
	}

	ModelLoader::ModelLoader(
		ModelLoaderLoad load,
		ModelLoaderUnload unload ) 
		: load( load )
		, unload( unload )
		, memoryFile( 1 * 1024 * 1024 )
	{
	}
	void* ModelLoader::Load( const EFK_CHAR* path ){
		// リソーステーブルを検索して存在したらそれを使う
		auto it = resources.find((const char16_t*)path);
		if (it != resources.end()) {
			it->second->referenceCount++;
			return it->second->internalData;
		}

		// Load with unity
		auto res = std::make_shared< ModelResource>();
		int size = load( (const char16_t*)path, &memoryFile.loadbuffer[0], (int)memoryFile.loadbuffer.size() );

		if (size == 0)
		{
			// Failed to load
			return nullptr;
		}

		if (size < 0)
		{
			// Lack of memory
			memoryFile.Resize(-size);

			// Load with unity
			size = load((const char16_t*)path, &memoryFile.loadbuffer[0], (int)memoryFile.loadbuffer.size());

			if(size <= 0)
			{
				// Failed to load
				return nullptr;
			}
		}

		// 内部ローダに渡してロード処理する
		memoryFile.loadsize = (size_t)size;
		res->internalData = internalLoader->Load( path );

		// DX11前提
#ifdef _WIN32

		RenderThreadEventQueue::GetInstance()->AddEvent([res]()-> void {
			std::lock_guard<std::mutex> lock(res->mtx);

			if (res->referenceCount > 0)
			{
				auto lasyModel = (LasyModelDX11*)res->internalData;
				lasyModel->LoadActually();
			}
		});

#endif

		auto key = std::u16string((const char16_t*)path);
		resources.insert(std::make_pair(
			key, res));
		return res->internalData;
	}
	void ModelLoader::Unload( void* source ){
		if (source == nullptr) {
			return;
		}

		// アンロードするモデルを検索
		auto it = std::find_if(resources.begin(), resources.end(), 
			[source](const std::pair<std::u16string, std::shared_ptr<ModelResource>>& pair){
				return pair.second->internalData == source;
			});

		// 参照カウンタが0になったら実際にアンロード
		auto res = it->second;

		std::lock_guard<std::mutex> lock(res->mtx);

		res->referenceCount--;
		if (res->referenceCount <= 0)
		{
			internalLoader->Unload(res->internalData);
			unload(it->first.c_str());
			resources.erase(it);
		}
	}
}