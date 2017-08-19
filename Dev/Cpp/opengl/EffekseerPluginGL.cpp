

#include "../common/EffekseerPluginCommon.h"
#include "../common/IUnityGraphics.h"

#include "EffekseerPluginGL.h"

#if _WIN32
#pragma comment(lib, "opengl32.lib")
#endif

using namespace Effekseer;
using namespace EffekseerPlugin;

#ifndef _WIN32

namespace EffekseerPlugin
{
	IUnityInterfaces*		g_UnityInterfaces = NULL;
	IUnityGraphics*			g_UnityGraphics = NULL;
	UnityGfxRenderer		g_UnityRendererType = kUnityGfxRendererOpenGLES20;

	Effekseer::Manager*				g_EffekseerManager = NULL;
	EffekseerRendererGL::Renderer*	g_EffekseerRenderer = NULL;
	int g_maxSquares = 8000;

	void InitRenderer()
	{
		using namespace EffekseerRendererGL;
		OpenGLDeviceType openglDeviceType = OpenGLDeviceType::OpenGL2;
		switch (g_UnityRendererType) {
			case kUnityGfxRendererOpenGL:
				openglDeviceType = OpenGLDeviceType::OpenGL2;
				break;
			case kUnityGfxRendererOpenGLES20:
				openglDeviceType = OpenGLDeviceType::OpenGLES2;
				break;
			case kUnityGfxRendererOpenGLES30:
				openglDeviceType = OpenGLDeviceType::OpenGLES3;
				break;
			case kUnityGfxRendererOpenGLCore:
				openglDeviceType = OpenGLDeviceType::OpenGL3;
				break;
			default:
				return;
		}
		
		g_EffekseerRenderer = EffekseerRendererGL::Renderer::Create(g_maxSquares, openglDeviceType);
		if (g_EffekseerRenderer == nullptr) {
			return;
		}
		
		g_EffekseerManager->SetSpriteRenderer(g_EffekseerRenderer->CreateSpriteRenderer());
		g_EffekseerManager->SetRibbonRenderer(g_EffekseerRenderer->CreateRibbonRenderer());
		g_EffekseerManager->SetRingRenderer(g_EffekseerRenderer->CreateRingRenderer());
		g_EffekseerManager->SetTrackRenderer(g_EffekseerRenderer->CreateTrackRenderer());
		g_EffekseerManager->SetModelRenderer(g_EffekseerRenderer->CreateModelRenderer());
	}
	
	void TermRenderer()
	{
		if (g_EffekseerRenderer != NULL) {
			g_EffekseerRenderer->Destroy();
			g_EffekseerRenderer = NULL;
		}
	}

	void SetBackGroundTexture(void *backgroundTexture)
	{
		// 背景テクスチャをセット
		((EffekseerRendererGL::Renderer*)g_EffekseerRenderer)->SetBackground(reinterpret_cast<GLuint>(backgroundTexture));
	}
	
	void UNITY_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType)
	{
		switch (eventType) {
		case kUnityGfxDeviceEventInitialize:
			g_UnityRendererType = g_UnityGraphics->GetRenderer();
			break;
		case kUnityGfxDeviceEventShutdown:
			g_UnityRendererType = kUnityGfxRendererNull;
			break;
		case kUnityGfxDeviceEventBeforeReset:
			break;
		case kUnityGfxDeviceEventAfterReset:
			break;
		}
	}
}

extern "C"
{
	// Unity plugin load event
	DLLEXPORT void UNITY_API UnityPluginLoad(IUnityInterfaces* unityInterfaces)
	{
		g_UnityInterfaces = unityInterfaces;
		g_UnityGraphics = g_UnityInterfaces->Get<IUnityGraphics>();

		g_UnityGraphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);

		// Run OnGraphicsDeviceEvent(initialize) manually on plugin load
		// to not miss the event in case the graphics device is already initialized
		OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
	}

	// Unity plugin unload event
	DLLEXPORT void UNITY_API UnityPluginUnload()
	{
		g_UnityGraphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
	}

	void UNITY_API EffekseerRender(int renderId)
	{
		if (g_EffekseerManager == NULL) {
			if (g_EffekseerRenderer != NULL) {
				// OpenGLコンテキストの都合上ここで終了処理する
				TermRenderer();
			}
			return;
		}
		if (g_EffekseerRenderer == NULL) {
			// OpenGLコンテキストの都合上ここで初期化する
			InitRenderer();
			
			if (g_EffekseerRenderer == NULL) {
				// 失敗したら終了処理
				if (g_EffekseerManager) {
					g_EffekseerManager->Destroy();
					g_EffekseerManager = NULL;
				}
				return;
			}
		}

		const RenderSettings& settings = renderSettings[renderId];

		// 行列をセット
		g_EffekseerRenderer->SetProjectionMatrix(settings.projectionMatrix);
		g_EffekseerRenderer->SetCameraMatrix(settings.cameraMatrix);
		
		// 背景テクスチャをセット
		SetBackGroundTexture(settings.backgroundTexture);
		
		// 描画実行(全体)
		g_EffekseerRenderer->BeginRendering();
		g_EffekseerManager->Draw();
		g_EffekseerRenderer->EndRendering();

		// 背景テクスチャを解除
		SetBackGroundTexture(nullptr);
	}


	DLLEXPORT UnityRenderingEvent UNITY_API EffekseerGetRenderFunc(int renderId)
	{
		return EffekseerRender;
	}

	DLLEXPORT void UNITY_API EffekseerInit(int maxInstances, int maxSquares, bool reversedDepth)
	{
		g_EffekseerManager = Effekseer::Manager::Create(maxInstances);
		if (g_EffekseerManager == nullptr) {
			return;
		}
	}

	DLLEXPORT void UNITY_API EffekseerTerm()
	{
		if (g_EffekseerManager != NULL) {
			g_EffekseerManager->Destroy();
			g_EffekseerManager = NULL;
		}
	}

	
	// 歪み用テクスチャ設定
	DLLEXPORT void UNITY_API EffekseerSetBackGroundTexture(int renderId, void* texture)
	{
		if (renderId >= 0 && renderId < MAX_RENDER_PATH) {
			renderSettings[renderId].backgroundTexture = texture;
		}
	}
}

#endif
