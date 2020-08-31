Shader "Effekseer/FakeShader" {

	Properties{
	}

	SubShader{

	Pass {

	CGPROGRAM

	#pragma target 4.0
	#pragma vertex vert
	#pragma fragment frag

	#include "UnityCG.cginc"

	struct ps_input
	{
		float4 pos : SV_POSITION;
		float4 color : COLOR0;
	};

	ps_input vert(uint id : SV_VertexID)
	{
		ps_input o;
		o.pos = float4(1.0, 1.0, 1.0, 1.0);
		o.color = float4(0.0, 0.0, 0.0, 0.0);
		return o;
	}

	float4 frag(ps_input i) : COLOR
	{
		float4 color = i.color;

		if (color.w <= 0.0f)
		{
			discard;
		}

		return color;
	}

	ENDCG

	}

	}

	Fallback Off
}