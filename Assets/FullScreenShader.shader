Shader "Unlit/FullScreenShader"
{
    Properties
    {
        _LabelCubeMap( "LabelCubeMap", Cube ) = "white" {}

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

            samplerCUBE _LabelCubeMap;
            float _LabelCubeMap_ST;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            // struct v2f
            // {
            //     float2 uv : TEXCOORD0;
            //     UNITY_FOG_COORDS(1)
            //     float4 vertex : SV_POSITION;
            // };

            struct v2f 
            {
                float4 pos : SV_Position;
                half3 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;


            v2f vert (appdata_img v)
            {
                // v2f o;
                // o.vertex = UnityObjectToClipPos(v.vertex);
                // o.uv = TRANSFORM_TEX(v.uv,_LabelCubeMap);
                // UNITY_TRANSFER_FOG(o,o.vertex);
                // return o;
                v2f o;
                o.pos = UnityObjectToClipPos( v.vertex );
                o.uv = v.vertex.xyz * half3(1,1,1); 

                return o;
            }

            fixed4 frag (v2f vdata) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, vdata.uv);
                fixed4 labelTex = texCUBE(_LabelCubeMap, vdata.uv);

                //turn col blue
                float blue = col.b;

                // // col = float4(0, 0, blue, 1);

          
                if (labelTex[3] != 0) // is a label pixel
                {
                    //col = tex2D(_MainTex, vdata.uv);
                    col = float4(0, 0, blue, 1);
                }
                

                // apply fog
                //UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
