
#pragma once

#include "Effekseer.h"
#include "EffekseerRendererDX9.h"

namespace EffekseerPlugin
{
	class DistortingCallbackDX9 
		: public EffekseerRenderer::DistortingCallback
	{
		::EffekseerRendererDX9::Renderer* renderer = nullptr;
		IDirect3DTexture9* backGroundTexture = nullptr;
		uint32_t backGroundTextureWidth = 0;
		uint32_t backGroundTextureHeight = 0;
		D3DFORMAT backGroundTextureFormat;
		IDirect3DDevice9* m_d3d9Device = nullptr;

	public:
		DistortingCallbackDX9(::EffekseerRendererDX9::Renderer* renderer, IDirect3DDevice9* d3d9Device);

		virtual ~DistortingCallbackDX9();

		void ReleaseTexture();

		// コピー先のテクスチャを準備
		void PrepareTexture(uint32_t width, uint32_t height, D3DFORMAT format);

		bool OnDistorting() override;
	};

	EffekseerRenderer::Renderer* CreateRendererDX9(int squareMaxCount, IDirect3DDevice9* d3d9Device);
}