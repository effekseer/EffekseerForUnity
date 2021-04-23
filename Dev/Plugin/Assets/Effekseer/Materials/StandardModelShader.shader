﻿Shader "Effekseer/StandardModelShader" {

Properties{
	_ColorTex("Color (RGBA)", 2D) = "white" {}
	[Enum(UnityEngine.Rendering.BlendMode)]_BlendSrc("Blend Src", Float) = 0
	[Enum(UnityEngine.Rendering.BlendMode)]_BlendDst("Blend Dst", Float) = 0
	_BlendOp("Blend Op", Float) = 0
	_Cull("Cull", Float) = 0
	[Enum(UnityEngine.Rendering.CompareFunction)]_ZTest("ZTest Mode", Float) = 0
	[Toggle]_ZWrite("ZWrite", Float) = 0
}

SubShader{

	Blend[_BlendSrc][_BlendDst]
	BlendOp[_BlendOp]
	ZTest[_ZTest]
	ZWrite[_ZWrite]
	Cull[_Cull]

	Pass {

	CGPROGRAM

	#pragma target 5.0
	#pragma vertex vert
	#pragma fragment frag

	#include "UnityCG.cginc"

	sampler2D _ColorTex;

	struct SimpleVertex
	{
		float3 Position;
		float3 Normal;
		float3 Binormal;
		float3 Tangent;
		float2 UV;
		float4 Color;
	};

	struct ModelParameter
	{
		float4x4 Mat;
		float4 UV;
		float4 Color;
		int Time;
	};

	StructuredBuffer<SimpleVertex> buf_vertex;
	StructuredBuffer<int> buf_index;

	StructuredBuffer<ModelParameter> buf_model_parameter;
	StructuredBuffer<int> buf_vertex_offsets;
	StructuredBuffer<int> buf_index_offsets;

	struct vs_input
	{
		uint id : SV_VertexID;
		uint inst : SV_InstanceID;
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	struct ps_input
	{
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
		float4 color : COLOR0;
		UNITY_VERTEX_OUTPUT_STEREO
	};

	ps_input vert(vs_input i)
	{
		ps_input o;
		
		UNITY_SETUP_INSTANCE_ID(i);
		UNITY_INITIALIZE_OUTPUT(ps_input, o);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

		uint v_id = i.id;

		float4x4 buf_matrix = buf_model_parameter[i.inst].Mat;
		float4 buf_uv = buf_model_parameter[i.inst].UV;
		float4 buf_color = buf_model_parameter[i.inst].Color;
		float buf_vertex_offset = buf_vertex_offsets[buf_model_parameter[i.inst].Time];
		float buf_index_offset = buf_index_offsets[buf_model_parameter[i.inst].Time];

		SimpleVertex v = buf_vertex[buf_index[v_id + buf_index_offset] + buf_vertex_offset];

		float3 localPos = v.Position;
		float4 worldPos = mul(buf_matrix, float4(localPos, 1.0f));
		//float4 worldPos = float4(localPos, 1.0f);
		o.pos = mul(UNITY_MATRIX_VP, worldPos);
		o.uv.xy = v.UV.xy * buf_uv.zw + buf_uv.xy;
		o.uv.y = 1.0 - o.uv.y;
		o.color = (float4)v.Color * buf_color;
		return o;
	}

	float4 frag(ps_input i) : COLOR
	{
		float4 color = tex2D(_ColorTex, i.uv) * i.color;

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