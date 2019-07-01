

#pragma once

#include "EffekseerPluginGraphics.h"
#include "EffekseerRendererDX9.h"

namespace EffekseerPlugin
{

class GraphicsDX9 : public Graphics
{
private:
	IDirect3DDevice9* d3d9Device = nullptr;

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

	void ShiftViewportForStereoSinglePass(bool isShift) override;
};

} // namespace EffekseerPlugin
