Shader "Unlit/InverseCullCubeMapShader"
{// Copied from https://stackoverflow.com/questions/40834272/how-to-apply-cubemap-to-inverse-of-a-sphere-in-unity-3d and modified
    Properties
    {
        _CubeMap( "Cube Map", Cube ) = "white" {}
        _LabelCubeMap( "LabelCubeMap", Cube ) = "white" {}
        _SampleKernelSize("Sample Blur Kernel Size", Range(0, 100)) = 15
        _ColorMethod("Color Method", Int) = 3
        _SampleSigma("Sample Blur Sigma", Range(0, 100)) = 50
        _SampleBoost("Sample Brightness Multiplier", Range(0, 5)) = 1.0
        _UseInterpolation("UseInterpolation", Int) = 0
    }
    SubShader
    {
        // Tags { "RenderType"="Opaque" } // Doesn't work, although this is supposed to make a material double-sided in unlit shader
        // LOD 100 
        // Cull off

        Pass 
        {
            Tags { "DisableBatching" = "True" }
            // LOD 200
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Initialize variables        
            samplerCUBE _CubeMap;
            samplerCUBE _LabelCubeMap;
            float _SampleKernelSize;
            int _ColorMethod;
            float4 _MainTex_TexelSize;
            float _SampleSigma;
            float _SampleBoost;
            int _UseInterpolation;
        
            struct v2f 
            {
                float4 pos : SV_Position;
                half3 uv : TEXCOORD0;
            };
        
            v2f vert( appdata_img v )
            {

                v2f o;
                o.pos = UnityObjectToClipPos( v.vertex );
                o.uv = v.vertex.xyz * half3(1,1,1); // mirror so cubemap projects as expected

                return o;
            }

            // Color assignment helper functions
            float4 RGB2HSV(float4 rgb)
            {
                float R = rgb.r;
                float G = rgb.g;
                float B = rgb.b;
                float var_Min = min(R, min(G, B));    //Min. value of RGB
                float var_Max = max(R, max(G, B));    //Max. value of RGB
                float del_Max = var_Max - var_Min;             //Delta RGB value

                float H, S;
                float V = var_Max;

                if (del_Max == 0)
                {//This is a gray, no chroma...
                    H = 0;
                    S = 0;
                } 
                else 
                {                                  //Chromatic data...
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

            float4 HSV2RGB(float4 hsv)
            {
                float H = hsv[0];
                float S = hsv[1];
                float V = hsv[2];
                float R, G, B;
                if (S == 0)
                { // gray
                    R = V;
                    G = V;
                    B = V;
                } 
                else 
                {
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


            float gaussian1D(float x, float sigma) 
            {
                float pi = 3.14159265359;
                return 1 / sqrt(2 * pi * sigma) * exp(-(x * x) / (2 * sigma));
            }

            float4 function_f (int method, float4 bgSample){
                float4 col = float4(0,0,0,0);
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
                    else if (_ColorMethod == 2) 
                    {
                        float dummy = float4(1.0, 1.0, 1.0, 1.0);
                        float4 flip_col = dummy - bgSample;//tex2D(_MainTex, coords_flip);
                        col = float4(flip_col[0], flip_col[1], flip_col[2], 1.0);
                    }
                    // HSV inversion
                    else if (_ColorMethod == 3) 
                    {
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
                    // CIELAB inversion
                    else if (_ColorMethod == 4)
                    {
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
                    return col;
            }

            float hash(float sample_x, float sample_y, float offset){
                return (sample_x + sample_y + offset) * sample_x * (sample_y)  % 10;

            }

            // Color assignment
            fixed4 frag( v2f vdata ) : SV_Target 
            {
                fixed4 col = texCUBE(_CubeMap, vdata.uv);
                float3 rotationVec = float3(-1.0,-1.0,-1.0);
                fixed4 labelTex = texCUBE(_LabelCubeMap, vdata.uv*rotationVec); // Delete *(1,1,-1) in non-direct rendering (the one that uses the png file) version

                float offset = 78;

                float sample_x = (vdata.uv.x+1) * 100;
                float sample_y = (vdata.uv.y+1) * 100;

                float isSample = hash(sample_x, sample_y, offset);

                float4 bgSample = texCUBE(_CubeMap, vdata.uv); 

                _ColorMethod = 3;
                float _sampled_prob = 0.2;
                int _neighborhoodSize= 5;

                if (labelTex[3] != 0) // is a label pixel
                {
                    //this is a sampled pixel
                    if(isSample < _sampled_prob){
                        col = function_f(_ColorMethod, bgSample);
                    }else{
                        //this is a unsampled pixel
                        float top_r = 0.001;
                        float top_g = 0.001;
                        float top_b = 0.001;
                        float top_a = 0.001;
                        float bot= 0.001;

                        //go through its neighbors
                        for (int i = _neighborhoodSize / 2; i >= -_neighborhoodSize / 2; i--) {
                        for(int j = _neighborhoodSize / 2; j >= -_neighborhoodSize / 2; j--){
                            float x = vdata.uv.x + i * _MainTex_TexelSize.x;
                            float y = vdata.uv.y + j * _MainTex_TexelSize.y;
                                if (hash(x, y, offset)){
                                    half3 coords = half3(x, y, vdata.uv.z);
                                    float4 sample_col = texCUBE(_CubeMap, coords); 
                                    float4 f_sample_col = function_f(_ColorMethod, bgSample);
                                    float dist = float(i*i) + float(j*j) +1 ;

                                    top_r += f_sample_col[0] / dist;
                                    top_g += f_sample_col[1] / dist;
                                    top_b += f_sample_col[2] / dist;
                                    top_a += f_sample_col[3] / dist;
                                    bot += 1.0 / dist;
                                }   
                        }
                        }

                        if (top_r != 0.001){
                            top_r = top_r / bot;
                            top_g = top_g / bot;
                            top_b = top_b / bot;
                            top_a = top_a / bot;
                            col = float4(top_r,top_g,top_b,top_a);
                        }else{
                            //if we are so unlucky that no sample presents in the neighborhood
                           col = function_f(_ColorMethod, bgSample);
                        }
                    }
                }

                return col;
            }
            ENDCG
        }

        
        // GrabPass { "_LastShaderTex" } 
        // Pass 
        // {
        //     // LOD 100
        //     // Cull Back// Edit this line

        //     CGPROGRAM
        //     #pragma vertex vert
        //     #pragma fragment frag
        //     #include "UnityCG.cginc"

        //     // Initialize variables        
        //     samplerCUBE _CubeMap;
        //     samplerCUBE _LabelCubeMap;
        //     samplerCUBE _LastShaderTex;  
        //     // sampler2D _LastShaderTex;  

        //     float _SampleKernelSize;
        //     int _ColorMethod;
        //     float4 _MainTex_TexelSize;
        //     float _SampleSigma;
        //     float _SampleBoost;
        
        //     struct v2f 
        //     {
        //         float4 pos : SV_Position;
        //         half3 uv : TEXCOORD0;
        //     };
        
        //     v2f vert( appdata_img v )
        //     {

        //         v2f o;
        //         o.pos = UnityObjectToClipPos( v.vertex );
        //         o.uv = v.vertex.xyz; // mirror so cubemap projects as expected

        //         return o;
        //     }

            

        //     //Color assignment
        //     fixed4 frag( v2f vdata ) : SV_Target 
        //     {
        //         fixed4 cubemap_sample = texCUBE(_CubeMap, vdata.uv);
        //         float3 rotationVec = float3(-1.0,-1.0,-1.0);
        //         fixed4 labelTex = texCUBE(_LabelCubeMap, vdata.uv*rotationVec); // Delete *(1,1,-1) in non-direct rendering (the one that uses the png file) version
        //         fixed4 last_shader = texCUBE(_LastShaderTex, vdata.uv);

        //         // fixed4 last_shader = tex2D(_LastShaderTex, float2(vdata.uv.x, vdata.uv.y));

        //         fixed4 col  = last_shader;
        //         // col = float4(1,0,0,0);
        //         float sample_x = (vdata.uv.x+1) * 100;
        //         float sample_y = (vdata.uv.y+1) * 100;
        //         float offset = 78;
        //         float hash = (sample_x + sample_y + offset) * sample_x * (sample_y)  % 10;
        //         float _sampled_prob = 0.2;
        //         int _neighborhoodSize= 10;

        //         //the pixel is in the label
        //         if (labelTex.g != 0){
        //             //the pixel is unsampled
        //             if (hash > _sampled_prob){
        //                 float4 top = 1.0;
        //                 float4 bot = 1.0;
        //             for (int i = _neighborhoodSize / 2; i >= -_neighborhoodSize / 2; i--) {
        //                 for(int j = _neighborhoodSize / 2; j >= -_neighborhoodSize / 2; j--){
        //                 float x = vdata.uv.x + i * _MainTex_TexelSize.x;
        //                 float y = vdata.uv.y + j * _MainTex_TexelSize.y;
        //                 half3 coords = half3(x, y, vdata.uv.z);

        //                 fixed4 lastShader_sample = texCUBE(_LastShaderTex, vdata.uv);
        //                 float dist = float(i*i) + float(j*j) +1 ;
        //                 top += lastShader_sample / dist;
        //                 bot += 1.0 / dist;
        //                 }
        //             }
        //             col = top / bot;
        //             }
        //         }


        //         return col;
        //     }

        //     ENDCG
        
        // }
    }
}