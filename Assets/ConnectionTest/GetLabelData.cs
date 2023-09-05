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

public class GetLabelData : MonoBehaviour
{
    public TMP_Text label;
    public Camera BackgroundAndLabelScreenshotCamera;
    public Camera BackgroundScreenshotCamera;
    RenderTexture renderTexture;
    Texture2D BackgroundAndLabelScreenshotToSend;
    public GameObject player;
    private readonly string url = "http://10.38.23.43:8000/predict";
    DateTime currentTime;


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
        BackgroundAndLabelScreenshotCamera = gameObject.GetComponent<Camera>(); 
    }

    

    void LateUpdate()
    {     
        // Take a screenshot of the current background and label to send to the server
        StartCoroutine(PostScreenshot());
    }


    // IEnumerator TakeScreenshot() // take screenshot of the current background and label as Texture2D
    // {
    //     yield return new WaitForEndOfFrame();
    //     BackgroundAndLabelScreenshotToSend = ScreenCapture.CaptureScreenshotAsTexture();
    //     if (BackgroundAndLabelScreenshotToSend != null)
    //     {
    //         StartCoroutine(PostScreeshot(BackgroundAndLabelScreenshotToSend));
    //     }

    //     UnityEngine.Object.Destroy(BackgroundAndLabelScreenshotToSend);
    // }


    IEnumerator PostScreenshot() // Post the screenshot taken by TakeScreenshot() to the server
    {
        yield return new WaitForEndOfFrame();
        // Initiate screenshotContainer
 		ScreenshotData screenshotContainer = new ScreenshotData();
        
        // Convert the 360 degree screenshot of the background and the label to a string so that it can be attached to the container
        //screenshot.Compress(false); // can't compress the screenshot if it needs to be encoded to jpg, png ... etc.
        byte[] bytesBackgroundAndLabel = I360Render.Capture(512, true, BackgroundAndLabelScreenshotCamera, true);
        //File.WriteAllBytes(Application.dataPath + "/backgroundAndLabelScreenshot.jpg", bytesBackgroundAndLabel); // save the screenshot locally for debugging purposes only 
        string background_and_label_rgb_base64 = Convert.ToBase64String(bytesBackgroundAndLabel);

        // Convert the screenshot of the background to a string so that it can be attached to the container
        // Get the current date time to find the corresponding background screenshot
        currentTime = DateTime.Now;
        string currentTimeAsString = currentTime.ToString("yyyy-MM-dd_hh-mm-ss")+".jpg";
        string filePath = System.IO.Path.Combine(Application.dataPath, currentTimeAsString);
        // StartCoroutine(WaitingLoop()); // Wait until SendBackgroundImg.cs writes the background jpg file
        // byte[] bytesBackground = File.ReadAllBytes(filePath);
        // string background_rgb_base64 = Convert.ToBase64String(bytesBackground);

        // Attach the strings created above to screenshotContainer
        screenshotContainer.background_and_label_rgb_base64 = background_and_label_rgb_base64;
        screenshotContainer.background_rgb_base64 = background_and_label_rgb_base64; // this should be background_rgb_base64 derived above
        
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

    // private IEnumerator WaitingLoop()
    // {
    //     WaitForSeconds waitTime = new WaitForSeconds(10);
    //     while (true)
    //     {
    //         Debug.Log("waiting waiting.");
    //         yield return waitTime;
    //     }
    // }

}