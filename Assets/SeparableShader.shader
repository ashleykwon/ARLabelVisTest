Shader "Unlit/SeparableShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _LabelTex("Texture", 2D) = "white" {}
        _KernelSize("Blur Kernel Size", Range(0, 100)) = 50
        _Sigma("Blur Sigma", Range(0, 100)) = 50
        _ShadowScale("Shadow Scale", Range(0.8, 1.05)) = 1.0
        _ShadowMultiplier("Shadow Multiplier", Range(0, 2)) = 1.0
        _Lamdba("lamdba", Range(0,1)) = 0.5
        [MaterialToggle] _EnableShadow("Enable Shadow", Int) = 1
        _ColorMethod("Color Method", Int) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
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

            sampler2D _MainTex;
            sampler2D _LabelTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            float4 _LabelTex_TexelSize;
            float4 _LabelTex_ST;
            float _KernelSize;
            float _Sigma;
            float _ShadowScale;
            float _Lamdba;
            int _EnableShadow;

            float gaussian1D(float x, float sigma) {
                float pi = 3.14159265359;
                return 1 / sqrt(2 * pi * sigma) * exp(-(x * x) / (2 * sigma));
            }

            v2f vert(appdata v)
            {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _LabelTex);
                    UNITY_TRANSFER_FOG(o,o.vertex);
                    return o;
            }

            fixed4 frag(v2f vdata) : SV_Target
            {

                float4 acc = float4(0, 0, 0, 0);
                for (int i = _KernelSize / 2; i >= -_KernelSize / 2; i--) {
                        float x = vdata.uv.x + i * _LabelTex_TexelSize.x;
                        float y = vdata.uv.y;
                        float2 coords = float2(x, y);
                        coords = (coords - 0.5) / _ShadowScale + 0.5;
                        float weight = gaussian1D(i, _Sigma);
                        float4 pix = tex2D(_LabelTex, coords);
                        acc += pix * weight;
                }

                // sample the texture
                // apply fog
                //UNITY_APPLY_FOG(i.fogCoord, col);
                                        float x = vdata.uv.x + i * _LabelTex_TexelSize.x;
                        float y = vdata.uv.y;
                        float2 coords = float2(x, y);

                return tex2D(_MainTex, coords);;
                }
                ENDCG
            }

    }
}
