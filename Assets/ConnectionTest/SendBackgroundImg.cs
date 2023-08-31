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

public class SendBackgroundImg : MonoBehaviour
{
    public Camera ScreenshotCamera;
    RenderTexture renderTexture;
    Texture2D backgroundScreenshotToSend;
    public GameObject player;
    private readonly string url = "http://10.38.23.43:8000/predict";


    // public class ScreenshotData{
    //     public string background_rgb_base64;
    // }


    // Start is called before the first frame update
    void Start()
    {
        int cubemapSize = 2048; // this can change for a better resolution
        
        // backgroundAndLabelSphereMaterial = backgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial;
     
        // Define a cube-shaped render texture for the background
        renderTexture = new RenderTexture(cubemapSize, cubemapSize, 16);
        renderTexture.dimension = UnityEngine.Rendering.TextureDimension.Cube;

         // To prevent antialiasing
        renderTexture.autoGenerateMips = false;
        renderTexture.useMipMap = false;
        renderTexture.filterMode = FilterMode.Point;

        // Access the screenshot camera
        ScreenshotCamera = gameObject.GetComponent<Camera>(); 
    }

    void Update()
    {
        //ScreenshotCamera.cullingMask &= (1 << LayerMask.NameToLayer("BackgroundAndLabel"));
        // Move and rotate the sphere with the player
        //backgroundAndLabelSphere.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
    }

    

    void LateUpdate()
    {     
        // Take a screenshot and render it to a cubemap
        // ScreenshotCamera.targetTexture = renderTexture;
        // RenderTexture.active = renderTexture;

        // Take a screenshot of the current background to send to the server
        StartCoroutine(TakeScreenshot());

        // RenderTexture.active = null;
    }


    IEnumerator TakeScreenshot() // take screenshot of the current background and label as Texture2D
    {
        yield return new WaitForEndOfFrame();
        backgroundScreenshotToSend = ScreenCapture.CaptureScreenshotAsTexture();
        UnityEngine.Object.Destroy(backgroundScreenshotToSend);
    }


   
}
