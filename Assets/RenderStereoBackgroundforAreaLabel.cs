using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Networking;
using System.IO;
using System;
using TMPro;
using System.Text;
using System.Linq;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using UnityEditor;

public class RenderStereoBackgroundforAreaLabel : MonoBehaviour
{
    public GameObject backgroundAndLabelSphere;
    public GameObject labelSphere;
    public GameObject player;
    Material backgroundAndLabelSphereMaterial;
    Camera backgroundScreenshotCamera;
    Camera labelScreenshotCamera;
    Camera centerEyeCamera;

    Texture2D maskedBackgroundAsTex2D; // this doesn't need to be assigned outside of this code
    public Texture2D LabelMask;
    public Texture2D EquirectangularBackground;
    public Texture2D BackgroundMask;

    RenderTexture backgroundRT;
    int w;
    int h;

    private Queue<AsyncGPUReadbackRequest> requests = new Queue<AsyncGPUReadbackRequest>();
    private Color32[] backgroundDataBuffer;

    public ComputeShader cShaderForMask;
    int maskBuffer_kernelID;
    List<Color32> CandidateCIELABVals;
    float[] CandidateCIELABValsAsArray;
    public Texture3D LookupTable;

    List<List<Color32>> ColorHistogram;
    List<Vector3> ColorHistogramBins;
    // public Texture2D CenterMarkedLabelTexture;
    // int[] labelCenterCoord;
    Color32 backgroundAvg;
    Color32 labelAvg;
    public bool backgroundOrLableChanged;


    void toTexture2D(RenderTexture rTex, Texture2D screenshot, int width, int height)
    {
        RenderTexture.active = rTex;
        screenshot.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        screenshot.Apply();
        // RenderTexture.active = null;
    }

    Color32 LAB2RGB(Vector3 LAB)
    {
        double L = LAB[0];
        double A = LAB[1];
        double B = LAB[2];

        // reference values, D65/2°
        double Xr = 95.047;  
        double Yr = 100.0;
        double Zr = 108.883;

        // first convert LAB to XYZ
        double var_Y = (L + 16.0) / 116.0;
        double var_X = A / 500 + var_Y;
        double var_Z = var_Y - B / 200.0;

        if (Math.Pow(var_Y, 3)  > 0.008856){
            var_Y = Math.Pow(var_Y, 3.0);
        }  
        else{
            var_Y = (var_Y - 16 / 116) / 7.787;
        }
            
        if (Math.Pow(var_X, 3)  > 0.008856){
            var_X = Math.Pow(var_X, 3.0);
        }
        else{
            var_X = (var_X - 16 / 116) / 7.787;
        }
            
        if (Math.Pow(var_Z, 3)  > 0.008856){
            var_Z = Math.Pow(var_Z, 3.0);
        } 
        else{
            var_Z = (var_Z - 16.0 / 116.0) / 7.787;
        }
            
        double X = var_X * Xr;
        double Y = var_Y * Yr;
        double Z = var_Z * Zr;

        // now convert XYZ to RGB
        X /= 100.0;
        Y /= 100.0;
        Z /= 100.0;

        double var_R = var_X *  3.2406 + var_Y * -1.5372 + var_Z * -0.4986;
        double var_G = var_X * -0.9689 + var_Y *  1.8758 + var_Z *  0.0415;
        double var_B = var_X *  0.0557 + var_Y * -0.2040 + var_Z *  1.0570;

        if (var_R > 0.0031308){
            var_R = 1.055 * (Math.Pow(var_R, (1 / 2.4))) - 0.055;
        } 
        else{
            var_R = 12.92 * var_R;
        }
            
        if (var_G > 0.0031308){
            var_G = 1.055 * (Math.Pow(var_G, (1 / 2.4))) - 0.055;
        } 
        else{
            var_G = 12.92 * var_G;
        }
            
        if (var_B > 0.0031308){
            var_B = 1.055 * (Math.Pow(var_B, (1 / 2.4))) - 0.055;
        } 
            
        else{
            var_B = 12.92 * var_B;
        }

        // ensure values are between 0 and 255
        int finalR = (int) (Math.Max(Math.Min(var_R*255, 255), 0));
        int finalG = (int) (Math.Max(Math.Min(var_G*255, 255), 0));
        int finalB = (int) (Math.Max(Math.Min(var_B*255, 255), 0));

        Color32 RGB = new Color32((byte) (finalR), (byte) (finalG), (byte) (finalB), 255);
        return RGB;
    }

