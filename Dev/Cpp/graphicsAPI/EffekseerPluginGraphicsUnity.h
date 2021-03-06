
#pragma once

#include "EffekseerPluginGraphics.h"

namespace EffekseerPlugin
{

class GraphicsUnity : public Graphics
{
private:
public:
	GraphicsUnity();

	~GraphicsUnity() override;

	bool Initialize(IUnityInterfaces* unityInterface) override;

	void AfterReset(IUnityInterfaces* unityInterface) override;

	void Shutdown(IUnityInterfaces* unityInterface) override;

	Effekseer::RefPtr<EffekseerRenderer::Renderer> CreateRenderer(int squareMaxCount, bool reversedDepth) override;

	void SetBackGroundTextureToRenderer(EffekseerRenderer::Renderer* renderer, void* backgroundTexture) override;

	void SetDepthTextureToRenderer(EffekseerRenderer::Renderer* renderer,
								   const Effekseer::Matrix44& projectionMatrix,
								   void* depthTexture) override;
	void SetExternalTexture(int renderId, ExternalTextureType type, void* texture) override;

	Effekseer::TextureLoaderRef Create(TextureLoaderLoad load, TextureLoaderUnload unload) override;

	Effekseer::ModelLoaderRef Create(ModelLoaderLoad load, ModelLoaderUnload unload) override;

	Effekseer::MaterialLoaderRef Create(MaterialLoaderLoad load, MaterialLoaderUnload unload) override;

	Effekseer::ProceduralModelGeneratorRef Create(ProceduralModelGeneratorGenerate generate,
												 ProceduralModelGeneratorUngenerate ungenerate) override;

	void ShiftViewportForStereoSinglePass(bool isShift) override;
};

} // namespace EffekseerPlugin
