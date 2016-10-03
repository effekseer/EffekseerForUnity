
#pragma once

#include "Effekseer.h"
#include "EffekseerRendererDX11.h"

namespace EffekseerPlugin
{
	EffekseerRenderer::Renderer* CreateRendererDX11(int squareMaxCount, bool reversedDepth, ID3D11Device* d3d11Device, ID3D11DeviceContext* d3d11Context);
}