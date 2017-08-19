
#pragma once

#include "Effekseer.h"
#include "EffekseerRendererDX9.h"

namespace EffekseerPlugin
{
	EffekseerRenderer::Renderer* CreateRendererDX9(int squareMaxCount, IDirect3DDevice9* d3d9Device);

	void SetBackGroundDX9(EffekseerRenderer::Renderer* renderer, void *background);
}