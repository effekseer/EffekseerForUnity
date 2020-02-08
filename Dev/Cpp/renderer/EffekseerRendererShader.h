#pragma once

#include <Effekseer/Material/Effekseer.Material.h>
#include <EffekseerRenderer.CommonUtils.h>
#include <EffekseerRenderer.ShaderBase.h>
#include <vector>

namespace EffekseerRendererUnity
{

class Shader
{
private:
	EffekseerRenderer::MaterialShaderParameterGenerator parameterGenerator_;
	std::shared_ptr<Effekseer::Material> material_;
	void* unityMaterial_ = nullptr;
	Effekseer::RendererMaterialType type_;
	std::vector<uint8_t> vertexConstantBuffer;
	std::vector<uint8_t> pixelConstantBuffer;
	bool isRefraction_;

public:
	/**
		@brief	Constructor for material
	*/
	Shader(void* unityMaterial, std::shared_ptr<Effekseer::Material> material, bool isModel, bool isRefraction);

	Shader(Effekseer::RendererMaterialType type);

	virtual ~Shader();

	void* GetVertexConstantBuffer() { return vertexConstantBuffer.data(); }

	void* GetPixelConstantBuffer() { return pixelConstantBuffer.data(); }

	void SetConstantBuffer() {}

	Effekseer::RendererMaterialType GetType() const;

	void* GetUnityMaterial() const;

	const std::shared_ptr<Effekseer::Material>& GetMaterial() { return material_; }

	const EffekseerRenderer::MaterialShaderParameterGenerator* GetParameterGenerator() const { return &parameterGenerator_; }

	bool GetIsRefraction() const;
};

} // namespace EffekseerRendererUnity