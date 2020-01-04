
#include "EffekseerPluginGraphicsUnity.h"
#include "../renderer/EffekseerRendererImplemented.h"
#include "../renderer/EffekseerRendererModelLoader.h"
#include "../renderer/EffekseerRendererTextureLoader.h"
#include "../renderer/EffekseerRendererMaterialLoader.h"
#include <algorithm>
#include <assert.h>

namespace EffekseerPlugin
{

GraphicsUnity::GraphicsUnity() {}

GraphicsUnity::~GraphicsUnity() {}

bool GraphicsUnity::Initialize(IUnityInterfaces* unityInterface) { return true; }

void GraphicsUnity::AfterReset(IUnityInterfaces* unityInterface) {}

void GraphicsUnity::Shutdown(IUnityInterfaces* unityInterface) {}

EffekseerRenderer::Renderer* GraphicsUnity::CreateRenderer(int squareMaxCount, bool reversedDepth)
{
	auto renderer = EffekseerRendererUnity::RendererImplemented::Create();
	if (renderer->Initialize(squareMaxCount))
	{
		return renderer;
	}
	else
	{
		ES_SAFE_RELEASE(renderer);
		return nullptr;
	}
}

void GraphicsUnity::SetBackGroundTextureToRenderer(EffekseerRenderer::Renderer* renderer, void* backgroundTexture)
{
	((EffekseerRendererUnity::RendererImplemented*)renderer)->SetBackground(backgroundTexture);
}

void GraphicsUnity::EffekseerSetBackGroundTexture(int renderId, void* texture) { renderSettings[renderId].backgroundTexture = texture; }

Effekseer::TextureLoader* GraphicsUnity::Create(TextureLoaderLoad load, TextureLoaderUnload unload)
{
	return new EffekseerRendererUnity::TextureLoader(load, unload);
}

Effekseer::ModelLoader* GraphicsUnity::Create(ModelLoaderLoad load, ModelLoaderUnload unload)
{
	return new EffekseerRendererUnity::ModelLoader(load, unload);
}

Effekseer::MaterialLoader* GraphicsUnity::Create(MaterialLoaderLoad load, MaterialLoaderUnload unload)
{
	return new EffekseerRendererUnity::MaterialLoader(load, unload);
}

void GraphicsUnity::ShiftViewportForStereoSinglePass(bool isShift) 
{
}

} // namespace EffekseerPlugin