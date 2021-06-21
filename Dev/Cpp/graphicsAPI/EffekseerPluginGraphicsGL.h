
#pragma once

#include "EffekseerPluginGraphics.h"

#ifdef EMSCRIPTEN
#define GL_GLEXT_PROTOTYPES
#define EGL_EGLEXT_PROTOTYPES
#endif // EMSCRIPTEN

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#include <EffekseerRendererGL/EffekseerRendererGL.h>
#else
#include <EffekseerRendererGL.h>
#endif

namespace EffekseerPlugin
{

class GraphicsGL : public Graphics
{
private:
	UnityGfxRenderer gfxRenderer;
	EffekseerRenderer::RendererRef renderer_ = nullptr;

	EffekseerRendererGL::OpenGLDeviceType openglDeviceType = EffekseerRendererGL::OpenGLDeviceType::OpenGL2;
	Effekseer::Backend::GraphicsDeviceRef graphicsDevice_ = nullptr;

public:
	GraphicsGL(UnityGfxRenderer renderer);

	~GraphicsGL() override;

	bool Initialize(IUnityInterfaces* unityInterface) override;

	void Shutdown(IUnityInterfaces* unityInterface) override;

	EffekseerRenderer::RendererRef CreateRenderer(int squareMaxCount, bool reversedDepth) override;

	void SetExternalTexture(int renderId, ExternalTextureType type, void* texture) override;

	Effekseer::TextureLoaderRef Create(TextureLoaderLoad load, TextureLoaderUnload unload, GetUnityIDFromPath getUnityId) override;

	Effekseer::ModelLoaderRef Create(ModelLoaderLoad load, ModelLoaderUnload unload, GetUnityIDFromPath getUnityId) override;

	Effekseer::MaterialLoaderRef Create(MaterialLoaderLoad load, MaterialLoaderUnload unload, GetUnityIDFromPath getUnityId) override;

	void ShiftViewportForStereoSinglePass(bool isShift) override;

	bool IsRequiredToFlipVerticallyWhenRenderToTexture() const override { return false; }
};

} // namespace EffekseerPlugin
