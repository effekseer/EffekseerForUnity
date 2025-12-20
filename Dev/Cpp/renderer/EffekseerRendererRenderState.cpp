
#include "EffekseerRendererRenderState.h"

namespace EffekseerRendererUnity
{
RenderState::RenderState() {}

RenderState::~RenderState() {}

void RenderState::Update(bool forced) { active_ = next_; }
} // namespace EffekseerRendererUnity