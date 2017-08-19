
#include "EffekseerPluginDX11.h"

namespace EffekseerPlugin
{
	EffekseerRenderer::Renderer* CreateRendererDX11(int squareMaxCount, bool reversedDepth, ID3D11Device* d3d11Device, ID3D11DeviceContext* d3d11Context)
	{
		// 深度テストの方法を切り替え
		const D3D11_COMPARISON_FUNC depthFunc = (reversedDepth) ? D3D11_COMPARISON_GREATER : D3D11_COMPARISON_LESS;

		auto renderer = EffekseerRendererDX11::Renderer::Create(
			d3d11Device, d3d11Context, squareMaxCount, depthFunc);
		return renderer;
	}
}