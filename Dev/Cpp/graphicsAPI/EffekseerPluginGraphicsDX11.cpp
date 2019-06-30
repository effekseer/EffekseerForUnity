
#include "EffekseerPluginGraphicsDX11.h"
#include "../unity/IUnityInterface.h"
#include "../unity/IUnityGraphics.h"
#include "../unity/IUnityGraphicsD3D11.h"
#include <algorithm>
#include <assert.h>

namespace EffekseerPlugin
{

class TextureLoaderDX11 : public TextureLoader
{
	struct TextureResource
	{
		int referenceCount = 1;
		Effekseer::TextureData* textureDataPtr = nullptr;
	};

	std::map<std::u16string, TextureResource> resources;
	std::map<void*, void*> textureData2NativePtr;

	ID3D11Device* d3d11Device = nullptr;

public:
	TextureLoaderDX11(TextureLoaderLoad load, TextureLoaderUnload unload, ID3D11Device* device)
		: TextureLoader(load, unload), d3d11Device(device)
	{
		ES_SAFE_ADDREF(device);
	}

	virtual ~TextureLoaderDX11() { ES_SAFE_RELEASE(d3d11Device); }

	virtual Effekseer::TextureData* Load(const EFK_CHAR* path, Effekseer::TextureType textureType)
	{
		// リソーステーブルを検索して存在したらそれを使う
		auto it = resources.find((const char16_t*)path);
		if (it != resources.end())
		{
			it->second.referenceCount++;
			return it->second.textureDataPtr;
		}

		// Unityでテクスチャをロード
		int32_t width, height, format;
		void* texturePtr = load((const char16_t*)path, &width, &height, &format);
		if (texturePtr == nullptr)
		{
			return nullptr;
		}

		// リソーステーブルに追加
		auto added = resources.insert(std::make_pair((const char16_t*)path, TextureResource()));
		TextureResource& res = added.first->second;
		res.textureDataPtr = new Effekseer::TextureData();
		res.textureDataPtr->Width = width;
		res.textureDataPtr->Height = height;
		res.textureDataPtr->TextureFormat = (Effekseer::TextureFormatType)format;

		// DX11の場合、UnityがロードするのはID3D11Texture2Dなので、
		// ID3D11ShaderResourceViewを作成する
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
		hr = d3d11Device->CreateShaderResourceView(textureDX11, &desc, &srv);
		if (FAILED(hr))
		{
			return nullptr;
		}

		res.textureDataPtr->UserPtr = srv;

		textureData2NativePtr[res.textureDataPtr] = texturePtr;

		return res.textureDataPtr;
	}

	virtual void Unload(Effekseer::TextureData* source)
	{
		if (source == nullptr)
		{
			return;
		}

		// アンロードするテクスチャを検索
		auto it = std::find_if(resources.begin(), resources.end(), [source](const std::pair<std::u16string, TextureResource>& pair) {
			return pair.second.textureDataPtr->UserPtr == source->UserPtr;
		});
		if (it == resources.end())
		{
			return;
		}

		// 参照カウンタが0になったら実際にアンロード
		it->second.referenceCount--;
		if (it->second.referenceCount <= 0)
		{

			// 作成したID3D11ShaderResourceViewを解放する
			ID3D11ShaderResourceView* srv = (ID3D11ShaderResourceView*)source->UserPtr;
			srv->Release();

			// Unload from unity
			unload(it->first.c_str(), textureData2NativePtr[source]);
			textureData2NativePtr.erase(source);
			ES_SAFE_DELETE(it->second.textureDataPtr);
			resources.erase(it);
		}
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
	return true;
}

void GraphicsDX11::AfterReset(IUnityInterfaces* unityInterface)
{
	ES_SAFE_RELEASE(d3d11Context);
	d3d11Device->GetImmediateContext(&d3d11Context);
}

void GraphicsDX11::Shutdown(IUnityInterfaces* unityInterface) { 
	ES_SAFE_RELEASE(d3d11Context); 
	ES_SAFE_RELEASE(d3d11Device);
}

EffekseerRenderer::Renderer* GraphicsDX11::CreateRenderer(int squareMaxCount, bool reversedDepth)
{

	const D3D11_COMPARISON_FUNC depthFunc = (reversedDepth) ? D3D11_COMPARISON_GREATER_EQUAL : D3D11_COMPARISON_LESS_EQUAL;
	auto renderer = EffekseerRendererDX11::Renderer::Create(d3d11Device, d3d11Context, squareMaxCount, depthFunc);
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
	}

	if (srv == nullptr)
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

Effekseer::TextureLoader* GraphicsDX11::Create(TextureLoaderLoad load, TextureLoaderUnload unload)
{
	return new TextureLoaderDX11(load, unload, d3d11Device);
}

Effekseer::ModelLoader* GraphicsDX11::Create(ModelLoaderLoad load, ModelLoaderUnload unload)
{
	auto loader = new ModelLoader(load, unload);
	auto internalLoader = EffekseerRendererDX11::CreateModelLoader(d3d11Device, loader->GetFileInterface());
	loader->SetInternalLoader(internalLoader);
	return loader;
}

void GraphicsDX11::ShiftViewportForStereoSinglePass()
{
	D3D11_VIEWPORT vp;
	UINT viewportNum = 1;
	d3d11Context->RSGetViewports(&viewportNum, &vp);
	vp.TopLeftX = vp.Width;
	d3d11Context->RSSetViewports(1, &vp);
}

} // namespace EffekseerPlugin