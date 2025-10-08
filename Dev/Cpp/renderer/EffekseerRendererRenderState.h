#pragma once

#include <EffekseerRendererCommon/EffekseerRenderer.RenderStateBase.h>

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