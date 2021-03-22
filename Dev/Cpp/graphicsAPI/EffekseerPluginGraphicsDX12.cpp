
#include "EffekseerPluginGraphicsDX12.h"
#include "../unity/IUnityGraphics.h"
#include "../unity/IUnityInterface.h"
#include <algorithm>
#include <assert.h>

#include "../common/EffekseerPluginMaterial.h"

/*
	The code is under development.
	It only runs without MSAA and HDR on Standard Renderpipeline in 2020 or later
*/

namespace EffekseerPlugin
{

bool RenderPassDX12::Initialize(IUnityInterfaces* unityInterface,
								EffekseerRenderer::RendererRef renderer,
								Effekseer::Backend::GraphicsDeviceRef device)
{
	unityInterface_ = unityInterface;
	renderer_ = renderer;
	memoryPool_ = EffekseerRenderer::CreateSingleFrameMemoryPool(device);
	commandList_ = EffekseerRenderer::CreateCommandList(device, memoryPool_);

	return true;
}

void RenderPassDX12::Begin(RenderSettings& setting, RenderPass* backRenderPass)
{
	if (memoryPool_ != nullptr)
	{
		memoryPool_->NewFrame();
	}

	EffekseerRenderer::RenderPassPipelineStateKey key;
	key.RenderTargetFormats[0] = setting.renderTargetType;
	key.DepthFormat = setting.depthTargetType;
	EffekseerRenderer::ChangeRenderPassPipelineState(renderer_, key);

	if (commandList_ != nullptr)
	{
		// 2020 or later
		auto dx12Interface = unityInterface_->Get<IUnityGraphicsD3D12v6>();

		UnityD3D12PluginEventConfig config;
		config.graphicsQueueAccess = kUnityD3D12GraphicsQueueAccess_DontCare;
		config.flags = kUnityD3D12EventConfigFlag_ModifiesCommandBuffersState;
		config.ensureActiveRenderTextureIsBound = true;
		dx12Interface->ConfigureEvent(setting.id, &config);

		UnityGraphicsD3D12RecordingState recordingState;
		if (dx12Interface->CommandRecordingState(&recordingState))
		{
			EffekseerRendererDX12::BeginCommandList(commandList_, recordingState.commandList);

			// 2021 requires to specify vieports at least
			D3D12_RECT rects[1];
			D3D12_VIEWPORT viewports[1];
			{
				rects[0].top = 0;
				rects[0].left = 0;
				rects[0].right = setting.screenWidth;
				rects[0].bottom = setting.screenHeight;

				viewports[0].TopLeftX = 0.0f;
				viewports[0].TopLeftY = 0.0f;
				viewports[0].Width = static_cast<float>(setting.screenWidth);
				viewports[0].Height = static_cast<float>(setting.screenHeight);
				viewports[0].MinDepth = 0.0f;
				viewports[0].MaxDepth = 1.0f;
			}
			recordingState.commandList->RSSetScissorRects(1, rects);
			recordingState.commandList->RSSetViewports(1, viewports);
		}
	}
}

void RenderPassDX12::End(RenderSettings& setting)
{
	if (commandList_ != nullptr)
	{
		EffekseerRendererDX12::EndCommandList(commandList_);
	}
}

void RenderPassDX12::Execute(RenderSettings& setting)
{
	// None
}

class TextureLoaderDX12 : public TextureLoader
{
	std::map<Effekseer::TextureRef, void*> textureData2NativePtr;
	Effekseer::Backend::GraphicsDeviceRef graphicsDevice_;

public:
	TextureLoaderDX12(TextureLoaderLoad load, TextureLoaderUnload unload, Effekseer::Backend::GraphicsDeviceRef graphicsDevice)
		: TextureLoader(load, unload), graphicsDevice_(graphicsDevice)
	{
	}

	virtual ~TextureLoaderDX12() override = default;

	virtual Effekseer::TextureRef Load(const EFK_CHAR* path, Effekseer::TextureType textureType)
	{
		// Load from unity
		int32_t width, height, format;
		void* texturePtr = load((const char16_t*)path, &width, &height, &format);
		if (texturePtr == nullptr)
		{
			return nullptr;
		}

		// Convert
		ID3D12Resource* resource = reinterpret_cast<ID3D12Resource*>(texturePtr);
		auto backend = EffekseerRendererDX12::CreateTexture(graphicsDevice_, resource);

		auto textureDataPtr = Effekseer::MakeRefPtr<Effekseer::Texture>();
		textureDataPtr->SetBackend(backend);

		textureData2NativePtr[textureDataPtr] = texturePtr;

		return textureDataPtr;
	}

