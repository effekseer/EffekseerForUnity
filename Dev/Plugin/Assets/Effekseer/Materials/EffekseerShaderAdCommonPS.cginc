
#include "EffekseerShader_Linear_sRGB.cginc"

void ApplyFlipbook(inout float4 dst,
	const Texture2D t,
	const SamplerState s,
	const float4 flipbookParameter,
	const float4 vcolor,
	const float2 nextUV,
	float flipbookRate,
	bool convertFromSRGB)
{
	if (flipbookParameter.x > 0)
	{
		float4 sampledColor = t.Sample(s, nextUV);

		if (convertFromSRGB) {
			sampledColor = ConvertFromSRGBTexture(sampledColor, convertFromSRGB);
		}

		float4 NextPixelColor = sampledColor * vcolor;

		// lerp
		if (flipbookParameter.y == 1)
		{
			dst = lerp(dst, NextPixelColor, flipbookRate);
		}
	}
}

void ApplyTextureBlending(inout float4 dstColor, const float4 blendColor, const float blendType)
{
	// alpha blend
	if (blendType.x == 0)
	{
		dstColor.rgb = blendColor.a * blendColor.rgb + (1.0 - blendColor.a) * dstColor.rgb;
	}
	// add
	else if (blendType.x == 1)
	{
		dstColor.rgb += blendColor.rgb * blendColor.a;
	}
	// sub
	else if (blendType.x == 2)
	{
		dstColor.rgb -= blendColor.rgb * blendColor.a;
	}
	// mul
	else if (blendType.x == 3)
	{
		dstColor.rgb *= blendColor.rgb * blendColor.a;
	}
}

float2 UVDistortionOffset(const Texture2D t, const SamplerState s, float2 uv, float2 uvInversed, bool convertFromSRGB)
{
	float4 sampledColor = t.Sample(s, uv);

	if (convertFromSRGB)
	{
		sampledColor = ConvertFromSRGBTexture(sampledColor, convertFromSRGB);
	}

	float2 UVOffset = sampledColor.rg * 2.0 - 1.0;
	UVOffset.y *= -1.0;
	UVOffset.y = uvInversed.x + uvInversed.y * UVOffset.y;
	return UVOffset;
}

struct AdvancedParameter
{
	float2 AlphaUV;
	float2 UVDistortionUV;
	float2 BlendUV;
	float2 BlendAlphaUV;
	float2 BlendUVDistortionUV;
	float2 FlipbookNextIndexUV;
	float FlipbookRate;
	float AlphaThreshold;
};

AdvancedParameter DisolveAdvancedParameter(in PS_Input psinput)
{
	AdvancedParameter ret;
	ret.AlphaUV = psinput.Alpha_Dist_UV.xy;
	ret.UVDistortionUV = psinput.Alpha_Dist_UV.zw;
	ret.BlendUV = psinput.Blend_FBNextIndex_UV.xy;
	ret.BlendAlphaUV = psinput.Blend_Alpha_Dist_UV.xy;
	ret.BlendUVDistortionUV = psinput.Blend_Alpha_Dist_UV.zw;
	ret.FlipbookNextIndexUV = psinput.Blend_FBNextIndex_UV.zw;
	ret.FlipbookRate = psinput.UV_Others.z;
	ret.AlphaThreshold = psinput.UV_Others.w;
	return ret;
}

