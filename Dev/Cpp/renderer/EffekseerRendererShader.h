#pragma once

#include <EffekseerRenderer.ShaderBase.h>
#include <vector>

namespace EffekseerRendererUnity
{
	/**
		@brief	Shader
	*/
	class Shader
	{
	private:
		std::vector<uint8_t>	vertexConstantBuffer;
		std::vector<uint8_t>	pixelConstantBuffer;

	public:
		Shader();

		virtual ~Shader();

		void* GetVertexConstantBuffer()
		{
			return vertexConstantBuffer.data();
		}

		void* GetPixelConstantBuffer()
		{
			return pixelConstantBuffer.data();
		}

		void SetConstantBuffer()
		{
			// TODO
		}
	};
}