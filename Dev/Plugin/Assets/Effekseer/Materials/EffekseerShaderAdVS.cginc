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
	// xy uv z - FlipbookRate, w - AlphaThreshold
	linear centroid float4 UV_Others : TEXCOORD0;
	float4 ProjBinormal : TEXCOORD1;
	float4 ProjTangent : TEXCOORD2;
	float4 PosP : TEXCOORD3;
	linear centroid float4 Color : COLOR0;

	float4 Alpha_Dist_UV : TEXCOORD4;
	float4 Blend_Alpha_Dist_UV : TEXCOORD5;

	// BlendUV, FlipbookNextIndexUV
	float4 Blend_FBNextIndex_UV : TEXCOORD6;

	UNITY_VERTEX_OUTPUT_STEREO
};

#else

struct VS_Output
{
	float4 PosVS : SV_POSITION;
	linear centroid float4 Color : COLOR;
	// xy uv z - FlipbookRate, w - AlphaThreshold
	linear centroid float4 UV_Others : TEXCOORD0;
	float3 WorldN : TEXCOORD1;
#ifdef ENABLE_LIGHTING
	float3 WorldB : TEXCOORD2;
	float3 WorldT : TEXCOORD3;
#endif

	float4 Alpha_Dist_UV : TEXCOORD4;
	float4 Blend_Alpha_Dist_UV : TEXCOORD5;

	// BlendUV, FlipbookNextIndexUV
	float4 Blend_FBNextIndex_UV : TEXCOORD6;

#ifndef DISABLED_SOFT_PARTICLE
	float4 PosP : TEXCOORD7;
#endif

	UNITY_VERTEX_OUTPUT_STEREO
};

#endif



#ifdef __OPENGL2__
float IntMod(float x, float y)
{
	return floor(fmod(x, y));
}
#endif

float2 GetFlipbookOneSizeUV(float DivideX, float DivideY)
{
	return (float2(1.0, 1.0) / float2(DivideX, DivideY));
}

float2 GetFlipbookOriginUV(float2 FlipbookUV, float FlipbookIndex, float DivideX, float2 flipbookOneSize, float2 flipbookOffset)
{
	float2 DivideIndex;

#ifdef __OPENGL2__
	DivideIndex.x = IntMod(FlipbookIndex, DivideX);
#else
	DivideIndex.x = int(FlipbookIndex) % int(DivideX);
#endif
	DivideIndex.y = int(FlipbookIndex) / int(DivideX);

	float2 UVOffset = DivideIndex * flipbookOneSize + flipbookOffset;
	return FlipbookUV - UVOffset;
}

float2 GetFlipbookUVForIndex(float2 OriginUV, float Index, float DivideX, float2 flipbookOneSize, float2 flipbookOffset)
{
	float2 DivideIndex;
#ifdef __OPENGL2__
	DivideIndex.x = IntMod(Index, DivideX);
#else
	DivideIndex.x = int(Index) % int(DivideX);
#endif
	DivideIndex.y = int(Index) / int(DivideX);

	return OriginUV + DivideIndex * flipbookOneSize + flipbookOffset;
}

void ApplyFlipbookVS(inout float flipbookRate, inout float2 flipbookUV, float4 flipbookParameter1, float4 flipbookParameter2, float flipbookIndex, float2 uv, float2 uvInversed)
{
	const float flipbookEnabled = flipbookParameter1.x;
	const float flipbookLoopType = flipbookParameter1.y;
	const float divideX = flipbookParameter1.z;
	const float divideY = flipbookParameter1.w;

	const float2 flipbookOneSize = flipbookParameter2.xy;
	const float2 flipbookOffset = flipbookParameter2.zw;

	if (flipbookEnabled > 0)
	{
		flipbookRate = frac(flipbookIndex);

		float Index = floor(flipbookIndex);
		float IndexOffset = 1.0;

		float NextIndex = Index + IndexOffset;

		float FlipbookMaxCount = (divideX * divideY);

		// loop none
		if (flipbookLoopType == 0)
		{
			if (NextIndex >= FlipbookMaxCount)
			{
				NextIndex = FlipbookMaxCount - 1;
				Index = FlipbookMaxCount - 1;
			}
		}
		// loop
		else if (flipbookLoopType == 1)
		{
			Index %= FlipbookMaxCount;
			NextIndex %= FlipbookMaxCount;
		}
		// loop reverse
		else if (flipbookLoopType == 2)
		{
			bool Reverse = floor(Index / FlipbookMaxCount) % 2 == 1;
			Index %= FlipbookMaxCount;
			if (Reverse)
			{
				Index = FlipbookMaxCount - 1 - floor(Index);
			}

			Reverse = floor(NextIndex / FlipbookMaxCount) % 2 == 1;
			NextIndex %= FlipbookMaxCount;
			if (Reverse)
			{
				NextIndex = FlipbookMaxCount - 1 - floor(NextIndex);
			}
		}

		float2 notInversedUV = uv;
		notInversedUV.y = uvInversed.x + uvInversed.y * notInversedUV.y;
		float2 OriginUV = GetFlipbookOriginUV(notInversedUV, Index, divideX, flipbookOneSize, flipbookOffset);
		flipbookUV = GetFlipbookUVForIndex(OriginUV, NextIndex, divideX, flipbookOneSize, flipbookOffset);
		flipbookUV.y = uvInversed.x + uvInversed.y * flipbookUV.y;
	}
}


