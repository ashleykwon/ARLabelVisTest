Shader "Unlit/PlaneShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LabelTex ("Texture", 2D) = "white" {}
		  _KernelSize ("Blur Kernel Size", Range (0, 100)) = 50
        _Sigma ("Blur Sigma", Range (0, 100)) = 50
        _ShadowScale ("Shadow Scale", Range (1.0, 1.05)) = 1.0
        _ShadowMultiplier ("Shadow Multiplier", Range (0, 2)) = 1.0
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

            sampler2D _MainTex;
            sampler2D _LabelTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            float4 _LabelTex_TexelSize;
            float4 _LabelTex_ST;
			   float _KernelSize;
            float _Sigma;
            float _ShadowScale;
            float _ShadowMultiplier;

            float gaussian2D(float x, float y, float sigma){
               float rsq = x * x + y * y;
               float pi = 3.14159265359;
               return 1 / (2 * pi * sigma) * exp(-rsq / (2 * sigma));
            }

            v2f vert (appdata v)
            {
                  v2f o;
                  o.vertex = UnityObjectToClipPos(v.vertex);
                  o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                  UNITY_TRANSFER_FOG(o,o.vertex);
                  return o;
            }

            fixed4 frag(v2f vdata) : SV_Target
            {

               fixed4 col = tex2D(_MainTex, vdata.uv);

               fixed4 textMatte = tex2D(_LabelTex, vdata.uv);

               
               float4 acc = float4(0, 0, 0, 0);
               for (int i = _KernelSize/2; i >= -_KernelSize/2; i--) {
                  for (int j = _KernelSize/2; j >= -_KernelSize/2; j--) {
                     float x = vdata.uv.x + j * _LabelTex_TexelSize.x;
                     float y = vdata.uv.y + i * _LabelTex_TexelSize.y;
                     float2 coords = float2(x, y);
                     coords = (coords - 0.5) / _ShadowScale + 0.5;
                     float weight = gaussian2D(i, j, _Sigma);
                     float4 pix = tex2D(_LabelTex, coords);
                     acc += pix * weight;
                  }
               }

               if (textMatte.r == 0){
                  col *= 1 - acc * _ShadowMultiplier;
               }
               

               // sample the texture
               // apply fog
               //UNITY_APPLY_FOG(i.fogCoord, col);
               return col;
               }
               ENDCG
        }
    }
}
