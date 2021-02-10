
#include "EffekseerPluginGraphicsGL.h"
#include <algorithm>
#include <assert.h>

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#else
#include <EffekseerRenderer/EffekseerRendererGL.MaterialLoader.h>
#endif

#include "../common/EffekseerPluginMaterial.h"

namespace EffekseerPlugin
{

class TextureLoaderGL : public TextureLoader
{
	Effekseer::Backend::GraphicsDeviceRef graphicsDevice_;
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
	// Load with unity
	int32_t width, height, format;
	int64_t textureID = reinterpret_cast<int64_t>(load((const char16_t*)path, &width, &height, &format));
	if (textureID == 0)
	{
		return nullptr;
	}

#if !defined(_WIN32)
	if (gfxRenderer != kUnityGfxRendererOpenGLES20 || (IsPowerOfTwo(width) && IsPowerOfTwo(height)))
	{
		glBindTexture(GL_TEXTURE_2D, (GLuint)textureID);
		glGenerateMipmap(GL_TEXTURE_2D);
		glBindTexture(GL_TEXTURE_2D, 0);
	}
#endif

	auto backend = EffekseerRendererGL::CreateTexture(graphicsDevice_, textureID, true, [] {});
	auto textureDataPtr = Effekseer::MakeRefPtr<Effekseer::Texture>();
	textureDataPtr->SetBackend(backend);
	textureData2NativePtr[textureDataPtr] = (void*)textureID;
	return textureDataPtr;
}

void TextureLoaderGL::Unload(Effekseer::TextureRef source)
{
	if (source == nullptr)
	{
		return;
	}

	unload(source->GetPath().c_str(), textureData2NativePtr[source]);
	textureData2NativePtr.erase(source);
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

void GraphicsGL::SetDepthTextureToRenderer(EffekseerRenderer::Renderer* renderer,
										   const Effekseer::Matrix44& projectionMatrix,
										   void* depthTexture)
{
	if (depthTexture == nullptr)
	{
		renderer->SetDepth(nullptr, EffekseerRenderer::DepthReconstructionParameter{});
		return;
	}

	EffekseerRenderer::DepthReconstructionParameter param;
	param.DepthBufferScale = 1.0f;
	param.DepthBufferOffset = 0.0f;
	param.ProjectionMatrix33 = projectionMatrix.Values[2][2];
	param.ProjectionMatrix43 = projectionMatrix.Values[2][3];
	param.ProjectionMatrix34 = projectionMatrix.Values[3][2];
	param.ProjectionMatrix44 = projectionMatrix.Values[3][3];

	auto texture = EffekseerRendererGL::CreateTexture(graphicsDevice_, (GLuint)(uintptr_t)depthTexture, false, []() -> void {});
	renderer->SetDepth(texture, param);
}

void GraphicsGL::SetExternalTexture(int renderId, ExternalTextureType type, void* texture)
{
	renderSettings[renderId].externalTextures[static_cast<int>(type)] = texture;
}

Effekseer::TextureLoaderRef GraphicsGL::Create(TextureLoaderLoad load, TextureLoaderUnload unload)
{
	return Effekseer::MakeRefPtr<TextureLoaderGL>(load, unload, graphicsDevice_, gfxRenderer);
}

Effekseer::ModelLoaderRef GraphicsGL::Create(ModelLoaderLoad load, ModelLoaderUnload unload)
{
	auto loader = Effekseer::MakeRefPtr<ModelLoader>(load, unload);

	auto internalLoader = EffekseerRendererGL::CreateModelLoader(loader->GetFileInterface());
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
