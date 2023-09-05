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
    
    public GameObject player;
    public TMP_Text label;
    RenderTexture renderTexture;
    private readonly string url = "http://10.38.23.43:8000/predict";
    Texture2D Screenshot;
    


    public class ScreenshotData{
        public string background_and_label_rgb_base64;
        public string background_rgb_base64;
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
    }

    void Update()
    {
        BackgroundScreenshotCamera.cullingMask &=  ~(1 << LayerMask.NameToLayer("UI"));
        Debug.Log(BackgroundScreenshotCamera);
    }


    void LateUpdate()
    {     
        // Take a screenshot of the current background and label to send to the server
        StartCoroutine(PostScreenshot());
    }


    IEnumerator PostScreenshot() // take screenshot of the current background and label as Texture2D
    {
        yield return new WaitForEndOfFrame();

        ScreenshotData screenshotContainer = new ScreenshotData();
        // Get the current time to generat time stamp
        // System.DateTime currentTime = DateTime.Now;
        // string currentTimeAsString = currentTime.ToString("yyyy-MM-dd_hh-mm-ss") +".jpg";
        // Debug.Log("saved file name");
        // Debug.Log(currentTimeAsString);

        // Take a 360 degree screenshot of the background only and convert it to a string so that it can be attached to the container
        // string filePath = System.IO.Path.Combine(Application.dataPath, currentTimeAsString);
        byte[] bytesBackground = I360Render.Capture(256, true, BackgroundScreenshotCamera, true);
        string background_rgb_base64 = Convert.ToBase64String(bytesBackground);
        //File.WriteAllBytes(Application.dataPath + "/"+currentTimeAsString, bytesBackground); // for debugging purposes only 

        // Take a 360 degree screenshot of the background AND the label. Then convert the screenshot to a string so that it can be attached to the container
        byte[] bytesBackgroundAndLabel = I360Render.Capture(256, true, Camera.main, true);
        string background_and_label_rgb_base64 = Convert.ToBase64String(bytesBackgroundAndLabel);
        //File.WriteAllBytes(Application.dataPath + "/"+currentTimeAsString+"_backgroundAndLabel.jpg", bytesBackgroundAndLabel); // for debugging purposes only 

        // Attach the strings created above to screenshotContainer
        screenshotContainer.background_rgb_base64 = background_rgb_base64; 
        screenshotContainer.background_and_label_rgb_base64 = background_and_label_rgb_base64;
        
        // Convert screenshotContainer to a JSON object and send it to the server
        string bodyJsonString = JsonUtility.ToJson(screenshotContainer);
        var request = new UnityWebRequest(url, "PUT");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
        
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        if (request.isNetworkError) // print network error if there is one
        {
            Debug.Log("Network Error: " + request.error);
        }
        else if (request.isHttpError) // print http error if there is one
        {
            Debug.Log("Http Error: " + request.error);
        }
        else // if there is no error, print and process the string received from the server
        {
            Debug.Log("Connection successful: " + request.downloadHandler.text);
            string labelsRaw = request.downloadHandler.text;
            Debug.Log(labelsRaw);
            label.text = labelsRaw;
        }

        // Dispose request after use to prevent memory loss
        request.Dispose();
    }


   
}
