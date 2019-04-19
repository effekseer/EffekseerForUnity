#pragma once

#include <EffekseerRenderer.RenderStateBase.h>

namespace EffekseerRendererUnity
{
class RenderState : public EffekseerRenderer::RenderStateBase
{
public:
	RenderState();
	~RenderState();
	void Update(bool forced);
};
} // namespace EffekseerRendererUnity