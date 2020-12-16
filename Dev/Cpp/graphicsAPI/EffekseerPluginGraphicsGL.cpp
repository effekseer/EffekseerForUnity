
#include "EffekseerPluginGraphicsGL.h"
#include <algorithm>
#include <assert.h>

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#else
#include <EffekseerRenderer/EffekseerRendererGL.MaterialLoader.h>
#include <EffekseerRenderer/EffekseerRendererGL.ModelLoader.h>
#endif

#include "../common/EffekseerPluginMaterial.h"

namespace EffekseerPlugin
{

class TextureLoaderGL : public TextureLoader
{
	struct TextureResource
	{
		int referenceCount = 1;
		Effekseer::TextureRef textureDataPtr = nullptr;
	};

	Effekseer::Backend::GraphicsDeviceRef graphicsDevice_;
	std::map<std::u16string, TextureResource> resources;
	std::map<Effekseer::TextureRef, void*> textureData2NativePtr;
	UnityGfxRenderer gfxRenderer;

public:
	TextureLoaderGL(TextureLoaderLoad load,
					TextureLoaderUnload unload,
					Effekseer::Backend::GraphicsDeviceRef graphicsDevice,
					UnityGfxRenderer renderer);

	virtual ~TextureLoaderGL();

	virtual Effekseer::TextureRef Load(const EFK_CHAR* path, Effekseer::TextureType textureType);

