
#ifndef	__EFFEKSEER_PLUGIN_MODEL_H__
#define __EFFEKSEER_PLUGIN_MODEL_H__

#include <string>
#include <map>
#include <memory>
#include <Effekseer.h>
#include "MemoryFile.h"
#include "EffekseerPluginCommon.h"
#include "../unity/IUnityInterface.h"

namespace EffekseerPlugin
{
	using ModelLoaderLoad = void* (UNITY_INTERFACE_API*)(const char16_t* path, void* data, int dataSize, int& requiredDataSize);
	using ModelLoaderUnload = void (UNITY_INTERFACE_API*)(const char16_t* path, void* modelPointer);

	class ModelLoader : public Effekseer::ModelLoader
	{
		ModelLoaderLoad load;
		ModelLoaderUnload unload;
		
		struct ModelResource {
			int referenceCount = 1;
			void* internalData;
		};
		std::map<std::u16string, ModelResource> resources;
		MemoryFile memoryFile;

		std::unique_ptr<Effekseer::ModelLoader> internalLoader;
		
	public:
		static Effekseer::ModelLoader* Create(
			ModelLoaderLoad load,
			ModelLoaderUnload unload);

		ModelLoader(
			ModelLoaderLoad load,
			ModelLoaderUnload unload );
		
		virtual ~ModelLoader() = default;
		virtual void* Load( const EFK_CHAR* path );
		virtual void Unload( void* source );

		Effekseer::FileInterface* GetFileInterface() {
			return &memoryFile;
		}
		void SetInternalLoader( Effekseer::ModelLoader* loader ) {
			internalLoader.reset( loader );
		}
	};
	
}

#endif
