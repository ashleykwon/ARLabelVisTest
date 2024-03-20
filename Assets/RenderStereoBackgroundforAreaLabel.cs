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

public class RenderStereoBackgroundforAreaLabel : MonoBehaviour
{
    public GameObject backgroundAndLabelSphere;
    public GameObject labelSphere;
    public GameObject player;
    Material backgroundAndLabelSphereMaterial;
    Camera backgroundScreenshotCamera;
    Camera labelScreenshotCamera;

    Texture2D backgroundScreenshotForSum; // this doesn't need to be assigned outside of this code
    Texture2D labelScreenshotForSum; // this doesn't need to be assigned outside of this code

    RenderTexture backgroundRT;
    RenderTexture labelRT;
    int w;
    int h;

    private Queue<AsyncGPUReadbackRequest> requests = new Queue<AsyncGPUReadbackRequest>();
    private Color32[] backgroundDataBuffer;

    public ComputeShader cShaderForMask;
    int maskBuffer_kernelID;
    List<Color32> CandidateCIELABVals;
    float[] CandidateCIELABValsAsArray;
    Texture3D LookupTable;


    void toTexture2D(RenderTexture rTex, Texture2D screenshot, int width, int height)
    {
        RenderTexture.active = rTex;
        screenshot.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        screenshot.Apply();
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

    

    // Start is called before the first frame update
    void Start()
    {
        // Get the material to which the Inverse Cull shader is attached
        backgroundAndLabelSphereMaterial = backgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial;
        // maskedBackgroundMaterial = maskedBackground.GetComponent<MeshRenderer>().sharedMaterial;
        // // backgroundAndLabelSphereMaterial.SetInt("_GranularityMethod", 1);
        
        // Set up the background and label screenshot cameras
        backgroundScreenshotCamera = FindObjectsOfType<Camera>()[0]; // right eye anchor
        labelScreenshotCamera = FindObjectsOfType<Camera>()[2]; // left eye anchor

        w = backgroundScreenshotCamera.pixelWidth;
        h = backgroundScreenshotCamera.pixelHeight;

        // Initiate the texture to which background pixels will be rendered
        backgroundScreenshotForSum = new Texture2D(w, h, TextureFormat.RGBA32, false);

        // Initiate the texture to which the black-white label pixels will be rendered
        labelScreenshotForSum = new Texture2D(w, h, TextureFormat.RGBA32, false);

        // Block out unwanted layers from label and background screenshot cameras
        labelScreenshotCamera.cullingMask &= (1 << LayerMask.NameToLayer("UI"));
        backgroundScreenshotCamera.cullingMask &=  ~(1 << LayerMask.NameToLayer("BackgroundAndLabel"));
        backgroundScreenshotCamera.cullingMask &=  ~(1 << LayerMask.NameToLayer("UI"));

        // Initialize temporary render textures
        backgroundRT = RenderTexture.GetTemporary(w, h);
        backgroundRT.enableRandomWrite = true;
        labelRT = RenderTexture.GetTemporary(w, h);

        // Color buffer
        backgroundDataBuffer = new Color32[w*h];

        // Find the ID of the average RGB value calculation function we'll use in Compute Shader
        maskBuffer_kernelID = cShaderForMask.FindKernel("CSMain");

        // Read the txt file that contains candidate LAB values and copy their values into CandidateCIELABVals
        CandidateCIELABVals = new List<Color32>();
        var linesReadLAB = File.ReadLines("./Assets/CandidateLABvals.txt");
        foreach (var lineReadLAB in linesReadLAB)
        {
            string[] num = lineReadLAB.Split(",");
            Vector3 currentLAB = new Vector3(float.Parse(num[0], System.Globalization.CultureInfo.InvariantCulture), 
                                            float.Parse(num[1], System.Globalization.CultureInfo.InvariantCulture), 
                                            float.Parse(num[2], System.Globalization.CultureInfo.InvariantCulture));
            Color32 currentLABAsRGB = LAB2RGB(currentLAB);
            CandidateCIELABVals.Add(currentLABAsRGB);
        }

        int lookupTableStepSize = 4; // change for a different step size

        // Make a lookup table (texture3d) with the corresponding LAB-to-RGB converted value at each RGB index
        int lineCounter = 0;
        LookupTable = new Texture3D(256, 256, 256, TextureFormat.RGBA32, false);
        // LookupTable.mipCount = 0;
        var linesReadRGB = File.ReadLines("./Assets/CorrespondingRGBVals.txt");
       
        foreach (var lineReadRGB in linesReadRGB){
            string[] num = lineReadRGB.Split(",");
            int rIdx = int.Parse(num[0]);
            int gIdx = int.Parse(num[1]);
            int bIdx = int.Parse(num[2]);
            LookupTable.SetPixel(rIdx, gIdx, bIdx, CandidateCIELABVals[lineCounter]);
            lineCounter += 1;
            // Debug.Log(LookupTable.GetPixel(rIdx, gIdx, bIdx));
        }
        LookupTable.Apply();

        backgroundAndLabelSphereMaterial.SetTexture("_CIELAB_LookupTable", LookupTable);
        backgroundAndLabelSphereMaterial.SetInt("_LookupTableStepSize", lookupTableStepSize);
    }

    // Update is called once per frame
    void Update()
    {
        backgroundAndLabelSphere.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
        labelSphere.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
        
        // Render to temporary render textures from both the background (right-eye) and the label (left-eye) cameras
        backgroundScreenshotCamera.targetTexture = backgroundRT;
        labelScreenshotCamera.targetTexture = labelRT;
        backgroundScreenshotCamera.Render();
        labelScreenshotCamera.Render();
    }


    void LateUpdate()
    {  
        // Convert the screenshot from the background and the label cameras to texture2D for sum calculation 
        toTexture2D(backgroundRT, backgroundScreenshotForSum, w, h);
        toTexture2D(labelRT, labelScreenshotForSum, w, h);

        // Get the current granularity method
        int granularityMethod = backgroundAndLabelSphereMaterial.GetInt("_GranularityMethod");

        if (granularityMethod == 1){ // area-based label
           
            // Using compute shader, mask the background so that it only contains pixels under the area label
            cShaderForMask.SetTexture(maskBuffer_kernelID, "backgroundScreenshotForSum", backgroundScreenshotForSum);
            cShaderForMask.SetTexture(maskBuffer_kernelID, "labelScreenshotForSum", labelScreenshotForSum);
            cShaderForMask.SetTexture(maskBuffer_kernelID, "Result", backgroundRT);
            cShaderForMask.Dispatch(maskBuffer_kernelID, w, h, 1);
            toTexture2D(backgroundRT, backgroundScreenshotForSum, w, h);

            // For mask debugging purposes only 
            // byte[] bytes = backgroundScreenshotForSum.EncodeToPNG();
            // File.WriteAllBytes(Application.dataPath + "/MaskedBackground2.png", bytes);
        }

        // Save images for debugging purposes only
        // byte[] bytes = backgroundScreenshotForSum.EncodeToPNG();
        // File.WriteAllBytes(Application.dataPath + "/DebuggingScreenshot.png", bytes);

        if (granularityMethod != 0){
            if (requests.Count < 8){
                requests.Enqueue(AsyncGPUReadback.Request(backgroundScreenshotForSum, 0, TextureFormat.RGBA32, (AsyncGPUReadbackRequest req) =>
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
                                averageR += backgroundDataBuffer[i].r;
                                averageG += backgroundDataBuffer[i].g;
                                averageB += backgroundDataBuffer[i].b;
                                ++count;
                            }
                        }

                        if (count != 0){ // handles the case when the label is not in the user's view 
                            averageR /= count;
                            averageG /= count;
                            averageB /= count;
                            
                            r = (float)(averageR/255.0);
                            g = (float)(averageG/255.0);
                            b = (float)(averageB/255.0);
                        }

                        backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_r", r);
                        backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_g", g);
                        backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_b", b);
                        }

                        requests.Dequeue();
                    }));
                }
        }

        else{ // per-pixel label 
            backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_r", 0.0f);
            backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_g", 0.0f);
            backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_b", 0.0f);
        }
        
    }


    void OnDestroy() // Destroy render textures upon stopping the run
    {
        RenderTexture.ReleaseTemporary(backgroundRT);
        RenderTexture.ReleaseTemporary(labelRT);
    }
}