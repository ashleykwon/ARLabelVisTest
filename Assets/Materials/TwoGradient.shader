Shader "Custom/GreyGradient"
{
    Properties {
        _Color1("Color 1", Color) = (1,1,1,1)
        _Color2("Color 2", Color) = (0,0,0,1)
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100 

        Pass {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 objPos : TEXCOORD0;
            };

            half4 _Color1;
            half4 _Color2;

            v2f vert(appdata_t v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.objPos = v.vertex.xyz;
                return o;
            }

            half4 frag(v2f i) : SV_Target {
                //float t = acos(i.objPos.x / 0.5) / UNITY_PI;
                float t = i.objPos.x + 0.5;
                //float smoothT = t * t * (3 - 2 * t);
                float smoothT = t * t * t * (t * (t * 6 - 15) + 10);
                half4 gradientColor = lerp(_Color1, _Color2, smoothT);
                return gradientColor;
            }
            ENDCG
        }
    }   
}
