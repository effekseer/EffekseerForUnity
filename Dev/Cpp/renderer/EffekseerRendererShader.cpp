#include "EffekseerRendererShader.h"

namespace EffekseerRendererUnity
{

Shader::Shader(void* unityMaterial, std::shared_ptr<Effekseer::Material> material, bool isModel, bool isRefraction)
	: unityMaterial_(unityMaterial)
	, parameterGenerator_(*material, isModel, isRefraction ? 1 : 0, 1)
	, shaderType_(EffekseerRenderer::RendererShaderType::Material)
	, material_(material)
	, isRefraction_(isRefraction)
{
	vertexConstantBuffer.resize(parameterGenerator_.VertexShaderUniformBufferSize);
	pixelConstantBuffer.resize(parameterGenerator_.PixelShaderUniformBufferSize);
}

Shader::Shader(EffekseerRenderer::RendererShaderType shaderType)
	: parameterGenerator_(::Effekseer::Material(), false, 0, 1), shaderType_(shaderType)
{
	// TODO
	assert(0);

	vertexConstantBuffer.resize(sizeof(::Effekseer::Matrix44) * 4);
	pixelConstantBuffer.resize(sizeof(float) * 16);
}

Shader::~Shader() {}

EffekseerRenderer::RendererShaderType Shader::GetType() const { return shaderType_; }

void* Shader::GetUnityMaterial() const { return unityMaterial_; }

bool Shader::GetIsRefraction() const { return isRefraction_; };

} // namespace EffekseerRendererUnity