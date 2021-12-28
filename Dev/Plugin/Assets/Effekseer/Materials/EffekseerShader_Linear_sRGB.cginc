#ifndef LINEAR_SRGB_FX
#define LINEAR_SRGB_FX

#define FLT_EPSILON 1.192092896e-07f

float3 PositivePow(float3 base, float3 power)
{
	return pow(max(abs(base), float3(FLT_EPSILON, FLT_EPSILON, FLT_EPSILON)), power);
}

// based on http://chilliant.blogspot.com/2012/08/srgb-approximations-for-hlsl.html?m=1
float3 SRGBToLinear(float3 c)
{
	return min(c, c * (c * (c * 0.305306011 + 0.682171111) + 0.012522878));
}

float4 SRGBToLinear(float4 c)
{
	return float4(SRGBToLinear(c.rgb), c.a);
}

float3 LinearToSRGB(float3 c)
{
	return max(1.055 * PositivePow(c, 0.416666667) - 0.055, 0.0);
}

float4 LinearToSRGB(float4 c)
{
	return float4(LinearToSRGB(c.rgb), c.a);
}

float4 ConvertFromSRGBTexture(float4 c, bool isValid)
{
#if defined(UNITY_COLORSPACE_GAMMA)
	return c;
#else
	if (!isValid)
	{
		return c;
	}

	return LinearToSRGB(c);
#endif
}

float4 ConvertToScreen(float4 c, bool isValid)
{
#if defined(UNITY_COLORSPACE_GAMMA)
	return c;
#else
	if (!isValid)
	{
		return c;
	}

	return SRGBToLinear(c);
#endif
}

#endif