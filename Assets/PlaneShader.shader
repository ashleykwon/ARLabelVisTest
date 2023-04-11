Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_UseFilter ("Use Filter", Integer) = 1
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
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			bool _UseFilter;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

			fixed4 frag(v2f vdata) : SV_Target
			{
				
				float box_blur[25] = {1, 1, 1, 1, 1,
										1, 1, 1, 1, 1,
										1, 1, 1, 1, 1,
										1, 1, 1, 1, 1,
										1, 1, 1, 1, 1};

				fixed4 col = tex2D(_MainTex, vdata.uv);

				if (_UseFilter == 1) {
					float4 acc = float4(0, 0, 0, 0);
					for (int i = 2; i >= -2; i--) {
						for (int j = 2; j >= -2; j--) {
							float weight = box_blur[(i + 2) * 5 + (j + 2)];
							float4 pix = tex2D(_MainTex, float2(vdata.uv.x + j * _MainTex_ST.x, vdata.uv.y + i * _MainTex_ST.y));
							acc += pix * weight;
						}
					}
					col = acc / 25.0;
				}

                // sample the texture
                // apply fog
                //UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
