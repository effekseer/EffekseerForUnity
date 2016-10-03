#pragma warning (disable : 4005)

#include <assert.h>
#include <windows.h>
#include <shlwapi.h>

#include "Effekseer.h"

#include "EffekseerRendererGL.h"
#include "EffekseerRendererDX9.h"
#include "EffekseerRendererDX11.h"

#include "../common/EffekseerPluginCommon.h"
#include "../common/IUnityGraphics.h"
#include "../common/IUnityGraphicsD3D9.h"
#include "../common/IUnityGraphicsD3D11.h"

#include "../opengl/EffekseerPluginGL.h"

#include "../Device/EffekseerPluginDX9.h"
#include "../Device/EffekseerPluginDX11.h"

#pragma comment(lib, "shlwapi.lib")

using namespace Effekseer;
using namespace EffekseerPlugin;

namespace EffekseerPlugin
{
	int32_t	g_maxInstances = 0;
	int32_t	g_maxSquares = 0;

	IUnityInterfaces*		g_UnityInterfaces = NULL;
	IUnityGraphics*			g_UnityGraphics = NULL;
	UnityGfxRenderer		g_UnityRendererType = kUnityGfxRendererNull;
	IDirect3DDevice9*		g_D3d9Device = NULL;
	ID3D11Device*			g_D3d11Device = NULL;
	ID3D11DeviceContext*	g_D3d11Context = NULL;

	Effekseer::Manager*				g_EffekseerManager = NULL;
	EffekseerRenderer::Renderer*	g_EffekseerRenderer = NULL;

	bool					g_isOpenGLMode = false;
	bool					g_isOpenGLInitialized = false;

	EffekseerRenderer::Renderer* CreateRendererOpenGL(int squareMaxCount)
	{
		auto renderer = EffekseerRendererGL::Renderer::Create(squareMaxCount, EffekseerRendererGL::OpenGLDeviceType::OpenGL3);
		renderer->SetDistortingCallback(new DistortingCallbackGL(renderer));
		return renderer;
	}

	void OnGraphicsDeviceEventD3D9(UnityGfxDeviceEventType eventType)
	{
		switch (eventType) {
		case kUnityGfxDeviceEventInitialize:
			g_D3d9Device = g_UnityInterfaces->Get<IUnityGraphicsD3D9>()->GetDevice();
			break;
		case kUnityGfxDeviceEventAfterReset:
			break;
		case kUnityGfxDeviceEventBeforeReset:
		case kUnityGfxDeviceEventShutdown:
			break;
		}
	}

	void OnGraphicsDeviceEventD3D11(UnityGfxDeviceEventType eventType)
	{
		switch (eventType) {
		case kUnityGfxDeviceEventInitialize:
			g_D3d11Device = g_UnityInterfaces->Get<IUnityGraphicsD3D11>()->GetDevice();
			// fall throuth
		case kUnityGfxDeviceEventAfterReset:
			g_D3d11Device->GetImmediateContext(&g_D3d11Context);
			break;
		case kUnityGfxDeviceEventBeforeReset:
		case kUnityGfxDeviceEventShutdown:
			if (g_D3d11Context != NULL) {
				g_D3d11Context->Release();
				g_D3d11Context = NULL;
			}
			break;
		}
	}

	void OnGraphicsDeviceEventOpenGL(UnityGfxDeviceEventType eventType)
	{
		switch (eventType)
		{
		case kUnityGfxDeviceEventInitialize:
			g_UnityRendererType = g_UnityGraphics->GetRenderer();
			g_isOpenGLMode = true;
			g_isOpenGLInitialized = false;
			break;

		case kUnityGfxDeviceEventShutdown:
			g_UnityRendererType = kUnityGfxRendererNull;
			break;
		}
	}

