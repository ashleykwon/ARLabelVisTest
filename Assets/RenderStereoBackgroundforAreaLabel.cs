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

public class RenderStereoBackgroundforAreaLabel : MonoBehaviour
{
    public GameObject backgroundAndLabelSphere;
    public GameObject labelSphere;
    public GameObject player;
    Material backgroundAndLabelSphereMaterial;
    public ComputeShader cShader;
    public ComputeBuffer sumBuffer;
    private int kernelID_main;
    private int kernelID_init;

    Camera backgroundScreenshotCamera;
    Camera labelScreenshotCamera;

    Texture2D backgroundScreenshotForSum; // this doesn't need to be assigned outside of this code
    Texture2D labelScreenshotForSum; // this doesn't need to be assigned outside of this code

    RenderTexture backgroundRT;
    RenderTexture labelRT;
    int w;
    int h;

    void toTexture2D(RenderTexture rTex, Texture2D screenshot, int width, int height)
    {
        RenderTexture.active = rTex;
        screenshot.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        screenshot.Apply();
        // RenderTexture.active = null;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Get the material to which the Inverse Cull shader is attached
        backgroundAndLabelSphereMaterial = backgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial;
        // // backgroundAndLabelSphereMaterial.SetInt("_GranularityMethod", 1);
        
        // Set up the background and label screenshot cameras
        backgroundScreenshotCamera = FindObjectsOfType<Camera>()[0]; // right eye anchor
        labelScreenshotCamera = FindObjectsOfType<Camera>()[2]; // left eye anchor

        w = backgroundScreenshotCamera.pixelWidth;
        h = backgroundScreenshotCamera.pixelHeight;

        // Initiate the texture to which background pixels will be rendered
        backgroundScreenshotForSum = new Texture2D(w, h, TextureFormat.RGB24, false);

        // Initiate the texture to which the black-white label pixels will be rendered
        labelScreenshotForSum = new Texture2D(w, h, TextureFormat.RGB24, false);

        //sum_all
        // SetUp_getSum();
        backgroundAndLabelSphereMaterial.SetBuffer("sum_all_results", sumBuffer);

        labelScreenshotCamera.cullingMask &= (1 << LayerMask.NameToLayer("UI"));

        backgroundScreenshotCamera.cullingMask &=  ~(1 << LayerMask.NameToLayer("BackgroundAndLabel"));
        backgroundScreenshotCamera.cullingMask &=  ~(1 << LayerMask.NameToLayer("UI"));

        backgroundRT = RenderTexture.GetTemporary(w, h);
        labelRT = RenderTexture.GetTemporary(w, h);
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
        // StartCoroutine(ComputeSum());
        
        
        // Wait for the current frame to finish rendering
        // WaitForEndOfFrame frameEnd = new WaitForEndOfFrame();
        // yield return frameEnd;

        // Convert the screenshot from the background and the label cameras to texture2D for sum calculation 
        toTexture2D(backgroundRT, backgroundScreenshotForSum, w, h);
        toTexture2D(labelRT, labelScreenshotForSum, w, h);
       
        // Update_getSum();

        // Calculate background pixel average for an area or the entire background
        float r = 0.0f;
        float g = 0.0f;
        float b = 0.0f;
        int granularityMethod = backgroundAndLabelSphereMaterial.GetInt("_GranularityMethod");
        int numSampledPixels = 0;

        if (granularityMethod != 0){
            for(int i = 0; i<w; i++){
                for(int j = 0; j<h; j++){
                    if (granularityMethod == 1){ // area-based
                        if (labelScreenshotForSum.GetPixel(i,j)[0] == 1 && labelScreenshotForSum.GetPixel(i,j)[1] == 1 && labelScreenshotForSum.GetPixel(i,j)[2] == 1){
                            r += backgroundScreenshotForSum.GetPixel(i,j)[0];
                            g += backgroundScreenshotForSum.GetPixel(i,j)[1];
                            b += backgroundScreenshotForSum.GetPixel(i,j)[2];
                            numSampledPixels += 1;
                        }
                    }
                    else if (granularityMethod == 2){ // all pixels in the user's view
                        r += backgroundScreenshotForSum.GetPixel(i,j)[0];
                        g += backgroundScreenshotForSum.GetPixel(i,j)[1];
                        b += backgroundScreenshotForSum.GetPixel(i,j)[2];
                        numSampledPixels += 1;
                    }
                    
                }
            }
        }
        
        if (numSampledPixels != 0){
            r = r/numSampledPixels;
            g = g/numSampledPixels;
            b = b/numSampledPixels;
        }
      

        // Save images for debugging purposes only
        // byte[] bytes = labelScreenshotForSum.EncodeToPNG();
        // File.WriteAllBytes(Application.dataPath + "/../DebuggingScreenshot_" + numSampledPixels + ".png", bytes);
        
        // Derive the average pixel value
        // if (granularityMethod == 1){ // area-bsed
        //     // Debug.Log(numPixelsInArea);
        //     r = r / numPixelsInArea;
        //     g = g / numPixelsInArea;
        //     b = b / numPixelsInArea;
        // }
        // else if (granularityMethod == 2){ // entire background
        //     // r = (r*100) / (w*h); // why is this multiplied by 100?
        //     // g = (g*100) / (w*h);
        //     // b = (b*100) / (w*h);

        // }


        backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_r", r);
        backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_g", g);
        backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_b", b);

        RenderTexture.ReleaseTemporary(backgroundRT);
        RenderTexture.ReleaseTemporary(labelRT);
    }


