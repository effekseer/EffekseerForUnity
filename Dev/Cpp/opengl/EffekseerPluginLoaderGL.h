
#pragma once

#include <memory>
#include <string>
#include <map>
#include <algorithm>

#include "../common/IUnityGraphics.h"
#include "../common/EffekseerPluginTexture.h"
#include "../common/EffekseerPluginModel.h"

namespace EffekseerPlugin
{
	class TextureLoaderGL : public TextureLoader
	{
		struct TextureResource
		{
			int referenceCount = 1;
			Effekseer::TextureData texture = {};
		};

		std::map<std::u16string, TextureResource> resources;

	public:
		TextureLoaderGL(
			TextureLoaderLoad load,
			TextureLoaderUnload unload);

		virtual ~TextureLoaderGL();

		virtual Effekseer::TextureData* Load(const EFK_CHAR* path, Effekseer::TextureType textureType);

		virtual void Unload(Effekseer::TextureData* source);
	};
};