Shader "Unlit/InverseCullCubeMapShader"
{// Copied from https://stackoverflow.com/questions/40834272/how-to-apply-cubemap-to-inverse-of-a-sphere-in-unity-3d and modified
    Properties
    {
        _CubeMap( "Cube Map (RGBA)", Cube ) = "white" {}
        _LabelCubeMap( "LabelCubeMap", Cube ) = "white" {}
        _BillboardCubeMap("BillboardCubeMap", Cube) = "white" {}
        _ModeCubeMap("ModeCubeMap", Cube) = "white" {}
        // _ShadowCubeMap("ShadowCubeMap", Cube) = "white" {}
        _SampleKernelSize("Sample Blur Kernel Size", Range(0, 100)) = 15
        _ColorMethod("Color Method", Int) = 3
        _SampleSigma("Sample Blur Sigma", Range(0, 100)) = 50
        _SampleBoost("Sample Brightness Multiplier", Range(0, 5)) = 1.0
        _UseInterpolation("UseInterpolation", Int) = 0

        _EnableOutline("Enable Outline", Int) = 1

        _BillboardColorMethod("Billboard Color Method", Int) = 1
        _BillboardLightnessContrastThreshold("Billboard lightness contrast threshold", Range(0,1)) = 0.5

        _GranularityMethod("Granularity Method", Int) = 0 // 0 for default, 1 for background
        

        _Background_sum_r("Background_sum_r", Range(0,1)) = 0.1
        _Background_sum_g("Background_sum_g", Range(0,1)) = 0.1
        _Background_sum_b("Background_sum_b", Range(0,1)) = 0.1

        _OpacityLevel("Label opacity level", Range(0, 1)) = 1

        _CIELAB_LookupTable("CIELAB lookup table", 3D) = "white" {}
        _LookupTableStepSize("Lookup table step size", Int) = 4

        _cielab_r("cielab_r", Range(0,1)) = 0.1
        _cielab_g("cielab_g", Range(0,1)) = 0.1
        _cielab_b("cielab_b", Range(0,1)) = 0.1
    }

    SubShader
    {
        // To be able to apply transparency to the sphere
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        LOD 100

    // Label color assignment + outline + billboard
        Pass 
        {
            // Cull off
            // Tags { "DisableBatching" = "True" }
            Tags {"Queue"="Transparent"}


            CGPROGRAM
            #pragma vertex vert alpha
            #pragma fragment frag alpha
            #include "UnityCG.cginc"

            // Initialize variables        
            samplerCUBE _CubeMap;
            samplerCUBE _LabelCubeMap;
            samplerCUBE _BillboardCubeMap;
            samplerCUBE _ModeCubeMap;
            float _SampleKernelSize;
            int _ColorMethod;
            float4 _MainTex_TexelSize;
            float _SampleSigma;
            float _SampleBoost;
            int _UseInterpolation;

            // Outline-related variables
            int _EnableOutline;

            // Billboard-related variables
            int _BillboardColorMethod;
            float _BillboardLightnessContrastThreshold;

            int _GranularityMethod;

            float _OpacityLevel;

            //rotation matrix - a buffer with 16 floats
            StructuredBuffer<float> rotation_matrix;
            
             //sum_all result
            StructuredBuffer<float> sum_all_results;

            float _Background_sum_r;
            float _Background_sum_g;
            float _Background_sum_b;

            float _cielab_r;
            float _cielab_g;
            float _cielab_b;

            sampler3D _CIELAB_LookupTable;
            int _LookupTableStepSize;
           
            struct v2f 
            {
                float4 pos : SV_Position;
                half3 uv : TEXCOORD0;
            };
        
            v2f vert( appdata_img v )
            {
                
                v2f o;
                o.pos = UnityObjectToClipPos( v.vertex );
                o.uv = v.vertex;
                // .xyz * half3(1,1,1); 

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
                float V = var_Max*100;

                if (del_Max == 0)
                {//This is a gray, no chroma...
                    H = 0;
                    S = 0;
                } 
                else 
                {                                  //Chromatic data...
                    S = (del_Max / var_Max)*100;

                    // float del_R = (((var_Max - R) / 6) + (del_Max / 2)) / del_Max;
                    // float del_G = (((var_Max - G) / 6) + (del_Max / 2)) / del_Max;
                    // float del_B = (((var_Max - B) / 6) + (del_Max / 2)) / del_Max;

                    if (R == var_Max) {
                        H = (60 * ((G-B)/del_Max) + 360) % 360;}
                    else if (G == var_Max){
                        H = (60 * ((B-R)/del_Max) + 120) % 360;}
                    else if (B == var_Max){
                        H = (60 * ((R-G)/del_Max) + 240) % 360;}

                }
                return float4(H, S, V, rgb.a);
            }

            float4 HSV2RGB(float4 hsv)
            {
                float H = hsv[0];
                float S = hsv[1];
                float V = hsv[2];

                float s = S/100;
                float v = V/100;
                float C = s*v;
                float X = C*(1-abs((H/60.0)%2 -1 ));
                float m = v - C;
                float r=0;
                float g=0;
                float b=0;

                if(H >= 0 && H <60){
                    r = C;
                    g = X;
                    b=0;
                }

                else if(H>=60 && H<120){
                    r=X;
                    g=C;
                    b=0;
                }

                else if(H>=120 && H<180){
                    r =0;
                    g=C;
                    b=X;
                }

                else if(H>=180 && H<240){
                    r=0;
                    g=X;
                    b=C;
                }

                else if(H>=240 && H<300){
                    r=X;
                    g=0;
                    b=C;
                }

                else{
                    r=C;
                    g=0;
                    b=X;
                }

                r = r+m;
                g = g+m;
                b = b+m;


                return float4(r, g, b, hsv.a);
            }

            float4 RGB2LAB(float4 RGB) // probably referenced from here:
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

            float4 RGB2HSL(float4 rgb) // based on https://en.wikipedia.org/wiki/HSL_and_HSV
            {
                float4 HSL = float4(0,0,0,rgb.a);
                float4 HSV = RGB2HSV(rgb);
                HSL[0] = HSV[0];
                HSL[2] = 1/2*min(min(rgb[0], rgb[1]), rgb[2]) + 1/2*max(max(rgb[0], rgb[1]), rgb[2]);
                if (HSL[2] != 0 && HSL[2] != 1)
                { 
                    HSL[1] = (HSV[2] - HSL[2])/min(HSL[2], 1-HSL[2]);
                } 
                return HSL;
            }

            float4 HSL2RGB(float4 HSL)
            {
                float4 RGBintermediate = float4(0,0,0,HSL[3]);
                float4 RGBfinal = float4(0,0,0,HSL[3]);

                float chroma = (1 - abs(2*HSL[2]-1))*HSL[1];
                float HPrime = HSL[0]/60;
                float x = chroma*(1 - abs(HPrime%2 - 1));

                if (HPrime >= 0 && HPrime < 1)
                {
                    RGBintermediate[0] = chroma;
                    RGBintermediate[1] = x;
                }
                else if (HPrime >= 1 && HPrime < 2)
                {
                    RGBintermediate[0] = x;
                    RGBintermediate[1] = chroma;
                }
                else if (HPrime >= 2 && HPrime < 3)
                {
                    RGBintermediate[1] = chroma;
                    RGBintermediate[2] = x;
                }
                else if (HPrime >= 3 && HPrime < 4)
                {
                    RGBintermediate[1] = x;
                    RGBintermediate[2] = chroma;
                }
                else if (HPrime >= 4 && HPrime > 5)
                {
                    RGBintermediate[0] = x;
                    RGBintermediate[2] = chroma;
                }
                else if (HPrime >= 5 && HPrime > 6)
                {
                    RGBintermediate[0] = chroma;
                    RGBintermediate[2] = x;
                }

                float m = HSL[2] - chroma/2;
                RGBfinal[0] = RGBintermediate[0] + m;
                RGBfinal[1] = RGBintermediate[1] + m;
                RGBfinal[2] = RGBintermediate[2] + m;

                return RGBfinal;
            }

            float4 interpolation2D(float4 lowerBound, float4 upperBound, float difference){
                return float4(lowerBound[0]*(1 - difference) + difference*upperBound[0], lowerBound[1]*(1 - difference) + difference*upperBound[1], lowerBound[2]*(1 - difference) + difference*upperBound[2], 1.0);
            }

            float4 trilinearInterpolation(int rIdx, int gIdx, int bIdx, int lookupTableStepSize){
                float rLowerBound = float((rIdx / lookupTableStepSize) * lookupTableStepSize)/255;
                float rUpperBound = float(rLowerBound + lookupTableStepSize)/255; 

                float gLowerBound = float((gIdx / lookupTableStepSize) * lookupTableStepSize)/255;
                float gUpperBound = float(gLowerBound + lookupTableStepSize)/255; 

                float bLowerBound = float((bIdx / lookupTableStepSize) * lookupTableStepSize)/255;
                float bUpperBound = float(bLowerBound + lookupTableStepSize)/255; 

                float rDiff = (rIdx - rLowerBound)/(rUpperBound - rLowerBound);
                float gDiff = (gIdx - gLowerBound)/(gUpperBound - gLowerBound);
                float bDiff = (bIdx - bLowerBound)/(bUpperBound - bLowerBound);

                float4 C000 = tex3D(_CIELAB_LookupTable, float3(rLowerBound, gLowerBound, bLowerBound)); // c000
                float4 C100 = tex3D(_CIELAB_LookupTable, float3(rUpperBound, gLowerBound, bLowerBound)); // c100
                float4 C010 = tex3D(_CIELAB_LookupTable, float3(rLowerBound, gUpperBound, bLowerBound)); // c010
                float4 C110 = tex3D(_CIELAB_LookupTable, float3(rUpperBound, gUpperBound, bLowerBound)); // c110
                float4 C001 = tex3D(_CIELAB_LookupTable, float3(rLowerBound, gLowerBound, bUpperBound)); // c001
                float4 C101 = tex3D(_CIELAB_LookupTable, float3(rUpperBound, gLowerBound, bUpperBound)); // c101
                float4 C011 = tex3D(_CIELAB_LookupTable, float3(rLowerBound, gUpperBound, bUpperBound)); // c011
                float4 C111 = tex3D(_CIELAB_LookupTable, float3(rUpperBound, gUpperBound, bUpperBound)); // c111

                float4 C00 = interpolation2D(C000, C100, rDiff);
                float4 C01 = interpolation2D(C001, C101, rDiff);
                float4 C10 = interpolation2D(C010, C110, rDiff);
                float4 C11 = interpolation2D(C011, C111, rDiff);

                float4 C0 = interpolation2D(C00, C10, gDiff);
                float4 C1 = interpolation2D(C10, C11, gDiff);

                float4 interpolatedColor = interpolation2D(C0, C1, bDiff);

                // float4 interpolatedColor = float4(C[0], C[1], C[2], 1);
                return interpolatedColor;
            }


            float gaussian1D(float x, float sigma) // based on https://mccormickml.com/2013/08/15/the-gaussian-kernel/
            {
                float pi = 3.14159265359;
                return 1 / sqrt(2 * pi * sigma) * exp(-(x * x) / (2 * sigma));
                // return 1 / (sigma*sqrt(2 * pi)) * exp(-1*(x * x) / (2 * (sigma*sigma)));
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
                //Yuanbo's method (RGB inversion)
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
                    h += 180;
                    h %= 360;

                    v = 100 - v;
                    
                    float4 inverted_hsv = float4(h, s, v, 1.0);
                    col = HSV2RGB(inverted_hsv);
                }
                // CIELAB inversion
                else if (_ColorMethod == 4)
                {
                    int R_idx = int(bgSample[0]*255);
                    int G_idx = int(bgSample[1]*255);
                    int B_idx = int(bgSample[2]*255);
                    if (R_idx%4 == 0 && G_idx%4 == 0 && B_idx%4 == 0){
                        col = tex3D(_CIELAB_LookupTable, bgSample.rgb); // assumes that the lookup table has no missing values
                        col.a = 1;
                    }
                    else{
                        col = trilinearInterpolation(R_idx, G_idx, B_idx, _LookupTableStepSize);
                        col.a = 1;
                    }
                } 
                // Green Label
                else if (_ColorMethod == 5){
                    col = float4(0.0, 1.0, 0.0, 1.0);
                }
                
                // No label
                else if (_ColorMethod == 6){
                    col = float4(0.0, 0.0, 0.0, 0.0);
                }
                return col;
            }

            // For interpolation
            float hash(float sample_x, float sample_y, float offset){
                return (sample_x + sample_y + offset) * sample_x * (sample_y)  % 10;
            }

            // For label and billboard separation
            float4 separateBillboardLabelMode(float4 nonBackgroundPixVal, int componentIdx)
            {
                float4 finalPixVal = float4(0,0,0,0);
                if (componentIdx == 0) // should be a label
                {
                    if (nonBackgroundPixVal[0] == 1 && nonBackgroundPixVal[1] == 1 && nonBackgroundPixVal[2] == 1 && nonBackgroundPixVal[3] == 1)
                    {
                        finalPixVal = float4(1,1,1,1);
                    }
                }
                else
                {
                    if (nonBackgroundPixVal[0] == 1 && nonBackgroundPixVal[1] == 0 && nonBackgroundPixVal[2] == 1 && nonBackgroundPixVal[3] == 1)
                    {
                        finalPixVal = float4(1,0,1,1);
                    }
                }
                return finalPixVal;
            }


            // Color assignment
            fixed4 frag( v2f vdata ) : SV_Target 
            {
                fixed4 col = texCUBE(_CubeMap, vdata.uv);
                // float3 rotationVec = float3(-1.0,-1.0,-1.0);
                fixed4 labelTex = texCUBE(_LabelCubeMap, vdata.uv); // Delete rotationVec in non-direct rendering (the one that uses the png file) version
                labelTex = separateBillboardLabelMode(labelTex, 0);

                float4 bgSample = texCUBE(_CubeMap, vdata.uv); 
                if (_GranularityMethod != 0) 
                {
                    // if we're not using the per-pixel assignment mode, then use average background or label area pixel value
                    bgSample = float4(_Background_sum_r, _Background_sum_g, _Background_sum_b, 1);
                }


                float _sampled_prob = 0.2;
                int _neighborhoodSize= 5;

                fixed4 modeTex = texCUBE(_ModeCubeMap,vdata.uv);
                modeTex = separateBillboardLabelMode(modeTex, 2);

                // //Label color and outline assignment
                if (labelTex[3] != 0) // is a label pixel
                {   
                    col = function_f(_ColorMethod, bgSample);
                   
                    // set the opacity level
                    col[3] = _OpacityLevel;
                    if (_ColorMethod == 6){
                        col[3] = 0.0;
                    }

                    // Apply outline if selected
                    if (_EnableOutline == 1)
                    {
                        // Applying sobel filter
                        float2 delta = float2(0.0075, 0.0015);
                        // float2 delta = float2(1,1);
                        
                        float4 hr = float4(0, 0, 0, 0);
                        float4 vt = float4(0, 0, 0, 0);

                        float filter[3][3] = {
                            {-1, 0, 1},
                            {-2, 0, 2},
                            {-1, 0, 1}
                        };

                        for (int i = -1; i <= 1; i++){
                            for (int j = -1; j <= 1; j++){
                                float2 xyCoords = float2(vdata.uv.x, vdata.uv.y) + float2(i, j) * delta;
                                float3 coords = float3(xyCoords.x, xyCoords.y, vdata.uv.z);
                                float4 pix = texCUBE(_LabelCubeMap, coords);
                                if (pix[0] == 1 && pix[1] == 1 && pix[2] == 1 && pix[3] == 1) // is a label pixel
                                {
                                    hr += pix *  filter[i + 1][j + 1];
                                    vt += pix *  filter[j + 1][i + 1];
                                }
                                else
                                {
                                    hr += float4(0,0,0,0)*  filter[i + 1][j + 1];
                                    vt += float4(0,0,0,0)*  filter[j + 1][i + 1];
                                }
                                // hr += texCUBE(_LabelCubeMap, coords*rotationVec) *  filter[i + 1][j + 1];
                                // vt += texCUBE(_LabelCubeMap, coords*rotationVec) *  filter[j + 1][i + 1];
                            }
                        }


                        float edges =  sqrt(hr * hr + vt * vt);
                        // sobel(_LabelTex, vdata.uv);

                        
                        if(edges != 0){ //Outline the edges
                            if (col.r + col.g + col.b < 0.5){
                                col = float4(1, 1, 1, _OpacityLevel); // White outline if low grayscale value
                            }else{
                            col = float4(0, 0, 0, _OpacityLevel); // black outline if high grayscale value
                            } 
                        }
                    }
                }
                
                // Render label color mode
                if (modeTex[3] != 0)
                {
                    col[0] = 1.0;
                    col[1] = 0.0;
                    col[2] = 1.0;
                    col[3] = 1.0;
                }

                return col;
            }
            ENDCG
        }

    }
}