#pragma once

#include <d3d11.h>
#include <vector>

#include "Effekseer.h"
#include "EffekseerRendererDX11.h"

namespace EffekseerPlugin
{
	class LasyModelDX11 : public EffekseerRendererDX11::Model
	{
		ID3D11Device* device_ = nullptr;
		bool isLoaded_ = false;

	public:
		LasyModelDX11(ID3D11Device* device, uint8_t* data, int32_t size) : EffekseerRendererDX11::Model(data, size)
		{
			ES_SAFE_ADDREF(device);
			device_ = device;
		}

		virtual ~LasyModelDX11()
		{
			ES_SAFE_RELEASE(device_);
		}

		bool GetIsLoaded() const { return isLoaded_; }

		// Copy from a renderer
		bool LoadActually()
		{
			auto model = this;

			{
				model->ModelCount = Effekseer::Min(Effekseer::Max(model->GetModelCount(), 1), 40);

				model->InternalModels = new Model::InternalModel[model->GetFrameCount()];

				for (int32_t f = 0; f < model->GetFrameCount(); f++)
				{
					model->InternalModels[f].VertexCount = model->GetVertexCount(f);

					{
						std::vector<Effekseer::Model::VertexWithIndex> vs;
						for (int32_t m = 0; m < model->ModelCount; m++)
						{
							for (int32_t i = 0; i < model->GetVertexCount(f); i++)
							{
								Effekseer::Model::VertexWithIndex v;
								v.Position = model->GetVertexes(f)[i].Position;
								v.Normal = model->GetVertexes(f)[i].Normal;
								v.Binormal = model->GetVertexes(f)[i].Binormal;
								v.Tangent = model->GetVertexes(f)[i].Tangent;
								v.UV = model->GetVertexes(f)[i].UV;
								v.VColor = model->GetVertexes(f)[i].VColor;
								v.Index[0] = m;

								vs.push_back(v);
							}
						}

						ID3D11Buffer* vb = NULL;

						D3D11_BUFFER_DESC hBufferDesc;
						hBufferDesc.ByteWidth = sizeof(Effekseer::Model::VertexWithIndex) * model->GetVertexCount(f) * model->ModelCount;
						hBufferDesc.Usage = D3D11_USAGE_DEFAULT;
						hBufferDesc.BindFlags = D3D11_BIND_VERTEX_BUFFER;
						hBufferDesc.CPUAccessFlags = 0;
						hBufferDesc.MiscFlags = 0;
						hBufferDesc.StructureByteStride = sizeof(float);

						D3D11_SUBRESOURCE_DATA hSubResourceData;
						hSubResourceData.pSysMem = &(vs[0]);
						hSubResourceData.SysMemPitch = 0;
						hSubResourceData.SysMemSlicePitch = 0;

						if (FAILED(device_->CreateBuffer(&hBufferDesc, &hSubResourceData, &vb)))
						{
							return false;
						}

						model->InternalModels[f].VertexBuffer = vb;
					}

					model->InternalModels[f].FaceCount = model->GetFaceCount(f);
					model->InternalModels[f].IndexCount = model->InternalModels[f].FaceCount * 3;

					{
						std::vector<Effekseer::Model::Face> fs;
						for (int32_t m = 0; m < model->ModelCount; m++)
						{
							for (int32_t i = 0; i < model->InternalModels[f].FaceCount; i++)
							{
								Effekseer::Model::Face face;
								face.Indexes[0] = model->GetFaces(f)[i].Indexes[0] + model->GetVertexCount(f) * m;
								face.Indexes[1] = model->GetFaces(f)[i].Indexes[1] + model->GetVertexCount(f) * m;
								face.Indexes[2] = model->GetFaces(f)[i].Indexes[2] + model->GetVertexCount(f) * m;
								fs.push_back(face);
							}
						}

						ID3D11Buffer* ib = NULL;
						D3D11_BUFFER_DESC hBufferDesc;
						hBufferDesc.Usage = D3D11_USAGE_DEFAULT;
						hBufferDesc.ByteWidth = sizeof(int32_t) * 3 * model->InternalModels[f].FaceCount * model->ModelCount;
						hBufferDesc.BindFlags = D3D11_BIND_INDEX_BUFFER;
						hBufferDesc.CPUAccessFlags = 0;
						hBufferDesc.MiscFlags = 0;
						hBufferDesc.StructureByteStride = sizeof(int32_t);

						D3D11_SUBRESOURCE_DATA hSubResourceData;
						hSubResourceData.pSysMem = &(fs[0]);
						hSubResourceData.SysMemPitch = 0;
						hSubResourceData.SysMemSlicePitch = 0;

						if (FAILED(device_->CreateBuffer(&hBufferDesc, &hSubResourceData, &ib)))
						{
							return false;
						}

						model->InternalModels[f].IndexBuffer = ib;
					}
				}
			}

			isLoaded_ = true;
			return true;
		}
	};
}