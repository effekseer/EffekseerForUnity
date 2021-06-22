#include "EffekseerRendererTextureLoader.h"
#include <algorithm>

namespace EffekseerRendererUnity
{
Effekseer::TextureRef TextureLoader::Load(const EFK_CHAR* path, Effekseer::TextureType textureType)
{
	auto id = getUnityId_(path);

	Effekseer::TextureRef generated;

	if (id2Texture_.TryLoad(id, generated))
	{
		return generated;
	}

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

	id2Texture_.Register(id, textureDataPtr, texturePtr);

	return textureDataPtr;
}

void TextureLoader::Unload(Effekseer::TextureRef source)
{
	if (source == nullptr)
	{
		return;
	}

	int32_t id{};
	void* nativePtr{};
	if (id2Texture_.Unload(source, id, nativePtr))
	{
		// Unload from unity
		unload(0, textureData2NativePtr[source]);
		textureData2NativePtr.erase(source);
	}
}
} // namespace EffekseerRendererUnity