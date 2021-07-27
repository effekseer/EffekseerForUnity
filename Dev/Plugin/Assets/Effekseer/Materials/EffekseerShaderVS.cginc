#include <UnityInstancing.cginc>

#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) || defined(UNITY_STEREO_INSTANCING_ENABLED)
#define VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_INPUT_INSTANCE_ID
#define GET_INSTANCE_ID(input) unity_InstanceID
#else
#define VERTEX_INPUT_INSTANCE_ID uint inst : SV_InstanceID;
#define GET_INSTANCE_ID(input) input.inst
#endif

struct VS_Input
{
	uint id : SV_VertexID;
	VERTEX_INPUT_INSTANCE_ID
};

#if defined(ENABLE_DISTORTION)

struct VS_Output
{
	float4 PosVS : SV_POSITION;
	linear centroid float2 UV : TEXCOORD0;
	float4 ProjBinormal : TEXCOORD1;
	float4 ProjTangent : TEXCOORD2;
	float4 PosP : TEXCOORD3;
	linear centroid float4 Color : COLOR0;
	
	UNITY_VERTEX_OUTPUT_STEREO
};

#else

struct VS_Output
{
	float4 PosVS : SV_POSITION;
	linear centroid float4 Color : COLOR;
	linear centroid float2 UV : TEXCOORD0;

#ifdef ENABLE_LIGHTING
	float3 WorldN : TEXCOORD1;
	float3 WorldB : TEXCOORD2;
	float3 WorldT : TEXCOORD3;
#endif

	float4 PosP : TEXCOORD4;
	
	UNITY_VERTEX_OUTPUT_STEREO
};

#endif

#if _MODEL_

// Model




// cbuffer VS_ConstantBuffer : register(b0)
// {
float4x4 mCameraProj;
// #ifdef DISABLE_INSTANCE
// 	float4x4 mModel;
// 	float4 fUV;
// 	float4 fModelColor;
// #else
// 	float4x4 mModel_Inst[__INST__];
// 	float4 fUV[__INST__];
// 	float4 fModelColor[__INST__];
// #endif
// float4 fLightDirection;
// float4 fLightColor;
// float4 fLightAmbient;
float4 mUVInversed;
// }

StructuredBuffer<ModelVertex> buf_vertex;
StructuredBuffer<int> buf_index;

StructuredBuffer<ModelParameter1> buf_model_parameter;
StructuredBuffer<ModelParameter2> buf_model_parameter2;

#if defined(SHADER_API_GLES3)
// GLES3 supports only 4 buffers
#else
StructuredBuffer<int> buf_vertex_offsets;
StructuredBuffer<int> buf_index_offsets;
#endif

