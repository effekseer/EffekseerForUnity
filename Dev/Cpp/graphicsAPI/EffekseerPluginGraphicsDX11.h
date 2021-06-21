
#pragma once

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#include <EffekseerRendererDX11/EffekseerRendererDX11.h>
#else
#include "EffekseerRendererDX11.h"
#endif

#include "EffekseerPluginGraphics.h"

namespace EffekseerPlugin
{

class GraphicsDX11 : public Graphics
{
private:
	ID3D11Device* d3d11Device = nullptr;
	ID3D11DeviceContext* d3d11Context = nullptr;
	Effekseer::Backend::GraphicsDeviceRef graphicsDevice_ = nullptr;
	Effekseer::RefPtr<EffekseerRenderer::Renderer> renderer_ = nullptr;
	std::shared_ptr<TextureConverter> converter_;

public:
	GraphicsDX11();

	~GraphicsDX11() override;

	bool Initialize(IUnityInterfaces* unityInterface) override;

	void AfterReset(IUnityInterfaces* unityInterface) override;

	void Shutdown(IUnityInterfaces* unityInterface) override;

	EffekseerRenderer::RendererRef CreateRenderer(int squareMaxCount, bool reversedDepth) override;

	void SetExternalTexture(int renderId, ExternalTextureType type, void* texture) override;

	Effekseer::TextureLoaderRef Create(TextureLoaderLoad load, TextureLoaderUnload unload, GetUnityIDFromPath getUnityId) override;

	Effekseer::ModelLoaderRef Create(ModelLoaderLoad load, ModelLoaderUnload unload, GetUnityIDFromPath getUnityId) override;

	Effekseer::MaterialLoaderRef Create(MaterialLoaderLoad load, MaterialLoaderUnload unload, GetUnityIDFromPath getUnityId) override;

	void ShiftViewportForStereoSinglePass(bool isShift) override;
};

} // namespace EffekseerPlugin
