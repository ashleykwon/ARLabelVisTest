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

    int w;
    int h;


    // Start is called before the first frame update
    void Start()
    {
        int cubemapSize = 2048; // this can change for a better resolution
        
        backgroundAndLabelSphereMaterial = backgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial;
        
        // Define a cube-shaped render texture for the background
        renderTexture = new RenderTexture(cubemapSize, cubemapSize, 16);
        renderTexture.dimension = UnityEngine.Rendering.TextureDimension.Cube;

         // To prevent antialiasing
        renderTexture.autoGenerateMips = false;
        renderTexture.useMipMap = false;
        renderTexture.filterMode = FilterMode.Point;
        // ScreenshotCamera = gameObject.GetComponent<Camera>(); 
        // ScreenshotCamera.cullingMask &=  ~(1 << LayerMask.NameToLayer("BackgroundAndLabel"));
    }

    // Update is called once per frame
    void Update()
    {     
        // Take a screenshot and render it to a cubemap
       
        
        
        ScreenshotCamera.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;
       
        
        // Render the background and the label
        ScreenshotCamera.RenderToCubemap(renderTexture, 63); 
        backgroundAndLabelSphereMaterial.SetTexture("_CubeMap", renderTexture);

        // if (RenderTexture.active != null)
        // {
        //     StartCoroutine(PostScreenshot(renderTexture));
        // }

        RenderTexture.active = null;

    }

}