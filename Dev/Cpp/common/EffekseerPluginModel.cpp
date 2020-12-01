#include <algorithm>
#include "EffekseerPluginModel.h"

namespace EffekseerPlugin
{
	ModelLoader::ModelLoader(
		ModelLoaderLoad load,
		ModelLoaderUnload unload ) 
		: load( load )
		, unload( unload )
		, memoryFile( 1 * 1024 * 1024 )
	{
	}

	Effekseer::Model* ModelLoader::Load(const EFK_CHAR* path)
	{
		// リソーステーブルを検索して存在したらそれを使う
		auto it = resources.find((const char16_t*)path);
		if (it != resources.end()) {
			it->second.referenceCount++;
			return it->second.internalData;
		}

		// Load with unity
		ModelResource res;
		int requiredDataSize = 0;
		auto modelPtr = load((const char16_t*)path, &memoryFile.LoadedBuffer[0], (int)memoryFile.LoadedBuffer.size(), requiredDataSize);

		if (requiredDataSize == 0)
		{
			// Failed to load
			return nullptr;
		}

		if (modelPtr == nullptr)
		{
			// Lack of memory
			memoryFile.Resize(requiredDataSize);

			// Load with unity
			modelPtr = load((const char16_t*)path, &memoryFile.LoadedBuffer[0], (int)memoryFile.LoadedBuffer.size(), requiredDataSize);

			if(modelPtr == nullptr)
			{
				// Failed to load
				return nullptr;
			}
		}

		// 内部ローダに渡してロード処理する
		memoryFile.LoadedSize = (size_t)requiredDataSize;
		res.internalData = internalLoader->Load( path );
			
		resources.insert( std::make_pair(
			(const char16_t*)path, res ) );
		return res.internalData;
	}
	void ModelLoader::Unload( void* source ){
		if (source == nullptr) {
			return;
		}

		// アンロードするモデルを検索
		auto it = std::find_if(resources.begin(), resources.end(), 
			[source](const std::pair<std::u16string, ModelResource>& pair){
				return pair.second.internalData == source;
			});
		if (it == resources.end()) {
			return;
		}

		// 参照カウンタが0になったら実際にアンロード
		it->second.referenceCount--;
		if (it->second.referenceCount <= 0)
		{
			internalLoader->Unload(it->second.internalData);
			unload(it->first.c_str(), nullptr);
			resources.erase(it);
		}
	}
}