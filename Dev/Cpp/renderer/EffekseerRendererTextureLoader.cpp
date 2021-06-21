#include "EffekseerRendererTextureLoader.h"
#include <algorithm>

namespace EffekseerRendererUnity
{
Effekseer::TextureRef TextureLoader::Load(const EFK_CHAR* path, Effekseer::TextureType textureType)
{
	// Load with unity
	int32_t width, height, format, miplevel;
	void* texturePtr = load((const char16_t*)path, &width, &height, &format, &miplevel);
	if (texturePtr == nullptr)
	{
		return nullptr;
	}

	auto background = Effekseer::MakeRefPtr<Texture>(texturePtr);
	auto textureDataPtr = Effekseer::MakeRefPtr<Effekseer::Texture>();
	textureDataPtr->SetBackend(background);

	textureData2NativePtr[textureDataPtr] = texturePtr;

	return textureDataPtr;
}

void TextureLoader::Unload(Effekseer::TextureRef source)
{
	if (source == nullptr)
	{
		return;
	}

	// Unload from unity
	unload(0, textureData2NativePtr[source]);
	textureData2NativePtr.erase(source);
}
} // namespace EffekseerRendererUnity