#if _MODEL_

// Model


//cbuffer VS_ConstantBuffer : register(b0)
//{
// float4x4 mCameraProj;

// #ifdef DISABLE_INSTANCE
// float4x4 mModel;
// float4 fUV;
// float4 fAlphaUV;
// float4 fUVDistortionUV;
// float4 fBlendUV;
// float4 fBlendAlphaUV;
// float4 fBlendUVDistortionUV;
// #else
// float4x4 mModel_Inst[__INST__];
// float4 fUV[__INST__];
// float4 fAlphaUV[__INST__];
// float4 fUVDistortionUV[__INST__];
// float4 fBlendUV[__INST__];
// float4 fBlendAlphaUV[__INST__];
// float4 fBlendUVDistortionUV[__INST__];
// #endif

float4 flipbookParameter1; // x:enable, y:loopType, z:divideX, w:divideY
float4 flipbookParameter2;

// #ifdef DISABLE_INSTANCE
// float4 fFlipbookIndexAndNextRate;
// 
// float4 fModelAlphaThreshold;
// 
// float4 fModelColor;
// #else
// float4 fFlipbookIndexAndNextRate[__INST__];
// 
// float4 fModelAlphaThreshold[__INST__];
// 
// float4 fModelColor[__INST__];
// #endif

// float4 fLightDirection;
// float4 fLightColor;
// float4 fLightAmbient;

float4 mUVInversed;
//};

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

