Shader "Custom/Checker"
{
    Properties {
        _Color1("Color 1", Color) = (1,0,0,1)
        _Color2("Color 2", Color) = (0,1,0,1)
        _Tiles("Tiles", Int) = 10
        _Noise("Noise", float) = 0.0
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
                float3 normal : TEXCOORD0;
            };

            half4 _Color1;
            half4 _Color2;
            int _Tiles;
            float _Noise;

            v2f vert(appdata_t v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = normalize(UnityObjectToWorldNormal(v.vertex.xyz));
                return o;
            }

            half4 frag(v2f i) : SV_Target {
                float3 spherePos = normalize(i.normal);
                float theta = atan2(spherePos.z, spherePos.x);
                float phi = acos(spherePos.y);

                int pTheta = floor((theta + UNITY_PI) / (UNITY_PI / _Tiles));
                int pPhi = floor((phi + UNITY_PI / 2.0) / (UNITY_PI / _Tiles));

                half4 color = (pTheta + pPhi) % 2 ? _Color1 : _Color2;
                float noise = frac(sin(dot(i.pos.xy, float2(12.9898,78.233))) * 43758.5453) - 1;
                half4 noise4 = half4(noise, noise, noise, 0);

                return color + noise4 * _Noise;
            }
            ENDCG
        }
    }   
}
