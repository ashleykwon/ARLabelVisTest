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


public class IntegratedCameras : MonoBehaviour
{
    public GameObject backgroundAndLabelSphere;
    public GameObject labelSphere;
    public GameObject player;
    Material backgroundAndLabelSphereMaterial;
    Camera backgroundScreenshotCamera;
    Camera labelScreenshotCamera;
    Camera centerEyeCamera;
    RenderTexture backgroundRT;
    RenderTexture labelRT;
    RenderTexture maskedBackgroundRT;
    Texture2D maskedBackgroundAsTex2D; // this doesn't need to be assigned outside of this code
    int w;
    int h;

    private Queue<AsyncGPUReadbackRequest> requests = new Queue<AsyncGPUReadbackRequest>();
    private Color32[] backgroundDataBuffer;

    public ComputeShader cShaderForMask;
    int maskBuffer_kernelID;
    List<Color32> CandidateCIELABVals;
    float[] CandidateCIELABValsAsArray;
    public Texture3D LookupTable;
    List<Vector3> ColorHistogramBins;
    List<List<Color32>> ColorHistogram;
    public Texture2D CenterMarkedLabelTexture;
    public Texture2D EquirectangularBackground;


    Color LAB2RGB(Vector3 LAB)
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
        float finalR = (float) (Math.Max(Math.Min(var_R, 1.0f), 0.0f));
        float finalG = (float) (Math.Max(Math.Min(var_G, 1.0f), 0.0f));
        float finalB = (float) (Math.Max(Math.Min(var_B, 1.0f), 0.0f));

