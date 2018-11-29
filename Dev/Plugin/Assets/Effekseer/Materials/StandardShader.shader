Shader "Effekseer/StandardShader" {

	Properties{
		_ColorTex("Color (RGBA)", 2D) = "white" {}
	}

	SubShader{

	Pass {

	CGPROGRAM

	#pragma target 5.0
	#pragma vertex vert
	#pragma fragment frag

	#include "UnityCG.cginc"

	sampler2D _ColorTex;

	struct SimpleVertex
	{
		float3 Pos;
		float2 UV;
		uint4 Color;
	};

	StructuredBuffer<SimpleVertex> buf_vertex;

	struct ps_input
	{
		float4 pos : SV_POSITION;
	};

	ps_input vert(uint id : SV_VertexID, uint inst : SV_InstanceID)
	{
		ps_input o;

		int qind = inst / 6;
		int vind = inst % 6;

		SimpleVertex v = buf_vertex[qind * 4 + vind % 3];

		float3 worldPos = v.Pos;
		o.pos = mul(UNITY_MATRIX_VP, float4(worldPos,1.0f));
		return o;
	}

	float4 frag(ps_input i) : COLOR
	{
		return float4(1, 0.5f, 0.0f, 1);
	}

	ENDCG

	}

	}

	Fallback Off
}