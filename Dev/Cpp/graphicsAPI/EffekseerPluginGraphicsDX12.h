
#pragma once

#include <EffekseerRendererDX12/EffekseerRendererDX12.h>

#include "../unity/IUnityGraphicsD3D12.h"

#include "EffekseerPluginGraphics.h"

namespace EffekseerPlugin
{

class RenderPassDX12 : public RenderPass
{
	IUnityInterfaces* unityInterface_ = nullptr;
	EffekseerRenderer::RendererRef renderer_;
	Effekseer::RefPtr<EffekseerRenderer::CommandList> commandList_ = nullptr;
	Effekseer::RefPtr<EffekseerRenderer::SingleFrameMemoryPool> memoryPool_ = nullptr;

public:
	bool
	Initialize(IUnityInterfaces* unityInterface, EffekseerRenderer::RendererRef renderer, Effekseer::Backend::GraphicsDeviceRef device);

	virtual ~RenderPassDX12() override = default;

	void Begin(RenderSettings& setting, RenderPass* backRenderPass) override;

	void End(RenderSettings& setting) override;

	void Execute(RenderSettings& setting) override;

	Effekseer::RefPtr<EffekseerRenderer::CommandList> GetCommandList() { return commandList_; }
};

class GraphicsDX12 : public Graphics
{
private:
	ID3D12Device* device_ = nullptr;
	ID3D12CommandQueue* commandQueue_ = nullptr;
	Effekseer::Backend::GraphicsDeviceRef graphicsDevice_ = nullptr;
	Effekseer::RefPtr<EffekseerRenderer::Renderer> renderer_ = nullptr;
	IUnityInterfaces* unityInterface_ = nullptr;

public:
	GraphicsDX12();

	~GraphicsDX12() override;

	bool Initialize(IUnityInterfaces* unityInterface) override;

	void AfterReset(IUnityInterfaces* unityInterface) override;

	void Shutdown(IUnityInterfaces* unityInterface) override;

	EffekseerRenderer::RendererRef CreateRenderer(int squareMaxCount, bool reversedDepth) override;

	void SetExternalTexture(int renderId, ExternalTextureType type, void* texture) override;

	Effekseer::TextureLoaderRef Create(TextureLoaderLoad load, TextureLoaderUnload unload) override;

	Effekseer::ModelLoaderRef Create(ModelLoaderLoad load, ModelLoaderUnload unload) override;

	Effekseer::MaterialLoaderRef Create(MaterialLoaderLoad load, MaterialLoaderUnload unload) override;

	void ShiftViewportForStereoSinglePass(bool isShift) override;

	RenderPass* CreateRenderPass() override;

	void SetRenderPath(EffekseerRenderer::Renderer* renderer, RenderPass* renderPath) override;

	void WaitFinish() override;
};

} // namespace EffekseerPlugin
