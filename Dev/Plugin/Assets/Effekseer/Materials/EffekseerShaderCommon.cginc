
struct SpriteUnlitVertex
{
	float3 Pos;
	float2 UV;
	float4 Color;
};

struct SpriteLitDistMatVertex
{
	float3 Pos;
	float4 Color;
	float3 Normal;
	float3 Tangent;
	float2 UV1;
	float2 UV2;
};

struct ModelVertex
{
	float3 Position;
	float3 Normal;
	float3 Binormal;
	float3 Tangent;
	float2 UV;
	float4 Color;
};

struct ModelParameter1
{
	float4x4 Mat;
	float4 VColor;
	float4 UV;
};

struct ModelParameter2
{
	float4 AlphaUV;
	float4 DistortionUV;
	float4 BlendUV;
	float4 BlendAlphaUV;
	float4 BlendDistortionUV;
	float FlipbookIndexAndNextRate;
	float AlphaThreshold;
	float ViewOffsetDistance;
	int Time;
};


struct AdvancedParameter
{
	float2 AlphaUV;
	float2 UVDistortionUV;
	float2 BlendUV;
	float2 BlendAlphaUV;
	float2 BlendUVDistortionUV;
	float FlipbookIndexAndNextRate;
	float AlphaThreshold;
};
