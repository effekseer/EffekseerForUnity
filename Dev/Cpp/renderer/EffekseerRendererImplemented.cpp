#include "EffekseerRendererImplemented.h"
#include "EffekseerRendererRenderState.h"
#include "EffekseerRendererVertexBuffer.h"
#include "EffekseerRendererIndexBuffer.h"
#include "EffekseerRendererShader.h"

namespace EffekseerPlugin
{
	extern EffekseerRenderer::Renderer*		g_EffekseerRenderer;
}

extern "C"
{
	UNITY_INTERFACE_EXPORT UnityRenderParameter* UNITY_INTERFACE_API GetUnityRenderParameter()
	{
		if (EffekseerPlugin::g_EffekseerRenderer == nullptr) return nullptr;
		auto renderer = (EffekseerRendererUnity::RendererImplemented*)EffekseerPlugin::g_EffekseerRenderer;
		return renderer->GetRenderParameters().data();
	}

	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API GetUnityRenderCount()
	{
		if (EffekseerPlugin::g_EffekseerRenderer == nullptr) return 0;
		auto renderer = (EffekseerRendererUnity::RendererImplemented*)EffekseerPlugin::g_EffekseerRenderer;
		return renderer->GetRenderParameters().size();
	}

	UNITY_INTERFACE_EXPORT void* UNITY_INTERFACE_API GetUnityVertexBuffer()
	{
		if (EffekseerPlugin::g_EffekseerRenderer == nullptr) return nullptr;
		auto renderer = (EffekseerRendererUnity::RendererImplemented*)EffekseerPlugin::g_EffekseerRenderer;
		return renderer->GetRenderVertexBuffer().data();
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API SetMaterial(void* material)
	{
		if (EffekseerPlugin::g_EffekseerRenderer == nullptr) return;
		auto renderer = (EffekseerRendererUnity::RendererImplemented*)EffekseerPlugin::g_EffekseerRenderer;
		renderer->SetMaterial(material);
	}
}

namespace EffekseerRendererUnity
{
	ModelRenderer::ModelRenderer(RendererImplemented* renderer)
		: m_renderer(renderer)
	{

	}


	ModelRenderer::~ModelRenderer()
	{
	
	}

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
		if (m_matrixes.size() == 0) return;
		if (parameter.ModelIndex < 0) return;

		//EffekseerInternalModel* model = (EffekseerInternalModel*)parameter.EffectPointer->GetModel(parameter.ModelIndex);
		Effekseer::Model* model = nullptr;
		if (model == nullptr) return;

		::EffekseerRenderer::RenderStateBase::State& state = m_renderer->GetRenderState()->Push();
		state.DepthTest = parameter.ZTest;
		state.DepthWrite = parameter.ZWrite;
		state.AlphaBlend = parameter.AlphaBlend;
		state.CullingType = parameter.Culling;
		
		m_renderer->GetRenderState()->Update(false);
		m_renderer->SetIsLighting(parameter.Lighting);
		m_renderer->SetIsDistorting(parameter.Distortion);
		m_renderer->SetDistortionIntensity(parameter.DistortionIntensity);

		Effekseer::TextureData* textures[1];

		if (parameter.ColorTextureIndex >= 0)
		{
			textures[0] = parameter.EffectPointer->GetColorImage(parameter.ColorTextureIndex);
		}
		else
		{
			textures[0] = nullptr;
		}

		m_renderer->SetTextures(nullptr, textures, 1);

		m_renderer->DrawModel(model, m_matrixes, m_uv, m_colors, m_times);

		m_renderer->GetRenderState()->Pop();
	}

	RendererImplemented* RendererImplemented::Create()
	{
		return new RendererImplemented();
	}

	RendererImplemented::RendererImplemented()
	{
		m_textures.fill(nullptr);
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
			this,
			m_stanShader,
			m_stanShader,
			m_distortionShader,
			m_distortionShader);


		return true;
	}

	void RendererImplemented::Destroy()
	{
		Release();
	}

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

		//GLCheckError();

		vertexBuffer.resize(0);
		renderParameters.resize(0);
		modelParameters.resize(0);
		return true;
	}

	bool RendererImplemented::EndRendering()
	{
//		GLCheckError();

		// レンダラーリセット
		m_standardRenderer->ResetAndRenderingIfRequired();

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

	const ::Effekseer::Vector3D& RendererImplemented::GetLightDirection() const
	{
		return m_lightDirection;
	}

	void RendererImplemented::SetLightDirection(const ::Effekseer::Vector3D& direction)
	{
		m_lightDirection = direction;
	}

	const ::Effekseer::Color& RendererImplemented::GetLightColor() const
	{
		return m_lightColor;
	}

	void RendererImplemented::SetLightColor(const ::Effekseer::Color& color)
	{
		m_lightColor = color;
	}

	const ::Effekseer::Color& RendererImplemented::GetLightAmbientColor() const
	{
		return m_lightAmbient;
	}

	void RendererImplemented::SetLightAmbientColor(const ::Effekseer::Color& color)
	{
		m_lightAmbient = color;
	}

	int32_t RendererImplemented::GetSquareMaxCount() const
	{
		return m_squareMaxCount;
	}

	const ::Effekseer::Matrix44& RendererImplemented::GetProjectionMatrix() const
	{
		return m_proj;
	}

	void RendererImplemented::SetProjectionMatrix(const ::Effekseer::Matrix44& mat)
	{
		m_proj = mat;
	}

	const ::Effekseer::Matrix44& RendererImplemented::GetCameraMatrix() const
	{
		return m_camera;
	}

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

	::Effekseer::Matrix44& RendererImplemented::GetCameraProjectionMatrix()
	{
		return m_cameraProj;
	}

	::Effekseer::Vector3D RendererImplemented::GetCameraFrontDirection() const
	{
		return m_cameraFrontDirection;
	}

	::Effekseer::Vector3D RendererImplemented::GetCameraPosition() const
	{
		return m_cameraPosition;
	}

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

	::Effekseer::ModelRenderer* RendererImplemented::CreateModelRenderer()
	{
		return ModelRenderer::Create(this);
	}

	::Effekseer::TrackRenderer* RendererImplemented::CreateTrackRenderer()
	{
		return new ::EffekseerRenderer::TrackRendererBase<RendererImplemented, Vertex, VertexDistortion>(this);
	}

	::Effekseer::TextureLoader* RendererImplemented::CreateTextureLoader(::Effekseer::FileInterface* fileInterface)
	{
		return nullptr;
	}

	::Effekseer::ModelLoader* RendererImplemented::CreateModelLoader(::Effekseer::FileInterface* fileInterface)
	{
		return nullptr;
	}

	void RendererImplemented::ResetRenderState()
	{
	}

	::EffekseerRenderer::DistortingCallback* RendererImplemented::GetDistortingCallback()
	{
		return nullptr;
	}

	void RendererImplemented::SetDistortingCallback(::EffekseerRenderer::DistortingCallback* callback)
	{

	}

	Effekseer::TextureData* RendererImplemented::GetBackground()
	{
		return (Effekseer::TextureData*)1;
	}

	VertexBuffer* RendererImplemented::GetVertexBuffer()
	{
		return m_vertexBuffer;
	}

	IndexBuffer* RendererImplemented::GetIndexBuffer()
	{
		return nullptr;
	}

	EffekseerRenderer::StandardRenderer<RendererImplemented, Shader, Vertex, VertexDistortion>* RendererImplemented::GetStandardRenderer()
	{
		return m_standardRenderer;
	}

	::EffekseerRenderer::RenderStateBase* RendererImplemented::GetRenderState()
	{
		return m_renderState;
	}

	void RendererImplemented::SetVertexBuffer(VertexBuffer* vertexBuffer, int32_t size)
	{
	}

	void RendererImplemented::SetIndexBuffer(IndexBuffer* indexBuffer)
	{
	}

	void RendererImplemented::SetLayout(Shader* shader)
	{
	}

	void RendererImplemented::DrawSprites(int32_t spriteCount, int32_t vertexOffset)
	{
		SetIsLighting(false);

		auto mat = FindMaterial();
		if (mat == nullptr) return;

		// is single ring?
		auto stanMat = ((Effekseer::Matrix44*)m_stanShader->GetVertexConstantBuffer())[0];
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

		//auto triangles = vertexOffset / 4 * 2;
		//glDrawElements(GL_TRIANGLES, spriteCount * 6, GL_UNSIGNED_SHORT, (void*)(triangles * 3 * sizeof(GLushort)));

		int32_t startOffset = vertexBuffer.size();

		if (m_isDistorting)
		{
			auto intensity = ((float*)m_distortionShader->GetPixelConstantBuffer())[0];
			SetDistortionIntensity(intensity);

			VertexDistortion* vs = (VertexDistortion*)m_vertexBuffer->GetResource();

			for (int32_t vi = vertexOffset; vi < vertexOffset + spriteCount * 4; vi++)
			{
				auto& v = vs[vi];

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

				auto targetOffset = vertexBuffer.size();
				vertexBuffer.resize(vertexBuffer.size() + sizeof(VertexDistortion));
				memcpy(vertexBuffer.data() + targetOffset, &v, sizeof(VertexDistortion));
			}

			UnityRenderParameter rp;
			rp.RenderMode = 0;
			rp.IsDistortingMode = 1;
			rp.VertexBufferOffset = startOffset;
			rp.TexturePtrs[0] = m_textures[0];
			rp.ModelPtr = nullptr;
			rp.MaterialPtr = mat;
			rp.ElementCount = spriteCount;
			renderParameters.push_back(rp);
		}
		else
		{
			Vertex* vs = (Vertex*)m_vertexBuffer->GetResource();

			for (int32_t vi = vertexOffset; vi < vertexOffset + spriteCount * 4; vi++)
			{
				auto& v = vs[vi];

				if (isSingleRing)
				{
					Effekseer::Matrix44 trans;
					trans.Translation(v.Pos.X, v.Pos.Y, v.Pos.Z);
					Effekseer::Matrix44::Mul(trans, trans, ringMat);
					v.Pos.X = trans.Values[3][0];
					v.Pos.Y = trans.Values[3][1];
					v.Pos.Z = trans.Values[3][2];
				}

				auto targetOffset = vertexBuffer.size();
				vertexBuffer.resize(vertexBuffer.size() + sizeof(Vertex));
				memcpy(vertexBuffer.data() + targetOffset, &v, sizeof(Vertex));
			}

			UnityRenderParameter rp;
			rp.RenderMode = 0;
			rp.IsDistortingMode = 0;
			rp.VertexBufferOffset = startOffset;
			rp.TexturePtrs[0] = m_textures[0];
			rp.ModelPtr = nullptr;
			rp.MaterialPtr = mat;
			rp.ElementCount = spriteCount;
			renderParameters.push_back(rp);
		}
	}

	void RendererImplemented::DrawModel(void* model, std::vector<Effekseer::Matrix44>& matrixes, std::vector<Effekseer::RectF>& uvs, std::vector<Effekseer::Color>& colors, std::vector<int32_t>& times)
	{
		
	}

	Shader* RendererImplemented::GetShader(bool useTexture, bool useDistortion) const
	{
		if (useDistortion) return m_distortionShader;
		return m_stanShader;
	}

	void RendererImplemented::BeginShader(Shader* shader)
	{
		m_currentShader = shader;
		m_isDistorting = m_currentShader != m_stanShader;
	}

	void RendererImplemented::RendererImplemented::EndShader(Shader* shader)
	{

	}

	void RendererImplemented::SetVertexBufferToShader(const void* data, int32_t size)
	{
		assert(m_currentShader != nullptr);
		memcpy(m_currentShader->GetVertexConstantBuffer(), data, size);
	}

	void RendererImplemented::SetPixelBufferToShader(const void* data, int32_t size)
	{
		assert(m_currentShader != nullptr);
		memcpy(m_currentShader->GetPixelConstantBuffer(), data, size);
	}

	void RendererImplemented::SetTextures(Shader* shader, Effekseer::TextureData** textures, int32_t count)
	{
		if (count > 0)
		{
			m_textures[0] = textures[0]->UserPtr;
		}
		else
		{
			m_textures[0] = nullptr;
		}
	}

	void* RendererImplemented::FindMaterial()
	{
		return materials[0];
	}

	void RendererImplemented::SetMaterial(void* material)
	{
		materials[0] = material;
	}
}