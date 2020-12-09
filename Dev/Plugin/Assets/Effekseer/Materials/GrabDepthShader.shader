Shader "Effekseer/GrabDepthShader"
{
    Properties
    {
    }
    SubShader
    {
        ZTest Always
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _CameraDepthTexture;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed frag (v2f i) : SV_Depth
            {
                fixed4 col = tex2D(_CameraDepthTexture, i.uv);
                return col.r;
            }
            ENDCG
        }
    }
}
