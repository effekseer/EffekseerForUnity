Shader "Effekseer/StandardShader" {

	Properties{
		_ColorTex("Color (RGBA)", 2D) = "white" {}
		[Enum(UnityEngine.Rendering.BlendMode)]_BlendSrc("Blend Src", Float) = 0
		[Enum(UnityEngine.Rendering.BlendMode)]_BlendDst("Blend Dst", Float) = 0
	}

	SubShader{

	Blend[_BlendSrc][_BlendDst]

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
		float4 Color;
	};

	StructuredBuffer<SimpleVertex> buf_vertex;
	float buf_offset;

	struct ps_input
	{
		float4 pos : SV_POSITION;
		float2 uv : UV0;
		float4 color : COLOR0;
	};

	ps_input vert(uint id : SV_VertexID, uint inst : SV_InstanceID)
	{
		ps_input o;

		int qind = (id + buf_offset) / 6;
		int vind = (id + buf_offset) % 6;

		int v_offset[6];
		v_offset[0] = 2;
		v_offset[1] = 1;
		v_offset[2] = 0;
		v_offset[3] = 1;
		v_offset[4] = 2;
		v_offset[5] = 3;

		SimpleVertex v = buf_vertex[qind * 4 + v_offset[vind]];
		
		float3 worldPos = v.Pos;
		o.pos = mul(UNITY_MATRIX_VP, float4(worldPos,1.0f));
		o.uv = v.UV;
		o.color = (float4)v.Color;
		return o;
	}

	float4 frag(ps_input i) : COLOR
	{
		return i.color;
	}

	ENDCG

	}

	}

	Fallback Off
}