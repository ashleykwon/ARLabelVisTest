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
    public Texture2D BackgroundScreenshotToSend;
    public GameObject player;
    public OVRCameraRig OVRCameraRig;
    private readonly string url = "http://Your IP Address:8000/predict";


    public class ScreenshotData{
        public string background_and_label_rgb_base64;
        public string background_rgb_base64;
    }


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
        BackgroundAndLabelScreenshotCamera = gameObject.GetComponent<Camera>(); 
        // BackgroundScreenshotCamera = player.GetComponent<Camera>();
        // Debug.Log(BackgroundScreenshotCamera);
    }

    void Update()
    {
        //ScreenshotCamera.cullingMask &= (1 << LayerMask.NameToLayer("BackgroundAndLabel"));
        // Move and rotate the sphere with the player
        //backgroundAndLabelSphere.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
    }

    

    void LateUpdate()
    {     
        // Take a screenshot of the current background and label to send to the server
        StartCoroutine(TakeScreenshot());
    }


    IEnumerator TakeScreenshot() // take screenshot of the current background and label as Texture2D
    {
        yield return new WaitForEndOfFrame();
        BackgroundAndLabelScreenshotToSend = ScreenCapture.CaptureScreenshotAsTexture();
        if (BackgroundAndLabelScreenshotToSend != null)
        {
            StartCoroutine(PostScreeshot(BackgroundAndLabelScreenshotToSend));
        }

        UnityEngine.Object.Destroy(BackgroundAndLabelScreenshotToSend);
    }


    IEnumerator PostScreeshot(Texture2D backgroundAndLabelScreenshot) // Post the screenshot taken by TakeScreenshot() to the server
    {
        // Initiate screenshotContainer
 		ScreenshotData screenshotContainer = new ScreenshotData();

        // Convert the screenshot of the background and the label to a string so that it can be attached to the container
        //screenshot.Compress(false); // can't compress the screenshot if it needs to be encoded to jpg, png ... etc.
        byte[] bytes = backgroundAndLabelScreenshot.EncodeToJPG();
        string background_and_label_rgb_base64 = Convert.ToBase64String(bytes);

        // Convert the screenshot of the background to a string so that it can be attached to the container
        // byte[] bytesBackground = BackgroundScreenshotToSend.EncodeToJPG();
        // string background_rgb_base64 = Convert.ToBase64String(bytesBackground)

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

}