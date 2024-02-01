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

public class RenderStereoBackgroundforDirectTextRendering : MonoBehaviour
{
    public Camera ScreenshotCamera;
    public GameObject backgroundAndLabelSphere;
    public GameObject labelSphere;
    public GameObject player;
    public Material backgroundAndLabelSphereMaterial;
    RenderTexture renderTexture;

    public ComputeShader cShader;
    public ComputeBuffer sumBuffer;
    private int kernelID_main;
    private int kernelID_init;
    private float time;
    private float timeLimit = 5;
    private readonly string url = "http://10.38.23.43:8000/predict";
    public Texture2D screenshotForSum;
    int w;
    int h;

    public class BackgroundImageContainer{
        public string rgb_base64;
    }

    // Start is called before the first frame update
    void Start()
    {
        int cubemapSize = 2048; // this can change for a better resolution
        
        backgroundAndLabelSphereMaterial = backgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial;
                        backgroundAndLabelSphereMaterial.SetInt("_GranularityMethod", 1);

        // Define a cube-shaped render texture for the background
        renderTexture = new RenderTexture(cubemapSize, cubemapSize, 16);
        renderTexture.dimension = UnityEngine.Rendering.TextureDimension.Cube;

         // To prevent antialiasing
        renderTexture.autoGenerateMips = false;
        renderTexture.useMipMap = false;
        renderTexture.filterMode = FilterMode.Point;



        // Access the screenshot camera
        ScreenshotCamera = gameObject.GetComponent<Camera>(); 
        w = ScreenshotCamera.pixelWidth;
        h = ScreenshotCamera.pixelHeight;
        screenshotForSum = new Texture2D (w, h, TextureFormat.RGB24, false);


        //sum_all
        Debug.Log("SetUp get_sum");
        SetUp_getSum();
        backgroundAndLabelSphereMaterial.SetBuffer("sum_all_results", sumBuffer);

        // StartCoroutine(PostScreenshot(renderTexture));
    }

    

    void Update()
    {
        StartCoroutine(ComputeSum());
    }

    IEnumerator ComputeSum(){
                // Block out the layer that contains the label and the black background behind the label (it's a plane object that has a material + shader)
        ScreenshotCamera.cullingMask &=  ~(1 << LayerMask.NameToLayer("BackgroundAndLabel"));
        ScreenshotCamera.cullingMask &=  ~(1 << LayerMask.NameToLayer("UI"));

        // Move and rotate the sphere with the player
        backgroundAndLabelSphere.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
        labelSphere.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
        WaitForEndOfFrame frameEnd = new WaitForEndOfFrame();

        yield return frameEnd;
       
        Rect regionToReadFrom = new Rect(0, 0, w-1, h-1);
        screenshotForSum.ReadPixels(regionToReadFrom, 0, 0); //ScreenshotCamera.pixelRect

        Debug.Log("pixel read");
        // Update_getSum();

        // int[] bk_sum = {0,0,0,0};

        // sumBuffer.GetData(bk_sum);

        // float r = (float)bk_sum[1];
        // float g = (float)bk_sum[2];
        // float b = (float)bk_sum[3];

        float r = 0.0f;
        float g = 0.0f;
        float b = 0.0f;

        for(int i = 0; i<w-10; i+=10){
            for(int j = 0; j<h-10; j+=10){

                r += screenshotForSum.GetPixel(i,j)[0];
                g += screenshotForSum.GetPixel(i,j)[1];
                b += screenshotForSum.GetPixel(i,j)[2];
            }}

        r = (r*100) / (w*h);
        g = (g*100) / (w*h);
        b = (b*100) / (w*h);



        backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_r", r);
        backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_g", g);
        backgroundAndLabelSphereMaterial.SetFloat("_Background_sum_b", b);
                // Debug.Log("num_pixels: " + bk_sum[0]);

        Debug.Log("sum1: " + r);
        Debug.Log("sum2: "  + g);
        Debug.Log("sum3: " + b); 
        // somehow bk_sum[1] is always zero, although other numbers in bk_sum seem to be reasonable numbers
    }

    // Update is called once per frame
    void LateUpdate()
    {     
        // Take a screenshot and render it to a cubemap
       
        backgroundAndLabelSphereMaterial.SetTexture("_CubeMap", renderTexture);
        
        ScreenshotCamera.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;
        
        // Render the background and the label
        ScreenshotCamera.RenderToCubemap(renderTexture, 63); 

        // if (RenderTexture.active != null)
        // {
        //     StartCoroutine(PostScreenshot(renderTexture));
        // }

        RenderTexture.active = null;

    }

    private void SetUp_getSum(){
        kernelID_main = cShader.FindKernel("CSMain");
        kernelID_init = cShader.FindKernel("CSInit");

        cShader.SetTexture(kernelID_main, "InputCubeMap", renderTexture);
        cShader.SetTexture(kernelID_init, "InputCubeMap", renderTexture);

        cShader.SetTexture(kernelID_main, "InputImage", screenshotForSum);
        cShader.SetTexture(kernelID_init, "InputImage", screenshotForSum);

        sumBuffer = new ComputeBuffer(4, 16);

        cShader.SetBuffer(kernelID_main, "_SumBuffer", sumBuffer); 
        cShader.SetBuffer(kernelID_init, "_SumBuffer", sumBuffer);

        cShader.Dispatch(kernelID_init, 1, 1, 1);
        cShader.Dispatch(kernelID_main, 16, 1, 1);

    }

    private void Update_getSum(){  
        

        Debug.Log("Update get_sum");
                sumBuffer = new ComputeBuffer(4, 16);

        cShader.SetBuffer(kernelID_main, "_SumBuffer", sumBuffer); 
        cShader.SetBuffer(kernelID_init, "_SumBuffer", sumBuffer);

        cShader.Dispatch(kernelID_main, 16, 1, 1);

    }

}