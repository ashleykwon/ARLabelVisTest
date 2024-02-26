Shader "Unlit/GetBGMask"
{
    Properties
    {
        _LabelTex("Label Image", 2D) = "white" {}
        _BackgroundTex("Background Image", 2D) = "white" {}
        _MaskedBackgroundTex("Masked Background Image", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _BackgroundTex;
            sampler2D _LabelTex;
            sampler2D _MaskedBackgroundTex;
            
            float4 _BackgroundTex_ST;
            float4 _BackgroundTex_TexelSize;

            float4 _LabelTex_TexelSize;
            float4 _LabelTex_ST;

            float4 _MaskedBackgroundTex_TexelSize;
            float4 _MaskedBackgroundTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _BackgroundTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_BackgroundTex, i.uv);
                fixed4 labelPixel = tex2D(_LabelTex, i.uv);
                fixed4 backgroundPixel = tex2D(_BackgroundTex, i.uv);
                if (labelPixel[3] == 0){
                    backgroundPixel = float4(0.0, 0.0, 0.0, 0.0);
                }
                // col = float4(1.0, 0.0, 0.0, 1.0);
                return col;
            }
            ENDCG
        }
    }
}
