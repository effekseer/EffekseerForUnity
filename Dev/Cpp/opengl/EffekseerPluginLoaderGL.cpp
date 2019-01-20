
#include <map>
#include <string>

#include "Effekseer.h"
#include "EffekseerRendererGL.h"

#include "../../../../Effekseer/Dev/Cpp/EffekseerRendererGL/EffekseerRenderer/EffekseerRendererGL.ModelLoader.h"

#include "EffekseerPluginLoaderGL.h"

using namespace Effekseer;

namespace EffekseerPlugin
{
	extern UnityGfxRenderer					g_UnityRendererType;
	extern RendererType g_rendererType;

	bool IsPowerOfTwo(uint32_t x) {
		return (x & (x - 1)) == 0;
	}

	TextureLoaderGL::TextureLoaderGL(
		TextureLoaderLoad load,
		TextureLoaderUnload unload)
		: TextureLoader(load, unload)
	{}

	TextureLoaderGL::~TextureLoaderGL()
	{}

	TextureData* TextureLoaderGL::Load(const EFK_CHAR* path, Effekseer::TextureType textureType)
	{
		// リソーステーブルを検索して存在したらそれを使う
		auto it = resources.find((const char16_t*) path);
		if (it != resources.end()) {
			it->second.referenceCount++;
			return &it->second.texture;
		}

		// Unityでテクスチャをロード
		int32_t width, height, format;
		int64_t textureID = reinterpret_cast<int64_t>(load((const char16_t*) path, &width, &height, &format));
		if (textureID == 0)
		{
			return nullptr;
		}
		// リソーステーブルに追加
		auto added = resources.insert(std::make_pair((const char16_t*) path, TextureResource()));
		TextureResource& res = added.first->second;

		res.texture.Width = width;
		res.texture.Height = height;
		res.texture.TextureFormat = (TextureFormatType)format;
		
		if (g_rendererType == RendererType::Native)
		{
			res.texture.UserID = textureID;
#if !defined(_WIN32)
			if (g_UnityRendererType != kUnityGfxRendererOpenGLES20 ||
				(IsPowerOfTwo(res.texture.Width) && IsPowerOfTwo(res.texture.Height)))
			{
				// テクスチャのミップマップを生成する
				glBindTexture(GL_TEXTURE_2D, (GLuint)textureID);
				glGenerateMipmap(GL_TEXTURE_2D);
				glBindTexture(GL_TEXTURE_2D, 0);
			}
#endif
		}
		else
		{
			res.texture.UserPtr = (void*)textureID;
		}

		textureData2NativePtr[&res.texture] = (void*)textureID;

		return &res.texture;
	}

	void TextureLoaderGL::Unload(TextureData* source)
	{
		if (source == nullptr) {
			return;
		}

		// アンロードするテクスチャを検索
		auto it = std::find_if(resources.begin(), resources.end(),
			[source](const std::pair<std::u16string, TextureResource>& pair){
			return &pair.second.texture == source;
		});
		if (it == resources.end()) {
			return;
		}

		// 参照カウンタが0になったら実際にアンロード
		it->second.referenceCount--;
		if (it->second.referenceCount <= 0) {
			// Unity側でアンロード
			unload(it->first.c_str(), textureData2NativePtr[source]);
			textureData2NativePtr.erase(source);
			resources.erase(it);
		}
	}

#ifdef _WIN32

#else
	TextureLoader* TextureLoader::Create(
		TextureLoaderLoad load,
		TextureLoaderUnload unload)
	{
		return new TextureLoaderGL( load, unload );
	}
	
	ModelLoader* ModelLoader::Create(
		ModelLoaderLoad load,
		ModelLoaderUnload unload)
	{
		auto loader = new ModelLoader( load, unload );
		auto internalLoader = new EffekseerRendererGL::ModelLoader(loader->GetFileInterface());
		loader->SetInternalLoader( internalLoader );
		return loader;
	}
#endif
};