
#include "EffekseerPluginGraphicsDX11.h"
#include "../unity/IUnityGraphics.h"
#include "../unity/IUnityGraphicsD3D11.h"
#include "../unity/IUnityInterface.h"
#include <algorithm>
#include <assert.h>

#include "../common/EffekseerPluginMaterial.h"

// TODO is default OK?
#pragma comment(lib, "d3dcompiler.lib")

namespace EffekseerPlugin
{

class TextureLoaderDX11 : public TextureLoader
{
	std::map<Effekseer::TextureRef, void*> textureData2NativePtr;

	ID3D11Device* d3d11Device_ = nullptr;
	Effekseer::Backend::GraphicsDeviceRef graphicsDevice_;

public:
	TextureLoaderDX11(TextureLoaderLoad load,
					  TextureLoaderUnload unload,
					  ID3D11Device* d3d11Device,
					  Effekseer::Backend::GraphicsDeviceRef graphicsDevice)
		: TextureLoader(load, unload), d3d11Device_(d3d11Device), graphicsDevice_(graphicsDevice)
	{
	}

	virtual ~TextureLoaderDX11() override = default;

	virtual Effekseer::TextureRef Load(const EFK_CHAR* path, Effekseer::TextureType textureType)
	{
		// Load from unity
		int32_t width, height, format;
		void* texturePtr = load((const char16_t*)path, &width, &height, &format);
		if (texturePtr == nullptr)
		{
			return nullptr;
		}

		// Create ID3D11ShaderResourceView from ID3D11Texture2D
		HRESULT hr;
		ID3D11Texture2D* textureDX11 = (ID3D11Texture2D*)texturePtr;

		D3D11_TEXTURE2D_DESC texDesc;
		textureDX11->GetDesc(&texDesc);

		ID3D11ShaderResourceView* srv = nullptr;
		D3D11_SHADER_RESOURCE_VIEW_DESC desc;
		ZeroMemory(&desc, sizeof(desc));
		desc.Format = texDesc.Format;
		desc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
		desc.Texture2D.MostDetailedMip = 0;
		desc.Texture2D.MipLevels = texDesc.MipLevels;
		hr = d3d11Device_->CreateShaderResourceView(textureDX11, &desc, &srv);
		if (FAILED(hr))
		{
			return nullptr;
		}

		auto backend = EffekseerRendererDX11::CreateTexture(graphicsDevice_, srv, nullptr, nullptr);
		auto textureDataPtr = Effekseer::MakeRefPtr<Effekseer::Texture>();
		textureDataPtr->SetBackend(backend);
		
		textureData2NativePtr[textureDataPtr] = texturePtr;

		ES_SAFE_RELEASE(srv);

		return textureDataPtr;
	}

