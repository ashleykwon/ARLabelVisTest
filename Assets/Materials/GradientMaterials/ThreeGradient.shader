Shader "Custom/ThreeGradient"
{
    Properties {
        _Color1("Color 1", Color) = (1,0,0,1)
        _Color2("Color 2", Color) = (0,1,0,1)
        _Color3("Color 3", Color) = (0,0,1,1)
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

                float t1 = (theta + UNITY_PI) / (2.0 * UNITY_PI);
                float t2 = phi / UNITY_PI;

                half4 interpolatedColor = lerp(lerp(_Color1, _Color2, t1), _Color3, t2);
                return interpolatedColor;
            }
            ENDCG
        }
    }   
}