    // IEnumerator ComputeSum(){

    //     // Move and rotate the sphere with the player
    //     backgroundAndLabelSphere.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
    //     labelSphere.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
        

    //     // Render to temporary render textures from both the background (right-eye) and the label (left-eye) cameras
    //     backgroundScreenshotCamera.targetTexture = backgroundRT;
    //     labelScreenshotCamera.targetTexture = labelRT;
    //     backgroundScreenshotCamera.Render();
    //     labelScreenshotCamera.Render();
        
    //     // Wait for the current frame to finish rendering
    //     WaitForEndOfFrame frameEnd = new WaitForEndOfFrame();
    //     yield return frameEnd;

    //     // Convert the screenshot from the background and the label cameras to texture2D for sum calculation 
    //     toTexture2D(backgroundRT, backgroundScreenshotForSum, w, h);
    //     toTexture2D(labelRT, labelScreenshotForSum, w, h);
       
    //     // Update_getSum();

    //     // Calculate background pixel average for an area or the entire background
    //     float r = 0.0f;
    //     float g = 0.0f;
    //     float b = 0.0f;
    //     int granularityMethod = backgroundAndLabelSphereMaterial.GetInt("_GranularityMethod");
    //     int numSampledPixels = 0;

    //     if (granularityMethod != 0){
    //         for(int i = 0; i<w-10; i+=10){
    //             for(int j = 0; j<h-10; j+=10){
    //                 if (granularityMethod == 1){ // area-based
    //                     if (labelScreenshotForSum.GetPixel(i,j)[0] == 1 && labelScreenshotForSum.GetPixel(i,j)[1] == 1 && labelScreenshotForSum.GetPixel(i,j)[2] == 1){
    //                         r += backgroundScreenshotForSum.GetPixel(i,j)[0];
    //                         g += backgroundScreenshotForSum.GetPixel(i,j)[1];
    //                         b += backgroundScreenshotForSum.GetPixel(i,j)[2];
    //                         numSampledPixels += 1;
    //                     }
    //                 }
    //                 else if (granularityMethod == 2){ // all pixels in the user's view
    //                     r += backgroundScreenshotForSum.GetPixel(i,j)[0];
    //                     g += backgroundScreenshotForSum.GetPixel(i,j)[1];
    //                     b += backgroundScreenshotForSum.GetPixel(i,j)[2];
    //                     numSampledPixels += 1;
    //                 }
                    
    //             }
    //         }
    //     }
        
    //     if (numSampledPixels != 0){
    //         r = r/numSampledPixels;
    //         g = g/numSampledPixels;
    //         b = b/numSampledPixels;
    //     }
      
    //     byte[] bytes = labelScreenshotForSum.EncodeToPNG();

    //     // For testing purposes, also write to a file in the project folder
    //     File.WriteAllBytes(Application.dataPath + "/../DebuggingScreenshot_" + numSampledPixels + ".png", bytes);
        
    //     // Derive the average pixel value
    //     // if (granularityMethod == 1){ // area-bsed
    //     //     // Debug.Log(numPixelsInArea);
    //     //     r = r / numPixelsInArea;
    //     //     g = g / numPixelsInArea;
    //     //     b = b / numPixelsInArea;
    //     // }
    //     // else if (granularityMethod == 2){ // entire background
    //     //     // r = (r*100) / (w*h); // why is this multiplied by 100?
    //     //     // g = (g*100) / (w*h);
    //     //     // b = (b*100) / (w*h);

    //     // }


    //     backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_r", r);
    //     backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_g", g);
    //     backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_b", b);

    //     RenderTexture.ReleaseTemporary(backgroundRT);
    //     RenderTexture.ReleaseTemporary(labelRT);

    //     // Debug.Log("sum1: " + r);
    //     // Debug.Log("sum2: " + g);
    //     // Debug.Log("sum3: " + b); 
    // }

    // private void SetUp_getSum(){
    //     kernelID_main = cShader.FindKernel("CSMain");
    //     kernelID_init = cShader.FindKernel("CSInit");

    //     cShader.SetTexture(kernelID_main, "InputCubeMap", renderTexture);
    //     cShader.SetTexture(kernelID_init, "InputCubeMap", renderTexture);

    //     cShader.SetTexture(kernelID_main, "InputImage", backgroundScreenshotForSum);
    //     cShader.SetTexture(kernelID_init, "InputImage", backgroundScreenshotForSum);

    //     sumBuffer = new ComputeBuffer(4, 16);

    //     cShader.SetBuffer(kernelID_main, "_SumBuffer", sumBuffer); 
    //     cShader.SetBuffer(kernelID_init, "_SumBuffer", sumBuffer);

    //     cShader.Dispatch(kernelID_init, 1, 1, 1);
    //     cShader.Dispatch(kernelID_main, 16, 1, 1);

    // }

    // private void Update_getSum(){  
        

    //     Debug.Log("Update get_sum");
    //             sumBuffer = new ComputeBuffer(4, 16);

    //     cShader.SetBuffer(kernelID_main, "_SumBuffer", sumBuffer); 
    //     cShader.SetBuffer(kernelID_init, "_SumBuffer", sumBuffer);

    //     cShader.Dispatch(kernelID_main, 16, 1, 1);

    // }
}
