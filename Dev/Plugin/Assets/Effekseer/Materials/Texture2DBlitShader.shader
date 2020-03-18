Shader "Effekseer/Texture2DBlitShader"
{
        SubShader
    {
        Cull Off
        ZTest Off
        ZWrite Off

        Tags { "RenderType" = "Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            sampler2D _BackgroundTex;
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

            float4 textureArea;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                o.uv.x = o.uv.x * textureArea.x + textureArea.z;
                o.uv.y = o.uv.y * textureArea.y + textureArea.w;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_BackgroundTex, float2(i.uv));
                return col;
            }
            ENDCG
        }
    }
}