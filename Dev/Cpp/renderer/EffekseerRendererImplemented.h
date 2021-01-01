
#pragma once

#include <Effekseer.h>
#include <EffekseerRenderer.ModelRendererBase.h>
#include <EffekseerRenderer.RenderStateBase.h>
#include <EffekseerRenderer.Renderer.h>
#include <EffekseerRenderer.RibbonRendererBase.h>
#include <EffekseerRenderer.RingRendererBase.h>
#include <EffekseerRenderer.SpriteRendererBase.h>
#include <EffekseerRenderer.StandardRenderer.h>
#include <EffekseerRenderer.TrackRendererBase.h>

#include "../unity/IUnityInterface.h"
#include <map>
#include <memory>

extern "C"
{
	struct StrideBufferParameter
	{
		int32_t Stride;
		int32_t Size;
		void* Ptr;
	};

	struct FlipbookParameters
	{
		int32_t Enable = 0;
		int32_t LoopType = 0;
		int32_t DivideX = 1;
		int32_t DivideY = 1;
	};

	struct EdgeParameters
	{
		std::array<float, 4> Color;
		float Threshold = 0;
		float ColorScaling = 1;
	};

	struct FalloffParameter
	{
		int32_t ColorBlendType = 0;
		std::array<float, 4> BeginColor;
		std::array<float, 4> EndColor;
		float Pow = 1.0f;
	};

	struct UnityRenderParameter
	{
		//! 0 - procedual, 1 - model
		int RenderMode = 0;

		EffekseerRenderer::RendererShaderType MaterialType = EffekseerRenderer::RendererShaderType::Unlit;

		//! VertexBuffer
		int VertexBufferOffset = 0;

		int AdvancedBufferOffset = 0;

		//! Stride for material
		int VertexBufferStride = 0;

		//! For model
		int CustomData1BufferOffset = 0;

		//! For model
		int CustomData2BufferOffset = 0;

		//! For model
		int UniformBufferOffset = 0;

		//! Element count (Triangle) or instance
		int32_t ElementCount = 0;

		FlipbookParameters FlipbookParams;
		float UVDistortionIntensity = 1.0f;
		int32_t TextureBlendType = -1;
		float BlendUVDistortionIntensity = 1.0f;
		int EnableFalloff = 0;
		FalloffParameter FalloffParam;
		float EmissiveScaling = 1;
		EdgeParameters EdgeParams;

		std::array<float, 4> SoftParticleParam;

		int ZTest = 0;

		int ZWrite = 0;

		int Blend = 0;

		int Culling = 0;

		float DistortionIntensity = 1.0f;

		//! for material
		int IsRefraction = 0;

		//! Texture ptr
		std::array<void*, Effekseer::TextureSlotMax> TexturePtrs;

		std::array<int, Effekseer::TextureSlotMax> TextureFilterTypes;

		std::array<int, Effekseer::TextureSlotMax> TextureWrapTypes;

		int32_t TextureCount = 0;

		//! Material ptr
		void* MaterialPtr = nullptr;

		//! Model ptri
		void* ModelPtr = nullptr;
	};

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API GetUnityRenderParameter(UnityRenderParameter* dst, int index);
	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API GetUnityRenderCount();
	UNITY_INTERFACE_EXPORT void* UNITY_INTERFACE_API GetUnityVertexBuffer();
	UNITY_INTERFACE_EXPORT void* UNITY_INTERFACE_API GetUnityInfoBuffer();

	UNITY_INTERFACE_EXPORT int32_t UNITY_INTERFACE_API GetUnityStrideBufferCount();
	UNITY_INTERFACE_EXPORT StrideBufferParameter UNITY_INTERFACE_API GetUnityStrideBufferParameter(int32_t index);
};

namespace EffekseerRendererUnity
{
class RendererImplemented;
class RenderState;
class VertexBuffer;
class IndexBuffer;
class Shader;

class Model : public Effekseer::Model
{
public:
	Model(void* data, int32_t size) : Effekseer::Model(data, size) {}