    void ApplyMask(RenderTexture backgroundRT, Texture2D EquirectangularBackground, Texture2D Mask, int w, int h)
    {
        // Using compute shader, mask the background so that it only contains pixels under the area label or pixels at a certain distance from the center of the label
        cShaderForMask.SetInt("image_width", w);
        cShaderForMask.SetInt("image_height", h);
        cShaderForMask.SetTexture(maskBuffer_kernelID, "backgroundScreenshotForSum", EquirectangularBackground);
        cShaderForMask.SetTexture(maskBuffer_kernelID, "labelScreenshotForSum", Mask);
        cShaderForMask.SetTexture(maskBuffer_kernelID, "Result", backgroundRT);
        cShaderForMask.Dispatch(maskBuffer_kernelID, w, h, 1);
        toTexture2D(backgroundRT, maskedBackgroundAsTex2D, w, h);
    }

    void FindHistogramAverageColor(Texture2D maskedBackgroundAsTex2D, int granularityMode)
    {   
        // Initialize the average value to return 
        Color32 avgVal = new Color32(0, 0, 0, 0);
        
        // Do the histogram-based average calculation 
        if (requests.Count < 8)
        {
            requests.Enqueue(AsyncGPUReadback.Request(maskedBackgroundAsTex2D, 0, TextureFormat.RGBA32, (AsyncGPUReadbackRequest req) =>
            {
                if (req.hasError)
                {
                    Debug.Log("GPU readback error detected.");
                    requests.Dequeue();
                    return;
                }
                else if (req.done)
                {
                    req.GetData<Color32>().CopyTo(backgroundDataBuffer);

                    int averageR = 0;
                    int averageG = 0;
                    int averageB = 0;
                    int count = 0;

                    for (int i = 0; i < backgroundDataBuffer.Length; ++i)
                    {   
                        if (backgroundDataBuffer[i].a != 0){

                            // Add the current color to the corresponding bin in ColorHistogram
                            for (int binIdx = 0; binIdx < ColorHistogramBins.Count; binIdx++){
                                Vector3 currentBinRange = ColorHistogramBins[binIdx];
                                if (backgroundDataBuffer[i].r <= currentBinRange[0] && backgroundDataBuffer[i].g <= currentBinRange[1] && backgroundDataBuffer[i].b <= currentBinRange[2]){
                                    ColorHistogram[binIdx].Add(backgroundDataBuffer[i]);
                                    break;
                                }
                            }
                        }
                    }
                    
                    // Find the bin that has the largest number of colors
                    ColorHistogram = ColorHistogram.OrderBy(bin => bin.Count).ToList();
                    
                    // Iterate through all colors in the bin that contained the maximum number of colors 
                    // and calculate the average color of the colors in that bin
                    int numBinsToTake = 2;
                    int binSizeSum = 0;
                    
                    for (int binIdxReversed = 0; binIdxReversed <= numBinsToTake; binIdxReversed ++){
                        List<Color32> currentBin = ColorHistogram[ColorHistogram.Count - 1 - binIdxReversed];

                        if (currentBin.Count != 0){
                            for (int i = 0; i < currentBin.Count; i++){
                                averageR += currentBin[i].r;
                                averageG += currentBin[i].g;
                                averageB += currentBin[i].b;
                            }

                            binSizeSum += currentBin.Count;
                        }
                    }
                    if (binSizeSum != 0){
                        averageR = (int)(averageR/binSizeSum);
                        averageG = (int)(averageG/binSizeSum);
                        averageB = (int)(averageB/binSizeSum);
                    }
                    else{
                        averageR = 0;
                        averageG = 0;
                        averageB = 0;
                    }
                    

                    // Convert the result into a Colro32 object to return
                    avgVal = new Color32((byte)averageR, (byte)averageG, (byte)averageB, 255);
                    if (granularityMode == 2){
                        backgroundAvg = avgVal;
                    }
                    else{
                        labelAvg = avgVal;
                    }
                    
                    // Debug.Log(avgVal);
                    // Empty bins for future uses
                    for (int bin = 0; bin < ColorHistogram.Count; bin++){
                        ColorHistogram[bin].Clear();
                    }
                   
                    
                }

                requests.Dequeue();
            }));
        }
        // Empty backgroundDataBuffer for future uses
        // Array.Clear(backgroundDataBuffer, 0, backgroundDataBuffer.Length);
    }

    

