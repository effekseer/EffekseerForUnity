
#pragma once

#include "EffekseerPluginGraphics.h"

#ifdef _DX12
#include <EffekseerRendererDX12/EffekseerRendererDX12.h>
#endif

#ifdef _PS4
#include "EffekseerRendererPS4.h"
#endif

#ifdef _SWITCH
#include "EffekseerRendererSwitch.h"
#endif
namespace EffekseerPlugin
{

class RenderPassLLGI : public RenderPass
{
protected:
	IUnityInterfaces* unityInterface_ = nullptr;
	EffekseerRenderer::RendererRef renderer_;
	Effekseer::RefPtr<EffekseerRenderer::CommandList> commandList_ = nullptr;
	Effekseer::RefPtr<EffekseerRenderer::SingleFrameMemoryPool> memoryPool_ = nullptr;

public:
	virtual bool
	Initialize(IUnityInterfaces* unityInterface, EffekseerRenderer::RendererRef renderer, Effekseer::Backend::GraphicsDeviceRef device);

	virtual ~RenderPassLLGI() override = default;

	Effekseer::RefPtr<EffekseerRenderer::CommandList> GetCommandList() { return commandList_; }
};

class GraphicsLLGI : public Graphics
{
protected:
	Effekseer::Backend::GraphicsDeviceRef graphicsDevice_ = nullptr;
	Effekseer::RefPtr<EffekseerRenderer::Renderer> renderer_ = nullptr;
	IUnityInterfaces* unityInterface_ = nullptr;
	std::shared_ptr<TextureConverter> textureConverter_;

public:
	GraphicsLLGI() = default;

	virtual ~GraphicsLLGI() override = default;

	void AfterReset(IUnityInterfaces* unityInterface) override;

	virtual void Shutdown(IUnityInterfaces* unityInterface) override;

	Effekseer::TextureLoaderRef Create(TextureLoaderLoad load, TextureLoaderUnload unload) override;

	Effekseer::ModelLoaderRef Create(ModelLoaderLoad load, ModelLoaderUnload unload) override;

	Effekseer::MaterialLoaderRef Create(MaterialLoaderLoad load, MaterialLoaderUnload unload) override;

	void SetExternalTexture(int renderId, ExternalTextureType type, void* texture) override;

	void ShiftViewportForStereoSinglePass(bool isShift) override;

	void SetRenderPath(EffekseerRenderer::Renderer* renderer, RenderPass* renderPath) override;

	void WaitFinish() override;
};

} // namespace EffekseerPlugin
