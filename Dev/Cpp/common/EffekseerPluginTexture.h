
#ifndef	__EFFEKSEER_PLUGIN_LOADER_H__
#define __EFFEKSEER_PLUGIN_LOADER_H__

#include <Effekseer.h>
#include "EffekseerPluginCommon.h"

namespace EffekseerPlugin
{
	using TextureLoaderLoad = void* (UNITY_API*)(const char16_t* path, int32_t* width, int32_t* height, int32_t* format);
	using TextureLoaderUnload = void (UNITY_API*)(const char16_t* path);

	class TextureLoader : public Effekseer::TextureLoader
	{
	protected:
		TextureLoaderLoad load;
		TextureLoaderUnload unload;

	public:
		static TextureLoader* Create(
			TextureLoaderLoad load,
			TextureLoaderUnload unload);
		TextureLoader(
			TextureLoaderLoad load,
			TextureLoaderUnload unload) 
			: load(load), unload(unload) {}
		virtual ~TextureLoader() {}
		virtual Effekseer::TextureData* Load( const EFK_CHAR* path, Effekseer::TextureType textureType ) = 0;
		virtual void Unload( Effekseer::TextureData* source ) = 0;
	};
};

#endif