VS_Output vert(VS_Input i)
{
	VS_Output Output;
	UNITY_SETUP_INSTANCE_ID(i);
	UNITY_INITIALIZE_OUTPUT(VS_Output, Output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(Output);
	
	uint inst_ind = GET_INSTANCE_ID(i);

	float4x4 mModel = buf_model_parameter[inst_ind].Mat;
	float4 fUV = buf_model_parameter[inst_ind].UV;
	float4 fModelColor = buf_model_parameter[inst_ind].VColor;

#if defined(SHADER_API_GLES3)
	int buf_vertex_offset = 0;
	int buf_index_offset = 0;
#else
	int buf_vertex_offset = buf_vertex_offsets[buf_model_parameter[inst_ind].Time];
	int buf_index_offset = buf_index_offsets[buf_model_parameter[inst_ind].Time];
#endif

	ModelVertex Input = buf_vertex[buf_index[i.id + buf_index_offset] + buf_vertex_offset];

	float4x4 mCameraProj = UNITY_MATRIX_VP;
	float4 uv = fUV;

	float4 localPos = { Input.Pos.x, Input.Pos.y, Input.Pos.z, 1.0 };

	float4 worldPos = mul(mModel, localPos);
	Output.PosVS = mul(mCameraProj, worldPos);
	Output.Color = fModelColor * Input.Color;

	float2 outputUV = Input.UV;
	outputUV.x = outputUV.x * uv.z + uv.x;
	outputUV.y = outputUV.y * uv.w + uv.y;
	outputUV.y = mUVInversed.x + mUVInversed.y * outputUV.y;
	Output.UV = outputUV;

#if defined(ENABLE_LIGHTING) || defined(ENABLE_DISTORTION)

	float4 localNormal = { Input.Normal.x, Input.Normal.y, Input.Normal.z, 0.0 };
	float4 localBinormal = { Input.Binormal.x, Input.Binormal.y, Input.Binormal.z, 0.0 };
	float4 localTangent = { Input.Tangent.x, Input.Tangent.y, Input.Tangent.z, 0.0 };

	float4 worldNormal = mul(mModel, localNormal);
	float4 worldBinormal = mul(mModel, localBinormal);
	float4 worldTangent = mul(mModel, localTangent);

	worldNormal = normalize(worldNormal);
	worldBinormal = normalize(worldBinormal);
	worldTangent = normalize(worldTangent);

#if defined(ENABLE_LIGHTING)

	Output.WorldN = worldNormal.xyz;
	Output.WorldB = worldBinormal.xyz;
	Output.WorldT = worldTangent.xyz;

#elif defined(ENABLE_DISTORTION)
	Output.ProjBinormal = mul(mCameraProj, worldPos + worldBinormal);
	Output.ProjTangent = mul(mCameraProj, worldPos + worldTangent);
#endif

#endif

	Output.PosP = Output.PosVS;

	return Output;
}






#else

// Sprite


// cbuffer VS_ConstantBuffer : register(b0)
// {
// float4x4 mCamera;
// float4x4 mProj;
float4 mUVInversed;

// Unused
// float4 mflipbookParameter; // x:enable, y:loopType, z:divideX, w:divideY
// }

#if defined(ENABLE_LIGHTING) || defined(ENABLE_DISTORTION)

StructuredBuffer<SpriteLitDistMatVertex> buf_vertex;
float buf_offset;

#else

StructuredBuffer<SpriteUnlitVertex> buf_vertex;
float buf_offset;

#endif



VS_Output vert(VS_Input i)
{
	VS_Output Output;
	UNITY_SETUP_INSTANCE_ID(i);
	UNITY_INITIALIZE_OUTPUT(VS_Output, Output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(Output);
	
	int qind = (i.id) / 6;
	int vind = (i.id) % 6;

	int v_offset[6];
	v_offset[0] = 2;
	v_offset[1] = 1;
	v_offset[2] = 0;
	v_offset[3] = 1;
	v_offset[4] = 2;
	v_offset[5] = 3;

#if defined(ENABLE_LIGHTING) || defined(ENABLE_DISTORTION)
	SpriteLitDistMatVertex Input = buf_vertex[buf_offset + qind * 4 + v_offset[vind]];
#else
	SpriteUnlitVertex Input = buf_vertex[buf_offset + qind * 4 + v_offset[vind]];
#endif


	float4x4 mCameraProj = UNITY_MATRIX_VP;

#if defined(ENABLE_LIGHTING) || defined(ENABLE_DISTORTION)
	float4 worldNormal = float4(Input.Normal.xyz, 0.0);
	float4 worldTangent = float4(Input.Tangent.xyz, 0.0);
	float4 worldBinormal = float4(cross(worldNormal.xyz, worldTangent.xyz), 0.0);
#endif

	float4 worldPos = { Input.Pos.x, Input.Pos.y, Input.Pos.z, 1.0 };
	Output.PosVS = mul(mCameraProj, worldPos);
	Output.Color = Input.Color;

	// UV
#if defined(ENABLE_LIGHTING) || defined(ENABLE_DISTORTION)
	float2 uv1 = Input.UV1;
#else
	float2 uv1 = Input.UV;
#endif
	uv1.y = mUVInversed.x + mUVInversed.y * uv1.y;
	Output.UV = uv1;

#ifdef ENABLE_LIGHTING
	// NBT
	Output.WorldN = worldNormal.xyz;
	Output.WorldB = worldBinormal.xyz;
	Output.WorldT = worldTangent.xyz;

#elif defined(ENABLE_DISTORTION)
	Output.ProjTangent = mul(mCameraProj, worldPos + worldTangent);
	Output.ProjBinormal = mul(mCameraProj, worldPos + worldBinormal);
#endif

	Output.PosP = Output.PosVS;

	return Output;
}



#endif