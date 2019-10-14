
#include <assert.h>
#include <unordered_map>
#include <mutex>

#ifdef _WIN32
#pragma warning(disable : 4005)
#include <shlwapi.h>
#include <windows.h>
#pragma comment(lib, "shlwapi.lib")
#endif

#include "Effekseer.h"

#include "../common/EffekseerPluginCommon.h"
#include "../unity/IUnityGraphics.h"
#include "../unity/IUnityRenderingExtensions.h"

#ifdef __APPLE__
#import <TargetConditionals.h>
#endif

// OpenGL
#if defined(_WIN32) || defined(__APPLE__) || defined(__ANDROID__) || defined(EMSCRIPTEN)
#include "EffekseerRendererGL.h"
#endif

// DirectX
#ifdef _WIN32
#include "EffekseerRendererDX11.h"
#include "EffekseerRendererDX9.h"
#endif

#ifdef _PS4
#include "EffekseerRendererPS4.h"
#endif

#ifdef _SWITCH
#include "EffekseerRendererSwitch.h"
#endif

#ifdef _XBOXONE
#include "EffekseerRendererXBoxOne.h"
#endif

#ifdef _XBOXONE_DX12
#include "EffekseerRendererXBoxOneDx12.h"
#endif

#include "../common/EffekseerPluginModel.h"
#include "../common/EffekseerPluginTexture.h"
#include "../graphicsAPI/EffekseerPluginGraphics.h"

// for static
typedef void(UNITY_INTERFACE_API* PluginLoadFunc)(IUnityInterfaces* unityInterfaces);
typedef void(UNITY_INTERFACE_API* PluginUnloadFunc)();

extern "C" void UnityRegisterRenderingPlugin(PluginLoadFunc loadPlugin, PluginUnloadFunc unloadPlugin);
extern "C" void UnityRegisterRenderingPluginV5(PluginLoadFunc loadPlugin, PluginUnloadFunc unloadPlugin);

extern "C"
{
	// Unity plugin load event
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces);

	// Unity plugin unload event
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API UnityPluginUnload();
}

