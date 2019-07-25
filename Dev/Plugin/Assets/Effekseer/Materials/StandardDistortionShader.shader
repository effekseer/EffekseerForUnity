Shader "Effekseer/StandardDistortionShader" {

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
			float3 Pos;
			float2 UV;
			float4 Color;
			float3 Tangent;
			float3 Binormal;
		};

		StructuredBuffer<SimpleVertex> buf_vertex;
		float buf_offset;

		float distortionIntensity;

		struct ps_input
		{
			float4 pos : SV_POSITION;
			float4 posC : NORMAL0;      // if this name is POS0, something is wrong with Metal API
			float4 posR : NORMAL1;      // if this name is POS1, something is wrong with Metal API
			float4 posU : NORMAL2;      // if this name is POS2, something is wrong with Metal API
			float2 uv : TEXCOORD0;
			float4 color : COLOR0;
		};

		ps_input vert(uint id : SV_VertexID, uint inst : SV_InstanceID)
		{
			ps_input o;

			int qind = (id) / 6;
			int vind = (id) % 6;

			int v_offset[6];
			v_offset[0] = 2;
			v_offset[1] = 1;
			v_offset[2] = 0;
			v_offset[3] = 1;
			v_offset[4] = 2;
			v_offset[5] = 3;

			SimpleVertex v = buf_vertex[buf_offset + qind * 4 + v_offset[vind]];
            
			float4 localBinormal = float4((v.Pos + v.Binormal), 1.0);
			float4 localTangent = float4((v.Pos + v.Tangent), 1.0);
			localBinormal = mul(UNITY_MATRIX_V, localBinormal);
			localTangent = mul(UNITY_MATRIX_V, localTangent);
			float4 cameraPos = mul(UNITY_MATRIX_V, float4(v.Pos, 1.0f));

			localBinormal = localBinormal / localBinormal.w;
			localTangent = localTangent / localTangent.w;

			localBinormal = cameraPos + normalize(localBinormal - cameraPos);
			localTangent = cameraPos + normalize(localTangent - cameraPos);

			o.posC = mul(UNITY_MATRIX_P, cameraPos);
			o.posR = mul(UNITY_MATRIX_P, localTangent);
			o.posU = mul(UNITY_MATRIX_P, localBinormal);

			float3 worldPos = v.Pos;
			o.pos = mul(UNITY_MATRIX_VP, float4(worldPos,1.0f));
			o.uv = v.UV;
			o.uv.y = 1.0 - o.uv.y;
			o.color = (float4)v.Color;
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