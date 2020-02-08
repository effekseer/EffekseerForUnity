#include "EffekseerRendererShader.h"

namespace EffekseerRendererUnity
{

Shader::Shader(void* unityMaterial, std::shared_ptr<Effekseer::Material> material, bool isModel, bool isRefraction)
	: unityMaterial_(unityMaterial)
	, parameterGenerator_(*material, isModel, isRefraction ? 1 : 0, 1)
	, type_(Effekseer::RendererMaterialType::File)
	, material_(material)
	, isRefraction_(isRefraction)
{
	vertexConstantBuffer.resize(parameterGenerator_.VertexShaderUniformBufferSize);
	pixelConstantBuffer.resize(parameterGenerator_.PixelShaderUniformBufferSize);
}

Shader::Shader(Effekseer::RendererMaterialType type) : parameterGenerator_(::Effekseer::Material(), false, 0, 1), type_(type)
{
	vertexConstantBuffer.resize(sizeof(::Effekseer::Matrix44) * 4);
	pixelConstantBuffer.resize(sizeof(float) * 16);
}

Shader::~Shader() {}

Effekseer::RendererMaterialType Shader::GetType() const { return type_; }

void* Shader::GetUnityMaterial() const { return unityMaterial_; }

bool Shader::GetIsRefraction() const { return isRefraction_; };

} // namespace EffekseerRendererUnity