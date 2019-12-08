
#pragma once

#include "EffekseerRendererDX11.h"
#include "EffekseerPluginGraphics.h"

namespace EffekseerPlugin
{

class GraphicsDX11 : public Graphics
{
private:
	ID3D11Device* d3d11Device = nullptr;
	ID3D11DeviceContext* d3d11Context = nullptr;
	EffekseerRendererDX11::Renderer* renderer_ = nullptr;

public:
	GraphicsDX11();

	virtual ~GraphicsDX11();

	bool Initialize(IUnityInterfaces* unityInterface) override;

	void AfterReset(IUnityInterfaces* unityInterface) override;

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
