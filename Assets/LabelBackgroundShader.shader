Shader "Unlit/LabelBackgroundShader"
{
    Properties
    {
        _CubeMap( "Cube Map", Cube ) = "white" {}
        // _Scale("label_scale", Range(0,1000)) = 0.1
    }
    SubShader
    {
        // Tags { "RenderType"="Opaque" }
        // LOD 100
        

        Pass
        {
            Tags { "DisableBatching" = "True" }

            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Initialize variables        
            samplerCUBE _CubeMap;
            // float _Scale;
    
        
            struct v2f 
            {
                float4 pos : SV_Position;
                half3 uv : TEXCOORD0;
            };
        
            v2f vert( appdata_img v )
            {

                v2f o;
                o.pos = UnityObjectToClipPos( v.vertex );
                o.uv = v.vertex.xyz; // mirror so cubemap projects as expected

                return o;
            }




            fixed4 frag (v2f vdata) : SV_Target
            {
                // sample the texture
                fixed4 col = texCUBE(_CubeMap, vdata.uv);
                if (col[0] != 0 && col[1] != 0){ // is a label pixel
                    col = float4(1.0, 1.0, 1.0, 1.0);
                }

                else{ // is a background pixel
                    col = float4(0.0, 0.0, 0.0, 1.0);
                }
                return col;
            }
            ENDCG
        }

        // GrabPass{"_CubeMap"}

        // Pass
        // {
        //     Tags { "DisableBatching" = "True" }

        //     Cull Off

        //     CGPROGRAM
        //     #pragma vertex vert
        //     #pragma fragment frag
        //     #include "UnityCG.cginc"

        //     // Initialize variables        
        //     samplerCUBE _CubeMap;
        //     float _Scale;
        
        //     struct v2f 
        //     {
        //         float4 pos : SV_Position;
        //         half3 uv : TEXCOORD0;
        //     };
        
        //     v2f vert( appdata_img v )
        //     {

        //         v2f o;
        //         o.pos = UnityObjectToClipPos( v.vertex );
        //         o.uv = v.vertex.xyz; // mirror so cubemap projects as expected

        //         return o;
        //     }

        //     fixed4 frag (v2f vdata) : SV_Target
        //     {
        //         // sample the texture
        //         float4 col = texCUBE(_CubeMap, vdata.uv);
        //         float3 centerCoord = float3(0, 0, 0);
        //         // col = texCUBE(_CubeMap, vdata.uv*5);
        //         if (col[0] != 0 && col[1] != 0){ // is a background pixel
        //             col = float4(0, 0, 1, 1);
        //         }
        //         return col;
        //     }
        //     ENDCG
        // }

    }
}
