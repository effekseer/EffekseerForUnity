#pragma warning (disable : 4005)

#include <assert.h>
#include <windows.h>
#include <shlwapi.h>
#include <functional>

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

#include "../windows/RenderThreadEventQueue.h"

#pragma comment(lib, "shlwapi.lib")

using namespace Effekseer;
using namespace EffekseerPlugin;

namespace EffekseerPlugin
{
	int32_t	g_maxInstances = 0;
	int32_t	g_maxSquares = 0;
	bool g_reversedDepth = false;
	bool g_isRightHandedCoordinate = false;

	IUnityInterfaces*		g_UnityInterfaces = NULL;
	IUnityGraphics*			g_UnityGraphics = NULL;
	UnityGfxRenderer		g_UnityRendererType = kUnityGfxRendererNull;
	IDirect3DDevice9*		g_D3d9Device = NULL;
	ID3D11Device*			g_D3d11Device = NULL;
	ID3D11DeviceContext*	g_D3d11Context = NULL;

	Effekseer::Manager*				g_EffekseerManager = NULL;
	EffekseerRenderer::Renderer*	g_EffekseerRenderer = NULL;

	bool					g_isOpenGLMode = false;
	bool					g_isInitialized = false;

	EffekseerRenderer::Renderer* CreateRendererOpenGL(int squareMaxCount)
	{
		auto renderer = EffekseerRendererGL::Renderer::Create(squareMaxCount, EffekseerRendererGL::OpenGLDeviceType::OpenGL3);
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
			g_isOpenGLMode = true;
			break;
		case kUnityGfxDeviceEventShutdown:
			g_isOpenGLMode = false;
			break;
		}
	}

	void InitRenderer()
	{
		if (g_EffekseerManager == nullptr) return;

		switch (g_UnityRendererType) {
		case kUnityGfxRendererD3D9:
			g_EffekseerRenderer = EffekseerPlugin::CreateRendererDX9( g_maxSquares, g_D3d9Device);
			break;
		case kUnityGfxRendererD3D11:
			g_EffekseerRenderer = EffekseerPlugin::CreateRendererDX11( g_maxSquares, g_reversedDepth, g_D3d11Device, g_D3d11Context );
			break;
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

		RenderThreadEventQueue::Initialize();
	}

	void TermRenderer()
	{
		for (int i = 0; i < MAX_RENDER_PATH; i++)
		{
			if (g_UnityRendererType == kUnityGfxRendererD3D11)
			{
				if (renderSettings[i].backgroundTexture)
				{
					((ID3D11ShaderResourceView*)renderSettings[i].backgroundTexture)->Release();
				}
			}
			renderSettings[i].backgroundTexture = nullptr;
		}

		RenderThreadEventQueue::GetInstance()->Execute();
		RenderThreadEventQueue::Terminate();

		if (g_EffekseerRenderer != NULL)
		{
			g_EffekseerRenderer->Destroy();
			g_EffekseerRenderer = NULL;
		}
	}

	void SetBackGroundTexture(void *backgroundTexture)
	{
		// 背景テクスチャをセット
		switch (g_UnityRendererType) {
		case kUnityGfxRendererD3D9:
			((EffekseerRendererDX9::Renderer*)g_EffekseerRenderer)->SetBackground((IDirect3DTexture9*)backgroundTexture);
			break;
		case kUnityGfxRendererD3D11:
			((EffekseerRendererDX11::Renderer*)g_EffekseerRenderer)->SetBackground((ID3D11ShaderResourceView*)backgroundTexture);
			break;
		case kUnityGfxRendererOpenGLCore:
			((EffekseerRendererGL::Renderer*)g_EffekseerRenderer)->SetBackground(reinterpret_cast<GLuint>(backgroundTexture));
			break;
		default:
			return;
		}
	}

	void UNITY_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType)
	{
		switch (eventType) {
		case kUnityGfxDeviceEventInitialize:
			g_UnityRendererType = g_UnityGraphics->GetRenderer();
			break;
		case kUnityGfxDeviceEventShutdown:
			TermRenderer();
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
	void UNITY_API UnityPluginLoad(IUnityInterfaces* unityInterfaces)
	{
		g_UnityInterfaces = unityInterfaces;
		g_UnityGraphics = g_UnityInterfaces->Get<IUnityGraphics>();
		g_UnityRendererType = g_UnityGraphics->GetRenderer();

		g_UnityGraphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);

		// Run OnGraphicsDeviceEvent(initialize) manually on plugin load
		// to not miss the event in case the graphics device is already initialized
		OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
	}

	// Unity plugin unload event
	void UNITY_API UnityPluginUnload()
	{
		g_UnityGraphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
	}

	void UNITY_API EffekseerRender(int renderId)
	{
		if (g_isInitialized == false)
		{
			if (g_EffekseerRenderer != nullptr)
			{
				// 遅延終了処理
				TermRenderer();
			}
			return;
		}
		else
		{
			if (g_EffekseerRenderer == nullptr)
			{
				// 遅延初期化処理
				InitRenderer();
			}
		}
		
		if (g_EffekseerManager == nullptr) return;
		if (g_EffekseerRenderer == nullptr) return;

		RenderSettings& settings = renderSettings[renderId];
		Effekseer::Matrix44 projectionMatrix, cameraMatrix;
		
		if (settings.stereoEnabled) {
			if (settings.stereoRenderCount == 0) {
				projectionMatrix = settings.leftProjectionMatrix;
				cameraMatrix = settings.leftCameraMatrix;
			} else if (settings.stereoRenderCount == 1) {
				projectionMatrix = settings.rightProjectionMatrix;
				cameraMatrix = settings.rightCameraMatrix;
			}
			settings.stereoRenderCount++;
		} else {
			projectionMatrix = settings.projectionMatrix;
			cameraMatrix = settings.cameraMatrix;
		}

		if (settings.renderIntoTexture && !g_isOpenGLMode)
		{
			// テクスチャに対してレンダリングするときは上下反転させる
			projectionMatrix.Values[1][1] = -projectionMatrix.Values[1][1];
		}

		// 行列をセット
		g_EffekseerRenderer->SetProjectionMatrix(projectionMatrix);
		g_EffekseerRenderer->SetCameraMatrix(cameraMatrix);

		// convert a right hand into a left hand
		::Effekseer::Vector3D cameraPosition;
		::Effekseer::Vector3D cameraFrontDirection;
		CalculateCameraDirectionAndPosition(cameraMatrix, cameraFrontDirection, cameraPosition);
		
		//if (!g_isRightHandedCoordinate)
		{
			cameraFrontDirection = -cameraFrontDirection;
			//cameraPosition.Z = -cameraPosition.Z;
		}

		g_EffekseerRenderer->SetCameraParameter(cameraFrontDirection, cameraPosition);
		
		// 背景テクスチャをセット
		SetBackGroundTexture(settings.backgroundTexture);

		// 描画実行(全体)
		RenderThreadEventQueue::GetInstance()->Execute();
		g_EffekseerRenderer->BeginRendering();
		g_EffekseerManager->Draw();
		g_EffekseerRenderer->EndRendering();
		
		// 背景テクスチャを解除
		SetBackGroundTexture(nullptr);
	}

	void UNITY_API EffekseerRenderFront(int renderId)
	{
		if (g_EffekseerManager == nullptr) return;
		if (g_EffekseerRenderer == nullptr) return;

		// Need not to assgin matrixes. Because these were assigned in EffekseerRenderBack

		RenderThreadEventQueue::GetInstance()->Execute();
		g_EffekseerRenderer->BeginRendering();
		g_EffekseerManager->DrawFront();
		g_EffekseerRenderer->EndRendering();

		// 背景テクスチャを解除
		SetBackGroundTexture(nullptr);
	}

	void UNITY_API EffekseerRenderBack(int renderId)
	{
		if (g_isInitialized == false)
		{
			if (g_EffekseerRenderer != nullptr)
			{
				// 遅延終了処理
				TermRenderer();
			}
			return;
		}
		else
		{
			if (g_EffekseerRenderer == nullptr)
			{
				// 遅延初期化処理
				InitRenderer();
			}
		}

		if (g_EffekseerManager == nullptr) return;
		if (g_EffekseerRenderer == nullptr) return;

		RenderSettings& settings = renderSettings[renderId];
		Effekseer::Matrix44 projectionMatrix, cameraMatrix;

		if (settings.stereoEnabled) {
			if (settings.stereoRenderCount == 0) {
				projectionMatrix = settings.leftProjectionMatrix;
				cameraMatrix = settings.leftCameraMatrix;
			}
			else if (settings.stereoRenderCount == 1) {
				projectionMatrix = settings.rightProjectionMatrix;
				cameraMatrix = settings.rightCameraMatrix;
			}
			settings.stereoRenderCount++;
		}
		else {
			projectionMatrix = settings.projectionMatrix;
			cameraMatrix = settings.cameraMatrix;
		}

		if (settings.renderIntoTexture && !g_isOpenGLMode)
		{
			// テクスチャに対してレンダリングするときは上下反転させる
			projectionMatrix.Values[1][1] = -projectionMatrix.Values[1][1];
		}

		// 行列をセット
		g_EffekseerRenderer->SetProjectionMatrix(projectionMatrix);
		g_EffekseerRenderer->SetCameraMatrix(cameraMatrix);

		// convert a right hand into a left hand
		::Effekseer::Vector3D cameraPosition;
		::Effekseer::Vector3D cameraFrontDirection;
		CalculateCameraDirectionAndPosition(cameraMatrix, cameraFrontDirection, cameraPosition);

		//if (!g_isRightHandedCoordinate)
		{
			cameraFrontDirection = -cameraFrontDirection;
		}

		g_EffekseerRenderer->SetCameraParameter(cameraFrontDirection, cameraPosition);

		// 背景テクスチャをセット
		SetBackGroundTexture(settings.backgroundTexture);

		// 描画実行(全体)
		RenderThreadEventQueue::GetInstance()->Execute();
		g_EffekseerRenderer->BeginRendering();
		g_EffekseerManager->DrawBack();
		g_EffekseerRenderer->EndRendering();
	}
	
	UnityRenderingEvent UNITY_API EffekseerGetRenderFunc(int renderId)
	{
		return EffekseerRender;
	}

	UnityRenderingEvent UNITY_API EffekseerGetRenderFrontFunc(int renderId)
	{
		return EffekseerRenderFront;
	}

	UnityRenderingEvent UNITY_API EffekseerGetRenderBackFunc(int renderId)
	{
		return EffekseerRenderBack;
	}

	void UNITY_API EffekseerInit(int maxInstances, int maxSquares, int reversedDepth, int isRightHandedCoordinate)
	{
		g_isInitialized = true;

		g_maxInstances = maxInstances;
		g_maxSquares = maxSquares;
		g_reversedDepth = reversedDepth != 0;
		g_isOpenGLMode = (g_UnityRendererType == kUnityGfxRendererOpenGLCore);
		g_isRightHandedCoordinate = isRightHandedCoordinate != 0;

		g_EffekseerManager = Effekseer::Manager::Create(maxInstances);

		if (g_isRightHandedCoordinate)
		{
			g_EffekseerManager->SetCoordinateSystem(Effekseer::CoordinateSystem::RH);
		}
		else
		{
			g_EffekseerManager->SetCoordinateSystem(Effekseer::CoordinateSystem::LH);
		}

		if (!g_isOpenGLMode)
		{
			InitRenderer();
		}
	}

	void UNITY_API EffekseerTerm()
	{
		if (g_EffekseerManager != NULL) {
			g_EffekseerManager->Destroy();
			g_EffekseerManager = NULL;
		}

		if (!g_isOpenGLMode)
		{
			TermRenderer();
		}

		g_isInitialized = false;
	}
	
	// 歪み用テクスチャ設定
	void UNITY_API EffekseerSetBackGroundTexture(int renderId, void* texture)
	{
		if (renderId >= 0 && renderId < MAX_RENDER_PATH) {
			if (g_UnityRendererType == kUnityGfxRendererD3D11)
			{
				HRESULT hr;
				
				// DX11の場合、Unityから渡されるのはID3D11Texture2Dなので、
				// ID3D11ShaderResourceViewを作成する

				ID3D11Texture2D* textureDX11 = (ID3D11Texture2D*)texture;
				ID3D11ShaderResourceView* srv = (ID3D11ShaderResourceView*)renderSettings[renderId].backgroundTexture;
				
				if (srv != nullptr)
				{
					ID3D11Resource* res = nullptr;
					srv->GetResource(&res);
					if (res != texture)
					{
						// 違うTextureが指定されたら一旦削除
						srv->Release();
						srv = nullptr;
						renderSettings[renderId].backgroundTexture = nullptr;
					}
				}

				if (srv == nullptr)
				{
					D3D11_TEXTURE2D_DESC texDesc;
					textureDX11->GetDesc(&texDesc);
				
					D3D11_SHADER_RESOURCE_VIEW_DESC desc;
					ZeroMemory(&desc, sizeof(desc));
					// フォーマットを調整する
					switch (texDesc.Format)
					{
					case DXGI_FORMAT_R8G8B8A8_TYPELESS:
						desc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
						break;
					case DXGI_FORMAT_R16G16B16A16_TYPELESS:
						desc.Format = DXGI_FORMAT_R16G16B16A16_FLOAT;
						break;
					default:
						desc.Format = texDesc.Format;
						break;
					}
					desc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
					desc.Texture2D.MostDetailedMip = 0;
					desc.Texture2D.MipLevels = texDesc.MipLevels;
					hr = g_D3d11Device->CreateShaderResourceView(textureDX11, &desc, &srv);
					if (SUCCEEDED(hr))
					{
						renderSettings[renderId].backgroundTexture = srv;
					}
				}
			}
			else	// DX9 or OpenGL
			{
				renderSettings[renderId].backgroundTexture = texture;
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