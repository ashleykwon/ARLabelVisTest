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



    void toTexture2D(RenderTexture rTex, Texture2D screenshot, int width, int height)
    {
        RenderTexture.active = rTex;
        //screenshot.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        //screenshot.Apply();
        // RenderTexture.active = null;
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
            // File.WriteAllBytes(Application.dataPath + "/MaskedBackground.png", bytes);
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

        else{
            backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_r", 0.0f);
            backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_g", 0.0f);
            backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_b", 0.0f);
        }

        RenderTexture.ReleaseTemporary(backgroundRT);
        RenderTexture.ReleaseTemporary(labelRT);
    }
}
