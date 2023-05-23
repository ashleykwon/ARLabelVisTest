Shader "Unlit/InverseCullCubeMapShader"
{
    Properties
    {
        _CubeMap( "Cube Map", Cube ) = "white" {}
        _LabelCubeMap( "LabelCubeMap", Cube ) = "white" {}
    }
    SubShader
    {
        Pass 
        {
            Tags { "DisableBatching" = "True" }

            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
        
            samplerCUBE _CubeMap;
            samplerCUBE _LabelCubeMap;
        
            struct v2f 
            {
                float4 pos : SV_Position;
                half3 uv : TEXCOORD0;
            };
        
            v2f vert( appdata_img v )
            {
                v2f o;
                o.pos = UnityObjectToClipPos( v.vertex );
                o.uv = v.vertex.xyz * half3(-1,1,1); // mirror so cubemap projects as expected
                return o;
            }
        
            fixed4 frag( v2f i ) : SV_Target 
            {
                fixed4 col = texCUBE(_CubeMap, i.uv);
                fixed4 labelTex = texCUBE(_LabelCubeMap, i.uv);

                float blue = col.b;

                if (labelTex[3] != 0) // is a label pixel
                {
                    col = float4(0, 0, blue, 1);
                }
                return col;
            }
            ENDCG
        }
    }
}