        Color RGB = new Color((byte) (finalR), (byte) (finalG), (byte) (finalB), 1);
        return RGB;
    }

    void toTexture2D(RenderTexture rTex, Texture2D screenshot, int width, int height)
    {
        RenderTexture.active = rTex;
        screenshot.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        screenshot.Apply();
    }

    // Start is called before the first frame update
    void Start()
    {
        // Get the material to which the Inverse Cull shader is attached
        backgroundAndLabelSphereMaterial = backgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial;
        
        // Set up the background and label screenshot cameras
        backgroundScreenshotCamera = FindObjectsOfType<Camera>()[0]; // right eye anchor --> set this as a physical camera
        labelScreenshotCamera = FindObjectsOfType<Camera>()[2]; // left eye anchor
        centerEyeCamera = FindObjectsOfType<Camera>()[1]; // center eye anchor 

        w = EquirectangularBackground.width;
        h = EquirectangularBackground.height;

        // Block out unwanted layers from label and background screenshot cameras
        labelScreenshotCamera.cullingMask &= (1 << LayerMask.NameToLayer("UI"));
        backgroundScreenshotCamera.cullingMask &=  ~(1 << LayerMask.NameToLayer("BackgroundAndLabel"));
        backgroundScreenshotCamera.cullingMask &=  ~(1 << LayerMask.NameToLayer("UI"));

        // Initialize label render texture as a cubemap
        int cubemapSize = 2048; // this can change for a better resolution            
        // Define a cube-shaped render texture for the white label + black background (default where alpha = 0)
        labelRT = new RenderTexture(cubemapSize, cubemapSize, 16, RenderTextureFormat.ARGB32); 
        labelRT.dimension = UnityEngine.Rendering.TextureDimension.Cube;
        // To prevent antialiasing
        labelRT.autoGenerateMips = false;
        labelRT.useMipMap = false;
        labelRT.filterMode = FilterMode.Point;
        
        // Initialize background render texture as a cubemap
        backgroundRT = new RenderTexture(cubemapSize, cubemapSize, 16, RenderTextureFormat.ARGB32);
        backgroundRT.dimension = UnityEngine.Rendering.TextureDimension.Cube;
        // To prevent antialiasing
        backgroundRT.autoGenerateMips = false;
        backgroundRT.useMipMap = false;
        backgroundRT.filterMode = FilterMode.Point;

        // Initialize masked background texture for histogram-based average pixel value calculation
        maskedBackgroundRT = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32);
        maskedBackgroundRT.enableRandomWrite = true;
        maskedBackgroundAsTex2D = new Texture2D(w, h, TextureFormat.RGBA32, false);

        // Color buffer
        backgroundDataBuffer = new Color32[w*h];

        // Find the ID of the average RGB value calculation function we'll use in Compute Shader
        maskBuffer_kernelID = cShaderForMask.FindKernel("CSMain");
    
        backgroundAndLabelSphereMaterial.SetTexture("_CIELAB_LookupTable", LookupTable);

        // Initialize the histogram for characteristic background color extraction (for per-label and per-background modes)
        ColorHistogram = new List<List<Color32>>();
        ColorHistogramBins = new List<Vector3>();

        // Initialize bins to store color values
        for (int i = 0; i < 27; i++){
            List<Color32> colorBin = new List<Color32>();
            ColorHistogram.Add(colorBin);
        }

        // Store in ColorHistogramBins from i to i+2 where i < 27*3-2
        for (int rRange = 85; rRange <= 255; rRange+=85){
            for (int gRange = 85; gRange <= 255; gRange+=85){
                for (int bRange = 85; bRange <= 255; bRange+=85){
                    Vector3 bin = new Vector3(rRange, gRange, bRange);
                    ColorHistogramBins.Add(bin);
                }
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        backgroundAndLabelSphere.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
        labelSphere.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
        
        // Render label
        labelScreenshotCamera.targetTexture = labelRT;
        labelScreenshotCamera.RenderToCubemap(labelRT, 63);
        backgroundAndLabelSphereMaterial.SetTexture("_LabelCubeMap", labelRT); // Extract render texture directly from UICamera, which renders the white label and the black background, along with blue billboard and red shadow 
        backgroundAndLabelSphereMaterial.SetTexture("_BillboardCubeMap", labelRT);
        backgroundAndLabelSphereMaterial.SetTexture("_ModeCubeMap", labelRT);

        // Render background
        backgroundScreenshotCamera.targetTexture = backgroundRT;
        backgroundScreenshotCamera.RenderToCubemap(backgroundRT, 63); 
        backgroundAndLabelSphereMaterial.SetTexture("_CubeMap", backgroundRT);
    }

    void LateUpdate()
    {
        // Get the current granularity method
        int granularityMethod = backgroundAndLabelSphereMaterial.GetInt("_GranularityMethod");

        // Using compute shader, mask the background so that it only contains pixels under the area label or pixels at a certain distance from the center of the label
        cShaderForMask.SetInt("granularityMethod", granularityMethod);
        cShaderForMask.SetInt("image_width", w);
        cShaderForMask.SetInt("image_height", h);
        cShaderForMask.SetFloat("distanceThreshold", 1000.0f);
        cShaderForMask.SetTexture(maskBuffer_kernelID, "backgroundScreenshotForSum", EquirectangularBackground);
        cShaderForMask.SetTexture(maskBuffer_kernelID, "labelScreenshotForSum", CenterMarkedLabelTexture);
        cShaderForMask.SetTexture(maskBuffer_kernelID, "Result", maskedBackgroundRT);
        cShaderForMask.Dispatch(maskBuffer_kernelID, w, h, 1);
        toTexture2D(maskedBackgroundRT, maskedBackgroundAsTex2D, w, h);

        byte[] bytes = maskedBackgroundAsTex2D.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/MaskedBackground_IntegratedCamera.png", bytes);

        

        if (requests.Count < 8){
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
                        // Calculate background pixel average for an area or the entire background
                        float r = 0.0f;
                        float g = 0.0f;
                        float b = 0.0f;

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


                        if (count != 0){ // handles the case when the label is not in the user's view 
                            r = (float)((averageR/count)/255.0);
                            g = (float)((averageG/count)/255.0);
                            b = (float)((averageB/count)/255.0);
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
                            r = (float)((averageR/binSizeSum)/255.0);
                            g = (float)((averageG/binSizeSum)/255.0);
                            b = (float)((averageB/binSizeSum)/255.0);
                        }
                        
                        // Assign the "average" background color
                        backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_r", r);
                        backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_g", g);
                        backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_b", b);

                           
                        // Debug.Log(averageR);
                        // Empty bins in ColorHistogram for the next frame
                        for (int bin = 0; bin < ColorHistogram.Count; bin++){
                            ColorHistogram[bin].Clear();
                        }
                        
                    }

                    requests.Dequeue();
                    }));
                }
    }
}
