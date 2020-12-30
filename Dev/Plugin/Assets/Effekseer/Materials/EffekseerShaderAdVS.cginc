
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

float2 GetFlipbookOriginUV(float2 FlipbookUV, float FlipbookIndex, float DivideX, float DivideY)
{
	float2 DivideIndex;

#ifdef __OPENGL2__
	DivideIndex.x = IntMod(FlipbookIndex, DivideX);
#else
	DivideIndex.x = int(FlipbookIndex) % int(DivideX);
#endif
	DivideIndex.y = int(FlipbookIndex) / int(DivideX);

	float2 FlipbookOneSize = GetFlipbookOneSizeUV(DivideX, DivideY);
	float2 UVOffset = DivideIndex * FlipbookOneSize;

	float2 OriginUV = FlipbookUV - UVOffset;
	OriginUV *= float2(DivideX, DivideY);

	return OriginUV;
}

float2 GetFlipbookUVForIndex(float2 OriginUV, float Index, float DivideX, float DivideY)
{
	float2 DivideIndex;
#ifdef __OPENGL2__
	DivideIndex.x = IntMod(Index, DivideX);
#else
	DivideIndex.x = int(Index) % int(DivideX);
#endif
	DivideIndex.y = int(Index) / int(DivideX);

	float2 FlipbookOneSize = GetFlipbookOneSizeUV(DivideX, DivideY);

	return (OriginUV * FlipbookOneSize) + (DivideIndex * FlipbookOneSize);
}

void ApplyFlipbookVS(inout float flipbookRate, inout float2 flipbookUV, float4 flipbookParameter, float flipbookIndex, float2 uv)
{
	if (flipbookParameter.x > 0)
	{
		flipbookRate = frac(flipbookIndex);

		float Index = floor(flipbookIndex);
		float IndexOffset = 1.0;

		float NextIndex = Index + IndexOffset;

		float FlipbookMaxCount = (flipbookParameter.z * flipbookParameter.w);

		// loop none
		if (flipbookParameter.y == 0)
		{
			if (NextIndex >= FlipbookMaxCount)
			{
				NextIndex = FlipbookMaxCount - 1;
				Index = FlipbookMaxCount - 1;
			}
		}
		// loop
		else if (flipbookParameter.y == 1)
		{
			Index %= FlipbookMaxCount;
			NextIndex %= FlipbookMaxCount;
		}
		// loop reverse
		else if (flipbookParameter.y == 2)
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

		float2 OriginUV = GetFlipbookOriginUV(uv, Index, flipbookParameter.z, flipbookParameter.w);
		flipbookUV = GetFlipbookUVForIndex(OriginUV, NextIndex, flipbookParameter.z, flipbookParameter.w);
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

float4 fFlipbookParameter; // x:enable, y:loopType, z:divideX, w:divideY

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

struct VS_Input
{
	float3 Pos : POSITION0;
	float3 Normal : NORMAL0;
	float3 Binormal : NORMAL1;
	float3 Tangent : NORMAL2;
	float2 UV : TEXCOORD0;
	float4 Color : NORMAL3;

#if defined(ENABLE_DIVISOR)
	float Index : BLENDINDICES0;
#elif !defined(DISABLE_INSTANCE)
	uint Index : SV_InstanceID;
#endif
};

StructuredBuffer<ModelVertex> buf_vertex;
StructuredBuffer<int> buf_index;

StructuredBuffer<ModelParameter1> buf_model_parameter;
StructuredBuffer<ModelParameter2> buf_model_parameter2;
StructuredBuffer<int> buf_vertex_offsets;
StructuredBuffer<int> buf_index_offsets;

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
	ApplyFlipbookVS(flipbookRate, flipbookNextIndexUV, fFlipbookParameter, flipbookIndexAndNextRate, uv1);

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


VS_Output vert(uint v_id : SV_VertexID, uint inst : SV_InstanceID)
{
	float4x4 mModel = buf_model_parameter[inst].Mat;
	float4 fUV = buf_model_parameter[inst].UV;
	float4 fModelColor = buf_model_parameter[inst].VColor;

	float4 fAlphaUV = buf_model_parameter2[inst].AlphaUV;
	float4 fUVDistortionUV = buf_model_parameter2[inst].DistortionUV;
	float4 fBlendUV = buf_model_parameter2[inst].BlendUV;
	float4 fBlendAlphaUV = buf_model_parameter2[inst].BlendAlphaUV;
	float4 fBlendUVDistortionUV = buf_model_parameter2[inst].BlendDistortionUV;
	float fFlipbookIndexAndNextRate = buf_model_parameter2[inst].FlipbookIndexAndNextRate;
	float fAlphaThreshold = buf_model_parameter2[inst].AlphaThreshold;

	float buf_vertex_offset = buf_vertex_offsets[buf_model_parameter[inst].Time];
	float buf_index_offset = buf_index_offsets[buf_model_parameter[inst].Time];

	ModelVertex Input = buf_vertex[buf_index[v_id + buf_index_offset] + buf_vertex_offset];

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

	VS_Output Output = (VS_Output)0;
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

float4 fFlipbookParameter; // x:enable, y:loopType, z:divideX, w:divideY
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
	ApplyFlipbookVS(flipbookRate, flipbookNextIndexUV, fFlipbookParameter, vsinput.FlipbookIndex, vsoutput.UV_Others.xy);

	vsoutput.Blend_FBNextIndex_UV.zw = flipbookNextIndexUV;
	vsoutput.UV_Others.z = flipbookRate;
	vsoutput.UV_Others.w = vsinput.AlphaThreshold;
}

VS_Output vert(uint id : SV_VertexID, uint inst : SV_InstanceID)
{
	int qind = (id) / 6;
	int vind = (id) % 6;

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
	VS_Output Output = (VS_Output)0;

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