void CalculateAndStoreAdvancedParameter(in float2 uv, in float2 uv1, in float4 alphaUV, in float4 uvDistortionUV, in float4 blendUV, in float4 blendAlphaUV, in float4 blendUVDistortionUV, in float flipbookIndexAndNextRate, in float modelAlphaThreshold, inout VS_Output vsoutput)
{
	// alpha texture
	vsoutput.Alpha_Dist_UV.x = uv.x * alphaUV.z + alphaUV.x;
	vsoutput.Alpha_Dist_UV.y = uv.y * alphaUV.w + alphaUV.y;

	// uv distortion texture
	vsoutput.Alpha_Dist_UV.z = uv.x * uvDistortionUV.z + uvDistortionUV.x;
	vsoutput.Alpha_Dist_UV.w = uv.y * uvDistortionUV.w + uvDistortionUV.y;

	// blend texture
	vsoutput.Blend_FBNextIndex_UV.x = uv.x * blendUV.z + blendUV.x;
	vsoutput.Blend_FBNextIndex_UV.y = uv.y * blendUV.w + blendUV.y;

	// blend alpha texture
	vsoutput.Blend_Alpha_Dist_UV.x = uv.x * blendAlphaUV.z + blendAlphaUV.x;
	vsoutput.Blend_Alpha_Dist_UV.y = uv.y * blendAlphaUV.w + blendAlphaUV.y;

	// blend uv distortion texture
	vsoutput.Blend_Alpha_Dist_UV.z = uv.x * blendUVDistortionUV.z + blendUVDistortionUV.x;
	vsoutput.Blend_Alpha_Dist_UV.w = uv.y * blendUVDistortionUV.w + blendUVDistortionUV.y;

	// flipbook interpolation
	float flipbookRate = 0.0f;
	float2 flipbookNextIndexUV = 0.0f;
	ApplyFlipbookVS(flipbookRate, flipbookNextIndexUV, flipbookParameter1, flipbookParameter2, flipbookIndexAndNextRate, uv1, mUVInversed);

	vsoutput.Blend_FBNextIndex_UV.zw = flipbookNextIndexUV;
	vsoutput.UV_Others.z = flipbookRate;
	vsoutput.UV_Others.w = modelAlphaThreshold;

	// flip
	vsoutput.Alpha_Dist_UV.y = mUVInversed.x + mUVInversed.y * vsoutput.Alpha_Dist_UV.y;
	vsoutput.Alpha_Dist_UV.w = mUVInversed.x + mUVInversed.y * vsoutput.Alpha_Dist_UV.w;
	vsoutput.Blend_FBNextIndex_UV.y = mUVInversed.x + mUVInversed.y * vsoutput.Blend_FBNextIndex_UV.y;
	vsoutput.Blend_Alpha_Dist_UV.y = mUVInversed.x + mUVInversed.y * vsoutput.Blend_Alpha_Dist_UV.y;
	vsoutput.Blend_Alpha_Dist_UV.w = mUVInversed.x + mUVInversed.y * vsoutput.Blend_Alpha_Dist_UV.w;
}


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

	float4 fAlphaUV = buf_model_parameter2[inst_ind].AlphaUV;
	float4 fUVDistortionUV = buf_model_parameter2[inst_ind].DistortionUV;
	float4 fBlendUV = buf_model_parameter2[inst_ind].BlendUV;
	float4 fBlendAlphaUV = buf_model_parameter2[inst_ind].BlendAlphaUV;
	float4 fBlendUVDistortionUV = buf_model_parameter2[inst_ind].BlendDistortionUV;
	float fFlipbookIndexAndNextRate = buf_model_parameter2[inst_ind].FlipbookIndexAndNextRate;
	float fAlphaThreshold = buf_model_parameter2[inst_ind].AlphaThreshold;

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
	float4 alphaUV = fAlphaUV;
	float4 uvDistortionUV = fUVDistortionUV;
	float4 blendUV = fBlendUV;
	float4 blendAlphaUV = fBlendAlphaUV;
	float4 blendUVDistortionUV = fBlendUVDistortionUV;
	float4 modelColor = fModelColor * Input.Color;
	float flipbookIndexAndNextRate = fFlipbookIndexAndNextRate;
	float modelAlphaThreshold = fAlphaThreshold;

	float4 localPosition = { Input.Pos.x, Input.Pos.y, Input.Pos.z, 1.0 };

	float4 worldPos = mul(mModel, localPosition);

	Output.PosVS = mul(mCameraProj, worldPos);

	float2 outputUV = Input.UV;
	outputUV.x = outputUV.x * uv.z + uv.x;
	outputUV.y = outputUV.y * uv.w + uv.y;
	outputUV.y = mUVInversed.x + mUVInversed.y * outputUV.y;
	Output.UV_Others.xy = outputUV;

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
	Output.ProjTangent = mul(mCameraProj, worldPos + worldTangent);
	Output.ProjBinormal = mul(mCameraProj, worldPos + worldBinormal);
#endif

#else
	// Unlit
	float4 localNormal = { Input.Normal.x, Input.Normal.y, Input.Normal.z, 0.0 };
	localNormal = normalize(mul(mModel, localNormal));
	Output.WorldN = localNormal.xyz;

#endif
	Output.Color = modelColor;

	CalculateAndStoreAdvancedParameter(Input.UV, Output.UV_Others.xy, alphaUV, uvDistortionUV, blendUV, blendAlphaUV, blendUVDistortionUV, flipbookIndexAndNextRate, modelAlphaThreshold, Output);

#ifndef DISABLED_SOFT_PARTICLE
	Output.PosP = Output.PosVS;
#endif

	return Output;
}


#else

// Sprite



//cbuffer VS_ConstantBuffer : register(b0)
//{
//float4x4 mCamera;
//float4x4 mProj;
float4 mUVInversed;

float4 flipbookParameter1; // x:enable, y:loopType, z:divideX, w:divideY
float4 flipbookParameter2;
//};

#if defined(ENABLE_LIGHTING) || defined(ENABLE_DISTORTION)

StructuredBuffer<SpriteLitDistMatVertex> buf_vertex;
float buf_offset;

#else

StructuredBuffer<SpriteUnlitVertex> buf_vertex;
float buf_offset;

#endif

StructuredBuffer<SpriteAdvancedParameter> buf_ad;
float buf_ad_offset;

#if defined(ENABLE_LIGHTING) || defined(ENABLE_DISTORTION)

struct VS_Input_Internal
{
	float3 Pos;
	float4 Color;
	float4 Normal;
	float4 Tangent;
	float2 UV1;
	float2 UV2;

