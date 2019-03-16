
#include <map>
#include <string>

#include "Effekseer.h"
#include "EffekseerRendererGL.h"

#include "../../../../Effekseer/Dev/Cpp/EffekseerRendererGL/EffekseerRenderer/EffekseerRendererGL.ModelLoader.h"

#include "EffekseerPluginLoaderGL.h"

#include "../common/EffekseerPluginTexture.h"
#include "../common/EffekseerPluginModel.h"

#include "../unity/IUnityGraphics.h"
#include "../common/EffekseerPluginTexture.h"
#include "../common/EffekseerPluginModel.h"

#include "../renderer/EffekseerRendererTextureLoader.h"
#include "../renderer/EffekseerRendererModelLoader.h"

using namespace Effekseer;

namespace EffekseerPlugin
{
	extern UnityGfxRenderer					g_UnityRendererType;
	extern EffekseerRenderer::Renderer*		g_EffekseerRenderer;
	extern RendererType g_rendererType;

#ifdef _WIN32

#else
	class TextureLoaderGL : public TextureLoader
	{
		struct TextureResource
		{
			int referenceCount = 1;
			Effekseer::TextureData texture = {};
		};

		std::map<std::u16string, TextureResource> resources;
		std::map<void*, void*> textureData2NativePtr;

	public:
		TextureLoaderGL(
			TextureLoaderLoad load,
			TextureLoaderUnload unload);

		virtual ~TextureLoaderGL();

		virtual Effekseer::TextureData* Load(const EFK_CHAR* path, Effekseer::TextureType textureType);

		virtual void Unload(Effekseer::TextureData* source);
	};

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

	Effekseer::TextureLoader* TextureLoader::Create(
		TextureLoaderLoad load,
		TextureLoaderUnload unload)
	{
		if (g_rendererType == RendererType::Native)
		{
			return new TextureLoaderGL(load, unload);
		}
		else
		{
			return new EffekseerRendererUnity::TextureLoader(load, unload);
		}
	}

	Effekseer::ModelLoader* ModelLoader::Create(
		ModelLoaderLoad load,
		ModelLoaderUnload unload)
	{
		if (g_rendererType == RendererType::Native)
		{
			auto loader = new ModelLoader(load, unload);
			auto internalLoader = EffekseerRendererGL::CreateModelLoader(loader->GetFileInterface());
			loader->SetInternalLoader(internalLoader);
			return loader;
		}
		else
		{
			return new EffekseerRendererUnity::ModelLoader(load, unload);
		}
	}
#endif
};