namespace EffekseerPlugin
{
int32_t g_maxInstances = 0;
int32_t g_maxSquares = 0;
RendererType g_rendererType = RendererType::Native;

bool g_reversedDepth = false;
bool g_isTextureFlipped = false;
bool g_isBackgroundTextureFlipped = false;
bool g_isRightHandedCoordinate = false;

IUnityInterfaces* g_UnityInterfaces = NULL;
IUnityGraphics* g_UnityGraphics = NULL;

#if (defined(__APPLE__) && (TARGET_OS_IPHONE || TARGET_IPHONE_SIMULATOR)) || defined(__ANDROID__) || defined(EMSCRIPTEN) || defined(_SWITCH)
// TODO adhoc code
UnityGfxRenderer g_UnityRendererType = kUnityGfxRendererOpenGLES20;
#else
UnityGfxRenderer g_UnityRendererType = kUnityGfxRendererNull;
#endif

Graphics* g_graphics = nullptr;

Effekseer::Manager* g_EffekseerManager = NULL;
EffekseerRenderer::Renderer* g_EffekseerRenderer = NULL;

bool g_isRunning = false;

std::unordered_map<int, std::shared_ptr<RenderPath>> g_frontRenderPathes;
std::unordered_map<int, std::shared_ptr<RenderPath>> g_backRenderPathes;
std::unordered_map<int, std::shared_ptr<RenderPath>> g_renderPathes;

std::vector<int32_t> g_removingRenderPathes;
std::mutex g_removingRenderPathMutex;

bool IsRequiredToInitOnRenderThread()
{
	if (g_rendererType == RendererType::Unity)
		return false;

	if (g_UnityRendererType == UnityGfxRenderer::kUnityGfxRendererOpenGL)
		return true;
	if (g_UnityRendererType == UnityGfxRenderer::kUnityGfxRendererOpenGLCore)
		return true;
	if (g_UnityRendererType == UnityGfxRenderer::kUnityGfxRendererOpenGLES20)
		return true;
	if (g_UnityRendererType == UnityGfxRenderer::kUnityGfxRendererOpenGLES30)
		return true;

	return false;
}

bool IsOpenGLRenderer()
{
	if (g_UnityRendererType == UnityGfxRenderer::kUnityGfxRendererOpenGL)
		return true;
	if (g_UnityRendererType == UnityGfxRenderer::kUnityGfxRendererOpenGLCore)
		return true;
	if (g_UnityRendererType == UnityGfxRenderer::kUnityGfxRendererOpenGLES20)
		return true;
	if (g_UnityRendererType == UnityGfxRenderer::kUnityGfxRendererOpenGLES30)
		return true;

	return false;
}

void InitRenderer()
{
	if (g_EffekseerManager == nullptr)
		return;

	if (g_rendererType == RendererType::Native)
	{
		g_EffekseerRenderer = g_graphics->CreateRenderer(g_maxSquares, g_reversedDepth);
	}
	else if (g_rendererType == RendererType::Unity)
	{
		g_EffekseerRenderer = g_graphics->CreateRenderer(g_maxSquares, g_reversedDepth);
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

	// light a model
	g_EffekseerRenderer->SetLightColor(Effekseer::Color(255, 255, 255, 255));
	g_EffekseerRenderer->SetLightDirection(Effekseer::Vector3D(1, 1, -1));
	g_EffekseerRenderer->SetLightAmbientColor(Effekseer::Color(40, 40, 40, 255));
}

void TermRenderer()
{
#ifdef _WIN32
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
#endif

	if (g_graphics != nullptr)
	{
		g_graphics->WaitFinish();
	}

	if (g_EffekseerRenderer != NULL)
	{
		g_EffekseerRenderer->Destroy();
		g_EffekseerRenderer = NULL;
	}

	g_frontRenderPathes.clear();
	g_backRenderPathes.clear();
	g_renderPathes.clear();

	g_removingRenderPathMutex.lock();
	g_removingRenderPathes.clear();
	g_removingRenderPathMutex.unlock();
}

void SetBackGroundTexture(void* backgroundTexture)
{
	if (g_graphics != nullptr)
		g_graphics->SetBackGroundTextureToRenderer(g_EffekseerRenderer, backgroundTexture);
}

UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType)
{
	switch (eventType)
	{
	case kUnityGfxDeviceEventInitialize:
		g_UnityRendererType = g_UnityGraphics->GetRenderer();
		break;
	case kUnityGfxDeviceEventShutdown:
		TermRenderer();
		g_UnityRendererType = kUnityGfxRendererNull;

		if (g_graphics != nullptr)
		{
			g_graphics->Shutdown(g_UnityInterfaces);
			ES_SAFE_DELETE(g_graphics);
		}

		break;
	case kUnityGfxDeviceEventBeforeReset:
		if (g_graphics != nullptr)
			g_graphics->BeforeReset(g_UnityInterfaces);
		break;
	case kUnityGfxDeviceEventAfterReset:
		if (g_graphics != nullptr)
			g_graphics->AfterReset(g_UnityInterfaces);
		break;
	}
}
} // namespace EffekseerPlugin

using namespace EffekseerPlugin;

