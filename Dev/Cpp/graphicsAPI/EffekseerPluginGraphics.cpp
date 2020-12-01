#include "EffekseerPluginGraphics.h"

#include "EffekseerPluginGraphicsUnity.h"

#ifdef _WIN32
#include "EffekseerPluginGraphicsDX11.h"
#endif

#ifdef _PS4
#include "../PS4/EffekseerPluginGraphicsPS4.h"
#endif

#ifdef _SWITCH
#include "../switch/EffekseerPluginGraphicsSwitch.h"
#endif

#ifdef _XBOXONE
#include "../XBoxOne/EffekseerPluginGraphicsXBoxOne.h"
#endif

#ifdef _XBOXONE_DX12
#include "../XBoxOneDX12/EffekseerPluginGraphicsXBoxOneDX12.h"
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

#ifdef _PS4
	{
		return new GraphicsPS4();
	}
#endif

#ifdef _SWITCH
	{
		return new GraphicsSwitch();
	}
#endif

#ifdef _XBOXONE
	{
		return new GraphicsXBoxOne();
	}
#endif

#ifdef _XBOXONE_DX12
	{
		return new GraphicsXBoxOneDX12();
	}
#endif

#ifdef _WIN32
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
