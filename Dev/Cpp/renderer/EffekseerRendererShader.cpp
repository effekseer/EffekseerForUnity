#include "EffekseerRendererShader.h"

namespace EffekseerRendererUnity
{
	Shader::Shader()
	{
		vertexConstantBuffer.resize(sizeof(::Effekseer::Matrix44) * 2);
		pixelConstantBuffer.resize(sizeof(float));
	}

	Shader::~Shader()
	{

	}
}