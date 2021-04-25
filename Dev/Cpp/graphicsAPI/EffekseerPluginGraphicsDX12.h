
#pragma once

#include "EffekseerPluginGraphicsLLGI.h"

#include <EffekseerRendererDX12/EffekseerRendererDX12.h>

#include "../unity/IUnityGraphicsD3D12.h"

#include "EffekseerPluginGraphics.h"

namespace EffekseerPlugin
{

class RenderPassDX12 : public RenderPassLLGI
{
	IUnityInterfaces* unityInterface_ = nullptr;
	EffekseerRenderer::RendererRef renderer_;

public:
	virtual ~RenderPassDX12() override = default;

	void Begin(RenderSettings& setting, RenderPass* backRenderPass) override;

	void End(RenderSettings& setting) override;
};

class GraphicsDX12 : public GraphicsLLGI
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

	RenderPass* CreateRenderPass() override;
};

} // namespace EffekseerPlugin
