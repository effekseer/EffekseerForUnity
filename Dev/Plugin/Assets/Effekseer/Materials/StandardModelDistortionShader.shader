Shader "Effekseer/StandardModelDistortionShader" {

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
		sampler2D _BackTex;

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
			float4x4 Matrix;
			float4 UV;
			float4 Color;
			int Time;
		};

		StructuredBuffer<SimpleVertex> buf_vertex;
		StructuredBuffer<int> buf_index;

		StructuredBuffer<ModelParameter> buf_model_parameter;
		StructuredBuffer<int> buf_vertex_offsets;
		StructuredBuffer<int> buf_index_offsets;

		struct ps_input
		{
			float4 pos : SV_POSITION;
            float4 posC : NORMAL0;      // if this name is POS0, something is wrong with Metal API
            float4 posR : NORMAL1;      // if this name is POS1, something is wrong with Metal API
            float4 posU : NORMAL2;      // if this name is POS2, something is wrong with Metal API
			float2 uv : TEXCOORD0;
			float4 color : COLOR0;
		};

		float distortionIntensity;

		ps_input vert(uint id : SV_VertexID, uint inst : SV_InstanceID)
		{
			ps_input o;
			uint v_id = id;

			float4x4 buf_matrix = buf_model_parameter[inst].Matrix;
			float4 buf_uv = buf_model_parameter[inst].UV;
			float4 buf_color = buf_model_parameter[inst].Color;
			float buf_vertex_offset = buf_vertex_offsets[buf_model_parameter[inst].Time];
			float buf_index_offset = buf_index_offsets[buf_model_parameter[inst].Time];

			SimpleVertex v = buf_vertex[buf_index[v_id + buf_index_offset] + buf_vertex_offset];

			float3 localPos = v.Position;
			float4 vPos = mul(buf_matrix, float4(localPos, 1.0f));
			float4 vBinormal = mul(buf_matrix, v.Binormal);
			float4 vTangent = mul(buf_matrix, v.Tangent);

			float4 localBinormal = float4((vPos + vBinormal));
			float4 localTangent = float4((vPos + vTangent));
			localBinormal = mul(UNITY_MATRIX_V, localBinormal);
			localTangent = mul(UNITY_MATRIX_V, localTangent);
			float4 cameraPos = mul(UNITY_MATRIX_V, vPos);

			localBinormal = localBinormal / localBinormal.w;
			localTangent = localTangent / localTangent.w;

			localBinormal = cameraPos + normalize(localBinormal - cameraPos);
			localTangent = cameraPos + normalize(localTangent - cameraPos);

			o.posC = mul(UNITY_MATRIX_P, cameraPos);
			o.posR = mul(UNITY_MATRIX_P, localTangent);
			o.posU = mul(UNITY_MATRIX_P, localBinormal);

			o.pos = mul(UNITY_MATRIX_VP, vPos);
			o.uv.xy = v.UV.xy * buf_uv.zw + buf_uv.xy;
			o.uv.y = 1.0 - o.uv.y;
			o.color = (float4)v.Color * buf_color;
			return o;
		}

		float4 frag(ps_input i) : COLOR
		{
			float2 g_scale = float2(distortionIntensity, distortionIntensity);
			float4 color = tex2D(_ColorTex, i.uv);
			color.w = color.w * i.color.w;

			float2 pos = i.posC.xy / i.posC.w;
			float2 posU = i.posU.xy / i.posU.w;
			float2 posR = i.posR.xy / i.posR.w;

			float2 uv = pos + (posR - pos) * (color.x * 2.0 - 1.0) * i.color.x * g_scale.x + (posU - pos) * (color.y * 2.0 - 1.0) * i.color.y * g_scale.x;
			uv.x = (uv.x + 1.0) * 0.5;
			uv.y = (uv.y + 1.0) * 0.5;

			// Flip
			uv.y = 1.0 - uv.y;

			color.xyz = tex2D(_BackTex, uv).xyz;

			return color;
		}

		ENDCG

		}

	}

		Fallback Off
}