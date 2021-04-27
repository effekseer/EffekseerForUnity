
#ifndef	__EFFEKSEER_PLUGIN_LOADER_H__
#define __EFFEKSEER_PLUGIN_LOADER_H__

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#include <Effekseer/Effekseer.h>
#else
#include <Effekseer.h>
#endif

#include "EffekseerPluginCommon.h"
#include "../unity/IUnityInterface.h"

namespace EffekseerPlugin
{
	using TextureLoaderLoad = void* (UNITY_INTERFACE_API*)(const char16_t* path, int32_t* width, int32_t* height, int32_t* format, int32_t* miplevel);
	using TextureLoaderUnload = void (UNITY_INTERFACE_API*)(const char16_t* path, void* nativePtr);

	class TextureLoader : public Effekseer::TextureLoader
	{
	protected:
		TextureLoaderLoad load;
		TextureLoaderUnload unload;

	public:
		static Effekseer::RefPtr<Effekseer::TextureLoader> Create(
			TextureLoaderLoad load,
			TextureLoaderUnload unload);

		TextureLoader(
			TextureLoaderLoad load,
			TextureLoaderUnload unload) 
			: load(load), unload(unload) {}
		virtual ~TextureLoader() override = default;
	};
};

#endif
