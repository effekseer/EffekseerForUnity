
#include "EffekseerPluginDX11.h"

namespace EffekseerPlugin
{
	class DistortingCallbackDX11 : public EffekseerRenderer::DistortingCallback
	{
		::EffekseerRendererDX11::Renderer* renderer = nullptr;
		ID3D11Texture2D* backGroundTexture = nullptr;
		ID3D11ShaderResourceView* backGroundTextureSRV = nullptr;
		D3D11_TEXTURE2D_DESC backGroundTextureDesc = {};

		ID3D11Device*			g_D3d11Device = NULL;
		ID3D11DeviceContext*	g_D3d11Context = NULL;


	public:
		DistortingCallbackDX11(::EffekseerRendererDX11::Renderer* renderer, ID3D11Device* d3d11Device, ID3D11DeviceContext* d3d11Context)
			: renderer(renderer)
			, g_D3d11Device(d3d11Device)
			, g_D3d11Context(d3d11Context)
		{
		}

		virtual ~DistortingCallbackDX11()
		{
			ReleaseTexture();
		}

		void ReleaseTexture()
		{
			ES_SAFE_RELEASE(backGroundTextureSRV);
			ES_SAFE_RELEASE(backGroundTexture);
		}

		// コピー先のテクスチャを準備
		void PrepareTexture(uint32_t width, uint32_t height, DXGI_FORMAT format)
		{
			ReleaseTexture();

			ZeroMemory(&backGroundTextureDesc, sizeof(backGroundTextureDesc));
			backGroundTextureDesc.Usage = D3D11_USAGE_DEFAULT;
			backGroundTextureDesc.Format = format;
			backGroundTextureDesc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
			backGroundTextureDesc.Width = width;
			backGroundTextureDesc.Height = height;
			backGroundTextureDesc.CPUAccessFlags = 0;
			backGroundTextureDesc.MipLevels = 1;
			backGroundTextureDesc.ArraySize = 1;
			backGroundTextureDesc.SampleDesc.Count = 1;
			backGroundTextureDesc.SampleDesc.Quality = 0;

			HRESULT hr = S_OK;
			hr = g_D3d11Device->CreateTexture2D(&backGroundTextureDesc, nullptr, &backGroundTexture);
			if (FAILED(hr)){
				return;
			}

			D3D11_SHADER_RESOURCE_VIEW_DESC srvDesc;
			ZeroMemory(&srvDesc, sizeof(srvDesc));
			switch (format)
			{
			case DXGI_FORMAT_R8G8B8A8_TYPELESS:
				srvDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
				break;
			case DXGI_FORMAT_R16G16B16A16_TYPELESS:
				srvDesc.Format = DXGI_FORMAT_R16G16B16A16_FLOAT;
				break;
			default:
				srvDesc.Format = format;
				break;
			}
			srvDesc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
			srvDesc.Texture2D.MipLevels = 1;
			hr = g_D3d11Device->CreateShaderResourceView(backGroundTexture, &srvDesc, &backGroundTextureSRV);
			if (FAILED(hr)){
				return;
			}
		}

		virtual void OnDistorting()
		{
			HRESULT hr = S_OK;

			ID3D11RenderTargetView* renderTargetView = nullptr;
			ID3D11Texture2D* renderTexture = nullptr;

			g_D3d11Context->OMGetRenderTargets(1, &renderTargetView, nullptr);
			renderTargetView->GetResource(reinterpret_cast<ID3D11Resource**>(&renderTexture));

			// レンダーターゲット情報を取得
			D3D11_TEXTURE2D_DESC renderTextureDesc;
			renderTexture->GetDesc(&renderTextureDesc);

			// シザリング範囲を取得
			UINT numScissorRects = 1;
			D3D11_RECT scissorRect;
			g_D3d11Context->RSGetScissorRects(&numScissorRects, &scissorRect);

			// 描画範囲を計算
			uint32_t width = renderTextureDesc.Width;
			uint32_t height = renderTextureDesc.Height;
			if (numScissorRects > 0){
				width = scissorRect.right - scissorRect.left;
				height = scissorRect.bottom - scissorRect.top;
			}

			// 保持テクスチャとフォーマットが異なればテクスチャを作り直す
			if (backGroundTextureSRV == nullptr ||
				backGroundTextureDesc.Width != width ||
				backGroundTextureDesc.Height != height ||
				backGroundTextureDesc.Format != renderTextureDesc.Format)
			{
				PrepareTexture(width, height, renderTextureDesc.Format);
			}

			if (width == renderTextureDesc.Width &&
				height == renderTextureDesc.Height)
			{
				// 背景テクスチャへコピー
				g_D3d11Context->CopyResource(backGroundTexture, renderTexture);
			}
			else
			{
				// 背景テクスチャへ部分的コピー
				D3D11_BOX srcBox;
				srcBox.left = scissorRect.left;
				srcBox.top = scissorRect.top;
				srcBox.right = scissorRect.right;
				srcBox.bottom = scissorRect.bottom;
				srcBox.front = 0;
				srcBox.back = 1;
				g_D3d11Context->CopySubresourceRegion(backGroundTexture, 0,
					0, 0, 0, renderTexture, 0, &srcBox);
			}

			// 取得したリソースの参照カウンタを下げる
			ES_SAFE_RELEASE(renderTexture);
			ES_SAFE_RELEASE(renderTargetView);

			renderer->SetBackground(backGroundTextureSRV);
		}
	};

	EffekseerRenderer::Renderer* CreateRendererDX11(int squareMaxCount, bool reversedDepth, ID3D11Device* d3d11Device, ID3D11DeviceContext* d3d11Context)
	{

		// 深度テストの方法を切り替え
		const D3D11_COMPARISON_FUNC depthFunc = (reversedDepth) ? D3D11_COMPARISON_GREATER : D3D11_COMPARISON_LESS;

		auto renderer = EffekseerRendererDX11::Renderer::Create(
			d3d11Device, d3d11Context, squareMaxCount, depthFunc);
		renderer->SetDistortingCallback(new DistortingCallbackDX11(renderer, d3d11Device, d3d11Context));
		return renderer;
	}
}