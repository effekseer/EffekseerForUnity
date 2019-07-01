
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

	virtual void StartRender(EffekseerRenderer::Renderer* renderer) {};
};

} // namespace EffekseerPlugin