	Model(const Effekseer::CustomVector<Effekseer::Model::Vertex>& vertecies, const Effekseer::CustomVector<Effekseer::Model::Face>& faces)
		: Effekseer::Model(vertecies, faces)
	{
	}

	virtual ~Model() = default;

	void* InternalPtr = nullptr;
};

using Vertex = EffekseerRenderer::SimpleVertex;
using VertexDistortion = EffekseerRenderer::LightingVertex;
using DynamicVertex = EffekseerRenderer::DynamicVertex;

typedef ::Effekseer::ModelRenderer::NodeParameter efkModelNodeParam;
typedef ::Effekseer::ModelRenderer::InstanceParameter efkModelInstanceParam;
typedef ::Effekseer::Vector3D efkVector3D;

class ModelRenderer : public ::EffekseerRenderer::ModelRendererBase
{
private:
	RendererImplemented* m_renderer;

public:
	ModelRenderer(RendererImplemented* renderer);

	virtual ~ModelRenderer();

	static ::Effekseer::ModelRendererRef Create(RendererImplemented* renderer);

public:
	void BeginRendering(const efkModelNodeParam& parameter, int32_t count, void* userData) override;

	void Rendering(const efkModelNodeParam& parameter, const efkModelInstanceParam& instanceParameter, void* userData) override;

	void EndRendering(const efkModelNodeParam& parameter, void* userData) override;
};

struct UnityVertex
{
	::Effekseer::Vector3D Pos;
	float UV[2];
	float Col[4];
};

struct UnityDynamicVertex
{
	::Effekseer::Vector3D Pos;
	float Col[4];
	::Effekseer::Vector3D Normal;
	::Effekseer::Vector3D Tangent;
	float UV1[2];
	float UV2[2];
};

struct AdvancedVertexParameter
{
	std::array<float, 2> AlphaUV;
	std::array<float, 2> UVDistortionUV;
	std::array<float, 2> BlendUV;
	std::array<float, 2> BlendAlphaUV;
	std::array<float, 2> BlendUVDistortionUV;
	float FlipbookIndexAndNextRate;
	float AlphaThreshold;
};

class RendererImplemented : public ::EffekseerRenderer::Renderer, public ::Effekseer::ReferenceObject
{
	struct StrideBuffer
	{
		int32_t Stride = 0;
		std::vector<uint8_t> Buffer;

		int32_t PushBuffer(const void* data, int32_t size);

		int32_t GetOffset() const { return static_cast<int32_t>(Buffer.size()); }
	};

	std::vector<std::shared_ptr<StrideBuffer>> strideBuffers_;
	std::unordered_map<int32_t, int32_t> strideToIndex_;

	void ClearStrideBuffers();
	std::shared_ptr<StrideBuffer> GetStrideBuffer(int32_t stride);

protected:
	int32_t m_squareMaxCount = 0;

	VertexBuffer* m_vertexBuffer = nullptr;

	std::unique_ptr<Shader> unlitShader_;
	std::unique_ptr<Shader> backDistortedShader_;
	std::unique_ptr<Shader> litShader_;
	std::unique_ptr<Shader> adUnlitShader_;
	std::unique_ptr<Shader> adBackDistortedShader_;
	std::unique_ptr<Shader> adLitShader_;

	Shader* m_currentShader = nullptr;
	RenderState* m_renderState = nullptr;

	std::vector<void*> textures_;

	std::vector<UnityRenderParameter> renderParameters;

	Effekseer::RendererMaterialType rendererMaterialType_ = Effekseer::RendererMaterialType::Default;

	// std::vector<uint8_t> exportedVertexBuffer;
	std::vector<uint8_t> exportedInfoBuffer;

	Effekseer::TextureRef backgroundData_;

	EffekseerRenderer::StandardRenderer<RendererImplemented, Shader>* m_standardRenderer = nullptr;

	// int32_t AddVertexBuffer(const void* data, int32_t size);
	int32_t AddInfoBuffer(const void* data, int32_t size);

	// void AlignVertexBuffer(int32_t alignment);

