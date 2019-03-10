#include "EffekseerRendererTextureLoader.h"
#include <algorithm>

namespace EffekseerRendererUnity
{
Effekseer::TextureData* TextureLoader::Load(const EFK_CHAR* path, Effekseer::TextureType textureType)
{
	// find it from resource table and if it exists, it is reused.
	auto it = resources.find((const char16_t*)path);
	if (it != resources.end())
	{
		it->second.referenceCount++;
		return &it->second.texture;
	}

	// Load with unity
	int32_t width, height, format;
	void* texturePtr = load((const char16_t*)path, &width, &height, &format);
	if (texturePtr == nullptr)
	{
		return nullptr;
	}

	auto added = resources.insert(std::make_pair((const char16_t*)path, TextureResource()));
	TextureResource& res = added.first->second;

	res.texture.Width = width;
	res.texture.Height = height;
	res.texture.TextureFormat = (Effekseer::TextureFormatType)format;
	res.texture.UserPtr = texturePtr;

	textureData2NativePtr[&res.texture] = texturePtr;

	return &res.texture;
}

void TextureLoader::Unload(Effekseer::TextureData* source)
{
	if (source == nullptr)
	{
		return;
	}

	// find a texture
	auto it = std::find_if(resources.begin(), resources.end(), [source](const std::pair<std::u16string, TextureResource>& pair) {
		return &pair.second.texture == source;
	});
	if (it == resources.end())
	{
		return;
	}

	// if refrercen count is zero, it is released
	it->second.referenceCount--;
	if (it->second.referenceCount <= 0)
	{
		// Unload from unity
		unload(it->first.c_str(), textureData2NativePtr[source]);
		textureData2NativePtr.erase(source);
		resources.erase(it);
	}
}
} // namespace EffekseerRendererUnity