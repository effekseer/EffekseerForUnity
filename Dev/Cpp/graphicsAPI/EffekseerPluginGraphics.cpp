#include "EffekseerPluginGraphics.h"

#include "EffekseerPluginGraphicsUnity.h"

#ifdef _WIN32
#include "EffekseerPluginGraphicsDX11.h"
#include "EffekseerPluginGraphicsDX9.h"
#endif

#if defined(_WIN32) || defined(__APPLE__) || defined(__ANDROID__) || defined(EMSCRIPTEN)
#include "EffekseerPluginGraphicsGL.h"
#endif

namespace EffekseerPlugin
{
Graphics* Graphics::Create(UnityGfxRenderer renderer, bool isUnityRenderer, bool doFallback)
{
	if (isUnityRenderer)
	{
		return new GraphicsUnity();
	}

#ifdef _WIN32
	if (renderer == UnityGfxRenderer::kUnityGfxRendererD3D9)
	{
		return new GraphicsDX9();
	}

	if (renderer == UnityGfxRenderer::kUnityGfxRendererD3D11)
	{
		return new GraphicsDX11();
	}
#endif

#if defined(_WIN32) || defined(__APPLE__) || defined(__ANDROID__) || defined(EMSCRIPTEN)
	if (renderer == UnityGfxRenderer::kUnityGfxRendererOpenGLCore || renderer == UnityGfxRenderer::kUnityGfxRendererOpenGL ||
		renderer == UnityGfxRenderer::kUnityGfxRendererOpenGLES20 || renderer == UnityGfxRenderer::kUnityGfxRendererOpenGLES30)
	{
		return new GraphicsGL(renderer);
	}
#endif

	if (doFallback)
	{
		return new GraphicsUnity();
	}

	return nullptr;
}

} // namespace EffekseerPlugin