	template <typename T> void AddVertexBufferAsVertex(const T& v, StrideBuffer& strideBuffer)
	{
		UnityVertex dst;
		dst.Pos = v.Pos;
		dst.UV[0] = v.UV[0];
		dst.UV[1] = v.UV[1];
		dst.Col[0] = v.Col.R / 255.0f;
		dst.Col[1] = v.Col.G / 255.0f;
		dst.Col[2] = v.Col.B / 255.0f;
		dst.Col[3] = v.Col.A / 255.0f;
		strideBuffer.PushBuffer(&dst, sizeof(UnityVertex));
		// AddVertexBuffer(&dst, sizeof(UnityVertex));
	}

	template <typename T> void AddVertexBufferAsDynamicVertex(const T& v, StrideBuffer& strideBuffer)
	{
		UnityDynamicVertex dst;
		dst.Pos = v.Pos;
		dst.UV1[0] = v.UV1[0];
		dst.UV1[1] = v.UV1[1];
		dst.UV2[0] = v.UV2[0];
		dst.UV2[1] = v.UV2[1];
		dst.Col[0] = v.Col.R / 255.0f;
		dst.Col[1] = v.Col.G / 255.0f;
		dst.Col[2] = v.Col.B / 255.0f;
		dst.Col[3] = v.Col.A / 255.0f;
		dst.Tangent = EffekseerRenderer::UnpackVector3DF(v.Tangent);
		dst.Normal = EffekseerRenderer::UnpackVector3DF(v.Normal);
		strideBuffer.PushBuffer(&dst, sizeof(UnityDynamicVertex));
		// AddVertexBuffer(&dst, sizeof(UnityDynamicVertex));
	}

	template <typename T> void AddVertexBufferAsAdvancedData(const T& v, StrideBuffer& strideBuffer)
	{
		AdvancedVertexParameter dst;

		dst.AlphaUV = EffekseerRenderer::GetVertexAlphaUV(v);
		dst.UVDistortionUV = EffekseerRenderer::GetVertexUVDistortionUV(v);
		dst.BlendUV = EffekseerRenderer::GetVertexBlendUV(v);
		dst.BlendAlphaUV = EffekseerRenderer::GetVertexBlendAlphaUV(v);
		dst.BlendUVDistortionUV = EffekseerRenderer::GetVertexUVDistortionUV(v);
		dst.FlipbookIndexAndNextRate = EffekseerRenderer::GetVertexFlipbookIndexAndNextRate(v);
		dst.AlphaThreshold = EffekseerRenderer::GetVertexAlphaThreshold(v);
		strideBuffer.PushBuffer(&dst, sizeof(AdvancedVertexParameter));
		// AddVertexBuffer(&dst, sizeof(AdvancedVertexParameter));
	}

public:
	static Effekseer::RefPtr<RendererImplemented> Create();

	RendererImplemented();
	virtual ~RendererImplemented();

	void OnLostDevice() override {}
	void OnResetDevice() override {}

	/**
	@brief	������
	*/
	bool Initialize(int32_t squareMaxCount);

	/**
	@brief	�X�e�[�g�𕜋A���邩�ǂ����̃t���O��ݒ肷��B
	*/
	void SetRestorationOfStatesFlag(bool flag) override;

	/**
	@brief	�`����J�n���鎞�Ɏ��s����B
	*/
	bool BeginRendering() override;

	/**
	@brief	�`����I�����鎞�Ɏ��s����B
	*/
	bool EndRendering() override;

	/**
	@brief	�ő�`��X�v���C�g�����擾����B
	*/
	int32_t GetSquareMaxCount() const override;

	/**
	@brief	�X�v���C�g�����_���[�𐶐�����B
	*/
	::Effekseer::SpriteRendererRef CreateSpriteRenderer() override;

	/**
	@brief	���{�������_���[�𐶐�����B
	*/
	::Effekseer::RibbonRendererRef CreateRibbonRenderer() override;

	/**
	@brief	�����O�����_���[�𐶐�����B
	*/
	::Effekseer::RingRendererRef CreateRingRenderer() override;

	/**
	@brief	���f�������_���[�𐶐�����B
	*/
	::Effekseer::ModelRendererRef CreateModelRenderer() override;

