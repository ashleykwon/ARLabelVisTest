Shader "Unlit/PlaneShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		  _BlurSize ("Blur Size", Integer) = 5
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
            float4 _MainTex_TexelSize;
			   int _BlurSize;

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
				
				// float box_blur[25] = {1, 1, 1, 1, 1,
				// 						1, 1, 1, 1, 1,
				// 						1, 1, 1, 1, 1,
				// 						1, 1, 1, 1, 1,
				// 						1, 1, 1, 1, 1};

            // int blur_size = 5;

				fixed4 col = tex2D(_MainTex, vdata.uv);

				if (_BlurSize > 0) {
					float4 acc = float4(0, 0, 0, 0);
					for (int i = _BlurSize/2; i >= -_BlurSize/2; i--) {
						for (int j = _BlurSize/2; j >= -_BlurSize/2; j--) {
							float weight = 1.0;//box_blur[(i + 2) * 5 + (j + 2)];
							float4 pix = tex2D(_MainTex, float2(vdata.uv.x + j * _MainTex_TexelSize.x, vdata.uv.y + i * _MainTex_TexelSize.y));
							acc += pix * weight;
						}
					}
					col = acc / (_BlurSize * _BlurSize);
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