	void InitRenderer()
	{
		if (g_EffekseerManager == nullptr) return;

		switch (g_UnityRendererType)
		{
		case kUnityGfxRendererOpenGLCore:
			g_EffekseerRenderer = CreateRendererOpenGL(g_maxSquares);
			break;
		default:
			return;
		}

		if (g_EffekseerRenderer == nullptr)
		{
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
		if (g_EffekseerRenderer != NULL)
		{
			g_EffekseerRenderer->Destory();
			g_EffekseerRenderer = NULL;
		}
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

		switch (g_UnityRendererType)
		{
		case kUnityGfxRendererD3D9:
			OnGraphicsDeviceEventD3D9(eventType);
			break;
		case kUnityGfxRendererD3D11:
			OnGraphicsDeviceEventD3D11(eventType);
			break;
		case kUnityGfxRendererOpenGLCore:
			OnGraphicsDeviceEventOpenGL(eventType);
			break;
		default:
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
		// 遅延処理
		if (g_isOpenGLMode && !g_isOpenGLInitialized)
		{
			InitRenderer();
			g_isOpenGLInitialized = true;
		}

		if (g_isOpenGLMode && g_EffekseerManager == nullptr)
		{
			TermRenderer();
		}

		if (g_EffekseerManager == nullptr) return;
		if (g_EffekseerRenderer == nullptr) return;

		const RenderSettings& settings = renderSettings[renderId];
		Effekseer::Matrix44 projectionMatrix = settings.projectionMatrix;
		if (settings.renderIntoTexture && !g_isOpenGLMode)
		{
			// テクスチャに対してレンダリングするときは上下反転させる
			projectionMatrix.Values[1][1] = -projectionMatrix.Values[1][1];
		}

		// 行列をセット
		g_EffekseerRenderer->SetProjectionMatrix(projectionMatrix);
		g_EffekseerRenderer->SetCameraMatrix(settings.cameraMatrix);

		// 描画実行(全体)
		g_EffekseerRenderer->BeginRendering();
		g_EffekseerManager->Draw();
		g_EffekseerRenderer->EndRendering();
	}
	
	DLLEXPORT UnityRenderingEvent UNITY_API EffekseerGetRenderFunc(int renderId)
	{
		return EffekseerRender;
	}

	DLLEXPORT void UNITY_API EffekseerInit(int maxInstances, int maxSquares, bool reversedDepth)
	{
		g_maxInstances = maxInstances;
		g_maxSquares = maxSquares;

		switch (g_UnityRendererType) {
		case kUnityGfxRendererD3D9:
			g_EffekseerRenderer = EffekseerPlugin::CreateRendererDX9( maxSquares, g_D3d9Device);
			break;
		case kUnityGfxRendererD3D11:
			g_EffekseerRenderer = EffekseerPlugin::CreateRendererDX11( maxSquares, reversedDepth, g_D3d11Device, g_D3d11Context );
			break;
		case kUnityGfxRendererOpenGLCore:
			return;
			g_isOpenGLMode = true;
			break;
		default:
			return;
		}

		g_EffekseerManager = Effekseer::Manager::Create(maxInstances);

		if (!g_isOpenGLMode)
		{
			g_EffekseerManager->SetSpriteRenderer(g_EffekseerRenderer->CreateSpriteRenderer());
			g_EffekseerManager->SetRibbonRenderer(g_EffekseerRenderer->CreateRibbonRenderer());
			g_EffekseerManager->SetRingRenderer(g_EffekseerRenderer->CreateRingRenderer());
			g_EffekseerManager->SetTrackRenderer(g_EffekseerRenderer->CreateTrackRenderer());
			g_EffekseerManager->SetModelRenderer(g_EffekseerRenderer->CreateModelRenderer());
		}
	}

	DLLEXPORT void UNITY_API EffekseerTerm()
	{
		if (g_EffekseerManager != NULL) {
			g_EffekseerManager->Destroy();
			g_EffekseerManager = NULL;
		}

		if (!g_isOpenGLMode)
		{
			if (g_EffekseerRenderer != NULL)
			{
				g_EffekseerRenderer->Destory();
				g_EffekseerRenderer = NULL;
			}
		}
	}
}

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
	bool res = true;
	switch (fdwReason) {
	case DLL_PROCESS_ATTACH:
		CoInitializeEx(NULL, COINIT_MULTITHREADED);
		break;
	case DLL_PROCESS_DETACH:
		CoUninitialize();
		break;
	case DLL_THREAD_ATTACH:
		CoInitializeEx(NULL, COINIT_MULTITHREADED);
		break;
	case DLL_THREAD_DETACH:
		CoUninitialize();
		break;
	default:
		break;
	}
	return res;
}