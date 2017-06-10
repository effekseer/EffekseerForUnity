
#include "EffekseerPluginDX9.h"

namespace EffekseerPlugin
{
	DistortingCallbackDX9::DistortingCallbackDX9(::EffekseerRendererDX9::Renderer* renderer, IDirect3DDevice9* d3d9Device)
		: renderer(renderer)
		, m_d3d9Device(d3d9Device)
	{
	}

	DistortingCallbackDX9::~DistortingCallbackDX9()
	{
		ReleaseTexture();
	}

	void DistortingCallbackDX9::ReleaseTexture()
	{
		ES_SAFE_RELEASE(backGroundTexture);
	}

	void DistortingCallbackDX9::PrepareTexture(uint32_t width, uint32_t height, D3DFORMAT format)
	{
		ReleaseTexture();

		backGroundTextureWidth = width;
		backGroundTextureHeight = height;
		backGroundTextureFormat = format;

		HRESULT hr = S_OK;
		hr = m_d3d9Device->CreateTexture(width, height, 0, D3DUSAGE_RENDERTARGET, format, D3DPOOL_DEFAULT, &backGroundTexture, nullptr);
		if (FAILED(hr)){
			return;
		}
	}

	bool DistortingCallbackDX9::OnDistorting()
	{
		IDirect3DSurface9* targetSurface = nullptr;
		IDirect3DSurface9* texSurface = nullptr;
		HRESULT hr = S_OK;

		// レンダーターゲットを取得
		hr = m_d3d9Device->GetRenderTarget(0, &targetSurface);
		if (FAILED(hr)){
			return false;
		}

		// レンダーターゲットの情報を取得
		D3DSURFACE_DESC targetSurfaceDesc;
		targetSurface->GetDesc(&targetSurfaceDesc);

		// シザリング範囲を取得
		RECT scissorRect;
		m_d3d9Device->GetScissorRect(&scissorRect);

		// 描画範囲を計算
		uint32_t width = scissorRect.right - scissorRect.left;
		uint32_t height = scissorRect.bottom - scissorRect.top;

		// 保持テクスチャとフォーマットが異なればテクスチャを作り直す
		if (backGroundTexture == nullptr ||
			backGroundTextureWidth != width ||
			backGroundTextureHeight != height ||
			backGroundTextureFormat != targetSurfaceDesc.Format)
		{
			PrepareTexture(width, height, targetSurfaceDesc.Format);
		}

		// コピーするためのサーフェスを取得
		hr = backGroundTexture->GetSurfaceLevel(0, &texSurface);
		if (FAILED(hr))
		{
			return false;
		}

		// サーフェス間コピー
		hr = m_d3d9Device->StretchRect(targetSurface, &scissorRect, texSurface, NULL, D3DTEXF_NONE);
		if (FAILED(hr)){
			return false;
		}

		// 取得したサーフェスの参照カウンタを下げる
		ES_SAFE_RELEASE(texSurface);
		ES_SAFE_RELEASE(targetSurface);

		renderer->SetBackground(backGroundTexture);

		return true;
	}

	EffekseerRenderer::Renderer* CreateRendererDX9(int squareMaxCount, IDirect3DDevice9* d3d9Device)
	{
		auto renderer = EffekseerRendererDX9::Renderer::Create(d3d9Device, squareMaxCount);
		renderer->SetDistortingCallback(new DistortingCallbackDX9(renderer, d3d9Device));
		return renderer;
	}
}