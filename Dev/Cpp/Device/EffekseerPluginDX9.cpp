
#include "EffekseerPluginDX9.h"

namespace EffekseerPlugin
{
	EffekseerRenderer::Renderer* CreateRendererDX9(int squareMaxCount, IDirect3DDevice9* d3d9Device)
	{
		auto renderer = EffekseerRendererDX9::Renderer::Create(d3d9Device, squareMaxCount);
		return renderer;
	}
}