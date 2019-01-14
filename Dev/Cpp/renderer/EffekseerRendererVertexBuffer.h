#pragma once

#include <EffekseerRenderer.VertexBufferBase.h>

#include <vector>

namespace EffekseerRendererUnity
{
	/**
		@brief	Vertex buffer
		@note
		TODO performance improvement
	*/
	class VertexBuffer
		: public EffekseerRenderer::VertexBufferBase
	{
	private:
		uint32_t				m_vertexRingStart;
		uint32_t				m_vertexRingOffset;
		bool					m_ringBufferLock = false;
	public:
		VertexBuffer(int size, bool isDynamic);
		virtual ~VertexBuffer();

		void Lock() override;
		bool RingBufferLock(int32_t size, int32_t& offset, void*& data) override;
		bool TryRingBufferLock(int32_t size, int32_t& offset, void*& data) override;
		void Unlock() override;

		uint8_t* GetResource()
		{
			return m_resource;
		}
	};
}