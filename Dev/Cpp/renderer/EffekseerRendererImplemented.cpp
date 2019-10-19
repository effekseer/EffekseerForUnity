#include "EffekseerRendererImplemented.h"
#include "EffekseerRendererIndexBuffer.h"
#include "EffekseerRendererRenderState.h"
#include "EffekseerRendererShader.h"
#include "EffekseerRendererVertexBuffer.h"

namespace EffekseerPlugin
{
extern EffekseerRenderer::Renderer* g_EffekseerRenderer;
}

extern "C"
{
	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API GetUnityRenderParameter(UnityRenderParameter* dst, int index)
	{
		if (EffekseerPlugin::g_EffekseerRenderer == nullptr)
			return;
		auto renderer = (EffekseerRendererUnity::RendererImplemented*)EffekseerPlugin::g_EffekseerRenderer;
		*dst = renderer->GetRenderParameters()[index];
	}

	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API GetUnityRenderParameterCount()
	{
		if (EffekseerPlugin::g_EffekseerRenderer == nullptr)
			return 0;
		auto renderer = (EffekseerRendererUnity::RendererImplemented*)EffekseerPlugin::g_EffekseerRenderer;
		return renderer->GetRenderParameters().size();
	}

	UNITY_INTERFACE_EXPORT void* UNITY_INTERFACE_API GetUnityRenderVertexBuffer()
	{
		if (EffekseerPlugin::g_EffekseerRenderer == nullptr)
			return nullptr;
		auto renderer = (EffekseerRendererUnity::RendererImplemented*)EffekseerPlugin::g_EffekseerRenderer;
		return renderer->GetRenderVertexBuffer().data();
	}

	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API GetUnityRenderVertexBufferCount()
	{
		if (EffekseerPlugin::g_EffekseerRenderer == nullptr)
			return 0;
		auto renderer = (EffekseerRendererUnity::RendererImplemented*)EffekseerPlugin::g_EffekseerRenderer;
		return renderer->GetRenderVertexBuffer().size();
	}

	UNITY_INTERFACE_EXPORT void* UNITY_INTERFACE_API GetUnityRenderInfoBuffer()
	{
		if (EffekseerPlugin::g_EffekseerRenderer == nullptr)
			return nullptr;
		auto renderer = (EffekseerRendererUnity::RendererImplemented*)EffekseerPlugin::g_EffekseerRenderer;
		return renderer->GetRenderInfoBuffer().data();
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API SetMaterial(void* material)
	{
		if (EffekseerPlugin::g_EffekseerRenderer == nullptr)
			return;
		auto renderer = (EffekseerRendererUnity::RendererImplemented*)EffekseerPlugin::g_EffekseerRenderer;
		renderer->SetMaterial(material);
	}
}

namespace EffekseerRendererUnity
{
struct UnityVertex
{
	::Effekseer::Vector3D Pos;
	float UV[2];
	float Col[4];
};

struct UnityDistortionVertex
{
	::Effekseer::Vector3D Pos;
	float UV[2];
	float Col[4];
	::Effekseer::Vector3D Tangent;
	::Effekseer::Vector3D Binormal;
};

struct UnityModelParameter
{
	Effekseer::Matrix44 Matrix;
	Effekseer::RectF UV;
	float VColor[4];
	int32_t Time;
};

static int GetAlignedOffset(int offset, int size) { return ((offset + (size - 1)) / size) * size; }

ModelRenderer::ModelRenderer(RendererImplemented* renderer) : m_renderer(renderer) {}

ModelRenderer::~ModelRenderer() {}

ModelRenderer* ModelRenderer::Create(RendererImplemented* renderer)
{
	assert(renderer != NULL);

	return new ModelRenderer(renderer);
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

	::EffekseerRenderer::RenderStateBase::State& state = m_renderer->GetRenderState()->Push();
	state.DepthTest = parameter.ZTest;
	state.DepthWrite = parameter.ZWrite;
	state.AlphaBlend = parameter.BasicParameterPtr->AlphaBlend;
	state.CullingType = parameter.Culling;

	Effekseer::TextureData* textures[1];

	if (parameter.BasicParameterPtr->MaterialType == Effekseer::RendererMaterialType::BackDistortion)
	{
		if (parameter.BasicParameterPtr->Texture1Index >= 0)
		{
			textures[0] = parameter.EffectPointer->GetDistortionImage(parameter.BasicParameterPtr->Texture1Index);
		}
		else
		{
			textures[0] = nullptr;
		}
	}
	else
	{
		if (parameter.BasicParameterPtr->Texture1Index >= 0)
		{
			textures[0] = parameter.EffectPointer->GetColorImage(parameter.BasicParameterPtr->Texture1Index);
		}
		else
		{
			textures[0] = nullptr;
		}
	}

	m_renderer->SetTextures(nullptr, textures, 1);

	state.TextureFilterTypes[0] = parameter.BasicParameterPtr->TextureFilter1;
	state.TextureWrapTypes[0] = parameter.BasicParameterPtr->TextureWrap1;
	state.TextureFilterTypes[1] = parameter.BasicParameterPtr->TextureFilter2;
	state.TextureWrapTypes[1] = parameter.BasicParameterPtr->TextureWrap2;

	m_renderer->GetRenderState()->Update(false);
	m_renderer->SetIsLighting(parameter.BasicParameterPtr->MaterialType == Effekseer::RendererMaterialType::Lighting);
	m_renderer->SetIsDistorting(parameter.BasicParameterPtr->MaterialType == Effekseer::RendererMaterialType::BackDistortion);
	m_renderer->SetDistortionIntensity(parameter.BasicParameterPtr->DistortionIntensity);

	m_renderer->DrawModel(model, m_matrixes, m_uv, m_colors, m_times);

	m_renderer->GetRenderState()->Pop();
}

RendererImplemented* RendererImplemented::Create() { return new RendererImplemented(); }

RendererImplemented::RendererImplemented()
{
	m_textures.fill(nullptr);

	backgroundData.Width = 0;
	backgroundData.Height = 0;
	backgroundData.TextureFormat = Effekseer::TextureFormatType::ABGR8;
	backgroundData.UserID = 0;
	backgroundData.UserPtr = nullptr;
}

RendererImplemented::~RendererImplemented()
{
	ES_SAFE_DELETE(m_renderState);
	ES_SAFE_DELETE(m_stanShader);
	ES_SAFE_DELETE(m_distortionShader);
	ES_SAFE_DELETE(m_standardRenderer);
	ES_SAFE_DELETE(m_vertexBuffer);
}

bool RendererImplemented::Initialize(int32_t squareMaxCount)
{
	m_squareMaxCount = squareMaxCount;
	m_renderState = new RenderState();
	m_vertexBuffer = new VertexBuffer(sizeof(Vertex) * m_squareMaxCount * 4, true);
	m_stanShader = new Shader();
	m_distortionShader = new Shader();

	m_standardRenderer = new EffekseerRenderer::StandardRenderer<RendererImplemented, Shader, Vertex, VertexDistortion>(
		this, m_stanShader, m_distortionShader);

	return true;
}

void RendererImplemented::Destroy() { Release(); }

void RendererImplemented::SetRestorationOfStatesFlag(bool flag)
{
	// TODO
}

bool RendererImplemented::BeginRendering()
{
	::Effekseer::Matrix44::Mul(m_cameraProj, m_camera, m_proj);

	//		// ステートを保存する
	//		if (m_restorationOfStates)
	//		{
	//			m_originalState.blend = glIsEnabled(GL_BLEND);
	//			m_originalState.cullFace = glIsEnabled(GL_CULL_FACE);
	//			m_originalState.depthTest = glIsEnabled(GL_DEPTH_TEST);
	//#if !defined(__EFFEKSEER_RENDERER_GL3__) && \
//	!defined(__EFFEKSEER_RENDERER_GLES3__) && \
//	!defined(__EFFEKSEER_RENDERER_GLES2__) && \
//	!defined(EMSCRIPTEN)
	//			m_originalState.texture = glIsEnabled(GL_TEXTURE_2D);
	//#endif
	//			glGetBooleanv(GL_DEPTH_WRITEMASK, &m_originalState.depthWrite);
	//			glGetIntegerv(GL_DEPTH_FUNC, &m_originalState.depthFunc);
	//			glGetIntegerv(GL_CULL_FACE_MODE, &m_originalState.cullFaceMode);
	//			glGetIntegerv(GL_BLEND_SRC_RGB, &m_originalState.blendSrc);
	//			glGetIntegerv(GL_BLEND_DST_RGB, &m_originalState.blendDst);
	//			glGetIntegerv(GL_BLEND_EQUATION, &m_originalState.blendEquation);
	//		}
	//
	//		glDepthFunc(GL_LEQUAL);
	//		glEnable(GL_BLEND);
	//		glDisable(GL_CULL_FACE);
	//
	//		m_renderState->GetActiveState().Reset();
	//		m_renderState->Update(true);
	//		m_currentTextures.clear();

	// レンダラーリセット
	m_standardRenderer->ResetAndRenderingIfRequired();

	// GLCheckError();

	exportedVertexBuffer.resize(0);
	exportedInfoBuffer.resize(0);
	renderParameters.resize(0);
	modelParameters.resize(0);
	return true;
}

bool RendererImplemented::EndRendering()
{
	//		GLCheckError();

	// レンダラーリセット
	m_standardRenderer->ResetAndRenderingIfRequired();

	// ForUnity
	exportedVertexBuffer.resize(GetAlignedOffset(exportedVertexBuffer.size(), sizeof(UnityVertex)));

	//		// ステートを復元する
	//		if (m_restorationOfStates)
	//		{
	//			if (m_originalState.blend) glEnable(GL_BLEND); else glDisable(GL_BLEND);
	//			if (m_originalState.cullFace) glEnable(GL_CULL_FACE); else glDisable(GL_CULL_FACE);
	//			if (m_originalState.depthTest) glEnable(GL_DEPTH_TEST); else glDisable(GL_DEPTH_TEST);
	//
	//#if !defined(__EFFEKSEER_RENDERER_GL3__) && \
//	!defined(__EFFEKSEER_RENDERER_GLES3__) && \
//	!defined(__EFFEKSEER_RENDERER_GLES2__) && \
//	!defined(EMSCRIPTEN)
	//			if (m_originalState.texture) glEnable(GL_TEXTURE_2D); else glDisable(GL_TEXTURE_2D);
	//#endif
	//
	//			glDepthFunc(m_originalState.depthFunc);
	//			glDepthMask(m_originalState.depthWrite);
	//			glCullFace(m_originalState.cullFaceMode);
	//			glBlendFunc(m_originalState.blendSrc, m_originalState.blendDst);
	//			GLExt::glBlendEquation(m_originalState.blendEquation);
	//
	//#if defined(__EFFEKSEER_RENDERER_GL3__) || defined(__EFFEKSEER_RENDERER_GLES3__)
	//			for (int32_t i = 0; i < 4; i++)
	//			{
	//				GLExt::glBindSampler(i, 0);
	//			}
	//#endif
	//		}
	//
	//		GLCheckError();

	return true;
}

const ::Effekseer::Vector3D& RendererImplemented::GetLightDirection() const { return m_lightDirection; }

void RendererImplemented::SetLightDirection(const ::Effekseer::Vector3D& direction) { m_lightDirection = direction; }

const ::Effekseer::Color& RendererImplemented::GetLightColor() const { return m_lightColor; }

void RendererImplemented::SetLightColor(const ::Effekseer::Color& color) { m_lightColor = color; }

const ::Effekseer::Color& RendererImplemented::GetLightAmbientColor() const { return m_lightAmbient; }

void RendererImplemented::SetLightAmbientColor(const ::Effekseer::Color& color) { m_lightAmbient = color; }

int32_t RendererImplemented::GetSquareMaxCount() const { return m_squareMaxCount; }

const ::Effekseer::Matrix44& RendererImplemented::GetProjectionMatrix() const { return m_proj; }

void RendererImplemented::SetProjectionMatrix(const ::Effekseer::Matrix44& mat) { m_proj = mat; }

const ::Effekseer::Matrix44& RendererImplemented::GetCameraMatrix() const { return m_camera; }

void RendererImplemented::SetCameraMatrix(const ::Effekseer::Matrix44& mat)
{
	m_cameraFrontDirection = ::Effekseer::Vector3D(mat.Values[0][2], mat.Values[1][2], mat.Values[2][2]);

	auto localPos = ::Effekseer::Vector3D(-mat.Values[3][0], -mat.Values[3][1], -mat.Values[3][2]);
	auto f = m_cameraFrontDirection;
	auto r = ::Effekseer::Vector3D(mat.Values[0][0], mat.Values[1][0], mat.Values[2][0]);
	auto u = ::Effekseer::Vector3D(mat.Values[0][1], mat.Values[1][1], mat.Values[2][1]);

	m_cameraPosition = r * localPos.X + u * localPos.Y + f * localPos.Z;

	m_camera = mat;
}

::Effekseer::Matrix44& RendererImplemented::GetCameraProjectionMatrix() { return m_cameraProj; }

::Effekseer::Vector3D RendererImplemented::GetCameraFrontDirection() const { return m_cameraFrontDirection; }

::Effekseer::Vector3D RendererImplemented::GetCameraPosition() const { return m_cameraPosition; }

void RendererImplemented::SetCameraParameter(const ::Effekseer::Vector3D& front, const ::Effekseer::Vector3D& position)
{
	m_cameraFrontDirection = front;
	m_cameraPosition = position;
}

::Effekseer::SpriteRenderer* RendererImplemented::CreateSpriteRenderer()
{
	return new ::EffekseerRenderer::SpriteRendererBase<RendererImplemented, Vertex, VertexDistortion>(this);
}

::Effekseer::RibbonRenderer* RendererImplemented::CreateRibbonRenderer()
{
	return new ::EffekseerRenderer::RibbonRendererBase<RendererImplemented, Vertex, VertexDistortion>(this);
}

::Effekseer::RingRenderer* RendererImplemented::CreateRingRenderer()
{
	return new ::EffekseerRenderer::RingRendererBase<RendererImplemented, Vertex, VertexDistortion>(this);
}

::Effekseer::ModelRenderer* RendererImplemented::CreateModelRenderer() { return ModelRenderer::Create(this); }

::Effekseer::TrackRenderer* RendererImplemented::CreateTrackRenderer()
{
	return new ::EffekseerRenderer::TrackRendererBase<RendererImplemented, Vertex, VertexDistortion>(this);
}

::Effekseer::TextureLoader* RendererImplemented::CreateTextureLoader(::Effekseer::FileInterface* fileInterface) { return nullptr; }

::Effekseer::ModelLoader* RendererImplemented::CreateModelLoader(::Effekseer::FileInterface* fileInterface) { return nullptr; }

void RendererImplemented::ResetRenderState() {}

::EffekseerRenderer::DistortingCallback* RendererImplemented::GetDistortingCallback() { return nullptr; }

void RendererImplemented::SetDistortingCallback(::EffekseerRenderer::DistortingCallback* callback) {}

Effekseer::TextureData* RendererImplemented::GetBackground() { return (Effekseer::TextureData*)(&backgroundData); }

void RendererImplemented::SetBackground(void* image) { backgroundData.UserPtr = image; }

VertexBuffer* RendererImplemented::GetVertexBuffer() { return m_vertexBuffer; }

IndexBuffer* RendererImplemented::GetIndexBuffer() { return nullptr; }

EffekseerRenderer::StandardRenderer<RendererImplemented, Shader, Vertex, VertexDistortion>* RendererImplemented::GetStandardRenderer()
{
	return m_standardRenderer;
}

::EffekseerRenderer::RenderStateBase* RendererImplemented::GetRenderState() { return m_renderState; }

void RendererImplemented::SetVertexBuffer(VertexBuffer* vertexBuffer, int32_t size) {}

void RendererImplemented::SetIndexBuffer(IndexBuffer* indexBuffer) {}

void RendererImplemented::SetLayout(Shader* shader) {}

void RendererImplemented::DrawSprites(int32_t spriteCount, int32_t vertexOffset)
{
	SetIsLighting(false);

	// auto mat = FindMaterial();
	// if (mat == nullptr) return;

	// is single ring?
	Effekseer::Matrix44 stanMat;

	if (m_isDistorting)
	{
		stanMat = ((Effekseer::Matrix44*)m_distortionShader->GetVertexConstantBuffer())[0];
	}
	else
	{
		stanMat = ((Effekseer::Matrix44*)m_stanShader->GetVertexConstantBuffer())[0];
	}

	auto cameraMat = m_camera;
	Effekseer::Matrix44 ringMat;

	bool isSingleRing = false;

	for (int32_t r = 0; r < 4; r++)
	{
		for (int32_t c = 0; c < 4; c++)
		{
			if (stanMat.Values[r][c] != cameraMat.Values[r][c])
			{
				isSingleRing = true;
				goto Exit;
			}
		}
	}
Exit:;

	if (isSingleRing)
	{
		Effekseer::Matrix44 inv;
		Effekseer::Matrix44::Mul(ringMat, stanMat, Effekseer::Matrix44::Inverse(inv, cameraMat));
	}

	// auto triangles = vertexOffset / 4 * 2;
	// glDrawElements(GL_TRIANGLES, spriteCount * 6, GL_UNSIGNED_SHORT, (void*)(triangles * 3 * sizeof(GLushort)));

	if (m_isDistorting)
	{
		if (m_textures[1] == nullptr)
		{
			return;
		}

		auto intensity = ((float*)m_distortionShader->GetPixelConstantBuffer())[0];
		SetDistortionIntensity(intensity);

		VertexDistortion* vs = (VertexDistortion*)m_vertexBuffer->GetResource();

		exportedVertexBuffer.resize(GetAlignedOffset(exportedVertexBuffer.size(), sizeof(UnityDistortionVertex)));
		int32_t startOffset = exportedVertexBuffer.size();

		for (int32_t vi = vertexOffset; vi < vertexOffset + spriteCount * 4; vi++)
		{
			auto& v = vs[vi];
			UnityDistortionVertex unity_v;

			if (isSingleRing)
			{
				Effekseer::Matrix44 trans;
				trans.Translation(v.Pos.X, v.Pos.Y, v.Pos.Z);
				Effekseer::Matrix44::Mul(trans, trans, ringMat);
				v.Pos.X = trans.Values[3][0];
				v.Pos.Y = trans.Values[3][1];
				v.Pos.Z = trans.Values[3][2];
			}

			Effekseer::Vector3D normal;
			Effekseer::Vector3D::Cross(normal, v.Binormal, v.Tangent);

			unity_v.Pos = v.Pos;
			unity_v.UV[0] = v.UV[0];
			unity_v.UV[1] = v.UV[1];
			unity_v.Col[0] = v.Col[0] / 255.0f;
			unity_v.Col[1] = v.Col[1] / 255.0f;
			unity_v.Col[2] = v.Col[2] / 255.0f;
			unity_v.Col[3] = v.Col[3] / 255.0f;
			unity_v.Tangent = v.Tangent;
			unity_v.Binormal = v.Binormal;

			auto targetOffset = exportedVertexBuffer.size();
			exportedVertexBuffer.resize(exportedVertexBuffer.size() + sizeof(UnityDistortionVertex));
			memcpy(exportedVertexBuffer.data() + targetOffset, &unity_v, sizeof(UnityDistortionVertex));
		}

		UnityRenderParameter rp;

		rp.ZTest = GetRenderState()->GetActiveState().DepthTest ? 1 : 0;
		rp.ZWrite = GetRenderState()->GetActiveState().DepthWrite ? 1 : 0;
		rp.Blend = (int)GetRenderState()->GetActiveState().AlphaBlend;
		rp.Culling = (int)GetRenderState()->GetActiveState().CullingType;
		rp.DistortionIntensity = m_distortionIntensity;

		rp.RenderMode = 0;
		rp.IsDistortingMode = 1;
		rp.VertexBufferOffset = startOffset;
		rp.TexturePtrs[0] = m_textures[0];
		rp.TexturePtrs[1] = m_textures[1];
		rp.TextureFilterTypes[0] = (int)GetRenderState()->GetActiveState().TextureFilterTypes[0];
		rp.TextureWrapTypes[0] = (int)GetRenderState()->GetActiveState().TextureWrapTypes[0];
		rp.ModelPtr = nullptr;
		rp.MaterialPtr = nullptr;
		rp.ElementCount = spriteCount;
		renderParameters.push_back(rp);
	}
	else
	{
		Vertex* vs = (Vertex*)m_vertexBuffer->GetResource();

		exportedVertexBuffer.resize(GetAlignedOffset(exportedVertexBuffer.size(), sizeof(UnityVertex)));
		int32_t startOffset = exportedVertexBuffer.size();

		for (int32_t vi = vertexOffset; vi < vertexOffset + spriteCount * 4; vi++)
		{
			auto& v = vs[vi];
			UnityVertex unity_v;

			if (isSingleRing)
			{
				Effekseer::Matrix44 trans;
				trans.Translation(v.Pos.X, v.Pos.Y, v.Pos.Z);
				Effekseer::Matrix44::Mul(trans, trans, ringMat);
				v.Pos.X = trans.Values[3][0];
				v.Pos.Y = trans.Values[3][1];
				v.Pos.Z = trans.Values[3][2];
			}

			unity_v.Pos = v.Pos;
			unity_v.UV[0] = v.UV[0];
			unity_v.UV[1] = v.UV[1];
			unity_v.Col[0] = v.Col[0] / 255.0f;
			unity_v.Col[1] = v.Col[1] / 255.0f;
			unity_v.Col[2] = v.Col[2] / 255.0f;
			unity_v.Col[3] = v.Col[3] / 255.0f;

			auto targetOffset = exportedVertexBuffer.size();
			exportedVertexBuffer.resize(exportedVertexBuffer.size() + sizeof(UnityVertex));
			memcpy(exportedVertexBuffer.data() + targetOffset, &unity_v, sizeof(UnityVertex));
		}

		UnityRenderParameter rp;

		rp.ZTest = GetRenderState()->GetActiveState().DepthTest ? 1 : 0;
		rp.ZWrite = GetRenderState()->GetActiveState().DepthWrite ? 1 : 0;
		rp.Blend = (int)GetRenderState()->GetActiveState().AlphaBlend;
		rp.Culling = (int)GetRenderState()->GetActiveState().CullingType;

		rp.RenderMode = 0;
		rp.IsDistortingMode = 0;
		rp.VertexBufferOffset = startOffset;
		rp.TexturePtrs[0] = m_textures[0];
		rp.TextureFilterTypes[0] = (int)GetRenderState()->GetActiveState().TextureFilterTypes[0];
		rp.TextureWrapTypes[0] = (int)GetRenderState()->GetActiveState().TextureWrapTypes[0];
		rp.ModelPtr = nullptr;
		rp.MaterialPtr = nullptr;
		rp.ElementCount = spriteCount;
		renderParameters.push_back(rp);
	}
}

void RendererImplemented::DrawModel(void* model,
									std::vector<Effekseer::Matrix44>& matrixes,
									std::vector<Effekseer::RectF>& uvs,
									std::vector<Effekseer::Color>& colors,
									std::vector<int32_t>& times)
{
	UnityRenderParameter rp;
	rp.RenderMode = 1;
	rp.IsDistortingMode = m_isDistorting ? 1 : 0;
	auto model_ = (Model*)model;

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

	rp.TexturePtrs[0] = m_textures[0];
	rp.TextureFilterTypes[0] = (int)GetRenderState()->GetActiveState().TextureFilterTypes[0];
	rp.TextureWrapTypes[0] = (int)GetRenderState()->GetActiveState().TextureWrapTypes[0];

	if (rp.IsDistortingMode)
	{
		rp.TexturePtrs[1] = m_textures[1];
		
		auto intensity = ((float*)m_distortionShader->GetPixelConstantBuffer())[0];
		SetDistortionIntensity(intensity);
	}

	rp.ElementCount = matrixes.size();
	rp.VertexBufferOffset = exportedInfoBuffer.size();

	rp.ZTest = GetRenderState()->GetActiveState().DepthTest ? 1 : 0;
	rp.ZWrite = GetRenderState()->GetActiveState().DepthWrite ? 1 : 0;
	rp.Blend = (int)GetRenderState()->GetActiveState().AlphaBlend;
	rp.Culling = (int)GetRenderState()->GetActiveState().CullingType;
	rp.DistortionIntensity = m_distortionIntensity;

	for (int i = 0; i < matrixes.size(); i++)
	{
		int offset = exportedInfoBuffer.size();

		exportedInfoBuffer.resize(exportedInfoBuffer.size() + sizeof(UnityModelParameter));

		UnityModelParameter modelParameter;
		modelParameter.Matrix = matrixes[i];
		modelParameter.UV = uvs[i];
		modelParameter.VColor[0] = colors[i].R / 255.0f;
		modelParameter.VColor[1] = colors[i].G / 255.0f;
		modelParameter.VColor[2] = colors[i].B / 255.0f;
		modelParameter.VColor[3] = colors[i].A / 255.0f;
		modelParameter.Time = times[i] % model_->GetFrameCount();

		*(UnityModelParameter*)(exportedInfoBuffer.data() + offset) = modelParameter;
	}

	renderParameters.push_back(rp);
}

Shader* RendererImplemented::GetShader(bool useTexture, ::Effekseer::RendererMaterialType type) const
{
	if (type == ::Effekseer::RendererMaterialType::BackDistortion)
		return m_distortionShader;
	return m_stanShader;
}

void RendererImplemented::BeginShader(Shader* shader)
{
	m_currentShader = shader;
	m_isDistorting = m_currentShader != m_stanShader;
}

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

void RendererImplemented::SetTextures(Shader* shader, Effekseer::TextureData** textures, int32_t count)
{
	if (count > 0)
	{
		for (int i = 0; i < count; i++)
		{
			if (textures[i] != nullptr)
			{
				m_textures[i] = textures[i]->UserPtr;
			}
			else
			{
				m_textures[i] = nullptr;
			}
		}
	}
	else
	{
		m_textures[0] = nullptr;
		m_textures[1] = nullptr;
		m_textures[2] = nullptr;
		m_textures[3] = nullptr;
	}
}

void* RendererImplemented::FindMaterial() { return nullptr; }

void RendererImplemented::SetMaterial(void* material) {}
} // namespace EffekseerRendererUnity