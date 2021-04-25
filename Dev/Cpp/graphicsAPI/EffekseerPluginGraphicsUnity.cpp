
#include "EffekseerPluginGraphicsUnity.h"
#include "../renderer/EffekseerRendererImplemented.h"
#include "../renderer/EffekseerRendererMaterialLoader.h"
#include "../renderer/EffekseerRendererModelLoader.h"
#include "../renderer/EffekseerRendererTextureLoader.h"
#include <algorithm>
#include <assert.h>

namespace EffekseerPlugin
{

GraphicsUnity::GraphicsUnity() {}

GraphicsUnity::~GraphicsUnity() {}

bool GraphicsUnity::Initialize(IUnityInterfaces* unityInterface) { return true; }

void GraphicsUnity::AfterReset(IUnityInterfaces* unityInterface) {}

void GraphicsUnity::Shutdown(IUnityInterfaces* unityInterface) {}

Effekseer::RefPtr<EffekseerRenderer::Renderer> GraphicsUnity::CreateRenderer(int squareMaxCount, bool reversedDepth)
{
	auto renderer = EffekseerRendererUnity::RendererImplemented::Create();
	if (renderer->Initialize(squareMaxCount))
	{
		return renderer;
	}
	else
	{
		return nullptr;
	}
}

void GraphicsUnity::SetExternalTexture(int renderId, ExternalTextureType type, void* texture)
{
	auto& externalTexture = renderSettings[renderId].externalTextures[static_cast<int>(type)];

	// not changed
	if (externalTexture.OriginalPtr == texture)
	{
		return;
	}

	if (texture == nullptr)
	{
		externalTexture.Reset();
		return;
	}

	if (texture != nullptr)
	{
		externalTexture.Texture= Effekseer::MakeRefPtr<EffekseerRendererUnity::Texture>(texture);
		externalTexture.OriginalPtr = texture;
	}
	else
	{
		externalTexture.Reset();
	}
}

Effekseer::TextureLoaderRef GraphicsUnity::Create(TextureLoaderLoad load, TextureLoaderUnload unload)
{
	return Effekseer::MakeRefPtr<EffekseerRendererUnity::TextureLoader>(load, unload);
}

Effekseer::ModelLoaderRef GraphicsUnity::Create(ModelLoaderLoad load, ModelLoaderUnload unload)
{
	return Effekseer::MakeRefPtr<EffekseerRendererUnity::ModelLoader>(load, unload);
}

Effekseer::MaterialLoaderRef GraphicsUnity::Create(MaterialLoaderLoad load, MaterialLoaderUnload unload)
{
	return Effekseer::MakeRefPtr<EffekseerRendererUnity::MaterialLoader>(load, unload);
}

Effekseer::ProceduralModelGeneratorRef GraphicsUnity::Create(ProceduralModelGeneratorGenerate generate,
															 ProceduralModelGeneratorUngenerate ungenerate)
{
	return Effekseer::MakeRefPtr<EffekseerRendererUnity::ProceduralModelGenerator>(generate, ungenerate);
}

void GraphicsUnity::ShiftViewportForStereoSinglePass(bool isShift) {}

} // namespace EffekseerPlugin