Shader "Unlit/LUT_test_shader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LUT ("LUT", 3D) = "white" {}
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
                // UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            uniform sampler3D _LUT;
            uniform sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                // fixed4 col = tex2D(_MainTex, i.uv);
                // // apply fog
                // UNITY_APPLY_FOG(i.fogCoord, col);
                // return col;

                float4 color = tex2D(_MainTex, i.uv);
                float4 converted = tex3D(_LUT, color.rgb);
                // converted = float4(color.rgb[0]/255, color.rgb[1]/255, color.rgb[2]/255, 1);
                converted.a = 1;

                return converted;
            }
            ENDCG
        }
    }
}