    // Start is called before the first frame update
    void Start()
    {
        // Get the material to which the Inverse Cull shader is attached
        backgroundAndLabelSphereMaterial = backgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial;
        
        // Set up the background and label screenshot cameras
        backgroundScreenshotCamera = FindObjectsOfType<Camera>()[0]; // right eye anchor
        labelScreenshotCamera = FindObjectsOfType<Camera>()[2]; // left eye anchor
        centerEyeCamera = FindObjectsOfType<Camera>()[1]; // center eye anchor -> this is a physical camera

        // backgroundScreenshotCamera = FindObjectsOfType<Camera>()[2]; // right eye anchor
        // labelScreenshotCamera = FindObjectsOfType<Camera>()[0]; // left eye anchor
        // centerEyeCamera = FindObjectsOfType<Camera>()[1];
        
        w = LabelMask.width;
        h = LabelMask.height;

        // Initiate the texture to which background pixels will be rendered
        maskedBackgroundAsTex2D = new Texture2D(w, h, TextureFormat.RGBA32, false);

        // // Initiate the texture to which the black-white label pixels will be rendered
        // labelScreenshotForSum = new Texture2D(w, h, TextureFormat.RGBA32, false);

        // Block out unwanted layers from label and background screenshot cameras
        labelScreenshotCamera.cullingMask &= (1 << LayerMask.NameToLayer("UI"));
        backgroundScreenshotCamera.cullingMask &=  ~(1 << LayerMask.NameToLayer("BackgroundAndLabel"));
        backgroundScreenshotCamera.cullingMask &=  ~(1 << LayerMask.NameToLayer("UI"));

        // Initialize temporary render textures
        backgroundRT = RenderTexture.GetTemporary(w, h);
        backgroundRT.enableRandomWrite = true;
       
        // Color buffer
        backgroundDataBuffer = new Color32[w*h];

        // Find the ID of the average RGB value calculation function we'll use in Compute Shader
        maskBuffer_kernelID = cShaderForMask.FindKernel("CSMain");

        ////// The lines below are for creating and saving a new lookup texture ///////////
        // Read the txt file that contains candidate LAB values and copy their values into CandidateCIELABVals
        // CandidateCIELABVals = new List<Color32>();
        // var linesReadLAB = File.ReadLines("./Assets/AllCandidateLABvals.txt");
        // foreach (var lineReadLAB in linesReadLAB)
        // {
        //     string[] num = lineReadLAB.Split(",");
        //     Vector3 currentLAB = new Vector3(float.Parse(num[0], System.Globalization.CultureInfo.InvariantCulture), 
        //                                     float.Parse(num[1], System.Globalization.CultureInfo.InvariantCulture), 
        //                                     float.Parse(num[2], System.Globalization.CultureInfo.InvariantCulture));
        //     Color32 currentLABAsRGB = LAB2RGB(currentLAB);
        //     CandidateCIELABVals.Add(currentLABAsRGB);
        // }

        // int lookupTableStepSize = 4; // change for a different step size

        // // Make a lookup table (texture3d) with the corresponding LAB-to-RGB converted value at each RGB index
        // int lineCounter = 0;
        // LookupTable = new Texture3D(256, 256, 256, TextureFormat.RGBA32, false);
        
        // // LookupTable.mipCount = 0;
        // var linesReadRGB = File.ReadLines("./Assets/AllCorrespondingRGBVals.txt");
       
        // foreach (var lineReadRGB in linesReadRGB){
        //     string[] num = lineReadRGB.Split(",");
        //     int rIdx = int.Parse(num[0]);
        //     int gIdx = int.Parse(num[1]);
        //     int bIdx = int.Parse(num[2]);
        //     LookupTable.SetPixel(rIdx, gIdx, bIdx, CandidateCIELABVals[lineCounter]);
        //     lineCounter += 1;
        //     // Debug.Log(LookupTable.GetPixel(rIdx, gIdx, bIdx));
        // }

        // LookupTable.Apply(updateMipmaps: false);
        // AssetDatabase.CreateAsset(LookupTable, $"Assets/LookupTexture.asset");
        // AssetDatabase.SaveAssetIfDirty(LookupTable);
        ////// The lines above are for creating and saving a new lookup texture ///////////
        
        backgroundAndLabelSphereMaterial.SetTexture("_CIELAB_LookupTable", LookupTable);

        // Initialize the histogram for characteristic background color extraction (for per-label and per-background modes)
        ColorHistogram = new List<List<Color32>>();
        ColorHistogramBins = new List<Vector3>();

        // Initialize bins to store color values
        for (int i = 0; i < 27; i++){
            List<Color32> colorBin = new List<Color32>();
            ColorHistogram.Add(colorBin);
        }

        // Store in a Vector3 object upper r, g, b bounds for each bin (there should be 27 bins)
        for (int rRange = 85; rRange <= 255; rRange+=85){
            for (int gRange = 85; gRange <= 255; gRange+=85){
                for (int bRange = 85; bRange <= 255; bRange+=85){
                    Vector3 bin = new Vector3(rRange, gRange, bRange);
                    ColorHistogramBins.Add(bin);
                }
            }
        }

        backgroundOrLableChanged = true;
    }

