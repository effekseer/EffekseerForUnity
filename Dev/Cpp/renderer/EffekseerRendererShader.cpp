#include "EffekseerRendererShader.h"

namespace EffekseerRendererUnity
{
	Shader::Shader()
	{
		vertexConstantBuffer.resize(sizeof(::Effekseer::Matrix44) * 4);
		pixelConstantBuffer.resize(sizeof(float) * 16);
	}

	Shader::~Shader()
	{

	}
}