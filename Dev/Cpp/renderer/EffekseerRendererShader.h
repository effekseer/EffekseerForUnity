#pragma once

#include <EffekseerRendererCommon/EffekseerRenderer.CommonUtils.h>
#include <EffekseerRendererCommon/EffekseerRenderer.ShaderBase.h>
#include <vector>

namespace EffekseerRendererUnity
{

class Shader : public EffekseerRenderer::ShaderBase
{
private:
	EffekseerRenderer::MaterialShaderParameterGenerator parameterGenerator_;
	std::shared_ptr<Effekseer::MaterialFile> material_;
	void* unityMaterial_ = nullptr;
	EffekseerRenderer::RendererShaderType shaderType_{};
	std::vector<uint8_t> vertexConstantBuffer;
	std::vector<uint8_t> pixelConstantBuffer;
	bool isRefraction_ = false;

public:
	/**
		@brief	Constructor for material
	*/
	Shader(void* unityMaterial, std::shared_ptr<Effekseer::MaterialFile> material, bool isModel, bool isRefraction);

	Shader(EffekseerRenderer::RendererShaderType shaderType);

	virtual ~Shader() override = default;

	virtual void SetVertexConstantBufferSize(int32_t size) override {}
	virtual void SetPixelConstantBufferSize(int32_t size) override {}

	void* GetVertexConstantBuffer() override { return vertexConstantBuffer.data(); }

	void* GetPixelConstantBuffer() override { return pixelConstantBuffer.data(); }

	template <typename T> T* GetVertexConstantBuffer() { return reinterpret_cast<T*>(vertexConstantBuffer.data()); }

	template <typename T> T* GettPixelConstantBuffer() { return reinterpret_cast<T*>(pixelConstantBuffer.data()); }

	void SetConstantBuffer() override {}

	EffekseerRenderer::RendererShaderType GetType() const;

	void* GetUnityMaterial() const;

	const std::shared_ptr<Effekseer::MaterialFile>& GetMaterial() { return material_; }

	const EffekseerRenderer::MaterialShaderParameterGenerator* GetParameterGenerator() const { return &parameterGenerator_; }

	bool GetIsRefraction() const;
};

} // namespace EffekseerRendererUnity