    // Update is called once per frame
    void Update()
    {
        backgroundAndLabelSphere.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
        labelSphere.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
        
        // Render to temporary render textures from both the background (right-eye) and the label (left-eye) cameras
        // backgroundScreenshotCamera.targetTexture = backgroundRT;
        // labelScreenshotCamera.targetTexture = labelRT;
        // backgroundScreenshotCamera.Render();
        // labelScreenshotCamera.Render();
    }


    void LateUpdate()
    {  
        // Get the current granularity method
        int granularityMethod = backgroundAndLabelSphereMaterial.GetInt("_GranularityMethod");

        // if the background scene or label changes, calculate the background average
        if (backgroundOrLableChanged == true){ 
            Debug.Log("background or label changed");
            // Calculate the new background average value
            ApplyMask(backgroundRT, EquirectangularBackground, BackgroundMask, w, h);
            FindHistogramAverageColor(maskedBackgroundAsTex2D, 2);
            
            // Calculate the new label average value
            ApplyMask(backgroundRT, EquirectangularBackground, LabelMask, w, h);
            FindHistogramAverageColor(maskedBackgroundAsTex2D, 1);

            // Debug.Log(backgroundAvg);
            // Debug.Log(labelAvg);
            backgroundOrLableChanged = false;

        }

        // // Saved the masked background texture for debugging purposes
        // byte[] bytes = maskedBackgroundAsTex2D.EncodeToPNG();
        // File.WriteAllBytes(Application.dataPath + "/MaskedBackground5.png", bytes);

        // if there's no scene change, assign pre-calculated average values based on different granularity levels
        if (granularityMethod == 2){
            // Assign the average background RGB colors to the inverse cull shader
            backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_r", (float)(backgroundAvg.r/255.0));
            backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_g", (float)(backgroundAvg.g/255.0));
            backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_b", (float)(backgroundAvg.b/255.0));
        }
        else{
            // Assign the average background RGB colors to the inverse cull shader
            backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_r", (float)(labelAvg.r/255.0));
            backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_g", (float)(labelAvg.g/255.0));
            backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_b", (float)(labelAvg.b/255.0));
        }
        
}


    void OnDestroy() // Destroy render textures upon stopping the run
    {
        RenderTexture.ReleaseTemporary(backgroundRT);
    }
}