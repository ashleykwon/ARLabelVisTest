Shader "Unlit/SeparableShader"
{
    Properties
    {
        

        _MainTex("Texture", 2D) = "white" {}
        _LabelTex("Label Image", 2D) = "white" {}

        _ColorMethod("Color Method", Int) = 1
        _Lamdba("lamdba", Range(0,1)) = 0.5

        _SampleKernelSize("Sample Blur Kernel Size", Range(0, 100)) = 15
        _SampleSigma("Sample Blur Sigma", Range(0, 100)) = 50
        _SampleBoost("Sample Brightness Multiplier", Range(0, 5)) = 1.0

        
        
        [MaterialToggle] _EnableShadow("Enable Shadow", Int) = 0
        _ShadowKernelSize("Shadow Blur Kernel Size", Range(0, 100)) = 50
        _ShadowSigma("Shadow Blur Sigma", Range(0, 100)) = 50
        _ShadowScale("Shadow Scale", Range(0.8, 1.05)) = 1.0
        _ShadowMultiplier("Shadow Intensity", Range(0, 2)) = 1.0

        
        
        [MaterialToggle] _EnableOutline("Enable Outline", Int) = 1
        

        
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        // Pass
        // {
        //     CGPROGRAM
        //     #pragma vertex vert
        //     #pragma fragment frag
        //     // make fog work
        //     #pragma multi_compile_fog

        //     #include "UnityCG.cginc"



        //     struct appdata
        //     {
        //         float4 vertex : POSITION;
        //         float2 uv : TEXCOORD0;
        //     };

        //     struct v2f
        //     {
        //         float2 uv : TEXCOORD0;
        //         UNITY_FOG_COORDS(1)
        //         float4 vertex : SV_POSITION;
        //     };

        //     sampler2D _MainTex;
        //     sampler2D _LabelTex;
        //     float4 _MainTex_ST;
        //     float4 _MainTex_TexelSize;

        //     float4 _LabelTex_TexelSize;
        //     float4 _LabelTex_ST;
        //     float _ShadowKernelSize;
        //     float _ShadowSigma;
        //     float _ShadowScale;
        //     float _Lamdba;
        //     int _EnableShadow;

        //     float gaussian1D(float x, float sigma) {
        //         float pi = 3.14159265359;
        //         return 1 / sqrt(2 * pi * sigma) * exp(-(x * x) / (2 * sigma));
        //     }

        //     v2f vert(appdata v)
        //     {
        //             v2f o;
        //             o.vertex = UnityObjectToClipPos(v.vertex);
        //             o.uv = TRANSFORM_TEX(v.uv, _LabelTex);
        //             UNITY_TRANSFER_FOG(o,o.vertex);
        //             return o;
        //     }

        //     fixed4 frag(v2f vdata) : SV_Target
        //     {

        //         float4 acc = float4(0, 0, 0, 0);
        //         for (int i = _ShadowKernelSize / 2; i >= -_ShadowKernelSize / 2; i--) {
        //                 float x = vdata.uv.x + i * _LabelTex_TexelSize.x;
        //                 float y = vdata.uv.y;
        //                 float2 coords = float2(x, y);
        //                 coords = (coords - 0.5) / _ShadowScale + 0.5;
        //                 float weight = gaussian1D(i, _ShadowSigma);
        //                 float4 pix = tex2D(_LabelTex, coords);
        //                 acc += pix * weight;
        //         }

        //         // sample the texture
        //         // apply fog
        //         //UNITY_APPLY_FOG(i.fogCoord, col);
        //         return acc;
        //         }
        //         ENDCG
        //     }
    

        //     GrabPass { "_BlurredLabelTex" }

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
             float _SampleKernelSize;
             float _SampleSigma;
             float _ShadowScale;

             /*float gaussian1d(float x, float sigma) {
                 float pi = 3.14159265359;
                 return 1 / sqrt(2 * pi * sigma) * exp(-(x * x) / (2 * sigma));
             }*/

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
                 for (int i = _SampleKernelSize / 2; i >= -_SampleKernelSize / 2; i--) {
                         float x = vdata.uv.x + i * _MainTex_TexelSize.x;
                         float y = vdata.uv.y;
                         float2 coords = float2(x, y);
						 float pi = 3.14159265359;
						 float weight = 1 / sqrt(2 * pi* _SampleSigma)* exp(-(i * i) / (2 * _SampleSigma));
                         // float weight = gaussian1D(i, _SampleSigma);
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
                // Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
                // #pragma exclude_renderers d3d11 gles
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
                float4 _LabelTex_ST;
                // sampler2D _BlurredLabelTex;
                // float4 _BlurredLabelTex_ST;
                // float4 _BlurredLabelTex_TexelSize;

                sampler2D _BlurredBGTex;
                float4 _MainTex_TexelSize;
                float4 _LabelTex_TexelSize;
                float4 _BlurredBGTex_TexelSize;
                float _SampleKernelSize;
                float _SampleSigma;
                float _ShadowKernelSize;
                float _ShadowSigma;
                float _SampleBoost;
                float _ShadowScale;
                float _ShadowMultiplier;
                float _Lamdba;
                int _EnableShadow;
                int _EnableOutline;
                int _ColorMethod;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _LabelTex);
                    UNITY_TRANSFER_FOG(o,o.vertex);
                    return o;
                }

                float gaussian1D(float x, float sigma) {
                    float pi = 3.14159265359;
                    return 1 / sqrt(2 * pi * sigma) * exp(-(x * x) / (2 * sigma));
                }

                // float sobel(sampler2D tex, float2 uv) {
                //     float2 delta = float2(0.0015, 0.0015);
                    
                //     float4 hr = float4(0, 0, 0, 0);
                //     float4 vt = float4(0, 0, 0, 0);

                //     float filter[3][3] = {
                //         {-1, 0, 1},
                //         {-2, 0, 2},
                //         {-1, 0, 1}
                //     };

                //     for (int i = -1; i <= 1; i++){
                //         for (int j = -1; j <= 1; j++){
                //             hr += tex2D(tex, (uv + float2(i, j) * delta)) *  filter[i + 1][j + 1];
                //             vt += tex2D(tex, (uv + float2(i, j) * delta)) *  filter[j + 1][i + 1];
                //         }
                //     }
                    
                //     return sqrt(hr * hr + vt * vt);
                // }

                // color conversion functions based off http://www.easyrgb.com/en/math.php
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

                float4 RGB2LAB(float4 RGB) 
                {
                    float R = RGB.r;
                    float G = RGB.g;
                    float B = RGB.b;

                    // reference values, D65/2°
                    float Xr = 95.047;  
                    float Yr = 100.0;
                    float Zr = 108.883;

                    float var_R = R; //(R / 255.0);
                    float var_G = G; //(G / 255.0);
                    float var_B = B; //(B / 255.0);

                    if (R > 0.04045) 
                        var_R = pow(((var_R + 0.055) / 1.055), 2.4);
                    else
                        var_R = var_R / 12.92;

                    if (var_G > 0.04045)
                        var_G = pow(((var_G + 0.055) / 1.055), 2.4);
                    else
                        var_G = var_G / 12.92;

                    if (var_B > 0.04045)
                        var_B = pow(((var_B + 0.055) / 1.055), 2.4);
                    else
                        var_B = var_B / 12.92;

                    var_R *= 100;
                    var_G *= 100;
                    var_B *= 100;

                    float X = var_R * 0.4124 + var_G * 0.3576 + var_B * 0.1805;
                    float Y = var_R * 0.2126 + var_G * 0.7152 + var_B * 0.0722;
                    float Z = var_R * 0.0193 + var_G * 0.1192 + var_B * 0.9505;

                    // now convert from XYZ to LAB

                    float var_X = X / Xr;
                    float var_Y = Y / Yr;
                    float var_Z = Z / Zr;

                    if (var_X > 0.008856)
                        var_X = pow(var_X, 1/3.0);
                    else
                        var_X = (7.787 * var_X) + (16.0 / 116.0);

                    if (var_Y > 0.008856)
                        var_Y = pow(var_Y, 1/3.0);
                    else
                        var_Y = (7.787 * var_Y) + (16.0 / 116.0);

                    if (var_Z > 0.008856)
                        var_Z = pow(var_Z, 1/3.0);
                    else
                        var_Z = (7.787 * var_Z) + (16.0 / 116.0);


                    float l = (116.0 * var_Y) - 16;
                    float a = 500.0 * (var_X - var_Y);
                    float b = 200.0 * (var_Y - var_Z); // Not sure why this was originally LAB[3]

                    return float4(l, a, b, RGB.a);
                } 

                float4 LAB2RGB(float4 LAB)
                {
                    float L = LAB[0];
                    float A = LAB[1];
                    float B = LAB[2];

                    // reference values, D65/2°
                    float Xr = 95.047;  
                    float Yr = 100.0;
                    float Zr = 108.883;

                    // first convert LAB to XYZ
                    float var_Y = (L + 16.0) / 116.0;
                    float var_X = A / 500 + var_Y;
                    float var_Z = var_Y - B / 200.0;

                    if (pow(var_Y, 3)  > 0.008856) 
                        var_Y = pow(var_Y, 3.0);
                    else
                        var_Y = (var_Y - 16 / 116) / 7.787;
                    if (pow(var_X, 3)  > 0.008856)
                        var_X = pow(var_X, 3.0);
                    else
                        var_X = (var_X - 16 / 116) / 7.787;
                    if (pow(var_Z, 3)  > 0.008856) 
                        var_Z = pow(var_Z, 3.0);
                    else
                        var_Z = (var_Z - 16.0 / 116.0) / 7.787;

                    float X = var_X * Xr;
                    float Y = var_Y * Yr;
                    float Z = var_Z * Zr;

                    // now convert XYZ to RGB
                    X /= 100.0;
                    Y /= 100.0;
                    Z /= 100.0;

                    float var_R = var_X *  3.2406 + var_Y * -1.5372 + var_Z * -0.4986;
                    float var_G = var_X * -0.9689 + var_Y *  1.8758 + var_Z *  0.0415;
                    float var_B = var_X *  0.0557 + var_Y * -0.2040 + var_Z *  1.0570;

                    if (var_R > 0.0031308) 
                        var_R = 1.055 * (pow(var_R, (1 / 2.4))) - 0.055;
                    else
                        var_R = 12.92 * var_R;
                    if (var_G > 0.0031308) 
                        var_G = 1.055 * (pow(var_G, (1 / 2.4))) - 0.055;
                    else
                        var_G = 12.92 * var_G;
                    if (var_B > 0.0031308) 
                        var_B = 1.055 * (pow(var_B, (1 / 2.4))) - 0.055;
                    else
                        var_B = 12.92 * var_B;

                    // ensure values are between 0 and 1
                    var_R = max(min(var_R, 1), 0);
                    var_G = max(min(var_G, 1), 0);
                    var_B = max(min(var_B, 1), 0);
                    return float4(var_R, var_G, var_B, LAB.a);
                }


                fixed4 frag(v2f vdata) : SV_Target //where colors are applied
                {

                    fixed4 col = tex2D(_MainTex, vdata.uv);
                    fixed4 textMatte = tex2D(_LabelTex, vdata.uv);


                    // if (_EnableShadow == 1) {
                    //     float4 acc = float4(0, 0, 0, 0);
                    //     for (int i = _ShadowKernelSize / 2; i >= -_ShadowKernelSize / 2; i--) {
                    //         float y = vdata.uv.y + i * _BlurredLabelTex_TexelSize.y;
                    //         float x = vdata.uv.x;
                    //         float2 coords = float2(x, y);
                    //         coords = (coords - 0.5) / _ShadowScale + 0.5;
                    //         float weight = gaussian1D(i, _ShadowSigma);
                    //         float4 pix = tex2D(_BlurredLabelTex, coords);
                    //         acc += pix * weight;
                    //     }

                    //     if (textMatte[0] == 0) {
                    //         col *= 1 - acc * _ShadowMultiplier;
                    //     }
                    // }

                    float4 bgSample = float4(0, 0, 0, 0); // Background pixel sampling
                    for (int i = _SampleKernelSize / 2; i >= -_SampleKernelSize / 2; i--) {
                        float y = vdata.uv.y + i * _BlurredBGTex_TexelSize.y;
                        float x = vdata.uv.x;
                        float2 coords = float2(x, y);
                        float weight = gaussian1D(i, _SampleSigma);
                        float4 pix = tex2D(_BlurredBGTex, coords);
                        bgSample += pix * weight;
                    }
                    bgSample *= _SampleBoost;
                    
                    // don't apply any changes if background 
                    if (textMatte[0] != 0)
                    { 
                        // Palette Method
                    // float4 flip_col = col;
                        if (_ColorMethod == 1)
                        {
                            /*  palette colors:
                            float4(0, 0, 0, 1), 
                            float4(0, 0, 1, 1),
                            // skip? float(0, 1, 0, 1) 
                            float4(0, 1, 1, 1),
                            float4(1, 0, 0, 1),
                            float4(1, 0, 1, 1),
                            float4(1, 1, 0, 1),
                            float4(1, 1, 1, 1)};
                            */
                            float maxDistSq = -1;
                            float4 paletteCol = float4(0, 0, 0, 1);
                            for (int i = 0; i < 8; i++){
                                if (i == 2) continue;
                                // extract first 3 bits of i
                                float r = i & 1; // mask i
                                float g = ((i & 2) >> 1); // mask i and shift right
                                float b = ((i & 4) >> 2); // mask i and shift right

                                float distSq = (bgSample.r - r) * (bgSample.r - r) 
                                            + (bgSample.g - g) * (bgSample.g - g)
                                            + (bgSample.b - b) * (bgSample.b - b);

                                if (distSq > maxDistSq){
                                    maxDistSq = distSq;
                                    paletteCol = float4(r, g, b, 1);
                                }
                            }
                            col = paletteCol; 
                        }
                        //Yuanbo's method
                        else if (_ColorMethod == 2) {
                            
                            float dummy = float4(1.0, 1.0, 1.0, 1.0);
                            float x = vdata.uv.x;
                            float y = vdata.uv.y;
                            float2 coords_flip = float2(x, y);
                            float4 flip_col = dummy - bgSample;//tex2D(_MainTex, coords_flip);
                            col = float4(flip_col[0], flip_col[1], flip_col[2], 1.0);
                        }
                        // HSV inversion
                        else if (_ColorMethod == 3) {
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
                            col = HSV2RGB(inverted_hsv);
                        }
                    //     // CIELAB inversion
                        else if (_ColorMethod == 4){
                            float4 lab = RGB2LAB(bgSample);
                            float l = lab[0];
                            float a = lab[1];
                            float b = lab[2];

                            if (l > 75 || l < 25){
                                l = 100 - l;
                            } else if (l > 50){
                                l = (100 - l) + 25;
                            } else {
                                l = (100 - l) - 25;
                            }

                            // l = 100 - l;
                            // a *= -1;
                            // b *= -1;
                            a = 62.1313548;// -81.1856371;
                            b = -95.50187772;//76.11578826;

                            float4 inverted_lab = float4(l, a, b, lab.a);
                            col = LAB2RGB(inverted_lab);
                        } 
                        //else {
                        //     return col; //float4(0, 0, 0, 0); // invalid color method
                        // }

                        if (_EnableOutline == 1)
                        {
                            // Applying sobel filter
                            float2 delta = float2(0.0015, 0.0015);
                            
                            float4 hr = float4(0, 0, 0, 0);
                            float4 vt = float4(0, 0, 0, 0);

                            float filter[3][3] = {
                                {-1, 0, 1},
                                {-2, 0, 2},
                                {-1, 0, 1}
                            };

                            for (int i = -1; i <= 1; i++){
                                for (int j = -1; j <= 1; j++){
                                    hr += tex2D(_LabelTex, (vdata.uv + float2(i, j) * delta)) *  filter[i + 1][j + 1];
                                    vt += tex2D(_LabelTex, (vdata.uv + float2(i, j) * delta)) *  filter[j + 1][i + 1];
                                }
                            }


                            float edges =  sqrt(hr * hr + vt * vt);
                            // sobel(_LabelTex, vdata.uv);

                            if(edges != 0){
                                if (col.r + col.g + col.b < 0.5){
                                    col = float4(1, 1, 1, 1);
                                } 
                                col = float4(0, 0, 0, 0);
                            }
                        }
                    }

                    


                // // sample the texture
                // // apply fog
                // //UNITY_APPLY_FOG(i.fogCoord, col);
                return col;//bgSample * _Lamdba + (1 - _Lamdba) * col
            }
            ENDCG
        }
        // GrabPass { "_LastShaderTex" } 

        // Pass 
        // {
        //     CGPROGRAM
        // // Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
        // // #pragma exclude_renderers d3d11 gles
        //     #pragma vertex vert
        //     #pragma fragment frag
        //     #include "UnityCG.cginc"



        //     struct appdata
        //     {
        //         float4 vertex : POSITION;
        //         float2 uv : TEXCOORD0;
        //     };

        //     struct v2f
        //     {
        //         float2 uv : TEXCOORD0;
        //         UNITY_FOG_COORDS(1)
        //         float4 vertex : SV_POSITION;
        //     };

        //     sampler2D _MainTex; 
        //     float4 _MainTex_ST;
        //     float4 _MainTex_ST_TexelSize;

        //     sampler2D _LastShaderTex;  
        //     float4 _LastShaderTex_ST;
           
        //     sampler2D _LabelTex; 
        //     float4 _LabelTex_ST; 
        //     int _EnableOutline;

        //     float _ShadowScale;

        //     v2f vert(appdata v)
        //     {
        //             v2f o;
        //             o.vertex = UnityObjectToClipPos(v.vertex);
        //             o.uv = TRANSFORM_TEX(v.uv, _LabelTex);
        //             UNITY_TRANSFER_FOG(o,o.vertex);
        //             return o;
        //     }

        //     float sobel (sampler2D tex, float2 uv) {
        //         float2 delta = float2(0.0015, 0.0015);
                
        //         float4 hr = float4(0, 0, 0, 0);
        //         float4 vt = float4(0, 0, 0, 0);

        //         float filter[3][3] = {
        //             {-1, 0, 1},
        //             {-2, 0, 2},
        //             {-1, 0, 1}
        //         };

        //         for (int i = -1; i <= 1; i++){
        //             for (int j = -1; j <= 1; j++){
        //                 hr += tex2D(tex, (uv + float2(i, j) * delta)) *  filter[i + 1][j + 1];
        //                 vt += tex2D(tex, (uv + float2(i, j) * delta)) *  filter[j + 1][i + 1];
        //             }
        //         }
                
        //         return sqrt(hr * hr + vt * vt);
        //     }

        //     float4 interpolate(sampler2D tex, v2f vdata){
                
        //         float4 col = float4(0, 0, 0, 0);

        //         float x1 = vdata.uv.x + 2 * _MainTex_ST_TexelSize.x;
        //         float y1 = vdata.uv.y + 0 * _MainTex_ST_TexelSize.y;
        //         float2 coords1 = float2(x1, y1);

        //         float x2 = vdata.uv.x - 2 * _MainTex_ST_TexelSize.x;
        //         float y2 = vdata.uv.y + 0 * _MainTex_ST_TexelSize.y;
        //         float2 coords2 = float2(x2, y2);

        //         float x3 = vdata.uv.x + 0 * _MainTex_ST_TexelSize.x;
        //         float y3 = vdata.uv.y + 2 * _MainTex_ST_TexelSize.y;
        //         float2 coords3 = float2(x3, y3);

        //         float x4 = vdata.uv.x + 0 * _MainTex_ST_TexelSize.x;
        //         float y4 = vdata.uv.y - 2 * _MainTex_ST_TexelSize.y;
        //         float2 coords4 = float2(x4, y4);

        //         float4 pix = tex2D(_LastShaderTex, coords1) * 0.25 
        //                     + tex2D(_LastShaderTex, coords2) * 0.25
        //                     + tex2D(_LastShaderTex, coords3) * 0.25
        //                     + tex2D(_LastShaderTex, coords4) * 0.25;

        //         return pix;

        //     }
            

        //     fixed4 frag(v2f vdata) : SV_Target
        //     {

        //         float4 lastshader_pix = tex2D(_LastShaderTex, vdata.uv);
        //         float4 label = tex2D(_LabelTex, vdata.uv);

        //         float2 coords = vdata.uv;
        //         coords = (coords - 0.5) / _ShadowScale + 0.5;

        //         float edges = sobel(_LabelTex, coords);

        //         if (edges != 0){
        //             return interpolate(_LastShaderTex, vdata);
        //         }

        //         return lastshader_pix;
        //     }
        //     ENDCG
        // }
    }
}
