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
    private readonly string url = "http://127.0.0.1:8000/predict"; 
    Texture2D Screenshot;
    
    bool serverOutputReceived = true;


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
        // Debug.Log(BackgroundScreenshotCamera);
    }


    void LateUpdate()
    {     
        // Take a screenshot of the current background and label to send to the server
        StartCoroutine(PostScreenshot());
    }


    IEnumerator PostScreenshot() // take screenshot of the current background and label as Texture2D
    {
        // Wait until the current frame is fully rendered
        yield return new WaitForEndOfFrame();

        // Initiate a container to hold the two screenshots
        ScreenshotData screenshotContainer = new ScreenshotData();

        // Take a 360 degree screenshot of the background only and convert it to a string so that it can be attached to the container
        // string filePath = System.IO.Path.Combine(Application.dataPath, "testBackground");
        byte[] bytesBackground = I360Render.Capture(1024, true, BackgroundScreenshotCamera, true);
        string background_rgb_base64 = Convert.ToBase64String(bytesBackground);
        // File.WriteAllBytes(Application.dataPath + "/background.jpg", bytesBackground); // for debugging purposes only 

        // Take a 360 degree screenshot of the background AND the label. Then convert the screenshot to a string so that it can be attached to the container
        byte[] bytesBackgroundAndLabel = I360Render.Capture(1024, true, Camera.main, true);
        string background_and_label_rgb_base64 = Convert.ToBase64String(bytesBackgroundAndLabel);
        // File.WriteAllBytes(Application.dataPath + "/backgroundAndLabel.jpg", bytesBackgroundAndLabel); // for debugging purposes only 

        // Debug.Log(Application.dataPath);

        // Debug.Log("Screenshot taken");

        // Attach the strings created above to screenshotContainer
        screenshotContainer.background_rgb_base64 = background_rgb_base64; 
        screenshotContainer.background_and_label_rgb_base64 = background_and_label_rgb_base64;
        
        // Convert screenshotContainer to a JSON object and send it to the server
        string bodyJsonString = JsonUtility.ToJson(screenshotContainer);

        // Initiate the request to send the screenshot
        var request = new UnityWebRequest(url, "PUT");
        byte[] encodedScreenshots = Encoding.UTF8.GetBytes(bodyJsonString);
        
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(encodedScreenshots);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

        // Specify the type of data in the request
        request.SetRequestHeader("Content-Type", "application/json");
        if (serverOutputReceived){
            yield return request.SendWebRequest();
            serverOutputReceived = false;
        }
        
        if (request.isNetworkError) // print network error if there is one
        {
            Debug.Log("Network Error: " + request.error);
            serverOutputReceived = false;
        }
        else if (request.isHttpError) // print http error if there is one
        {
            Debug.Log("Http Error: " + request.error);
            serverOutputReceived = false;
        }
        else // if there is no error, extract and render the string from the server
        {   
            string labelsRaw = request.downloadHandler.text;
            label.text = labelsRaw;
            serverOutputReceived = true;
        }

        // Dispose request after use to prevent memory loss
        request.Dispose();
    
    }
}