extern "C"
{
	static bool g_IsEffekseerPluginRegistered = false;
	static int g_eyeIndex = 0;

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API RegisterPlugin()
	{
		if (g_IsEffekseerPluginRegistered)
			return;

#if (defined(__APPLE__) && (TARGET_OS_IPHONE || TARGET_IPHONE_SIMULATOR))
		UnityRegisterRenderingPluginV5(UnityPluginLoad, UnityPluginUnload);
#elif defined(EMSCRIPTEN) || defined(_SWITCH)
		UnityRegisterRenderingPlugin(UnityPluginLoad, UnityPluginUnload);
#else
		printf("Warning : Check preprocesser.\n");
#endif

		g_IsEffekseerPluginRegistered = true;
	}

	// Unity plugin load event
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces)
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
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API UnityPluginUnload()
	{
		g_UnityGraphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
	}

	void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityRenderingExtEvent(UnityRenderingExtEventType event, void* data)
	{
		UnityRenderingExtBeforeDrawCallParams* param = nullptr;
		switch (event)
		{
		case kUnityRenderingExtEventSetStereoTarget:
			break;
		case kUnityRenderingExtEventSetStereoEye:
			break;
		case kUnityRenderingExtEventBeforeDrawCall:
			param = (UnityRenderingExtBeforeDrawCallParams*)data;
			if (param != nullptr)
			{
				g_eyeIndex = param->eyeIndex;
			}

			break;
		case kUnityRenderingExtEventAfterDrawCall:
			break;
		}
	}

	void TryToRemoveRenderPathes()
	{
		g_removingRenderPathMutex.lock();
		for (auto id : g_removingRenderPathes)
		{
			g_renderPathes.erase(id);
			g_frontRenderPathes.erase(id);
			g_backRenderPathes.erase(id);
		}
		g_removingRenderPathes.clear();
		g_removingRenderPathMutex.unlock();
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerRender(int renderId)
	{
		if (!g_isRunning)
		{
			if (g_EffekseerRenderer != nullptr)
			{
				// terminate (Delay)
				TermRenderer();
			}
			return;
		}
		else
		{
			if (g_EffekseerRenderer == nullptr)
			{
				// initialize (Delay)
				InitRenderer();
			}
		}

		if (g_EffekseerManager == nullptr)
			return;
		if (g_EffekseerRenderer == nullptr)
			return;

		assert(g_graphics != nullptr);
		// g_graphics->StartRender(g_EffekseerRenderer);

		TryToRemoveRenderPathes();

		// assign flipped
		if (g_isTextureFlipped)
		{
			g_EffekseerRenderer->SetTextureUVStyle(EffekseerRenderer::UVStyle::VerticalFlipped);
		}
		else
		{
			g_EffekseerRenderer->SetTextureUVStyle(EffekseerRenderer::UVStyle::Normal);
		}

		if (g_isBackgroundTextureFlipped)
		{
			g_EffekseerRenderer->SetBackgroundTextureUVStyle(EffekseerRenderer::UVStyle::VerticalFlipped);
		}
		else
		{
			g_EffekseerRenderer->SetBackgroundTextureUVStyle(EffekseerRenderer::UVStyle::Normal);
		}

		RenderSettings& settings = renderSettings[renderId];
		Effekseer::Matrix44 projectionMatrix, cameraMatrix;

		if (settings.stereoEnabled)
		{
			if (settings.stereoRenderCount == 0)
			{
				projectionMatrix = settings.leftProjectionMatrix;
				cameraMatrix = settings.leftCameraMatrix;
			}
			else if (settings.stereoRenderCount == 1)
			{
				projectionMatrix = settings.rightProjectionMatrix;
				cameraMatrix = settings.rightCameraMatrix;
			}
			settings.stereoRenderCount++;
		}
		else
		{
			projectionMatrix = settings.projectionMatrix;
			cameraMatrix = settings.cameraMatrix;
		}

		// if renderer is not opengl, render flipped image when render to a texture.
		if (settings.renderIntoTexture && !IsOpenGLRenderer())
		{
			projectionMatrix.Values[1][1] = -projectionMatrix.Values[1][1];
		}

		// 行列をセット
		g_EffekseerRenderer->SetProjectionMatrix(projectionMatrix);
		g_EffekseerRenderer->SetCameraMatrix(cameraMatrix);

		// convert a right hand into a left hand
		::Effekseer::Vector3D cameraPosition;
		::Effekseer::Vector3D cameraFrontDirection;
		CalculateCameraDirectionAndPosition(cameraMatrix, cameraFrontDirection, cameraPosition);

		// if (!g_isRightHandedCoordinate)
		{
			cameraFrontDirection = -cameraFrontDirection;
			// cameraPosition.Z = -cameraPosition.Z;
		}

		g_EffekseerRenderer->SetCameraParameter(cameraFrontDirection, cameraPosition);

		// 背景テクスチャをセット
		SetBackGroundTexture(settings.backgroundTexture);

		// render

		std::shared_ptr<RenderPath> renderPath = nullptr;
		auto it = g_renderPathes.find(renderId);
		if (it != g_renderPathes.end())
		{
			renderPath = it->second;
		}
		else
		{
			auto created = g_graphics->CreateRenderPath();
			if (created != nullptr)
			{
				g_renderPathes[renderId] = std::shared_ptr<RenderPath>(created);
				renderPath = g_renderPathes[renderId];
			}
		}

		if (renderPath != nullptr)
		{
			renderPath->Begin();
			g_graphics->SetRenderPath(g_EffekseerRenderer, renderPath.get());
		}

		Effekseer::Manager::DrawParameter drawParameter;
		drawParameter.CameraCullingMask = settings.cameraCullingMask;
		g_EffekseerRenderer->BeginRendering();
		g_EffekseerManager->Draw(drawParameter);
		g_EffekseerRenderer->EndRendering();

		if (renderPath != nullptr)
		{
			renderPath->End();
			renderPath->Execute();
			g_graphics->SetRenderPath(g_EffekseerRenderer, nullptr);
		}

		// 背景テクスチャを解除
		SetBackGroundTexture(nullptr);
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerRenderFront(int renderId)
	{
		if (g_EffekseerManager == nullptr)
			return;
		if (g_EffekseerRenderer == nullptr)
			return;

		RenderSettings& settings = renderSettings[renderId];

		std::shared_ptr<RenderPath> renderPath = nullptr;
		auto it = g_frontRenderPathes.find(renderId);
		if (it != g_frontRenderPathes.end())
		{
			renderPath = it->second;
		}
		else
		{
			auto created = g_graphics->CreateRenderPath();
			if (created != nullptr)
			{
				g_frontRenderPathes[renderId] = std::shared_ptr<RenderPath>(created);
				renderPath = g_frontRenderPathes[renderId];
			}
		}

		if (renderPath != nullptr)
		{
			renderPath->Begin();
			g_graphics->SetRenderPath(g_EffekseerRenderer, renderPath.get());
		}

		// Need not to assgin matrixes. Because these were assigned in EffekseerRenderBack
		Effekseer::Manager::DrawParameter drawParameter;
		drawParameter.CameraCullingMask = settings.cameraCullingMask;
		g_EffekseerRenderer->BeginRendering();
		g_EffekseerManager->DrawFront(drawParameter);
		g_EffekseerRenderer->EndRendering();

		if (renderPath != nullptr)
		{
			renderPath->End();
			renderPath->Execute();
			g_graphics->SetRenderPath(g_EffekseerRenderer, nullptr);
		}

		// 背景テクスチャを解除
		SetBackGroundTexture(nullptr);

		// Viewportを初期化
		if (settings.stereoRenderingType == StereoRenderingType::SinglePass)
		{
			g_graphics->ShiftViewportForStereoSinglePass(false);
		}
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerRenderBack(int renderId)
	{
		if (!g_isRunning)
		{
			if (g_EffekseerRenderer != nullptr)
			{
				// terminate (Delay)
				TermRenderer();
			}
			return;
		}
		else
		{
			if (g_EffekseerRenderer == nullptr)
			{
				// initialize (Delay)
				InitRenderer();
			}
		}

		if (g_EffekseerManager == nullptr)
			return;
		if (g_EffekseerRenderer == nullptr)
			return;

		assert(g_graphics != nullptr);
		//g_graphics->StartRender(g_EffekseerRenderer);

		TryToRemoveRenderPathes();

		// assign flipped
		if (g_isTextureFlipped)
		{
			g_EffekseerRenderer->SetTextureUVStyle(EffekseerRenderer::UVStyle::VerticalFlipped);
		}
		else
		{
			g_EffekseerRenderer->SetTextureUVStyle(EffekseerRenderer::UVStyle::Normal);
		}

		if (g_isBackgroundTextureFlipped)
		{
			g_EffekseerRenderer->SetBackgroundTextureUVStyle(EffekseerRenderer::UVStyle::VerticalFlipped);
		}
		else
		{
			g_EffekseerRenderer->SetBackgroundTextureUVStyle(EffekseerRenderer::UVStyle::Normal);
		}

		RenderSettings& settings = renderSettings[renderId];
		Effekseer::Matrix44 projectionMatrix, cameraMatrix, cameraPositionMatrix;

		if (settings.stereoEnabled)
		{
			if (settings.stereoRenderCount == 0)
			{
				projectionMatrix = settings.leftProjectionMatrix;
				cameraMatrix = settings.leftCameraMatrix;

				if (settings.stereoRenderingType == StereoRenderingType::SinglePass)
				{
					g_graphics->ShiftViewportForStereoSinglePass(false);
				}
			}
			else if (settings.stereoRenderCount == 1)
			{
				projectionMatrix = settings.rightProjectionMatrix;
				cameraMatrix = settings.rightCameraMatrix;

				if (settings.stereoRenderingType == StereoRenderingType::SinglePass)
				{
					g_graphics->ShiftViewportForStereoSinglePass(true);
				}
			}
			cameraPositionMatrix = settings.cameraMatrix;

			settings.stereoRenderCount++;
		}
		else
		{
			projectionMatrix = settings.projectionMatrix;
			cameraMatrix = settings.cameraMatrix;
			cameraPositionMatrix = settings.cameraMatrix;
		}

		// if renderer is not opengl, render flipped image when render to a texture.
		if (settings.renderIntoTexture && !IsOpenGLRenderer())
		{
			projectionMatrix.Values[1][1] = -projectionMatrix.Values[1][1];
		}

		// 行列をセット
		g_EffekseerRenderer->SetProjectionMatrix(projectionMatrix);
		g_EffekseerRenderer->SetCameraMatrix(cameraMatrix);

		// convert a right hand into a left hand
		::Effekseer::Vector3D cameraPosition;
		::Effekseer::Vector3D cameraFrontDirection;
		CalculateCameraDirectionAndPosition(cameraPositionMatrix, cameraFrontDirection, cameraPosition);

		// if (!g_isRightHandedCoordinate)
		{
			cameraFrontDirection = -cameraFrontDirection;
		}

		g_EffekseerRenderer->SetCameraParameter(cameraFrontDirection, cameraPosition);

		// 背景テクスチャをセット
		SetBackGroundTexture(settings.backgroundTexture);

		std::shared_ptr<RenderPath> renderPath = nullptr;
		auto it = g_backRenderPathes.find(renderId);
		if (it != g_backRenderPathes.end())
		{
			renderPath = it->second;
		}
		else
		{
			auto created = g_graphics->CreateRenderPath();
			if (created != nullptr)
			{
				g_backRenderPathes[renderId] = std::shared_ptr<RenderPath>(created);
				renderPath = g_backRenderPathes[renderId];
			}
		}

		if (renderPath != nullptr)
		{
			renderPath->Begin();
			g_graphics->SetRenderPath(g_EffekseerRenderer, renderPath.get());
		}


		// render
		Effekseer::Manager::DrawParameter drawParameter;
		drawParameter.CameraCullingMask = settings.cameraCullingMask;
		g_EffekseerRenderer->BeginRendering();
		g_EffekseerManager->DrawBack(drawParameter);
		g_EffekseerRenderer->EndRendering();

		if (renderPath != nullptr)
		{
			renderPath->End();
			renderPath->Execute();
			g_graphics->SetRenderPath(g_EffekseerRenderer, nullptr);
		}
	}

	UNITY_INTERFACE_EXPORT UnityRenderingEvent UNITY_INTERFACE_API EffekseerGetRenderFunc(int renderId) { return EffekseerRender; }

	UNITY_INTERFACE_EXPORT UnityRenderingEvent UNITY_INTERFACE_API EffekseerGetRenderFrontFunc(int renderId)
	{
		return EffekseerRenderFront;
	}

	UNITY_INTERFACE_EXPORT UnityRenderingEvent UNITY_INTERFACE_API EffekseerGetRenderBackFunc(int renderId) { return EffekseerRenderBack; }

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API
	EffekseerInit(int maxInstances, int maxSquares, int reversedDepth, int isRightHandedCoordinate, int rendererType)
	{
		g_maxInstances = maxInstances;
		g_maxSquares = maxSquares;
		g_reversedDepth = reversedDepth != 0;
		g_isRightHandedCoordinate = isRightHandedCoordinate != 0;
		g_rendererType = (RendererType)rendererType;

		g_EffekseerManager = Effekseer::Manager::Create(maxInstances);

		if (g_isRightHandedCoordinate)
		{
			g_EffekseerManager->SetCoordinateSystem(Effekseer::CoordinateSystem::RH);
		}
		else
		{
			g_EffekseerManager->SetCoordinateSystem(Effekseer::CoordinateSystem::LH);
		}

		assert(g_graphics == nullptr);
		if (g_rendererType == RendererType::Native)
		{
			g_graphics = Graphics::Create(g_UnityRendererType, false, true);
			g_graphics->Initialize(g_UnityInterfaces);
		}
		else
		{
			g_graphics = Graphics::Create(g_UnityRendererType, true, true);
			g_graphics->Initialize(g_UnityInterfaces);
		}

		g_isRunning = true;

		if (IsRequiredToInitOnRenderThread())
		{
			// initialize on render thread
		}
		else
		{
			InitRenderer();
		}
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerTerm()
	{
		if (g_EffekseerManager != NULL)
		{
			g_EffekseerManager->Destroy();
			g_EffekseerManager = NULL;
		}

		if (IsRequiredToInitOnRenderThread())
		{
			// term on render thread
		}
		else
		{
			TermRenderer();
		}

		g_isRunning = false;

		if (g_graphics != nullptr)
		{
			g_graphics->Shutdown(g_UnityInterfaces);
			ES_SAFE_DELETE(g_graphics);
		}
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetBackGroundTexture(int renderId, void* texture)
	{
		if (g_graphics != nullptr)
		{
			g_graphics->EffekseerSetBackGroundTexture(renderId, texture);
		}
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetIsTextureFlipped(int isFlipped) { g_isTextureFlipped = isFlipped; }

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetIsBackgroundTextureFlipped(int isFlipped)
	{
		g_isBackgroundTextureFlipped = isFlipped;
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerAddRemovingRenderPath(int renderID)
	{
		g_removingRenderPathMutex.lock();
		g_removingRenderPathes.push_back(renderID);
		g_removingRenderPathMutex.unlock();
	}

	Effekseer::TextureLoader* TextureLoader::Create(TextureLoaderLoad load, TextureLoaderUnload unload)
	{
		if (g_graphics != nullptr)
			return g_graphics->Create(load, unload);
		return nullptr;
	}

	Effekseer::ModelLoader* ModelLoader::Create(ModelLoaderLoad load, ModelLoaderUnload unload)
	{
		if (g_graphics != nullptr)
			return g_graphics->Create(load, unload);
		return nullptr;
	}
}

#ifdef _WIN32

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
	bool res = true;
	switch (fdwReason)
	{
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

#endif
