

#pragma once

#include "EffekseerPluginGraphics.h"


#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#include <EffekseerRendererDX9/EffekseerRendererDX9.h>
#else
#include "EffekseerRendererDX9.h"
#endif

namespace EffekseerPlugin
{

class GraphicsDX9 : public Graphics
{
private:
	IDirect3DDevice9* d3d9Device = nullptr;
	EffekseerRendererDX9::Renderer* renderer_ = nullptr;

public:
	GraphicsDX9();

	virtual ~GraphicsDX9();

	bool Initialize(IUnityInterfaces* unityInterface) override;

	void Shutdown(IUnityInterfaces* unityInterface) override;

	EffekseerRenderer::Renderer* CreateRenderer(int squareMaxCount, bool reversedDepth) override;

	void SetBackGroundTextureToRenderer(EffekseerRenderer::Renderer* renderer, void* backgroundTexture) override;

	void EffekseerSetBackGroundTexture(int renderId, void* texture) override;

	Effekseer::TextureLoader* Create(TextureLoaderLoad load, TextureLoaderUnload unload) override;

	Effekseer::ModelLoader* Create(ModelLoaderLoad load, ModelLoaderUnload unload) override;

	Effekseer::MaterialLoader* Create(MaterialLoaderLoad load, MaterialLoaderUnload unload) override;

	void ShiftViewportForStereoSinglePass(bool isShift) override;
};

} // namespace EffekseerPlugin
