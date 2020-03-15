Shader "Effekseer/StandardLightingShader" {

	Properties{
		_ColorTex("Color (RGBA)", 2D) = "white" {}
		_NormalTex("Color (RGBA)", 2D) = "white" {}
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

		#define MOD fmod
		#define FRAC frac
		#define LERP lerp

		#pragma target 5.0
		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile _ _MODEL_

		#include "UnityCG.cginc"

		sampler2D _ColorTex;
		sampler2D _NormalTex;

		#if _MODEL_

		struct SimpleVertex
		{
			float3 Pos;
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

		#else

		struct Vertex
		{
			float3 Pos;
			float4 Color;
			float3 Normal;
			float3 Tangent;
			float2 UV1;
			float2 UV2;
		};

		StructuredBuffer<Vertex> buf_vertex;
		float buf_offset;

		#endif

		struct ps_input
		{
			float4 Position		: SV_POSITION;
			float4 VColor		: COLOR;
			float2 UV1		: TEXCOORD0;
			float2 UV2		: TEXCOORD1;
			float3 WorldP	: TEXCOORD2;
			float3 WorldN : TEXCOORD3;
			float3 WorldT : TEXCOORD4;
			float3 WorldB : TEXCOORD5;
			float2 ScreenUV : TEXCOORD6;
		};

		float4 lightDirection;
		float4 lightColor;
		float4 lightAmbientColor;

		float2 GetUV(float2 uv)
		{
			uv.y = 1.0 - uv.y;
			return uv;
		}

		float2 GetUVBack(float2 uv)
		{
			uv.y = uv.y;
			return uv;
		}

		ps_input vert(uint id : SV_VertexID, uint inst : SV_InstanceID)
		{
			// Unity
			float4 cameraPosition = float4(UNITY_MATRIX_V[3].xyzw);

			#if _MODEL_

			uint v_id = id;

			float4x4 buf_matrix = buf_model_parameter[inst].Mat;
			float4 buf_uv = buf_model_parameter[inst].UV;
			float4 buf_color = buf_model_parameter[inst].Color;
			float buf_vertex_offset = buf_vertex_offsets[buf_model_parameter[inst].Time];
			float buf_index_offset = buf_index_offsets[buf_model_parameter[inst].Time];

			SimpleVertex Input = buf_vertex[buf_index[v_id + buf_index_offset] + buf_vertex_offset];


			float3 localPos = Input.Pos;


			#else

			int qind = (id) / 6;
			int vind = (id) % 6;

			int v_offset[6];
			v_offset[0] = 2;
			v_offset[1] = 1;
			v_offset[2] = 0;
			v_offset[3] = 1;
			v_offset[4] = 2;
			v_offset[5] = 3;

			Vertex Input = buf_vertex[buf_offset + qind * 4 + v_offset[vind]];

			#endif

			ps_input Output;

			#if _MODEL_
			float3x3 matRotModel = (float3x3)buf_matrix;
			float3 worldPos = mul(buf_matrix, float4(localPos, 1.0f)).xyz;
			float3 worldNormal = normalize(mul(matRotModel, Input.Normal));
			float3 worldTangent = normalize(mul(matRotModel, Input.Tangent));
			float3 worldBinormal = cross(worldNormal, worldTangent);
			#else
			float3 worldPos = Input.Pos;
			float3 worldNormal = Input.Normal;
			float3 worldTangent = Input.Tangent;
			float3 worldBinormal = cross(worldNormal, worldTangent);
			#endif

			// UV
			#if _MODEL_
			float2 uv1 = Input.UV.xy * buf_uv.zw + buf_uv.xy;
			float2 uv2 = Input.UV.xy * buf_uv.zw + buf_uv.xy;
			#else
			float2 uv1 = Input.UV1;
			float2 uv2 = Input.UV2;
			#endif

			// NBT
			Output.WorldN = worldNormal;
			Output.WorldB = worldBinormal;
			Output.WorldT = worldTangent;

			float3 pixelNormalDir = worldNormal;

			#if _MODEL_
			float4 vcolor = Input.Color * buf_color;
			#else
			float4 vcolor = Input.Color;
			#endif

			// Unity Ext
			float4 cameraPos = mul(UNITY_MATRIX_V, float4(worldPos, 1.0f));
			cameraPos = cameraPos / cameraPos.w;
			Output.Position = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0f));

			Output.WorldP = worldPos;
			Output.VColor = vcolor;
			Output.UV1 = uv1;
			Output.UV2 = uv2;
			Output.ScreenUV = Output.Position.xy / Output.Position.w;
			Output.ScreenUV.xy = float2(Output.ScreenUV.x + 1.0, 1.0 - Output.ScreenUV.y) * 0.5;

			return Output;
		}

		float4 frag(ps_input Input) : COLOR
		{
			//			
			float2 uv1 = Input.UV1;
			float2 uv2 = Input.UV2;
			float3 worldPos = Input.WorldP;
			float3 worldNormal = Input.WorldN;
			float3 worldBinormal = Input.WorldB;
			float3 worldTangent = Input.WorldT;

			float3 pixelNormalDir = worldNormal;
			float4 vcolor = Input.VColor;

			float3 normalDir = tex2D(_NormalTex, uv1).xyz;
			float3 tempNormalDir = ((normalDir - float3 (0.5, 0.5, 0.5)) * 2.0);
			pixelNormalDir = tempNormalDir.x * worldTangent + tempNormalDir.y * worldBinormal + tempNormalDir.z * worldNormal;

			float diffuse = max(0.0, dot(pixelNormalDir, lightDirection.xyz));

			float4 ret = vcolor * tex2D(_ColorTex, uv1);
			ret.xyz = ret.xyz * (lightColor.xyz * diffuse + lightAmbientColor.xyz);
			return ret;
		}

		ENDCG

		}

		}

			Fallback Off
}