	/**
	@brief	�O�Ճ����_���[�𐶐�����B
	*/
	::Effekseer::TrackRendererRef CreateTrackRenderer() override;

	/**
	@brief	�W���̃e�N�X�`���Ǎ��N���X�𐶐�����B
	*/
	::Effekseer::TextureLoaderRef CreateTextureLoader(::Effekseer::FileInterface* fileInterface = NULL) override;

	/**
	@brief	�W���̃��f���Ǎ��N���X�𐶐�����B
	*/
	::Effekseer::ModelLoaderRef CreateModelLoader(::Effekseer::FileInterface* fileInterface = NULL) override;

	::Effekseer::MaterialLoaderRef CreateMaterialLoader(::Effekseer::FileInterface* fileInterface = nullptr) override { return nullptr; }

	/**
	@brief	�����_�[�X�e�[�g�������I�Ƀ��Z�b�g����B
	*/
	void ResetRenderState() override;

	/**
	@brief	�w�i��c�܂���G�t�F�N�g���`�悳���O�ɌĂ΂��R�[���o�b�N���擾����B
	*/
	::EffekseerRenderer::DistortingCallback* GetDistortingCallback() override;

	/**
	@brief	�w�i��c�܂���G�t�F�N�g���`�悳���O�ɌĂ΂��R�[���o�b�N��ݒ肷��B
	*/
	void SetDistortingCallback(::EffekseerRenderer::DistortingCallback* callback) override;

	void SetBackground(void* image);

	VertexBuffer* GetVertexBuffer();

	IndexBuffer* GetIndexBuffer();

	EffekseerRenderer::StandardRenderer<RendererImplemented, Shader>* GetStandardRenderer();

	::EffekseerRenderer::RenderStateBase* GetRenderState();

	void SetVertexBuffer(VertexBuffer* vertexBuffer, int32_t size);
	void SetIndexBuffer(IndexBuffer* indexBuffer);

	void SetLayout(Shader* shader);
	void DrawSprites(int32_t spriteCount, int32_t vertexOffset);

	void DrawModel(Effekseer::ModelRef model,
				   std::vector<Effekseer::Matrix44>& matrixes,
				   std::vector<Effekseer::RectF>& uvs,
				   std::vector<Effekseer::RectF>& alphaUVs,
				   std::vector<Effekseer::RectF>& uvDistortionUVs,
				   std::vector<Effekseer::RectF>& blendUVs,
				   std::vector<Effekseer::RectF>& blendAlphaUVs,
				   std::vector<Effekseer::RectF>& blendUVDistortionUVs,
				   std::vector<float>& flipbookIndexAndNextRates,
				   std::vector<float>& alphaThresholds,
				   std::vector<Effekseer::Color>& colors,
				   std::vector<int32_t>& times,
				   std::vector<std::array<float, 4>>& customData1,
				   std::vector<std::array<float, 4>>& customData2);

	Shader* GetShader(::EffekseerRenderer::RendererShaderType materialType) const;

	void BeginShader(Shader* shader);
	void EndShader(Shader* shader);

	void SetVertexBufferToShader(const void* data, int32_t size, int32_t dstOffset);

	void SetPixelBufferToShader(const void* data, int32_t size, int32_t dstOffset);

	void SetTextures(Shader* shader, Effekseer::Backend::TextureRef* textures, int32_t count);

	std::vector<UnityRenderParameter>& GetRenderParameters() { return renderParameters; };
	// std::vector<uint8_t>& GetRenderVertexBuffer() { return exportedVertexBuffer; }
	std::vector<uint8_t>& GetRenderInfoBuffer() { return exportedInfoBuffer; }

	int32_t GetStrideBufferCount() const;
	StrideBufferParameter GetStrideBufferParameter(int32_t index) const;

	virtual int GetRef() { return ::Effekseer::ReferenceObject::GetRef(); }
	virtual int AddRef() { return ::Effekseer::ReferenceObject::AddRef(); }
	virtual int Release() { return ::Effekseer::ReferenceObject::Release(); }
};

} // namespace EffekseerRendererUnity