	virtual void Unload(Effekseer::TextureRef source)
	{
		if (source == nullptr)
		{
			return;
		}

		unload(source->GetPath().c_str(), textureData2NativePtr[source]);
		textureData2NativePtr.erase(source);
	}
};

GraphicsDX12::GraphicsDX12() {}

GraphicsDX12::~GraphicsDX12()
{
	assert(device_ == nullptr);
	assert(commandQueue_ == nullptr);
}

bool GraphicsDX12::Initialize(IUnityInterfaces* unityInterface)
{
	unityInterface_ = unityInterface;
	// 2020 or later
	auto dx12Interface = unityInterface->Get<IUnityGraphicsD3D12v6>();

	device_ = dx12Interface->GetDevice();
	commandQueue_ = dx12Interface->GetCommandQueue();
	const int swapCount = 2;

	MaterialEvent::Initialize();

	graphicsDevice_ = EffekseerRendererDX12::CreateGraphicsDevice(device_, commandQueue_, swapCount);

	ES_SAFE_ADDREF(device_);
	ES_SAFE_ADDREF(commandQueue_);

	return true;
}

void GraphicsDX12::AfterReset(IUnityInterfaces* unityInterface) {}

void GraphicsDX12::Shutdown(IUnityInterfaces* unityInterface)
{
	MaterialEvent::Terminate();
	graphicsDevice_.Reset();
	renderer_ = nullptr;
	ES_SAFE_RELEASE(device_);
	ES_SAFE_RELEASE(commandQueue_);
}

EffekseerRenderer::RendererRef GraphicsDX12::CreateRenderer(int squareMaxCount, bool reversedDepth)
{
	// temp format
	std::array<DXGI_FORMAT, 1> renderTargetFormats;
	renderTargetFormats[0] = DXGI_FORMAT_R8G8B8A8_UNORM;
	DXGI_FORMAT depthFormat = DXGI_FORMAT_D32_FLOAT_S8X24_UINT;

	renderer_ = EffekseerRendererDX12::Create(
		graphicsDevice_, renderTargetFormats.data(), renderTargetFormats.size(), depthFormat, reversedDepth, squareMaxCount);
	return renderer_;
}

void GraphicsDX12::SetExternalTexture(int renderId, ExternalTextureType type, void* texture)
{
	if (texture != nullptr)
	{
		ID3D12Resource* resource = reinterpret_cast<ID3D12Resource*>(texture);
		auto backend = EffekseerRendererDX12::CreateTexture(graphicsDevice_, resource);
		renderSettings[renderId].externalTextures[static_cast<int>(type)] = backend;
	}
	else
	{
		renderSettings[renderId].externalTextures[static_cast<int>(type)] = nullptr;
	}
}

Effekseer::TextureLoaderRef GraphicsDX12::Create(TextureLoaderLoad load, TextureLoaderUnload unload)
{
	return Effekseer::MakeRefPtr<TextureLoaderDX12>(load, unload, graphicsDevice_);
}

Effekseer::ModelLoaderRef GraphicsDX12::Create(ModelLoaderLoad load, ModelLoaderUnload unload)
{
	if (renderer_ == nullptr)
		return nullptr;

	auto loader = Effekseer::MakeRefPtr<ModelLoader>(load, unload);
	auto internalLoader = EffekseerRenderer::CreateModelLoader(renderer_->GetGraphicsDevice(), loader->GetFileInterface());
	loader->SetInternalLoader(internalLoader);
	return loader;
}

Effekseer::MaterialLoaderRef GraphicsDX12::Create(MaterialLoaderLoad load, MaterialLoaderUnload unload)
{
	if (renderer_ == nullptr)
		return nullptr;

	auto loader = Effekseer::MakeRefPtr<MaterialLoader>(load, unload);
	auto internalLoader = renderer_->CreateMaterialLoader();
	auto holder = std::make_shared<MaterialLoaderHolder>(internalLoader);
	loader->SetInternalLoader(holder);
	return loader;
}

void GraphicsDX12::ShiftViewportForStereoSinglePass(bool isShift) {}

RenderPass* GraphicsDX12::CreateRenderPass()
{
	auto ret = new RenderPassDX12();
	ret->Initialize(unityInterface_, renderer_, renderer_->GetGraphicsDevice());
	return ret;
}

void GraphicsDX12::SetRenderPath(EffekseerRenderer::Renderer* renderer, RenderPass* renderPath)
{
	if (renderPath != nullptr)
	{
		auto rt = static_cast<RenderPassDX12*>(renderPath);
		renderer_->SetCommandList(rt->GetCommandList());
	}
	else
	{
		renderer_->SetCommandList(nullptr);
	}
}

void GraphicsDX12::WaitFinish()
{
	if (renderer_ == nullptr)
	{
		return;
	}

	EffekseerRenderer::FlushAndWait(renderer_->GetGraphicsDevice());
}

} // namespace EffekseerPlugin