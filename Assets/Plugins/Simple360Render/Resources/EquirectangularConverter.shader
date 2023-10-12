// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Credit: https://github.com/Mapiarz/CubemapToEquirectangular/blob/master/Assets/Shaders/CubemapToEquirectangular.shader

Shader "Hidden/I360CubemapToEquirectangular"
{
	Properties
	{
		// _MainTex ("Cubemap (RGB)", CUBE) = "" {}
		_MainTex("Texture", 2D) = "white" {}
		_PaddingX ("Padding X", Float) = 0.0
	}

	Subshader
	{
		Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest
				//#pragma fragmentoption ARB_precision_hint_nicest
				#include "UnityCG.cginc"

				#define PI    3.141592653589793
				#define TWOPI 6.283185307179587

				// struct v2f
				// {
				// 	float4 pos : POSITION;
				// 	float2 uv : TEXCOORD0;
				// };
		
				// samplerCUBE _MainTex;
				sampler2D _MainTex;
				float4 _MainTex_ST;
				float _PaddingX;

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

				// v2f vert(appdata v)
				// {
				// 	v2f o;
				// 	o.vertex = UnityObjectToClipPos(v.vertex);
				// 	o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				// 	UNITY_TRANSFER_FOG(o,o.vertex);
				// 	return o;
				// }
				
				v2f vert(appdata_img v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					// o.uv = (v.texcoord.xy + float2(_PaddingX,0)) * float2(TWOPI, PI);
					o.uv =  v.vertex.xy * half2(1,1);
					return o;
				}
		
				fixed4 frag(v2f i) : COLOR 
				{
					// float theta = i.uv.y;
					// float phi = i.uv.x;
					// float2 unit = float2(0,0);

					// unit.x = sin(phi) * sin(theta) * -1;
					// unit.y = cos(theta) * -1;
					// unit.z = cos(phi) * sin(theta) * -1;

					// return texCUBE(_MainTex, unit);
					return tex2D(_MainTex, i.uv);
				}
			ENDCG
		}
	}
	Fallback Off
}