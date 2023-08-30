Shader "Unlit/TestShader"
{// Copied from https://stackoverflow.com/questions/40834272/how-to-apply-cubemap-to-inverse-of-a-sphere-in-unity-3d and modified
    Properties
    {
        _CubeMap( "Cube Map (RGBA)", Cube ) = "white" {}

    }
    SubShader
    {
        // To be able to apply transparency to the sphere
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        LOD 100

    // Label color assignment + outline + billboard
        Pass 
        {
            // Cull off
            // Tags { "DisableBatching" = "True" }
            Tags {"Queue"="Transparent"}


            CGPROGRAM
            #pragma vertex vert alpha
            #pragma fragment frag alpha
            #include "UnityCG.cginc"

            // Initialize variables        
            samplerCUBE _CubeMap;

           
            struct v2f 
            {
                float4 pos : SV_Position;
                half3 uv : TEXCOORD0;
            };
        
            v2f vert( appdata_img v )
            {
                
                v2f o;
                o.pos = UnityObjectToClipPos( v.vertex );
                o.uv = v.vertex.xyz * half3(1,1,1); // mirror so cubemap projects as expected

                return o;
            }


            // Color assignment
            fixed4 frag( v2f vdata ) : SV_Target 
            {
                fixed4 col = texCUBE(_CubeMap, vdata.uv);

                return col;
            }
            ENDCG
        }

    }
}