	virtual void Unload(Effekseer::TextureRef source);
};

bool IsPowerOfTwo(uint32_t x) { return (x & (x - 1)) == 0; }

TextureLoaderGL::TextureLoaderGL(TextureLoaderLoad load,
								 TextureLoaderUnload unload,
								 Effekseer::Backend::GraphicsDeviceRef graphicsDevice,
								 UnityGfxRenderer renderer)
	: TextureLoader(load, unload), graphicsDevice_(graphicsDevice), gfxRenderer(renderer)
{
}

TextureLoaderGL::~TextureLoaderGL() {}

Effekseer::TextureRef TextureLoaderGL::Load(const EFK_CHAR* path, Effekseer::TextureType textureType)
{
	// リソーステーブルを検索して存在したらそれを使う
	auto it = resources.find((const char16_t*)path);
	if (it != resources.end())
	{
		it->second.referenceCount++;
		return it->second.textureDataPtr;
	}

	// Unityでテクスチャをロード
	int32_t width, height, format;
	int64_t textureID = reinterpret_cast<int64_t>(load((const char16_t*)path, &width, &height, &format));
	if (textureID == 0)
	{
		return nullptr;
	}
	// リソーステーブルに追加
	auto added = resources.insert(std::make_pair((const char16_t*)path, TextureResource()));
	TextureResource& res = added.first->second;

#if !defined(_WIN32)
	if (gfxRenderer != kUnityGfxRendererOpenGLES20 || (IsPowerOfTwo(res.textureDataPtr->Width) && IsPowerOfTwo(res.textureDataPtr->Height)))
	{
		// テクスチャのミップマップを生成する
		glBindTexture(GL_TEXTURE_2D, (GLuint)textureID);
		glGenerateMipmap(GL_TEXTURE_2D);
		glBindTexture(GL_TEXTURE_2D, 0);
	}
#endif

	textureData2NativePtr[res.textureDataPtr] = (void*)textureID;

	res.textureDataPtr = EffekseerRendererGL::CreateTexture(graphicsDevice_, textureID, true, [] {});

	return res.textureDataPtr;
}

void TextureLoaderGL::Unload(Effekseer::TextureRef source)
{
	if (source == nullptr)
	{
		return;
	}

	// アンロードするテクスチャを検索
	auto it = std::find_if(resources.begin(), resources.end(), [source](const std::pair<std::u16string, TextureResource>& pair) {
		return pair.second.textureDataPtr == source;
	});
	if (it == resources.end())
	{
		return;
	}

	// 参照カウンタが0になったら実際にアンロード
	it->second.referenceCount--;
	if (it->second.referenceCount <= 0)
	{
		// Unity側でアンロード
		unload(it->first.c_str(), textureData2NativePtr[source]);
		textureData2NativePtr.erase(source);
		resources.erase(it);
	}
}

GraphicsGL::GraphicsGL(UnityGfxRenderer renderer) : gfxRenderer(renderer) { MaterialEvent::Initialize(); }

GraphicsGL::~GraphicsGL() {}

bool GraphicsGL::Initialize(IUnityInterfaces* unityInterfaces)
{

	switch (gfxRenderer)
	{
	case kUnityGfxRendererOpenGL:
		openglDeviceType = EffekseerRendererGL::OpenGLDeviceType::OpenGL2;
		break;
	case kUnityGfxRendererOpenGLES20:
		openglDeviceType = EffekseerRendererGL::OpenGLDeviceType::OpenGLES2;
		break;
	case kUnityGfxRendererOpenGLES30:
		openglDeviceType = EffekseerRendererGL::OpenGLDeviceType::OpenGLES3;
		break;
	case kUnityGfxRendererOpenGLCore:
		openglDeviceType = EffekseerRendererGL::OpenGLDeviceType::OpenGL3;
		break;
	}

	graphicsDevice_ = ::EffekseerRendererGL::CreateGraphicsDevice(openglDeviceType);

	return true;
}

void GraphicsGL::Shutdown(IUnityInterfaces* unityInterface)
{
	renderer_.Reset();
	graphicsDevice_.Reset();
	MaterialEvent::Terminate();
}

EffekseerRenderer::RendererRef GraphicsGL::CreateRenderer(int squareMaxCount, bool reversedDepth)
{
#ifdef __ANDROID__
	squareMaxCount = 600;
#endif
	auto renderer = EffekseerRendererGL::Renderer::Create(graphicsDevice_, squareMaxCount);
	renderer_ = renderer;
	return renderer;
}

void GraphicsGL::SetBackGroundTextureToRenderer(EffekseerRenderer::Renderer* renderer, void* backgroundTexture)
{
	((EffekseerRendererGL::Renderer*)renderer)->SetBackground((GLuint)(uintptr_t)backgroundTexture);
}

void GraphicsGL::EffekseerSetBackGroundTexture(int renderId, void* texture) { renderSettings[renderId].backgroundTexture = texture; }

Effekseer::TextureLoaderRef GraphicsGL::Create(TextureLoaderLoad load, TextureLoaderUnload unload)
{
	return Effekseer::MakeRefPtr<TextureLoaderGL>(load, unload, graphicsDevice_, gfxRenderer);
}

Effekseer::ModelLoaderRef GraphicsGL::Create(ModelLoaderLoad load, ModelLoaderUnload unload)
{
	auto loader = Effekseer::MakeRefPtr<ModelLoader>(load, unload);

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
	auto internalLoader = EffekseerRendererGL::CreateModelLoader(loader->GetFileInterface());
#else
	auto internalLoader = new EffekseerRendererGL::ModelLoader(loader->GetFileInterface());
#endif
	loader->SetInternalLoader(internalLoader);
	return loader;
}

Effekseer::MaterialLoaderRef GraphicsGL::Create(MaterialLoaderLoad load, MaterialLoaderUnload unload)
{
	auto loader = Effekseer::MakeRefPtr<MaterialLoader>(load, unload);
	auto internalLoader = ::EffekseerRendererGL::CreateMaterialLoader(graphicsDevice_);
	auto holder = std::make_shared<MaterialLoaderHolder>(internalLoader);
	loader->SetInternalLoader(holder);
	return loader;
}

void GraphicsGL::ShiftViewportForStereoSinglePass(bool isShift)
{
	GLint viewport[4];
	glGetIntegerv(GL_VIEWPORT, viewport);
	if (isShift)
		viewport[0] = viewport[2];
	else
		viewport[0] = 0;
	glViewport(viewport[0], viewport[1], viewport[2], viewport[3]);
}

} // namespace EffekseerPlugin