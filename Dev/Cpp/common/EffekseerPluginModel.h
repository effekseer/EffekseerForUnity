
#ifndef	__EFFEKSEER_PLUGIN_MODEL_H__
#define __EFFEKSEER_PLUGIN_MODEL_H__

#include <string>
#include <map>
#include <memory>

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#include <Effekseer/Effekseer.h>
#else
#include <Effekseer.h>
#endif

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
			Effekseer::ModelRef internalData;
		};
		std::map<std::u16string, ModelResource> resources;
		MemoryFile memoryFile;

		Effekseer::RefPtr<Effekseer::ModelLoader> internalLoader;
		
	public:
		static Effekseer::RefPtr<Effekseer::ModelLoader> Create(
			ModelLoaderLoad load,
			ModelLoaderUnload unload);

		ModelLoader(
			ModelLoaderLoad load,
			ModelLoaderUnload unload );
		
		virtual ~ModelLoader() = default;
		virtual Effekseer::ModelRef Load(const char16_t* path) override;
		virtual void Unload(Effekseer::ModelRef source) override;

		Effekseer::FileInterface* GetFileInterface() {
			return &memoryFile;
		}
		void SetInternalLoader(Effekseer::RefPtr<Effekseer::ModelLoader> loader) {
			internalLoader = loader;
		}
	};
	
}

#endif
