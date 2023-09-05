using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Text;
using System.Linq;
using System;
using UnityEngine.Rendering;

public class SendBackgroundImg : MonoBehaviour
{
    public Camera BackgroundScreenshotCamera;
    RenderTexture renderTexture;
    public GameObject player;
    private readonly string url = "http://10.38.23.43:8000/predict";
    Texture2D Screenshot;


    public class ScreenshotData{
        public string rgb_base64;
        //public string background_rgb_base64;
    }


    // Start is called before the first frame update
    void Start()
    {
        int cubemapSize = 2048; // this can change for a better resolution
        
        // Define a cube-shaped render texture for the background
        renderTexture = new RenderTexture(cubemapSize, cubemapSize, 16);
        renderTexture.dimension = UnityEngine.Rendering.TextureDimension.Cube;

         // To prevent antialiasing
        renderTexture.autoGenerateMips = false;
        renderTexture.useMipMap = false;
        renderTexture.filterMode = FilterMode.Point;

        // Access the screenshot camera
        BackgroundScreenshotCamera = gameObject.GetComponent<Camera>(); 
        
        // BackgroundScreenshotCamera = OVRCameraRig.rightEyeCamera; // how do I get the right eye camera??
        // Debug.Log(BackgroundScreenshotCamera);
    }

    void Update()
    {
        BackgroundScreenshotCamera.cullingMask &=  ~(1 << LayerMask.NameToLayer("UI"));
        Debug.Log(BackgroundScreenshotCamera);
    }


    void LateUpdate()
    {     
        // Take a screenshot of the current background and label to send to the server
        StartCoroutine(TakeScreenshot());
    }


    IEnumerator TakeScreenshot() // take screenshot of the current background and label as Texture2D
    {
        yield return new WaitForEndOfFrame();
        // Get the current time to generat time stamp
        System.DateTime currentTime = DateTime.Now;
        string currentTimeAsString = currentTime.ToString("yyyy-MM-dd_hh-mm-ss") +".jpg";
        Debug.Log("saved file name");
        Debug.Log(currentTimeAsString);
        // Take a screenshot and save it with the timestamp above as its file name
        string filePath = System.IO.Path.Combine(Application.dataPath, currentTimeAsString);
        byte[] bytesBackground = I360Render.Capture(512, true, BackgroundScreenshotCamera, true);
        Debug.Log("background encoding executed"); // Uncomment for headset installation
        //File.WriteAllBytes(Application.dataPath + "/"+currentTimeAsString, bytesBackground);
    }


   
}
