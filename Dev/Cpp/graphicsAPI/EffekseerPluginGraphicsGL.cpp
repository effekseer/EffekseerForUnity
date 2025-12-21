
#include "EffekseerPluginGraphicsGL.h"
#include <algorithm>
#include <assert.h>

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#else
#include <EffekseerRendererGL/EffekseerRendererGL.MaterialLoader.h>
#endif

#include "../common/EffekseerPluginMaterial.h"

namespace EffekseerPlugin
{

class TextureLoaderGL : public TextureLoader
{
	Effekseer::Backend::GraphicsDeviceRef graphicsDevice_;
	IDtoResourceTable<Effekseer::TextureRef> id2Texture_;
	UnityGfxRenderer gfxRenderer;

public:
	TextureLoaderGL(TextureLoaderLoad load,
					TextureLoaderUnload unload,
					GetUnityIDFromPath getUnityId,
					Effekseer::Backend::GraphicsDeviceRef graphicsDevice,
					UnityGfxRenderer renderer);

	virtual ~TextureLoaderGL();

	virtual Effekseer::TextureRef Load(const EFK_CHAR* path, Effekseer::TextureType textureType);

	virtual void Unload(Effekseer::TextureRef source);
};

bool IsPowerOfTwo(uint32_t x) { return (x & (x - 1)) == 0; }

TextureLoaderGL::TextureLoaderGL(TextureLoaderLoad load,
								 TextureLoaderUnload unload,
								 GetUnityIDFromPath getUnityId,
								 Effekseer::Backend::GraphicsDeviceRef graphicsDevice,
								 UnityGfxRenderer renderer)
	: TextureLoader(load, unload, getUnityId), graphicsDevice_(graphicsDevice), gfxRenderer(renderer)
{
}

TextureLoaderGL::~TextureLoaderGL() {}

Effekseer::TextureRef TextureLoaderGL::Load(const EFK_CHAR* path, Effekseer::TextureType textureType)
{
	auto id = getUnityId_(path);

	Effekseer::TextureRef generated;

	if (id2Texture_.TryLoad(id, generated))
	{
		return generated;
	}

	// Load with unity
	int32_t width, height, format, miplevel;
	auto texturePtr = load((const char16_t*)path, &width, &height, &format, &miplevel);
	int64_t textureID = reinterpret_cast<int64_t>(texturePtr);
	if (textureID == 0)
	{
		return nullptr;
	}

	auto backend = EffekseerRendererGL::CreateTexture(graphicsDevice_, textureID, miplevel > 1, [] {});
	auto textureDataPtr = Effekseer::MakeRefPtr<Effekseer::Texture>();
	textureDataPtr->SetBackend(backend);

	id2Texture_.Register(id, textureDataPtr, texturePtr);

	return textureDataPtr;
}

void TextureLoaderGL::Unload(Effekseer::TextureRef source)
{
	if (source == nullptr)
	{
		return;
	}

	int32_t id{};
	void* nativePtr{};
	if (id2Texture_.Unload(source, id, nativePtr))
	{
		unload(id, nativePtr);
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

void GraphicsGL::SetExternalTexture(int renderId, ExternalTextureType type, void* texture)
{
	auto& externalTexture = renderSettings[renderId].externalTextures[static_cast<int>(type)];

	// not changed
	if (externalTexture.OriginalPtr == texture)
	{
		return;
	}

	if (texture == nullptr)
	{
		externalTexture.Reset();
		return;
	}

	if (texture != nullptr)
	{
		externalTexture.Texture = EffekseerRendererGL::CreateTexture(graphicsDevice_, (GLuint)(uintptr_t)texture, false, []() -> void {});
		externalTexture.OriginalPtr = texture;
	}
	else
	{
		externalTexture.Reset();
	}
}

Effekseer::TextureLoaderRef GraphicsGL::Create(TextureLoaderLoad load, TextureLoaderUnload unload, GetUnityIDFromPath getUnityId)
{
	return Effekseer::MakeRefPtr<TextureLoaderGL>(load, unload, getUnityId, graphicsDevice_, gfxRenderer);
}

Effekseer::ModelLoaderRef GraphicsGL::Create(ModelLoaderLoad load, ModelLoaderUnload unload, GetUnityIDFromPath getUnityId)
{
	auto loader = Effekseer::MakeRefPtr<ModelLoader>(load, unload, getUnityId);

	auto internalLoader = EffekseerRenderer::CreateModelLoader(graphicsDevice_, loader->GetFileInterface());
	loader->SetInternalLoader(internalLoader);
	return loader;
}

Effekseer::MaterialLoaderRef GraphicsGL::Create(MaterialLoaderLoad load, MaterialLoaderUnload unload, GetUnityIDFromPath getUnityId)
{
	auto loader = Effekseer::MakeRefPtr<MaterialLoader>(load, unload, getUnityId);
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
