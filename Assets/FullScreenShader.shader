Shader "Unlit/FullScreenShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LabelTex("Label Image", 2D) = "white" {}
        _CurrentFrameTex("CurrentFrameTex", 2D) = "white" {}

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
            float4 _MainTex_ST;
            sampler2D _LabelTex;
            float4 _LabelTex_ST;
            sampler2D _CurrentFrameTex;
            float4 _CurrentFrameTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _LabelTex); // _LabelTex was _MainTex
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f vdata) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, vdata.uv);
                fixed4 textMatte = tex2D(_LabelTex, vdata.uv);

                //turn col blue
                float blue = col.b;

                // col = float4(0, 0, blue, 1);

          
                if (textMatte[3] != 0) // is a label pixel
                {
                    //col = tex2D(_MainTex, vdata.uv);
                    col = float4(0, 0, blue, 1);
                }

                else
                {
                    col = tex2D(_CurrentFrameTex, vdata.uv);
                }

                

                // apply fog
                //UNITY_APPLY_FOG(i.fogCoord, col);
                return col;



            }
            ENDCG
        }
    }
}
