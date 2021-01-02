
#pragma once

#include "../common/EffekseerPluginModel.h"
#include "../common/EffekseerPluginTexture.h"
#include "../common/EffekseerPluginMaterial.h"
#include "../unity/IUnityGraphics.h"

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#include <Effekseer/Effekseer.h>
#else
#include "Effekseer.h"
#endif

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

	virtual Effekseer::RefPtr<EffekseerRenderer::Renderer> CreateRenderer(int squareMaxCount, bool reversedDepth) = 0;

	virtual void SetBackGroundTextureToRenderer(EffekseerRenderer::Renderer* renderer, void* backgroundTexture) = 0;

	virtual void EffekseerSetBackGroundTexture(int renderId, void* texture) = 0;

	virtual Effekseer::TextureLoaderRef Create(TextureLoaderLoad load, TextureLoaderUnload unload) = 0;

	virtual Effekseer::ModelLoaderRef Create(ModelLoaderLoad load, ModelLoaderUnload unload) = 0;

	virtual Effekseer::MaterialLoaderRef Create(MaterialLoaderLoad load, MaterialLoaderUnload unload) { return nullptr; }

	virtual Effekseer::ProcedualModelGeneratorRef Create(ProcedualModelGeneratorGenerate generate,
														 ProcedualModelGeneratorUngenerate ungenerate)
	{
		return nullptr;
	}

	virtual void ShiftViewportForStereoSinglePass(bool isShift) = 0;

	virtual RenderPass* CreateRenderPass() { return nullptr; }

	virtual void SetRenderPath(EffekseerRenderer::Renderer* renderer, RenderPass* renderPath) {}

	virtual void WaitFinish() {}

	virtual bool IsRequiredToFlipVerticallyWhenRenderToTexture() const { return true; }
};

} // namespace EffekseerPlugin