	float4 Alpha_Dist_UV;
	float2 BlendUV;
	float4 Blend_Alpha_Dist_UV;
	float FlipbookIndex;
	float AlphaThreshold;
};

#else

struct VS_Input_Internal
{
	float3 Pos;
	float4 Color;
	float2 UV;
	float4 Alpha_Dist_UV;
	float2 BlendUV;
	float4 Blend_Alpha_Dist_UV;
	float FlipbookIndex;
	float AlphaThreshold;
};

#endif

void CalculateAndStoreAdvancedParameter(in VS_Input_Internal vsinput, inout VS_Output vsoutput)
{
	// alpha uv distortion texture
	vsoutput.Alpha_Dist_UV = vsinput.Alpha_Dist_UV;
	vsoutput.Alpha_Dist_UV.y = mUVInversed.x + mUVInversed.y * vsinput.Alpha_Dist_UV.y;
	vsoutput.Alpha_Dist_UV.w = mUVInversed.x + mUVInversed.y * vsinput.Alpha_Dist_UV.w;

	// blend texture
	vsoutput.Blend_FBNextIndex_UV.xy = vsinput.BlendUV;
	vsoutput.Blend_FBNextIndex_UV.y = mUVInversed.x + mUVInversed.y * vsinput.BlendUV.y;

	// blend alpha uv distortion texture
	vsoutput.Blend_Alpha_Dist_UV = vsinput.Blend_Alpha_Dist_UV;
	vsoutput.Blend_Alpha_Dist_UV.y = mUVInversed.x + mUVInversed.y * vsinput.Blend_Alpha_Dist_UV.y;
	vsoutput.Blend_Alpha_Dist_UV.w = mUVInversed.x + mUVInversed.y * vsinput.Blend_Alpha_Dist_UV.w;

	// flipbook interpolation
	float flipbookRate = 0.0f;
	float2 flipbookNextIndexUV = 0.0f;
	ApplyFlipbookVS(flipbookRate, flipbookNextIndexUV, flipbookParameter1, flipbookParameter2, vsinput.FlipbookIndex, vsoutput.UV_Others.xy, mUVInversed);

	vsoutput.Blend_FBNextIndex_UV.zw = flipbookNextIndexUV;
	vsoutput.UV_Others.z = flipbookRate;
	vsoutput.UV_Others.w = vsinput.AlphaThreshold;
}

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

	VS_Input_Internal Input_Internal = (VS_Input_Internal)0;
	Input_Internal.Alpha_Dist_UV.xy = buf_ad[buf_ad_offset + qind * 4 + v_offset[vind]].AlphaUV;
	Input_Internal.Alpha_Dist_UV.zw = buf_ad[buf_ad_offset + qind * 4 + v_offset[vind]].UVDistortionUV;
	Input_Internal.BlendUV = buf_ad[buf_ad_offset + qind * 4 + v_offset[vind]].BlendUV;
	Input_Internal.Blend_Alpha_Dist_UV.xy = buf_ad[buf_ad_offset + qind * 4 + v_offset[vind]].BlendAlphaUV;
	Input_Internal.FlipbookIndex = buf_ad[buf_ad_offset + qind * 4 + v_offset[vind]].FlipbookIndexAndNextRate;
	Input_Internal.AlphaThreshold = buf_ad[buf_ad_offset + qind * 4 + v_offset[vind]].AlphaThreshold;

	// UV
#if defined(ENABLE_LIGHTING) || defined(ENABLE_DISTORTION)
	float2 uv1 = Input.UV1;
#else
	float2 uv1 = Input.UV;
#endif
	uv1.y = mUVInversed.x + mUVInversed.y * uv1.y;
	Output.UV_Others.xy = uv1;

	float4 worldPos = { Input.Pos.x, Input.Pos.y, Input.Pos.z, 1.0 };

	Output.PosVS = mul(mCameraProj, worldPos);

#ifdef ENABLE_LIGHTING
	// NBT
	Output.WorldN = worldNormal.xyz;
	Output.WorldB = worldBinormal.xyz;
	Output.WorldT = worldTangent.xyz;

#elif defined(ENABLE_DISTORTION)
	Output.ProjTangent = mul(mCameraProj, worldPos + worldTangent);
	Output.ProjBinormal = mul(mCameraProj, worldPos + worldBinormal);
#endif

	Output.Color = Input.Color;

	CalculateAndStoreAdvancedParameter(Input_Internal, Output);

#ifndef DISABLED_SOFT_PARTICLE
	Output.PosP = Output.PosVS;
#endif

	return Output;
}


#endif