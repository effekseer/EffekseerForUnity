
#include "EffekseerRendererRenderState.h"

namespace EffekseerRendererUnity
{
RenderState::RenderState() {}

RenderState::~RenderState() {}

void RenderState::Update(bool forced) { m_active = m_next; }
} // namespace EffekseerRendererUnity