	virtual void Unload(Effekseer::TextureRef source)
	{
		if (source == nullptr)
		{
			return;
		}

		unload(source->GetPath().c_str(), textureData2NativePtr[source]);
		textureData2NativePtr.erase(source);
	}
};

GraphicsDX11::GraphicsDX11() {}

GraphicsDX11::~GraphicsDX11()
{
	assert(d3d11Device == nullptr);
	assert(d3d11Context == nullptr);
}

bool GraphicsDX11::Initialize(IUnityInterfaces* unityInterface)
{
	d3d11Device = unityInterface->Get<IUnityGraphicsD3D11>()->GetDevice();
	d3d11Device->GetImmediateContext(&d3d11Context);
	ES_SAFE_ADDREF(d3d11Device);
	MaterialEvent::Initialize();
	graphicsDevice_ = EffekseerRendererDX11::CreateGraphicsDevice(d3d11Device, d3d11Context);
	return true;
}

void GraphicsDX11::AfterReset(IUnityInterfaces* unityInterface)
{
	ES_SAFE_RELEASE(d3d11Context);
	d3d11Device->GetImmediateContext(&d3d11Context);
}

void GraphicsDX11::Shutdown(IUnityInterfaces* unityInterface)
{
	MaterialEvent::Terminate();
	graphicsDevice_.Reset();
	renderer_ = nullptr;
	ES_SAFE_RELEASE(d3d11Context);
	ES_SAFE_RELEASE(d3d11Device);
}

EffekseerRenderer::RendererRef GraphicsDX11::CreateRenderer(int squareMaxCount, bool reversedDepth)
{
	const D3D11_COMPARISON_FUNC depthFunc = (reversedDepth) ? D3D11_COMPARISON_GREATER_EQUAL : D3D11_COMPARISON_LESS_EQUAL;
	auto renderer = EffekseerRendererDX11::Renderer::Create(d3d11Device, d3d11Context, squareMaxCount, depthFunc);
	renderer_ = renderer;
	return renderer;
}

void GraphicsDX11::SetBackGroundTextureToRenderer(EffekseerRenderer::Renderer* renderer, void* backgroundTexture)
{
	((EffekseerRendererDX11::Renderer*)renderer)->SetBackground((ID3D11ShaderResourceView*)backgroundTexture);
}

void GraphicsDX11::EffekseerSetBackGroundTexture(int renderId, void* texture)
{
	HRESULT hr;

	// create ID3D11ShaderResourceView because a texture type is ID3D11Texture2D from Unity on DX11
	ID3D11Texture2D* textureDX11 = (ID3D11Texture2D*)texture;
	ID3D11ShaderResourceView* srv = (ID3D11ShaderResourceView*)renderSettings[renderId].backgroundTexture;

	if (srv != nullptr)
	{
		ID3D11Resource* res = nullptr;
		srv->GetResource(&res);
		if (res != texture)
		{
			// if texture is not same, delete it
			srv->Release();
			srv = nullptr;
			renderSettings[renderId].backgroundTexture = nullptr;
		}
		ES_SAFE_RELEASE(res);
	}

	if (srv == nullptr && texture != nullptr)
	{
		D3D11_TEXTURE2D_DESC texDesc;
		textureDX11->GetDesc(&texDesc);

		D3D11_SHADER_RESOURCE_VIEW_DESC desc;
		ZeroMemory(&desc, sizeof(desc));
		// adjust format
		switch (texDesc.Format)
		{
		case DXGI_FORMAT_R8G8B8A8_TYPELESS:
			desc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
			break;
		case DXGI_FORMAT_R16G16B16A16_TYPELESS:
			desc.Format = DXGI_FORMAT_R16G16B16A16_FLOAT;
			break;
		default:
			desc.Format = texDesc.Format;
			break;
		}
		desc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
		desc.Texture2D.MostDetailedMip = 0;
		desc.Texture2D.MipLevels = texDesc.MipLevels;
		hr = d3d11Device->CreateShaderResourceView(textureDX11, &desc, &srv);
		if (SUCCEEDED(hr))
		{
			renderSettings[renderId].backgroundTexture = srv;
		}
	}
}

Effekseer::TextureLoaderRef GraphicsDX11::Create(TextureLoaderLoad load, TextureLoaderUnload unload)
{
	return Effekseer::MakeRefPtr<TextureLoaderDX11>(load, unload, d3d11Device, graphicsDevice_);
}

Effekseer::ModelLoaderRef GraphicsDX11::Create(ModelLoaderLoad load, ModelLoaderUnload unload)
{
	if (renderer_ == nullptr)
		return nullptr;

	auto loader = Effekseer::MakeRefPtr<ModelLoader>(load, unload);
	auto internalLoader = EffekseerRendererDX11::CreateModelLoader(renderer_->GetGraphicsDevice(), loader->GetFileInterface());
	loader->SetInternalLoader(internalLoader);
	return loader;
}

Effekseer::MaterialLoaderRef GraphicsDX11::Create(MaterialLoaderLoad load, MaterialLoaderUnload unload)
{
	if (renderer_ == nullptr)
		return nullptr;

	auto loader = Effekseer::MakeRefPtr<MaterialLoader>(load, unload);
	auto internalLoader = renderer_->CreateMaterialLoader();
	auto holder = std::make_shared<MaterialLoaderHolder>(internalLoader);
	loader->SetInternalLoader(holder);
	return loader;
}

void GraphicsDX11::ShiftViewportForStereoSinglePass(bool isShift)
{
	D3D11_VIEWPORT vp;
	UINT viewportNum = 1;
	d3d11Context->RSGetViewports(&viewportNum, &vp);
	if (isShift)
		vp.TopLeftX = vp.Width;
	else
		vp.TopLeftX = 0;
	d3d11Context->RSSetViewports(1, &vp);
}

} // namespace EffekseerPlugin