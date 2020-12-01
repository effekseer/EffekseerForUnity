#pragma once

#include <Effekseer/Material/Effekseer.Material.h>
#include <EffekseerRenderer.CommonUtils.h>
#include <EffekseerRenderer.ShaderBase.h>
#include <vector>

namespace EffekseerRendererUnity
{

class Shader final
{
private:
	EffekseerRenderer::MaterialShaderParameterGenerator parameterGenerator_;
	std::shared_ptr<Effekseer::Material> material_;
	void* unityMaterial_ = nullptr;
	EffekseerRenderer::RendererShaderType shaderType_{};
	std::vector<uint8_t> vertexConstantBuffer;
	std::vector<uint8_t> pixelConstantBuffer;
	bool isRefraction_;

public:
	/**
		@brief	Constructor for material
	*/
	Shader(void* unityMaterial, std::shared_ptr<Effekseer::Material> material, bool isModel, bool isRefraction);

	Shader(EffekseerRenderer::RendererShaderType shaderType);

	~Shader();

	void* GetVertexConstantBuffer() { return vertexConstantBuffer.data(); }

	void* GetPixelConstantBuffer() { return pixelConstantBuffer.data(); }

	template <typename T> T* GetVertexConstantBuffer() { return reinterpret_cast<T*>(vertexConstantBuffer.data()); }

	template <typename T> T* GettPixelConstantBuffer() { return reinterpret_cast<T*>(pixelConstantBuffer.data()); }

	void SetConstantBuffer() {}

	EffekseerRenderer::RendererShaderType GetType() const;

	void* GetUnityMaterial() const;

	const std::shared_ptr<Effekseer::Material>& GetMaterial() { return material_; }

	const EffekseerRenderer::MaterialShaderParameterGenerator* GetParameterGenerator() const { return &parameterGenerator_; }

	bool GetIsRefraction() const;
};

} // namespace EffekseerRendererUnity