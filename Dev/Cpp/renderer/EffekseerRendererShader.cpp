#include "EffekseerRendererShader.h"
#include <EffekseerRenderer.ModelRendererBase.h>
#include <EffekseerRenderer.StandardRenderer.h>

namespace EffekseerRendererUnity
{

Shader::Shader(void* unityMaterial, std::shared_ptr<Effekseer::MaterialFile> material, bool isModel, bool isRefraction)
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
	: parameterGenerator_(::Effekseer::MaterialFile(), false, 0, 1), shaderType_(shaderType)
{
	auto vertexConstantBufferSize = sizeof(EffekseerRenderer::ModelRendererAdvancedVertexConstantBuffer<1>);
	vertexConstantBufferSize = std::max(vertexConstantBufferSize, sizeof(EffekseerRenderer::ModelRendererVertexConstantBuffer<1>));
	vertexConstantBufferSize = std::max(vertexConstantBufferSize, sizeof(EffekseerRenderer::StandardRendererVertexBuffer));

	vertexConstantBuffer.resize(vertexConstantBufferSize);

	pixelConstantBuffer.resize(
		std::max(sizeof(EffekseerRenderer::PixelConstantBuffer), sizeof(EffekseerRenderer::PixelConstantBufferDistortion)));
}

Shader::~Shader() {}

EffekseerRenderer::RendererShaderType Shader::GetType() const { return shaderType_; }

void* Shader::GetUnityMaterial() const { return unityMaterial_; }

bool Shader::GetIsRefraction() const { return isRefraction_; };

} // namespace EffekseerRendererUnity
