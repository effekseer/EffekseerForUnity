
#pragma once

#include "../common/EffekseerPluginModel.h"
#include "../common/EffekseerPluginTexture.h"
#include "../unity/IUnityGraphics.h"
#include "Effekseer.h"

namespace EffekseerRenderer
{
class Renderer;
}

namespace EffekseerPlugin
{


class RenderPass
{
public:
	RenderPass() = default;
	virtual ~RenderPass() = default;

	//! if this renderpass is front, back render pass is sent as an argument
	virtual void Begin(RenderPass* backRenderPass) {}
	virtual void End() {}
	virtual void Execute() {}
};

class Graphics
{
public:
	static Graphics* Create(UnityGfxRenderer renderer, bool isUnityRenderer, bool doFallback);

	Graphics() = default;

	virtual ~Graphics() = default;

	virtual bool Initialize(IUnityInterfaces* unityInterface) = 0;

	virtual void AfterReset(IUnityInterfaces* unityInterface) {}

	virtual void BeforeReset(IUnityInterfaces* unityInterface) {}

	virtual void Shutdown(IUnityInterfaces* unityInterface) = 0;

	virtual EffekseerRenderer::Renderer* CreateRenderer(int squareMaxCount, bool reversedDepth) = 0;

	virtual void SetBackGroundTextureToRenderer(EffekseerRenderer::Renderer* renderer, void* backgroundTexture) = 0;

	virtual void EffekseerSetBackGroundTexture(int renderId, void* texture) = 0;

	virtual Effekseer::TextureLoader* Create(TextureLoaderLoad load, TextureLoaderUnload unload) = 0;

	virtual Effekseer::ModelLoader* Create(ModelLoaderLoad load, ModelLoaderUnload unload) = 0;

	virtual void ShiftViewportForStereoSinglePass(bool isShift) = 0;

	virtual RenderPass* CreateRenderPass() { return nullptr; }

	virtual void SetRenderPath(EffekseerRenderer::Renderer* renderer, RenderPass* renderPath) {}

	virtual void WaitFinish() {}

	virtual bool IsRequiredToFlipVerticallyWhenRenderToTexture() const { return true; }
};

} // namespace EffekseerPlugin
