#include "EffekseerRendererImplemented.h"
#include "EffekseerRendererIndexBuffer.h"
#include "EffekseerRendererRenderState.h"
#include "EffekseerRendererShader.h"
#include "EffekseerRendererTextureLoader.h"
#include "EffekseerRendererVertexBuffer.h"

namespace EffekseerPlugin
{
extern EffekseerRenderer::RendererRef g_EffekseerRenderer;
}

extern "C"
{
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API GetUnityRenderParameter(UnityRenderParameter* dst, int index)
	{
		if (EffekseerPlugin::g_EffekseerRenderer == nullptr)
			return;
		auto renderer = (EffekseerRendererUnity::RendererImplemented*)EffekseerPlugin::g_EffekseerRenderer.Get();
		*dst = renderer->GetRenderParameters()[index];
	}

	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API GetUnityRenderParameterCount()
	{
		if (EffekseerPlugin::g_EffekseerRenderer == nullptr)
			return 0;
		auto renderer = (EffekseerRendererUnity::RendererImplemented*)EffekseerPlugin::g_EffekseerRenderer.Get();
		return static_cast<int32_t>(renderer->GetRenderParameters().size());
	}

	UNITY_INTERFACE_EXPORT void* UNITY_INTERFACE_API GetUnityRenderVertexBuffer()
	{
		if (EffekseerPlugin::g_EffekseerRenderer == nullptr)
			return nullptr;
		auto renderer = (EffekseerRendererUnity::RendererImplemented*)EffekseerPlugin::g_EffekseerRenderer.Get();
		return renderer->GetRenderVertexBuffer().data();
	}

	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API GetUnityRenderVertexBufferCount()
	{
		if (EffekseerPlugin::g_EffekseerRenderer == nullptr)
			return 0;
		auto renderer = (EffekseerRendererUnity::RendererImplemented*)EffekseerPlugin::g_EffekseerRenderer.Get();
		return static_cast<int32_t>(renderer->GetRenderVertexBuffer().size());
	}

	UNITY_INTERFACE_EXPORT void* UNITY_INTERFACE_API GetUnityRenderInfoBuffer()
	{
		if (EffekseerPlugin::g_EffekseerRenderer == nullptr)
			return nullptr;
		auto renderer = (EffekseerRendererUnity::RendererImplemented*)EffekseerPlugin::g_EffekseerRenderer.Get();
		return renderer->GetRenderInfoBuffer().data();
	}
}

