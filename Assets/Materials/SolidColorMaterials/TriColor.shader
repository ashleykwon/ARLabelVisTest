Shader "Custom/TriColor"
{
    Properties {
        _Color1("Color 1", Color) = (1,0,0,1)
        _Color2("Color 2", Color) = (0,1,0,1)
        _Color3("Color 3", Color) = (0,0,1,1)
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
            half4 _Color3;
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

                half4 color;
                float noise = frac(sin(dot(i.pos.xy, float2(12.9898,78.233))) * 43758.5453) - 1;
                half4 noise4 = half4(noise, noise, noise, 0);
                
                if (-UNITY_PI <= theta && theta < -UNITY_PI / 3.0)
                {
                    color = _Color1;
                } 
                else if (-UNITY_PI / 3.0 <= theta && theta < UNITY_PI / 3.0)
                {
                    color = _Color2;
                }
                else
                {
                    color = _Color3;
                }

                return color + noise4 * _Noise;
            }
            ENDCG
        }
    }   
}
