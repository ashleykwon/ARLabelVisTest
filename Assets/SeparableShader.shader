Shader "Unlit/SeparableShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _LabelTex("Texture", 2D) = "white" {}
        _Lamdba("lamdba", Range(0,1)) = 0.5
        _ColorMethod("Color Method", Int) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }

            Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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

                float x = vdata.uv.x ;
                float y = vdata.uv.y;
                float2 coords = float2(x, y);
                return tex2D(_MainTex, coords);
                }
                ENDCG
            }
    }
}