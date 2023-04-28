Shader "Unlit/SeparableShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _LabelTex("Texture", 2D) = "white" {}
        _KernelSize("Blur Kernel Size", Range(0, 100)) = 50
        _Sigma("Blur Sigma", Range(0, 100)) = 50
        _ShadowScale("Shadow Scale", Range(0.8, 1.05)) = 1.0
        _ShadowMultiplier("Shadow Multiplier", Range(0, 2)) = 1.0
        _Lamdba("lamdba", Range(0,1)) = 0.5
        [MaterialToggle] _EnableShadow("Enable Shadow", Int) = 1
        _ColorMethod("Color Method", Int) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
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
            sampler2D _LabelTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            float4 _LabelTex_TexelSize;
            float4 _LabelTex_ST;
            float _KernelSize;
            float _Sigma;
            float _ShadowScale;
            float _Lamdba;
            int _EnableShadow;

            float gaussian1D(float x, float sigma) {
                float pi = 3.14159265359;
                return 1 / sqrt(2 * pi * sigma) * exp(-(x * x) / (2 * sigma));
            }

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

                float4 acc = float4(0, 0, 0, 0);
                for (int i = _KernelSize / 2; i >= -_KernelSize / 2; i--) {
                        float x = vdata.uv.x + i * _LabelTex_TexelSize.x;
                        float y = vdata.uv.y;
                        float2 coords = float2(x, y);
                        coords = (coords - 0.5) / _ShadowScale + 0.5;
                        float weight = gaussian1D(i, _Sigma);
                        float4 pix = tex2D(_LabelTex, coords);
                        acc += pix * weight;
                }

                // sample the texture
                // apply fog
                //UNITY_APPLY_FOG(i.fogCoord, col);
                return acc;
                }
                ENDCG
            }

            GrabPass { "_BlurredLabelTex" }

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
            sampler2D _LabelTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            float4 _LabelTex_TexelSize;
            float4 _LabelTex_ST;
            float _KernelSize;
            float _Sigma;
            float _ShadowScale;

            float gaussian1D(float x, float sigma) {
                float pi = 3.14159265359;
                return 1 / sqrt(2 * pi * sigma) * exp(-(x * x) / (2 * sigma));
            }

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

                float4 acc = float4(0, 0, 0, 0);
                for (int i = _KernelSize / 2; i >= -_KernelSize / 2; i--) {
                        float x = vdata.uv.x + i * _MainTex_TexelSize.x;
                        float y = vdata.uv.y;
                        float2 coords = float2(x, y);
                        float weight = gaussian1D(i, _Sigma);
                        float4 pix = tex2D(_MainTex, coords);
                        acc += pix * weight;
                }

                // sample the texture
                // apply fog
                //UNITY_APPLY_FOG(i.fogCoord, col);
                return acc;
                }
                ENDCG
            }

            GrabPass { "_BlurredBGTex" }

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
                sampler2D _LabelTex;
                sampler2D _BlurredLabelTex;
                float4 _BlurredLabelTex_ST;
                float4 _BlurredLabelTex_TexelSize;

                sampler2D _BlurredBGTex;
                float4 _MainTex_TexelSize;
                float4 _LabelTex_TexelSize;
                float4 _BlurredBGTex_TexelSize;
                float _KernelSize;
                float _Sigma;
                float _ShadowScale;
                float _ShadowMultiplier;
                float _Lamdba;
                int _EnableShadow;
                int _EnableLambda;
                int _ColorMethod;



                float gaussian1D(float x, float sigma) {
                    float pi = 3.14159265359;
                    return 1 / sqrt(2 * pi * sigma) * exp(-(x * x) / (2 * sigma));
                }


                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _BlurredLabelTex);
                    UNITY_TRANSFER_FOG(o,o.vertex);
                    return o;
                }

                float4 RGB2HSV(float4 rgb){
                    float R = rgb.r;
                    float G = rgb.g;
                    float B = rgb.b;
                    float var_Min = min(R, min(G, B));    //Min. value of RGB
                    float var_Max = max(R, max(G, B));    //Max. value of RGB
                    float del_Max = var_Max - var_Min;             //Delta RGB value

                    float H, S;
                    float V = var_Max;

                    if (del_Max == 0){//This is a gray, no chroma...
                        H = 0;
                        S = 0;
                    } else {                                  //Chromatic data...
                        S = del_Max / var_Max;

                        float del_R = (((var_Max - R) / 6) + (del_Max / 2)) / del_Max;
                        float del_G = (((var_Max - G) / 6) + (del_Max / 2)) / del_Max;
                        float del_B = (((var_Max - B) / 6) + (del_Max / 2)) / del_Max;

                        if (R == var_Max) 
                            H = del_B - del_G;
                        else if (G == var_Max)
                            H = (1 / 3) + del_R - del_B;
                        else if (B == var_Max)
                            H = (2 / 3) + del_G - del_R;

                        if (H < 0) 
                            H += 1;
                        if (H > 1)
                            H -= 1;

                    }
                    return float4(H, S, V, rgb.a);
                }

                float4 HSV2RGB(float4 hsv){
                    float H = hsv[0];
                    float S = hsv[1];
                    float V = hsv[2];
                    float R, G, B;
                    if (S == 0){ // gray
                        R = V;
                        G = V;
                        B = V;
                    } else {
                        float var_h = H * 6;
                        if (var_h == 6) 
                            var_h = 0;     //H must be < 1

                        float var_i = (int) var_h; //Or ... var_i = floor(var_h)
                        float var_1 = V * (1 - S);
                        float var_2 = V * (1 - S * (var_h - var_i));
                        float var_3 = V * (1 - S * (1 - (var_h - var_i)));

                        if (var_i == 0) { 
                            R = V; 
                            G = var_3;
                            B = var_1;
                        } else if (var_i == 1) { 
                            R = var_2;
                            G = V;
                            B = var_1;
                        } else if (var_i == 2) { 
                            R = var_1;
                            G = V;
                            B = var_3;
                        } else if (var_i == 3) {
                            R = var_1;
                            G = var_2;
                            B = V;
                        } else if (var_i == 4) {
                            R = var_3;
                            G = var_1;
                            B = V;
                        } else { 
                            R = V;
                            G = var_1;
                            B = var_2;
                        }
                    }

                    return float4(R, G, B, hsv.a);
                }

                fixed4 frag(v2f vdata) : SV_Target
                {

                fixed4 col = tex2D(_MainTex, vdata.uv);
                fixed4 textMatte = tex2D(_LabelTex, vdata.uv);

                if (_EnableShadow == 1) {
                    float4 acc = float4(0, 0, 0, 0);
                    for (int i = _KernelSize / 2; i >= -_KernelSize / 2; i--) {
                        float y = vdata.uv.y + i * _BlurredLabelTex_TexelSize.y;
                        float x = vdata.uv.x;
                        float2 coords = float2(x, y);
                        coords = (coords - 0.5) / _ShadowScale + 0.5;
                        float weight = gaussian1D(i, _Sigma);
                        float4 pix = tex2D(_BlurredLabelTex, coords);
                        acc += pix * weight;
                    }

                    if (textMatte.r == 0) {
                        col *= 1 - acc * _ShadowMultiplier;
                    }
                }

                float4 bgSample = float4(0, 0, 0, 0);
                for (int i = _KernelSize / 2; i >= -_KernelSize / 2; i--) {
                    float y = vdata.uv.y + i * _BlurredBGTex_TexelSize.y;
                    float x = vdata.uv.x;
                    float2 coords = float2(x, y);
                    float weight = gaussian1D(i, _Sigma);
                    float4 pix = tex2D(_BlurredBGTex, coords);
                    bgSample += pix * weight;
                }

                if (_ColorMethod == 1){
                    //Yuanbo's method
                    float dummy = float4(1.0, 1.0, 1.0, 1.0);
                    float x = vdata.uv.x;
                    float y = vdata.uv.y;
                    float2 coords_flip = float2(x, y);
                    float4 flip_col = dummy - tex2D(_MainTex, coords_flip);
                    flip_col = float4(flip_col[0], flip_col[1], flip_col[2], 1.0);

                    if(textMatte.r != 0){
                        col = bgSample* _Lamdba + (1-_Lamdba) * flip_col;
                    }
                }

                // HSV inversion
                else if (_ColorMethod == 2){
                    float4 hsv = RGB2HSV(bgSample);
                    float h = hsv[0];
                    float s = hsv[1];
                    float v = hsv[2];
                    h += 0.5;
                    h %= 1.0;
                    if (v > 0.5){
                        v = 0;
                    } else {
                        v = 1;
                    }

                    float4 inverted_hsv = float4(h, s, v, hsv.a);
                    float4 flip_col = HSV2RGB(inverted_hsv);
                    if(textMatte.r != 0){
                        col = bgSample* _Lamdba + (1-_Lamdba) * flip_col;
                    }
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