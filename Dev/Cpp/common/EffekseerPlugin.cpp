
#include <assert.h>
#include <mutex>
#include <unordered_map>

#ifdef _WIN32
#pragma warning(disable : 4005)
#include <shlwapi.h>
#include <windows.h>
#pragma comment(lib, "shlwapi.lib")
#endif

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#include <Effekseer/Effekseer.h>
#else
#include "Effekseer.h"
#endif

#include "../common/EffekseerPluginCommon.h"
#include "../unity/IUnityGraphics.h"
#include "../unity/IUnityRenderingExtensions.h"

#ifdef __APPLE__
#import <TargetConditionals.h>
#endif

// OpenGL
#if defined(_WIN32) || defined(__ANDROID__) || defined(EMSCRIPTEN) || (defined(__APPLE__) && !(TARGET_OS_IPHONE || TARGET_IPHONE_SIMULATOR))

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#include <EffekseerRendererGL/EffekseerRendererGL.h>
#else
#include "EffekseerRendererGL.h"
#endif

#else

#include <EffekseerRenderer.Renderer.h>

#endif

// DirectX
#ifdef _WIN32
#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#include <EffekseerRendererDX11/EffekseerRendererDX11.h>
#include <EffekseerRendererDX9/EffekseerRendererDX9.h>
#else
#include "EffekseerRendererDX11.h"
#include "EffekseerRendererDX9.h"
#endif
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

#include "../common/EffekseerPluginMaterial.h"
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
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerUnityPluginLoad(IUnityInterfaces* unityInterfaces);

	// Unity plugin unload event
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerUnityPluginUnload();
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
bool g_maintainGammaColor = false;

IUnityInterfaces* g_UnityInterfaces = NULL;
IUnityGraphics* g_UnityGraphics = NULL;

#if (defined(__APPLE__) && (TARGET_OS_IPHONE || TARGET_IPHONE_SIMULATOR)) || defined(__ANDROID__) || defined(EMSCRIPTEN) || defined(_SWITCH)
// TODO adhoc code
UnityGfxRenderer g_UnityRendererType = kUnityGfxRendererOpenGLES20;
#else
UnityGfxRenderer g_UnityRendererType = kUnityGfxRendererNull;
#endif

Graphics* g_graphics = nullptr;

Effekseer::ManagerRef g_EffekseerManager = NULL;
EffekseerRenderer::RendererRef g_EffekseerRenderer = NULL;
float g_time = 0.0f;
Effekseer::Vector3D g_lightDirection = Effekseer::Vector3D(1, 1, -1);
Effekseer::Color g_lightColor = Effekseer::Color(255, 255, 255);
Effekseer::Color g_lightAmbientColor = Effekseer::Color(40, 40, 40);

bool g_isRunning = false;

std::unordered_map<int, std::shared_ptr<RenderPass>> g_frontRenderPasses;
std::unordered_map<int, std::shared_ptr<RenderPass>> g_backRenderPasses;
std::unordered_map<int, std::shared_ptr<RenderPass>> g_renderPasses;

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

	g_EffekseerRenderer->SetMaintainGammaColorInLinearColorSpace(g_maintainGammaColor);
}

void TermRenderer()
{
#ifdef _WIN32
	for (int i = 0; i < MAX_RENDER_PATH; i++)
	{
		for (size_t j = 0; j < renderSettings[i].externalTextures.size(); j++)
		{
			renderSettings[i].externalTextures[j].Reset();
		}
	}
#endif

	if (g_graphics != nullptr)
	{
		g_graphics->WaitFinish();
	}

	g_EffekseerRenderer.Reset();

	g_frontRenderPasses.clear();
	g_backRenderPasses.clear();
	g_renderPasses.clear();

	g_removingRenderPathMutex.lock();
	g_removingRenderPathes.clear();
	g_removingRenderPathMutex.unlock();
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
		UnityRegisterRenderingPluginV5(EffekseerUnityPluginLoad, EffekseerUnityPluginUnload);
#elif defined(EMSCRIPTEN) || defined(_SWITCH)
		UnityRegisterRenderingPlugin(EffekseerUnityPluginLoad, EffekseerUnityPluginUnload);
#else
		printf("Warning : Check preprocesser.\n");
#endif

		g_IsEffekseerPluginRegistered = true;
	}

	// Unity plugin load event
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerUnityPluginLoad(IUnityInterfaces* unityInterfaces)
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
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerUnityPluginUnload()
	{
		g_UnityGraphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
	}