namespace EffekseerRendererUnity
{

/**
	@note
	The size must be lower than 100byte
*/
struct UnityModelParameter1
{
	Effekseer::Matrix44 Matrix;
	float VColor[4];
	Effekseer::RectF UV;
	int32_t Time;
};

/**
	@note
	The size must be lower than 100byte
*/
struct UnityModelParameter2
{
	Effekseer::RectF AlphaUV;
	Effekseer::RectF DistortionUV;
	Effekseer::RectF BlendUV;
	Effekseer::RectF BlendAlphaUV;
	Effekseer::RectF BlendDistortionUV;
	float FlipbookIndexAndNextRate;
	float AlphaThreshold;
	float ViewOffsetDistance;
};

static int GetAlignedOffset(int offset, int size) { return ((offset + (size - 1)) / size) * size; }

void ExtractTextures(const Effekseer::Effect* effect,
					 const Effekseer::NodeRendererBasicParameter* param,
					 std::array<Effekseer::TextureRef, ::Effekseer::TextureSlotMax>& textures,
					 int32_t& textureCount)
{
	if (param->MaterialType == Effekseer::RendererMaterialType::File)
	{
		auto materialParam = param->MaterialRenderDataPtr;

		textureCount = 0;

		if (materialParam->MaterialTextures.size() > 0)
		{
			textureCount = Effekseer::Min(static_cast<int32_t>(materialParam->MaterialTextures.size()), ::Effekseer::UserTextureSlotMax);

			for (size_t i = 0; i < textureCount; i++)
			{
				if (materialParam->MaterialTextures[i].Type == 1)
				{
					if (materialParam->MaterialTextures[i].Index >= 0)
					{
						textures[i] = effect->GetNormalImage(materialParam->MaterialTextures[i].Index);
					}
					else
					{
						textures[i] = nullptr;
					}
				}
				else
				{
					if (materialParam->MaterialTextures[i].Index >= 0)
					{
						textures[i] = effect->GetColorImage(materialParam->MaterialTextures[i].Index);
					}
					else
					{
						textures[i] = nullptr;
					}
				}
			}
		}
	}
}

ModelRenderer::ModelRenderer(RendererImplemented* renderer) : m_renderer(renderer) {}

ModelRenderer::~ModelRenderer() {}

::Effekseer::ModelRendererRef ModelRenderer::Create(RendererImplemented* renderer)
{
	assert(renderer != NULL);

	return Effekseer::MakeRefPtr<ModelRenderer>(renderer);
}

void ModelRenderer::BeginRendering(const efkModelNodeParam& parameter, int32_t count, void* userData)
{
	BeginRendering_(m_renderer, parameter, count, userData);
}

void ModelRenderer::Rendering(const efkModelNodeParam& parameter, const efkModelInstanceParam& instanceParameter, void* userData)
{
	Rendering_(m_renderer, parameter, instanceParameter, userData);
}

void ModelRenderer::EndRendering(const efkModelNodeParam& parameter, void* userData)
{
	if (m_matrixes.size() == 0)
		return;
	if (parameter.ModelIndex < 0)
		return;

	auto model = parameter.EffectPointer->GetModel(parameter.ModelIndex);
	if (model == nullptr)
		return;

	if (parameter.BasicParameterPtr->MaterialType == Effekseer::RendererMaterialType::File)
	{
		if (parameter.BasicParameterPtr->MaterialRenderDataPtr == nullptr ||
			parameter.BasicParameterPtr->MaterialRenderDataPtr->MaterialIndex < 0)
		{
			return;
		}

		auto material = parameter.EffectPointer->GetMaterial(parameter.BasicParameterPtr->MaterialRenderDataPtr->MaterialIndex);

		if (material == nullptr)
		{
			return;
		}
	}

	SortTemporaryValues(m_renderer, parameter);

	bool fileRefraction = false;

	if (parameter.BasicParameterPtr->MaterialRenderDataPtr != nullptr &&
		parameter.BasicParameterPtr->MaterialRenderDataPtr->MaterialIndex >= 0 &&
		parameter.EffectPointer->GetMaterial(parameter.BasicParameterPtr->MaterialRenderDataPtr->MaterialIndex) != nullptr)
	{
		fileRefraction =
			parameter.EffectPointer->GetMaterial(parameter.BasicParameterPtr->MaterialRenderDataPtr->MaterialIndex)->IsRefractionRequired;
	}

	int stageCount = 1;
	if (fileRefraction)
	{
		stageCount = 2;
	}

	for (int stageInd = 0; stageInd < stageCount; stageInd++)
	{

		Shader* shader = nullptr;
		if (parameter.BasicParameterPtr->MaterialType == Effekseer::RendererMaterialType::File)
		{
			if (parameter.BasicParameterPtr->MaterialRenderDataPtr != nullptr &&
				parameter.BasicParameterPtr->MaterialRenderDataPtr->MaterialIndex >= 0 &&
				parameter.EffectPointer->GetMaterial(parameter.BasicParameterPtr->MaterialRenderDataPtr->MaterialIndex) != nullptr)
			{
				if (fileRefraction && stageInd == 0)
				{
					shader =
						(Shader*)parameter.EffectPointer->GetMaterial(parameter.BasicParameterPtr->MaterialRenderDataPtr->MaterialIndex)
							->RefractionModelUserPtr;
				}
				else
				{
					shader =
						(Shader*)parameter.EffectPointer->GetMaterial(parameter.BasicParameterPtr->MaterialRenderDataPtr->MaterialIndex)
							->ModelUserPtr;
				}
			}
			else
			{
				return;
			}
		}
		else
		{
			shader = m_renderer->GetShader(collector_.ShaderType);
		}

		::EffekseerRenderer::RenderStateBase::State& state = m_renderer->GetRenderState()->Push();
		state.DepthTest = parameter.ZTest;
		state.DepthWrite = parameter.ZWrite;
		state.AlphaBlend = parameter.BasicParameterPtr->AlphaBlend;
		state.CullingType = parameter.Culling;

		m_renderer->BeginShader(shader);

		int32_t textureCount = collector_.TextureCount;
		std::array<Effekseer::Backend::TextureRef, ::Effekseer::TextureSlotMax> textures = collector_.Textures;

		for (int32_t i = 0; i < textureCount; i++)
		{
			state.TextureFilterTypes[i] = parameter.BasicParameterPtr->TextureFilters[i];
			state.TextureWrapTypes[i] = parameter.BasicParameterPtr->TextureWraps[i];
		}

		if (textureCount > 0)
		{
			m_renderer->SetTextures(nullptr, textures.data(), textureCount);
		}

		if (parameter.BasicParameterPtr->MaterialType == Effekseer::RendererMaterialType::File)
		{
			float* cutomData1Ptr = nullptr;
			float* cutomData2Ptr = nullptr;

			auto materialParam = parameter.BasicParameterPtr->MaterialRenderDataPtr;
			auto material = parameter.EffectPointer->GetMaterial(parameter.BasicParameterPtr->MaterialRenderDataPtr->MaterialIndex);

			StoreFileUniform<RendererImplemented, Shader, 1>(
				m_renderer, shader, material, materialParam, parameter, stageInd, cutomData1Ptr, cutomData2Ptr);
		}

		m_renderer->GetRenderState()->Update(false);

		m_renderer->DrawModel(model,
							  m_matrixes,
							  m_uv,
							  m_alphaUV,
							  m_uvDistortionUV,
							  m_blendUV,
							  m_blendAlphaUV,
							  m_blendUVDistortionUV,
							  m_flipbookIndexAndNextRate,
							  m_alphaThreshold,
							  m_colors,
							  m_times,
							  customData1_,
							  customData2_);

		m_renderer->EndShader(shader);

		m_renderer->GetRenderState()->Pop();
	}
}

int32_t RendererImplemented::AddVertexBuffer(const void* data, int32_t size)
{
	auto ret = static_cast<int32_t>(exportedVertexBuffer.size());

	exportedVertexBuffer.resize(exportedVertexBuffer.size() + size);
	memcpy(exportedVertexBuffer.data() + ret, data, size);
	return ret;
}

int32_t RendererImplemented::AddInfoBuffer(const void* data, int32_t size)
{
	auto ret = static_cast<int32_t>(exportedInfoBuffer.size());

	exportedInfoBuffer.resize(exportedInfoBuffer.size() + size);
	memcpy(exportedInfoBuffer.data() + ret, data, size);
	return ret;
}

void RendererImplemented::AlignVertexBuffer(int32_t alignment)
{
	exportedVertexBuffer.resize(GetAlignedOffset(static_cast<int32_t>(exportedVertexBuffer.size()), alignment));
}

Effekseer::RefPtr<RendererImplemented> RendererImplemented::Create() { return Effekseer::MakeRefPtr<RendererImplemented>(); }

RendererImplemented::RendererImplemented() { 
	backgroundData_ = Effekseer::MakeRefPtr<Effekseer::Texture>();
	auto backend = Effekseer::MakeRefPtr<Texture>(nullptr);
	backgroundData_->SetBackend(backend);
}

RendererImplemented::~RendererImplemented()
{
	ES_SAFE_DELETE(m_renderState);
	ES_SAFE_DELETE(m_standardRenderer);
	ES_SAFE_DELETE(m_vertexBuffer);
}

bool RendererImplemented::Initialize(int32_t squareMaxCount)
{
	m_squareMaxCount = squareMaxCount;
	m_renderState = new RenderState();
	m_vertexBuffer = new VertexBuffer(EffekseerRenderer::GetMaximumVertexSizeInAllTypes() * m_squareMaxCount * 4, true);

	unlitShader_ = std::unique_ptr<Shader>(new Shader(EffekseerRenderer::RendererShaderType::Unlit));
	backDistortedShader_ = std::unique_ptr<Shader>(new Shader(EffekseerRenderer::RendererShaderType::BackDistortion));
	litShader_ = std::unique_ptr<Shader>(new Shader(EffekseerRenderer::RendererShaderType::Lit));

	m_standardRenderer = new EffekseerRenderer::StandardRenderer<RendererImplemented, Shader>(this);

	exportedVertexBuffer.reserve(sizeof(UnityVertex) * 2000);
	return true;
}

void RendererImplemented::SetRestorationOfStatesFlag(bool flag) {}

bool RendererImplemented::BeginRendering()
{
	impl->CalculateCameraProjectionMatrix();

	// Reset the renderer
	m_standardRenderer->ResetAndRenderingIfRequired();

	exportedVertexBuffer.resize(0);
	exportedInfoBuffer.resize(0);
	renderParameters.resize(0);
	return true;
}

bool RendererImplemented::EndRendering()
{
	// Reset the renderer
	m_standardRenderer->ResetAndRenderingIfRequired();

	// ForUnity
	AlignVertexBuffer(sizeof(UnityVertex));

	return true;
}

int32_t RendererImplemented::GetSquareMaxCount() const { return m_squareMaxCount; }

::Effekseer::SpriteRendererRef RendererImplemented::CreateSpriteRenderer()
{
	return Effekseer::MakeRefPtr<::EffekseerRenderer::SpriteRendererBase<RendererImplemented, false>>(this);
}

::Effekseer::RibbonRendererRef RendererImplemented::CreateRibbonRenderer()
{
	return Effekseer::MakeRefPtr<::EffekseerRenderer::RibbonRendererBase<RendererImplemented, false>>(this);
}

::Effekseer::RingRendererRef RendererImplemented::CreateRingRenderer()
{
	auto ret = Effekseer::MakeRefPtr<::EffekseerRenderer::RingRendererBase<RendererImplemented, false>>(this);
	ret->SetFasterSngleRingModeEnabled(false);
	return ret;
}

::Effekseer::ModelRendererRef RendererImplemented::CreateModelRenderer() { return ModelRenderer::Create(this); }

::Effekseer::TrackRendererRef RendererImplemented::CreateTrackRenderer()
{
	return Effekseer::MakeRefPtr<::EffekseerRenderer::TrackRendererBase<RendererImplemented, false>>(this);
}

::Effekseer::TextureLoaderRef RendererImplemented::CreateTextureLoader(::Effekseer::FileInterface* fileInterface) { return nullptr; }

::Effekseer::ModelLoaderRef RendererImplemented::CreateModelLoader(::Effekseer::FileInterface* fileInterface) { return nullptr; }

void RendererImplemented::ResetRenderState() {}

::EffekseerRenderer::DistortingCallback* RendererImplemented::GetDistortingCallback() { return nullptr; }

void RendererImplemented::SetDistortingCallback(::EffekseerRenderer::DistortingCallback* callback) {}

void RendererImplemented::SetBackground(void* image)
{
	auto backend = backgroundData_->GetBackend();
	backend.DownCast<Texture>()->UserData = image;
	EffekseerRenderer::Renderer::SetBackground(backend);
}

VertexBuffer* RendererImplemented::GetVertexBuffer() { return m_vertexBuffer; }

IndexBuffer* RendererImplemented::GetIndexBuffer() { return nullptr; }

EffekseerRenderer::StandardRenderer<RendererImplemented, Shader>* RendererImplemented::GetStandardRenderer() { return m_standardRenderer; }

::EffekseerRenderer::RenderStateBase* RendererImplemented::GetRenderState() { return m_renderState; }

void RendererImplemented::SetVertexBuffer(VertexBuffer* vertexBuffer, int32_t size) {}

void RendererImplemented::SetIndexBuffer(IndexBuffer* indexBuffer) {}

void RendererImplemented::SetLayout(Shader* shader) {}

inline Effekseer::Vector3D UnpackVector3DF(const Effekseer::Color& v)
{
	Effekseer::Vector3D ret;
	ret.X = (v.R / 255.0f * 2.0f - 1.0f);
	ret.Y = (v.G / 255.0f * 2.0f - 1.0f);
	ret.Z = (v.B / 255.0f * 2.0f - 1.0f);
	return ret;
}

template <typename T, typename U> void CopyBuffer(const T& dstUnityVertex, const U& srcVertex)
{
	dstUnityVertex.Pos = srcVertex.Pos;
	dstUnityVertex.UV[0] = srcVertex.UV[0];
	dstUnityVertex.UV[1] = srcVertex.UV[1];
	dstUnityVertex.Col[0] = srcVertex.Col.R / 255.0f;
	dstUnityVertex.Col[1] = srcVertex.Col.G / 255.0f;
	dstUnityVertex.Col[2] = srcVertex.Col.B / 255.0f;
	dstUnityVertex.Col[3] = srcVertex.Col.A / 255.0f;
}

void StorePixelConstantBuffer(UnityRenderParameter& rp, EffekseerRenderer::PixelConstantBuffer* constantBuffer)
{
	rp.FlipbookParams.Enable = constantBuffer->FlipbookParam.EnableInterpolation;
	rp.FlipbookParams.LoopType = constantBuffer->FlipbookParam.InterpolationType;
	rp.UVDistortionIntensity = constantBuffer->UVDistortionParam.Intensity;
	rp.TextureBlendType = constantBuffer->BlendTextureParam.BlendType;
	rp.BlendUVDistortionIntensity = constantBuffer->UVDistortionParam.BlendIntensity;
	rp.EnableFalloff = constantBuffer->FalloffParam.Enable;
	rp.FalloffParam.BeginColor = constantBuffer->FalloffParam.BeginColor;
	rp.FalloffParam.EndColor = constantBuffer->FalloffParam.EndColor;
	rp.FalloffParam.ColorBlendType = constantBuffer->FalloffParam.ColorBlendType;
	rp.FalloffParam.Pow = constantBuffer->FalloffParam.Pow;
	rp.EmissiveScaling = constantBuffer->EmmisiveParam.EmissiveScaling;
	rp.EdgeParams.Threshold = constantBuffer->EdgeParam.Threshold;
	rp.EdgeParams.ColorScaling = constantBuffer->EdgeParam.ColorScaling;
	rp.EdgeParams.Color = constantBuffer->EdgeParam.EdgeColor;
}

void StoreDistortionPixelConstantBuffer(UnityRenderParameter& rp, EffekseerRenderer::PixelConstantBufferDistortion* constantBuffer)
{
	rp.FlipbookParams.Enable = constantBuffer->FlipbookParam.EnableInterpolation;
	rp.FlipbookParams.LoopType = constantBuffer->FlipbookParam.InterpolationType;
	rp.UVDistortionIntensity = constantBuffer->UVDistortionParam.Intensity;
	rp.TextureBlendType = constantBuffer->BlendTextureParam.BlendType;
	rp.BlendUVDistortionIntensity = constantBuffer->UVDistortionParam.BlendIntensity;
}

void RendererImplemented::DrawSprites(int32_t spriteCount, int32_t vertexOffset)
{
	UnityRenderParameter rp;
	rp.MaterialType = m_currentShader->GetType();
	rp.ZTest = GetRenderState()->GetActiveState().DepthTest ? 1 : 0;
	rp.ZWrite = GetRenderState()->GetActiveState().DepthWrite ? 1 : 0;
	rp.Blend = (int)GetRenderState()->GetActiveState().AlphaBlend;
	rp.Culling = (int)GetRenderState()->GetActiveState().CullingType;
	rp.RenderMode = 0;
	rp.ModelPtr = nullptr;

	for (size_t i = 0; i < textures_.size(); i++)
	{
		rp.TexturePtrs[i] = textures_[i];
		rp.TextureFilterTypes[i] = (int)GetRenderState()->GetActiveState().TextureFilterTypes[i];
		rp.TextureWrapTypes[i] = (int)GetRenderState()->GetActiveState().TextureWrapTypes[i];
	}

	if (m_currentShader->GetType() == EffekseerRenderer::RendererShaderType::Material)
	{
		if (m_currentShader == nullptr)
		{
			return;
		}

		if (m_currentShader->GetMaterial() == nullptr)
		{
			return;
		}

		rp.MaterialPtr = m_currentShader->GetUnityMaterial();
		rp.IsRefraction = m_currentShader->GetIsRefraction() ? 1 : 0;

		const auto& nativeMaterial = m_currentShader->GetMaterial();
		assert(!nativeMaterial->GetIsSimpleVertex());

		auto* origin = (uint8_t*)m_vertexBuffer->GetResource();

		int32_t customDataStride = (nativeMaterial->GetCustomData1Count() + nativeMaterial->GetCustomData2Count()) * sizeof(float);

		rp.VertexBufferStride = sizeof(UnityDynamicVertex) + customDataStride;
		AlignVertexBuffer(rp.VertexBufferStride);

		int32_t startOffset = static_cast<int32_t>(exportedVertexBuffer.size());

		const int32_t stride = static_cast<int>(sizeof(EffekseerRenderer::DynamicVertex)) + customDataStride;

		EffekseerRenderer::StrideView<EffekseerRenderer::DynamicVertex> vs(origin, stride, vertexOffset + spriteCount * 4);
		EffekseerRenderer::StrideView<EffekseerRenderer::DynamicVertex> custom1(
			origin + sizeof(EffekseerRenderer::DynamicVertex), stride, vertexOffset + spriteCount * 4);
		EffekseerRenderer::StrideView<EffekseerRenderer::DynamicVertex> custom2(origin + sizeof(EffekseerRenderer::DynamicVertex) +
																					sizeof(float) * nativeMaterial->GetCustomData1Count(),
																				stride,
																				vertexOffset + spriteCount * 4);

		// Uniform
		auto uniformOffset = m_currentShader->GetParameterGenerator()->PixelUserUniformOffset;
		auto uniformBuffer = static_cast<uint8_t*>(m_currentShader->GetPixelConstantBuffer()) + uniformOffset;
		rp.UniformBufferOffset = AddInfoBuffer(uniformBuffer, m_currentShader->GetMaterial()->GetUniformCount() * sizeof(float) * 4);

		for (int32_t vi = vertexOffset; vi < vertexOffset + spriteCount * 4; vi++)
		{
			auto& v = vs[vi];
			UnityDynamicVertex unity_v;

			unity_v.Pos = v.Pos;
			unity_v.UV1[0] = v.UV1[0];
			unity_v.UV1[1] = v.UV1[1];
			unity_v.UV2[0] = v.UV2[0];
			unity_v.UV2[1] = v.UV2[1];
			unity_v.Col[0] = v.Col.R / 255.0f;
			unity_v.Col[1] = v.Col.G / 255.0f;
			unity_v.Col[2] = v.Col.B / 255.0f;
			unity_v.Col[3] = v.Col.A / 255.0f;
			unity_v.Tangent = UnpackVector3DF(v.Tangent);
			unity_v.Normal = UnpackVector3DF(v.Normal);

			AddVertexBuffer(&unity_v, sizeof(UnityDynamicVertex));

			if (nativeMaterial->GetCustomData1Count() > 0)
			{
				std::array<float, 4> customData1;
				auto c = (float*)(&custom1[vi]);
				memcpy(customData1.data(), c, sizeof(float) * nativeMaterial->GetCustomData1Count());

				AddVertexBuffer(customData1.data(), sizeof(float) * nativeMaterial->GetCustomData1Count());
			}

			if (nativeMaterial->GetCustomData2Count() > 0)
			{
				std::array<float, 4> customData2;
				auto c = (float*)(&custom2[vi]);
				memcpy(customData2.data(), c, sizeof(float) * nativeMaterial->GetCustomData2Count());

				AddVertexBuffer(customData2.data(), sizeof(float) * nativeMaterial->GetCustomData2Count());
			}
		}

		rp.VertexBufferOffset = startOffset;

		rp.ElementCount = spriteCount;
		renderParameters.push_back(rp);
		return;
	}
	else if (m_currentShader->GetType() == EffekseerRenderer::RendererShaderType::BackDistortion ||
			 m_currentShader->GetType() == EffekseerRenderer::RendererShaderType::AdvancedBackDistortion)
	{
		if (textures_[1] == nullptr)
		{
			return;
		}

		rp.VertexBufferStride = sizeof(UnityDynamicVertex);
		AlignVertexBuffer(rp.VertexBufferStride);
		int32_t startOffset = static_cast<int32_t>(exportedVertexBuffer.size());

		if (m_currentShader->GetType() == EffekseerRenderer::RendererShaderType::BackDistortion)
		{
			auto vs = (DynamicVertex*)m_vertexBuffer->GetResource();
			for (int32_t vi = vertexOffset; vi < vertexOffset + spriteCount * 4; vi++)
			{
				auto& v = vs[vi];
				AddVertexBufferAsDynamicVertex(v);
			}
		}
		else
		{
			auto vs = (EffekseerRenderer::AdvancedLightingVertex*)m_vertexBuffer->GetResource();
			for (int32_t vi = vertexOffset; vi < vertexOffset + spriteCount * 4; vi++)
			{
				auto& v = vs[vi];
				AddVertexBufferAsDynamicVertex(v);
			}

			AlignVertexBuffer(sizeof(AdvancedVertexParameter));
			rp.AdvancedBufferOffset = static_cast<int32_t>(exportedVertexBuffer.size());
			for (int32_t vi = vertexOffset; vi < vertexOffset + spriteCount * 4; vi++)
			{
				auto& v = vs[vi];
				AddVertexBufferAsAdvancedData(v);
			}
		}

		auto constantBuffer = (EffekseerRenderer::PixelConstantBufferDistortion*)m_currentShader->GetPixelConstantBuffer();
		rp.DistortionIntensity = constantBuffer->DistortionIntencity[0];

		StoreDistortionPixelConstantBuffer(rp, constantBuffer);

		rp.VertexBufferOffset = startOffset;
		rp.MaterialPtr = nullptr;
		rp.ElementCount = spriteCount;
		renderParameters.push_back(rp);
	}
	else if (m_currentShader->GetType() == EffekseerRenderer::RendererShaderType::Lit ||
			 m_currentShader->GetType() == EffekseerRenderer::RendererShaderType::AdvancedLit)
	{
		rp.VertexBufferStride = sizeof(UnityDynamicVertex);
		AlignVertexBuffer(rp.VertexBufferStride);
		int32_t startOffset = static_cast<int32_t>(exportedVertexBuffer.size());

		if (m_currentShader->GetType() == EffekseerRenderer::RendererShaderType::Lit)
		{
			auto vs = (DynamicVertex*)m_vertexBuffer->GetResource();
			for (int32_t vi = vertexOffset; vi < vertexOffset + spriteCount * 4; vi++)
			{
				auto& v = vs[vi];
				AddVertexBufferAsDynamicVertex(v);
			}
		}
		else
		{
			auto vs = (EffekseerRenderer::AdvancedLightingVertex*)m_vertexBuffer->GetResource();
			for (int32_t vi = vertexOffset; vi < vertexOffset + spriteCount * 4; vi++)
			{
				auto& v = vs[vi];
				AddVertexBufferAsDynamicVertex(v);
			}

			AlignVertexBuffer(sizeof(AdvancedVertexParameter));
			rp.AdvancedBufferOffset = static_cast<int32_t>(exportedVertexBuffer.size());
			for (int32_t vi = vertexOffset; vi < vertexOffset + spriteCount * 4; vi++)
			{
				auto& v = vs[vi];
				AddVertexBufferAsAdvancedData(v);
			}
		}

		auto constantBuffer = static_cast<EffekseerRenderer::PixelConstantBuffer*>(m_currentShader->GetPixelConstantBuffer());
		StorePixelConstantBuffer(rp, constantBuffer);

		rp.VertexBufferOffset = startOffset;
		rp.MaterialPtr = nullptr;
		rp.ElementCount = spriteCount;
		renderParameters.push_back(rp);
	}
	else if (m_currentShader->GetType() == EffekseerRenderer::RendererShaderType::Unlit ||
			 m_currentShader->GetType() == EffekseerRenderer::RendererShaderType::AdvancedUnlit)
	{
		rp.VertexBufferStride = sizeof(UnityVertex);
		AlignVertexBuffer(rp.VertexBufferStride);
		int32_t startOffset = static_cast<int32_t>(exportedVertexBuffer.size());

		if (m_currentShader->GetType() == EffekseerRenderer::RendererShaderType::Unlit)
		{
			auto vs = (Vertex*)m_vertexBuffer->GetResource();
			for (int32_t vi = vertexOffset; vi < vertexOffset + spriteCount * 4; vi++)
			{
				auto& v = vs[vi];
				AddVertexBufferAsVertex(v);
			}
		}
		else
		{
			auto vs = (EffekseerRenderer::AdvancedLightingVertex*)m_vertexBuffer->GetResource();
			for (int32_t vi = vertexOffset; vi < vertexOffset + spriteCount * 4; vi++)
			{
				auto& v = vs[vi];
				AddVertexBufferAsVertex(v);
			}

			AlignVertexBuffer(sizeof(AdvancedVertexParameter));
			rp.AdvancedBufferOffset = static_cast<int32_t>(exportedVertexBuffer.size());
			for (int32_t vi = vertexOffset; vi < vertexOffset + spriteCount * 4; vi++)
			{
				auto& v = vs[vi];
				AddVertexBufferAsAdvancedData(v);
			}
		}

		auto constantBuffer = static_cast<EffekseerRenderer::PixelConstantBuffer*>(m_currentShader->GetPixelConstantBuffer());
		StorePixelConstantBuffer(rp, constantBuffer);

		rp.VertexBufferOffset = startOffset;
		rp.MaterialPtr = nullptr;
		rp.ElementCount = spriteCount;
		renderParameters.push_back(rp);
	}
}

void RendererImplemented::DrawModel(Effekseer::ModelRef model,
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
									std::vector<std::array<float, 4>>& customData2)
{
	UnityRenderParameter rp;
	rp.RenderMode = 1;
	rp.MaterialType = m_currentShader->GetType();

	auto model_ = (Model*)model.Get();

	if (model != nullptr)
	{
		rp.ModelPtr = model_->InternalPtr;
	}
	else
	{
		rp.ModelPtr = nullptr;
	}

	if (model == nullptr)
		return;

	for (size_t i = 0; i < textures_.size(); i++)
	{
		rp.TexturePtrs[i] = textures_[i];
		rp.TextureFilterTypes[i] = (int)GetRenderState()->GetActiveState().TextureFilterTypes[i];
		rp.TextureWrapTypes[i] = (int)GetRenderState()->GetActiveState().TextureWrapTypes[i];
	}

	if (m_currentShader->GetType() == EffekseerRenderer::RendererShaderType::Material)
	{
		rp.MaterialPtr = m_currentShader->GetUnityMaterial();
		rp.IsRefraction = m_currentShader->GetIsRefraction() ? 1 : 0;
	}
	else
	{
		if (m_currentShader->GetType() == EffekseerRenderer::RendererShaderType::Lit ||
			m_currentShader->GetType() == EffekseerRenderer::RendererShaderType::AdvancedLit)
		{
			auto constantBuffer = static_cast<EffekseerRenderer::PixelConstantBuffer*>(m_currentShader->GetPixelConstantBuffer());
			StorePixelConstantBuffer(rp, constantBuffer);
		}
		else if (m_currentShader->GetType() == EffekseerRenderer::RendererShaderType::BackDistortion ||
				 m_currentShader->GetType() == EffekseerRenderer::RendererShaderType::AdvancedBackDistortion)
		{
			auto constantBuffer = static_cast<EffekseerRenderer::PixelConstantBufferDistortion*>(m_currentShader->GetPixelConstantBuffer());

			rp.DistortionIntensity = constantBuffer->DistortionIntencity[0];
		}
		else if (m_currentShader->GetType() == EffekseerRenderer::RendererShaderType::Unlit ||
				 m_currentShader->GetType() == EffekseerRenderer::RendererShaderType::AdvancedUnlit)
		{
			auto constantBuffer = static_cast<EffekseerRenderer::PixelConstantBuffer*>(m_currentShader->GetPixelConstantBuffer());
			StorePixelConstantBuffer(rp, constantBuffer);
		}
	}

	rp.ElementCount = static_cast<int32_t>(matrixes.size());
	
	rp.CustomData1BufferOffset = 0;
	rp.CustomData2BufferOffset = 0;

	rp.ZTest = GetRenderState()->GetActiveState().DepthTest ? 1 : 0;
	rp.ZWrite = GetRenderState()->GetActiveState().DepthWrite ? 1 : 0;
	rp.Blend = (int)GetRenderState()->GetActiveState().AlphaBlend;
	rp.Culling = (int)GetRenderState()->GetActiveState().CullingType;

	rp.VertexBufferOffset = static_cast<int32_t>(exportedInfoBuffer.size());

	for (int i = 0; i < matrixes.size(); i++)
	{
		UnityModelParameter1 modelParameter;
		modelParameter.Matrix = matrixes[i];
		modelParameter.UV = uvs[i];
		modelParameter.VColor[0] = colors[i].R / 255.0f;
		modelParameter.VColor[1] = colors[i].G / 255.0f;
		modelParameter.VColor[2] = colors[i].B / 255.0f;
		modelParameter.VColor[3] = colors[i].A / 255.0f;
		modelParameter.Time = times[i] % model_->GetFrameCount();
		AddInfoBuffer(&modelParameter, sizeof(UnityModelParameter1));
	}

	rp.AdvancedBufferOffset = static_cast<int32_t>(exportedInfoBuffer.size());

	for (int i = 0; i < matrixes.size(); i++)
	{
		UnityModelParameter2 modelParameter;
		modelParameter.AlphaUV = alphaUVs[i];
		modelParameter.DistortionUV = uvDistortionUVs[i];
		modelParameter.BlendUV = blendUVs[i];
		modelParameter.BlendAlphaUV = blendAlphaUVs[i];
		modelParameter.BlendDistortionUV = blendUVDistortionUVs[i];
		modelParameter.FlipbookIndexAndNextRate = flipbookIndexAndNextRates[i];
		modelParameter.AlphaThreshold = alphaThresholds[i];

		AddInfoBuffer(&modelParameter, sizeof(UnityModelParameter2));
	}

	if (m_currentShader->GetType() == EffekseerRenderer::RendererShaderType::Material)
	{
		const auto& nativeMaterial = m_currentShader->GetMaterial();
		assert(!nativeMaterial->GetIsSimpleVertex());

		// Uniform
		auto uniformOffset = m_currentShader->GetParameterGenerator()->PixelUserUniformOffset;
		auto uniformBuffer = static_cast<uint8_t*>(m_currentShader->GetPixelConstantBuffer()) + uniformOffset;
		rp.UniformBufferOffset = AddInfoBuffer(uniformBuffer, m_currentShader->GetMaterial()->GetUniformCount() * sizeof(float) * 4);

		if (nativeMaterial->GetCustomData1Count() > 0)
		{
			rp.CustomData1BufferOffset =
				AddInfoBuffer(customData1.data(), static_cast<int32_t>(sizeof(std::array<float, 4>) * customData1.size()));
		}

		if (nativeMaterial->GetCustomData2Count() > 0)
		{
			rp.CustomData2BufferOffset =
				AddInfoBuffer(customData2.data(), static_cast<int32_t>(sizeof(std::array<float, 4>) * customData2.size()));
		}
	}

	renderParameters.push_back(rp);
}

Shader* RendererImplemented::GetShader(::EffekseerRenderer::RendererShaderType materialType) const
{
	if (materialType == ::EffekseerRenderer::RendererShaderType::BackDistortion)
	{
		return backDistortedShader_.get();
	}
	else if (materialType == ::EffekseerRenderer::RendererShaderType::Lit)
	{
		return litShader_.get();
	}
	else if (materialType == ::EffekseerRenderer::RendererShaderType::Unlit)
	{
		return unlitShader_.get();
	}

	// retuan as a default shader
	return unlitShader_.get();
}

void RendererImplemented::BeginShader(Shader* shader) { m_currentShader = shader; }

void RendererImplemented::RendererImplemented::EndShader(Shader* shader) {}

void RendererImplemented::SetVertexBufferToShader(const void* data, int32_t size, int32_t dstOffset)
{
	assert(m_currentShader != nullptr);
	auto p = static_cast<uint8_t*>(m_currentShader->GetVertexConstantBuffer()) + dstOffset;
	memcpy(p, data, size);
}

void RendererImplemented::SetPixelBufferToShader(const void* data, int32_t size, int32_t dstOffset)
{
	assert(m_currentShader != nullptr);
	auto p = static_cast<uint8_t*>(m_currentShader->GetPixelConstantBuffer()) + dstOffset;
	memcpy(p, data, size);
}

void RendererImplemented::SetTextures(Shader* shader, Effekseer::Backend::TextureRef* textures, int32_t count)
{
	textures_.resize(count);
	if (count > 0)
	{
		for (int i = 0; i < count; i++)
		{
			if (textures[i] != nullptr)
			{
				textures_[i] = textures[i].DownCast<Texture>()->UserData;
			}
			else
			{
				textures_[i] = nullptr;
			}
		}
	}
}

} // namespace EffekseerRendererUnity
