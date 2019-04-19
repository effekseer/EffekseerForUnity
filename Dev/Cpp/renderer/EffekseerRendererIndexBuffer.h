#pragma once

#include <EffekseerRenderer.IndexBufferBase.h>

namespace EffekseerRendererUE4
{
class IndexBuffer : public EffekseerRenderer::IndexBufferBase
{
public:
	IndexBuffer(int maxCount, bool isDynamic) : EffekseerRenderer::IndexBufferBase(maxCount, isDynamic) {}

	virtual ~IndexBuffer() {}

	void Lock() override {}
	void Unlock() override {}
};
} // namespace EffekseerRendererUE4