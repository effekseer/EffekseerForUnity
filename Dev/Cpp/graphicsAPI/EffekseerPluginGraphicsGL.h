
#pragma once

#include "EffekseerPluginGraphics.h"

#ifdef EMSCRIPTEN
#define GL_GLEXT_PROTOTYPES
#define EGL_EGLEXT_PROTOTYPES
#endif // EMSCRIPTEN

#include <Effekseer.h>
#include <EffekseerRendererGL.h>

namespace EffekseerPlugin
{

class GraphicsGL : public Graphics
{
private:
	UnityGfxRenderer gfxRenderer;
	EffekseerRendererGL::Renderer* renderer_ = nullptr;

	EffekseerRendererGL::OpenGLDeviceType openglDeviceType = EffekseerRendererGL::OpenGLDeviceType::OpenGL2;

public:
	GraphicsGL(UnityGfxRenderer renderer);

	virtual ~GraphicsGL();

	bool Initialize(IUnityInterfaces* unityInterface) override;

	void Shutdown(IUnityInterfaces* unityInterface) override;

	EffekseerRenderer::Renderer* CreateRenderer(int squareMaxCount, bool reversedDepth) override;

	void SetBackGroundTextureToRenderer(EffekseerRenderer::Renderer* renderer, void* backgroundTexture) override;

	void EffekseerSetBackGroundTexture(int renderId, void* texture) override;

	Effekseer::TextureLoader* Create(TextureLoaderLoad load, TextureLoaderUnload unload) override;

	Effekseer::ModelLoader* Create(ModelLoaderLoad load, ModelLoaderUnload unload) override;

	Effekseer::MaterialLoader* Create(MaterialLoaderLoad load, MaterialLoaderUnload unload) override;

	void ShiftViewportForStereoSinglePass(bool isShift) override;

	bool IsRequiredToFlipVerticallyWhenRenderToTexture() const override { return false; }
};

} // namespace EffekseerPlugin