#if (defined(__APPLE__) && (TARGET_OS_IPHONE || TARGET_IPHONE_SIMULATOR)) || defined(EMSCRIPTEN) || defined(_SWITCH)
	// None
#else
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces)
	{
		EffekseerUnityPluginLoad(unityInterfaces);
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API UnityPluginUnload() { EffekseerUnityPluginUnload(); }
#endif

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
			g_renderPasses.erase(id);
			g_frontRenderPasses.erase(id);
			g_backRenderPasses.erase(id);
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

		// call events
		{
			auto instance = MaterialEvent::GetInstance();
			if (instance != nullptr)
			{
				instance->Execute();
			}
		}

		{
			auto instance = RenderThreadEvent::GetInstance();
			if (instance != nullptr)
			{
				instance->Execute();
			}
		}

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
		settings.id = renderId;
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

		// if renderer is some backend, render flipped image when render to a texture.
		if (settings.renderIntoTexture && g_graphics->IsRequiredToFlipVerticallyWhenRenderToTexture())
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

		g_EffekseerRenderer->SetCameraParameter(cameraFrontDirection, cameraPosition);

		Effekseer::Manager::LayerParameter layerParam;
		layerParam.ViewerPosition = cameraPosition;
		g_EffekseerManager->SetLayerParameter(0, layerParam);

		// Specify textures
		if (g_graphics != nullptr)
		{
			g_graphics->SetBackGroundTextureToRenderer(
				g_EffekseerRenderer.Get(), settings.externalTextures[static_cast<int>(ExternalTextureType::Background)].Texture);
			g_graphics->SetDepthTextureToRenderer(g_EffekseerRenderer.Get(),
												  projectionMatrix,
												  settings.externalTextures[static_cast<int>(ExternalTextureType::Depth)].Texture);
		}

		// render

		std::shared_ptr<RenderPass> renderPath = nullptr;
		auto it = g_renderPasses.find(renderId);
		if (it != g_renderPasses.end())
		{
			renderPath = it->second;
		}
		else
		{
			auto created = g_graphics->CreateRenderPass();
			if (created != nullptr)
			{
				g_renderPasses[renderId] = std::shared_ptr<RenderPass>(created);
				renderPath = g_renderPasses[renderId];
			}
		}

		if (renderPath != nullptr)
		{
			renderPath->Begin(settings, nullptr);
			g_graphics->SetRenderPath(g_EffekseerRenderer.Get(), renderPath.get());
		}

		Effekseer::Manager::DrawParameter drawParameter;
		drawParameter.CameraCullingMask = settings.cameraCullingMask;
		drawParameter.IsSortingEffectsEnabled = true;
		drawParameter.CameraPosition = cameraPosition;
		drawParameter.CameraFrontDirection = cameraFrontDirection;
		g_EffekseerRenderer->SetTime(g_time);
		g_EffekseerRenderer->BeginRendering();
		g_EffekseerManager->Draw(drawParameter);
		g_EffekseerRenderer->EndRendering();

		if (renderPath != nullptr)
		{
			renderPath->End(settings);
			renderPath->Execute(settings);
			g_graphics->SetRenderPath(g_EffekseerRenderer.Get(), nullptr);
		}

		if (g_graphics != nullptr)
		{
			g_graphics->SetBackGroundTextureToRenderer(g_EffekseerRenderer.Get(), nullptr);
			g_graphics->SetDepthTextureToRenderer(g_EffekseerRenderer.Get(), projectionMatrix, nullptr);
		}
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerRenderFront(int renderId)
	{
		if (g_EffekseerManager == nullptr)
			return;
		if (g_EffekseerRenderer == nullptr)
			return;

		RenderSettings& settings = renderSettings[renderId];
		settings.id = renderId;

		std::shared_ptr<RenderPass> renderPass = nullptr;
		auto it = g_frontRenderPasses.find(renderId);
		if (it != g_frontRenderPasses.end())
		{
			renderPass = it->second;
		}
		else
		{
			auto created = g_graphics->CreateRenderPass();
			if (created != nullptr)
			{
				g_frontRenderPasses[renderId] = std::shared_ptr<RenderPass>(created);
				renderPass = g_frontRenderPasses[renderId];
			}
		}

		if (renderPass != nullptr)
		{
			std::shared_ptr<RenderPass> backRenderPass = nullptr;
			auto itb = g_backRenderPasses.find(renderId);
			if (itb != g_backRenderPasses.end())
			{
				backRenderPass = itb->second;
			}

			renderPass->Begin(settings, backRenderPass.get());
			g_graphics->SetRenderPath(g_EffekseerRenderer.Get(), renderPass.get());
		}

		Effekseer::Matrix44 projectionMatrix, cameraMatrix;

		projectionMatrix = g_EffekseerRenderer->GetProjectionMatrix();
		cameraMatrix = g_EffekseerRenderer->GetCameraMatrix();

		// convert a right hand into a left hand
		::Effekseer::Vector3D cameraPosition;
		::Effekseer::Vector3D cameraFrontDirection;
		CalculateCameraDirectionAndPosition(cameraMatrix, cameraFrontDirection, cameraPosition);

		Effekseer::Manager::LayerParameter layerParam;
		layerParam.ViewerPosition = cameraPosition;
		g_EffekseerManager->SetLayerParameter(0, layerParam);

		// Need not to assgin matrixes. Because these were assigned in EffekseerRenderBack
		Effekseer::Manager::DrawParameter drawParameter;
		drawParameter.CameraCullingMask = settings.cameraCullingMask;
		drawParameter.IsSortingEffectsEnabled = true;
		drawParameter.CameraPosition = cameraPosition;
		drawParameter.CameraFrontDirection = cameraFrontDirection;
		g_EffekseerRenderer->SetTime(g_time);
		g_EffekseerRenderer->BeginRendering();
		g_EffekseerManager->DrawFront(drawParameter);
		g_EffekseerRenderer->EndRendering();

		if (renderPass != nullptr)
		{
			renderPass->End(settings);
			renderPass->Execute(settings);
			g_graphics->SetRenderPath(g_EffekseerRenderer.Get(), nullptr);
		}

		if (g_graphics != nullptr)
		{
			g_graphics->SetBackGroundTextureToRenderer(g_EffekseerRenderer.Get(), nullptr);
			g_graphics->SetDepthTextureToRenderer(g_EffekseerRenderer.Get(), projectionMatrix, nullptr);
		}

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
		// g_graphics->StartRender(g_EffekseerRenderer);

		TryToRemoveRenderPathes();

		// call events
		{
			auto instance = MaterialEvent::GetInstance();
			if (instance != nullptr)
			{
				instance->Execute();
			}
		}

		{
			auto instance = RenderThreadEvent::GetInstance();
			if (instance != nullptr)
			{
				instance->Execute();
			}
		}

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
		settings.id = renderId;

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

		// if renderer is some backend, render flipped image when render to a texture.
		if (settings.renderIntoTexture && g_graphics->IsRequiredToFlipVerticallyWhenRenderToTexture())
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

		Effekseer::Manager::LayerParameter layerParam;
		layerParam.ViewerPosition = cameraPosition;
		g_EffekseerManager->SetLayerParameter(0, layerParam);

		g_EffekseerRenderer->SetCameraParameter(cameraFrontDirection, cameraPosition);

		// Specify textures
		if (g_graphics != nullptr)
		{
			g_graphics->SetBackGroundTextureToRenderer(
				g_EffekseerRenderer.Get(), settings.externalTextures[static_cast<int>(ExternalTextureType::Background)].Texture);
			g_graphics->SetDepthTextureToRenderer(g_EffekseerRenderer.Get(),
												  projectionMatrix,
												  settings.externalTextures[static_cast<int>(ExternalTextureType::Depth)].Texture);
		}

		std::shared_ptr<RenderPass> renderPath = nullptr;
		auto it = g_backRenderPasses.find(renderId);
		if (it != g_backRenderPasses.end())
		{
			renderPath = it->second;
		}
		else
		{
			auto created = g_graphics->CreateRenderPass();
			if (created != nullptr)
			{
				g_backRenderPasses[renderId] = std::shared_ptr<RenderPass>(created);
				renderPath = g_backRenderPasses[renderId];
			}
		}

		if (renderPath != nullptr)
		{
			renderPath->Begin(settings, nullptr);
			g_graphics->SetRenderPath(g_EffekseerRenderer.Get(), renderPath.get());
		}

		// render
		Effekseer::Manager::DrawParameter drawParameter;
		drawParameter.CameraCullingMask = settings.cameraCullingMask;
		drawParameter.IsSortingEffectsEnabled = true;
		drawParameter.CameraPosition = cameraPosition;
		drawParameter.CameraFrontDirection = cameraFrontDirection;

		g_EffekseerRenderer->SetTime(g_time);
		g_EffekseerRenderer->SetLightColor(g_lightColor);
		g_EffekseerRenderer->SetLightAmbientColor(g_lightAmbientColor);
		g_EffekseerRenderer->SetLightDirection(g_lightDirection);
		g_EffekseerRenderer->BeginRendering();
		g_EffekseerManager->DrawBack(drawParameter);
		g_EffekseerRenderer->EndRendering();

		if (renderPath != nullptr)
		{
			renderPath->End(settings);
			renderPath->Execute(settings);
			g_graphics->SetRenderPath(g_EffekseerRenderer.Get(), nullptr);
		}
	}

	UNITY_INTERFACE_EXPORT UnityRenderingEvent UNITY_INTERFACE_API EffekseerGetRenderFunc(int renderId) { return EffekseerRender; }

	UNITY_INTERFACE_EXPORT UnityRenderingEvent UNITY_INTERFACE_API EffekseerGetRenderFrontFunc(int renderId)
	{
		return EffekseerRenderFront;
	}

	UNITY_INTERFACE_EXPORT UnityRenderingEvent UNITY_INTERFACE_API EffekseerGetRenderBackFunc(int renderId) { return EffekseerRenderBack; }

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerInit(int maxInstances,
																  int maxSquares,
																  int reversedDepth,
																  int maintainGammaColor,
																  int isRightHandedCoordinate,
																  int threadCount,
																  int rendererType)
	{
		g_maxInstances = maxInstances;
		g_maxSquares = maxSquares;
		g_reversedDepth = reversedDepth != 0;
		g_maintainGammaColor = maintainGammaColor != 0;
		g_isRightHandedCoordinate = isRightHandedCoordinate != 0;
		g_rendererType = (RendererType)rendererType;

		g_EffekseerManager = Effekseer::Manager::Create(maxInstances);
		g_EffekseerManager->GetSetting()->SetIsFileCacheEnabled(false);

#ifndef __EMSCRIPTEN__
		if (threadCount >= 2)
		{
			g_EffekseerManager->LaunchWorkerThreads(threadCount);
		}
#endif

		if (g_isRightHandedCoordinate)
		{
			g_EffekseerManager->SetCoordinateSystem(Effekseer::CoordinateSystem::RH);
		}
		else
		{
			g_EffekseerManager->SetCoordinateSystem(Effekseer::CoordinateSystem::LH);
		}

		g_time = 0.0f;

		RenderThreadEvent::Initialize();

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
		g_EffekseerManager.Reset();

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

		RenderThreadEvent::Terminate();
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API EffekseerSetExternalTexture(int renderId, ExternalTextureType type, void* texture)
	{
		if (g_graphics != nullptr)
		{
			g_graphics->SetExternalTexture(renderId, type, texture);
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
}

namespace EffekseerPlugin
{

Effekseer::RefPtr<Effekseer::TextureLoader>
TextureLoader::Create(TextureLoaderLoad load, TextureLoaderUnload unload, EffekseerPlugin::GetUnityIDFromPath getUnityId)
{
	if (g_graphics != nullptr)
		return g_graphics->Create(load, unload, getUnityId);
	return {};
}

Effekseer::RefPtr<Effekseer::ModelLoader> ModelLoader::Create(ModelLoaderLoad load, ModelLoaderUnload unload, GetUnityIDFromPath getUnityId)
{
	if (g_graphics != nullptr)
		return g_graphics->Create(load, unload, getUnityId);
	return {};
}

} // namespace EffekseerPlugin
