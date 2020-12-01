
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
	struct UnityRenderParameter
	{
		//! 0 - procedual, 1 - model
		int RenderMode = 0;

		EffekseerRenderer::RendererShaderType MaterialType = EffekseerRenderer::RendererShaderType::Unlit;

		//! VertexBuffer
		int VertexBufferOffset = 0;

		//! Stride for material
		int VertexBufferStride = 0;

		//! For model
		int CustomData1BufferOffset = 0;

		//! For model
		int CustomData2BufferOffset = 0;

		//! For model
		int UniformBufferOffset = 0;

		//! Element count (Triangle) or instance
		int32_t ElementCount;

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

	virtual ~Model() = default;

	void* InternalPtr = nullptr;
};

using Vertex = EffekseerRenderer::SimpleVertex;
using VertexDistortion = EffekseerRenderer::VertexDistortion;
using DynamicVertex = EffekseerRenderer::DynamicVertex;

struct ModelParameter
{
	Effekseer::Matrix44 Matrix;
	Effekseer::Color VertexColors;
	Effekseer::RectF UV;
	int32_t Time;
};

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

class RendererImplemented : public ::EffekseerRenderer::Renderer, public ::Effekseer::ReferenceObject
{
protected:
	int32_t m_squareMaxCount = 0;

	VertexBuffer* m_vertexBuffer = nullptr;

	std::unique_ptr<Shader> unlitShader_;
	std::unique_ptr<Shader> backDistortedShader_;
	std::unique_ptr<Shader> litShader_;

	Shader* m_currentShader = nullptr;
	RenderState* m_renderState = nullptr;

	int32_t textureCount_ = 0;
	std::array<void*, Effekseer::TextureSlotMax> m_textures;

	std::vector<UnityRenderParameter> renderParameters;
	std::vector<ModelParameter> modelParameters;

	Effekseer::RendererMaterialType rendererMaterialType_ = Effekseer::RendererMaterialType::Default;
	float m_distortionIntensity = 0.0f;

	std::vector<uint8_t> exportedVertexBuffer;
	std::vector<uint8_t> exportedInfoBuffer;

	Effekseer::TextureData backgroundData;

	EffekseerRenderer::StandardRenderer<RendererImplemented, Shader>* m_standardRenderer = nullptr;

	int32_t AddVertexBuffer(const void* data, int32_t size);
	int32_t AddInfoBuffer(const void* data, int32_t size);
	void AlignVertexBuffer(int32_t alignment);


public:
	static Effekseer::RefPtr<RendererImplemented> Create();

	RendererImplemented();
	virtual ~RendererImplemented();

	void OnLostDevice() override {}
	void OnResetDevice() override {}

	/**
	@brief	初期化
	*/
	bool Initialize(int32_t squareMaxCount);

	/**
	@brief	ステートを復帰するかどうかのフラグを設定する。
	*/
	void SetRestorationOfStatesFlag(bool flag) override;

	/**
	@brief	描画を開始する時に実行する。
	*/
	bool BeginRendering() override;

	/**
	@brief	描画を終了する時に実行する。
	*/
	bool EndRendering() override;

	/**
	@brief	最大描画スプライト数を取得する。
	*/
	int32_t GetSquareMaxCount() const override;

	/**
	@brief	スプライトレンダラーを生成する。
	*/
	::Effekseer::SpriteRendererRef CreateSpriteRenderer() override;

	/**
	@brief	リボンレンダラーを生成する。
	*/
	::Effekseer::RibbonRendererRef CreateRibbonRenderer() override;

	/**
	@brief	リングレンダラーを生成する。
	*/
	::Effekseer::RingRendererRef CreateRingRenderer() override;

	/**
	@brief	モデルレンダラーを生成する。
	*/
	::Effekseer::ModelRendererRef CreateModelRenderer() override;

	/**
	@brief	軌跡レンダラーを生成する。
	*/
	::Effekseer::TrackRendererRef CreateTrackRenderer() override;

	/**
	@brief	標準のテクスチャ読込クラスを生成する。
	*/
	::Effekseer::TextureLoaderRef CreateTextureLoader(::Effekseer::FileInterface* fileInterface = NULL) override;

	/**
	@brief	標準のモデル読込クラスを生成する。
	*/
	::Effekseer::ModelLoaderRef CreateModelLoader(::Effekseer::FileInterface* fileInterface = NULL) override;

	::Effekseer::MaterialLoaderRef CreateMaterialLoader(::Effekseer::FileInterface* fileInterface = nullptr) override { return nullptr; }

	/**
	@brief	レンダーステートを強制的にリセットする。
	*/
	void ResetRenderState() override;

	/**
	@brief	背景を歪ませるエフェクトが描画される前に呼ばれるコールバックを取得する。
	*/
	::EffekseerRenderer::DistortingCallback* GetDistortingCallback() override;

	/**
	@brief	背景を歪ませるエフェクトが描画される前に呼ばれるコールバックを設定する。
	*/
	void SetDistortingCallback(::EffekseerRenderer::DistortingCallback* callback) override;

	Effekseer::TextureData* GetBackground();

	void SetBackground(void* image);

	VertexBuffer* GetVertexBuffer();

	IndexBuffer* GetIndexBuffer();

	EffekseerRenderer::StandardRenderer<RendererImplemented, Shader>* GetStandardRenderer();

	::EffekseerRenderer::RenderStateBase* GetRenderState();

	void SetVertexBuffer(VertexBuffer* vertexBuffer, int32_t size);
	void SetIndexBuffer(IndexBuffer* indexBuffer);

	void SetLayout(Shader* shader);
	void DrawSprites(int32_t spriteCount, int32_t vertexOffset);

	void DrawModel(void* model,
				   std::vector<Effekseer::Matrix44>& matrixes,
				   std::vector<Effekseer::RectF>& uvs,
				   std::vector<Effekseer::Color>& colors,
				   std::vector<int32_t>& times,
				   std::vector<std::array<float, 4>>& customData1,
				   std::vector<std::array<float, 4>>& customData2);

	Shader* GetShader(::EffekseerRenderer::RendererShaderType materialType) const;

	void BeginShader(Shader* shader);
	void EndShader(Shader* shader);

	void SetVertexBufferToShader(const void* data, int32_t size, int32_t dstOffset);

	void SetPixelBufferToShader(const void* data, int32_t size, int32_t dstOffset);

	void SetTextures(Shader* shader, Effekseer::TextureData** textures, int32_t count);
	void SetDistortionIntensity(float value) { m_distortionIntensity = value; }

	std::vector<UnityRenderParameter>& GetRenderParameters() { return renderParameters; };
	std::vector<uint8_t>& GetRenderVertexBuffer() { return exportedVertexBuffer; }
	std::vector<uint8_t>& GetRenderInfoBuffer() { return exportedInfoBuffer; }

	virtual int GetRef() { return ::Effekseer::ReferenceObject::GetRef(); }
	virtual int AddRef() { return ::Effekseer::ReferenceObject::AddRef(); }
	virtual int Release() { return ::Effekseer::ReferenceObject::Release(); }
};

} // namespace EffekseerRendererUnity