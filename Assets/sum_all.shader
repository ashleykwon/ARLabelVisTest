Shader "Unlit/sum_all"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            
            #pragma kernel CSr_sum
            #pragma kernel CSInit

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
            float4 _MainTex_ST;
            RWStructuredBuffer<float> ResultBuffer;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            [numthreads(8, 8, 1)]
            void CSr_sum(uint3 id : SV_DispatchThreadID)
            {
                // uint4 col = InputImage[id.xy];
                float4 col = tex2D(_MainTex, id.xy);
                InterlockedAdd(ResultBuffer[0], col.r);
                InterlockedAdd(ResultBuffer[1], col.g);
                InterlockedAdd(ResultBuffer[2], col.b);

            }


            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);

                //get color here
                //sum red = ResultBuffer[0]
                return col;
            }
            ENDCG
        }
    }
}
