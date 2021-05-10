Shader "Effekseer/FixedShader" {

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
			#include "EffekseerShaderCommon.cginc"

			#pragma multi_compile _ _MODEL_
			#pragma multi_compile _ _ADVANCED_
			#pragma multi_compile _ ENABLE_LIGHTING ENABLE_DISTORTION
			#pragma multi_compile_instancing
			
#if _ADVANCED_

			#include "EffekseerShaderAdVS.cginc"
#if ENABLE_DISTORTION
			#include "EffekseerShaderAdDistortionPS.cginc"
#else
			#include "EffekseerShaderAdLitUnlitPS.cginc"
#endif

#else

			#include "EffekseerShaderVS.cginc"
#if ENABLE_DISTORTION
			#include "EffekseerShaderDistortionPS.cginc"
#else
			#include "EffekseerShaderLitUnlitPS.cginc"
#endif

#endif

			ENDCG
		}
	}

	Fallback Off
}