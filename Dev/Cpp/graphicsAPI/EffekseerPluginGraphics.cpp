#include "EffekseerPluginGraphics.h"

#include "EffekseerPluginGraphicsUnity.h"

#ifdef __APPLE__
#import <TargetConditionals.h>
#endif

#ifdef _WIN32
#include "EffekseerPluginGraphicsDX11.h"
#endif

#ifdef _DX12
#include "EffekseerPluginGraphicsDX12.h"
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

#if defined(_WIN32) || defined(__ANDROID__) || defined(EMSCRIPTEN) || (defined(__APPLE__) && !(TARGET_OS_IPHONE || TARGET_IPHONE_SIMULATOR))
#include "EffekseerPluginGraphicsGL.h"
#endif

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#include <EffekseerRendererCommon/EffekseerRenderer.Renderer.h>
#else
#include <EffekseerRendererCommon/EffekseerRenderer.Renderer.h>
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

#ifdef _DX12
	if (renderer == UnityGfxRenderer::kUnityGfxRendererD3D12)
	{
		return new GraphicsDX12();
	}
#endif

#ifdef _WIN32
	if (renderer == UnityGfxRenderer::kUnityGfxRendererD3D11)
	{
		return new GraphicsDX11();
	}
#endif

#if defined(_WIN32) || defined(__ANDROID__) || defined(EMSCRIPTEN) || (defined(__APPLE__) && !(TARGET_OS_IPHONE || TARGET_IPHONE_SIMULATOR))
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

void Graphics::SetBackGroundTextureToRenderer(EffekseerRenderer::Renderer* renderer, Effekseer::Backend::TextureRef backgroundTexture)
{
	renderer->SetBackground(backgroundTexture);
}

void Graphics::SetDepthTextureToRenderer(EffekseerRenderer::Renderer* renderer,
										 const Effekseer::Matrix44& projectionMatrix,
										 Effekseer::Backend::TextureRef depthTexture)
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

	renderer->SetDepth(depthTexture, param);
}

Effekseer::ProceduralModelGeneratorRef Graphics::Create(ProceduralModelGeneratorGenerate generate,
														ProceduralModelGeneratorUngenerate ungenerate)
{
	return Effekseer::MakeRefPtr<ProceduralModelGenerator>();
}

} // namespace EffekseerPlugin
