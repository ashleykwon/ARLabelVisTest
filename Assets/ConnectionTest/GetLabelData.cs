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
    public Camera ScreenshotCamera;
    public GameObject backgroundAndLabelSphere;
    public Material backgroundAndLabelSphereMaterial;
    RenderTexture renderTexture;
    Texture2D screenshotToSend;
    public GameObject player;
    private readonly string url = "http://Your IP Address:8000/predict";


    public class ScreenshotData{
        public string rgb_base64;
    }


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

        // Access the screenshot camera
        ScreenshotCamera = gameObject.GetComponent<Camera>(); 

        ScreenshotCamera.RenderToCubemap(renderTexture, 63);
    }

    void Update()
    {
        //ScreenshotCamera.cullingMask &= (1 << LayerMask.NameToLayer("BackgroundAndLabel"));
        // Move and rotate the sphere with the player
        backgroundAndLabelSphere.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
    }

    

    void LateUpdate()
    {     
        // Take a screenshot and render it to a cubemap
        ScreenshotCamera.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;

        // Render the background and the label
        ScreenshotCamera.RenderToCubemap(renderTexture, 63); 
        backgroundAndLabelSphereMaterial.SetTexture("_CubeMap", renderTexture);

        // Take a screenshot of the current background and label to send to the server
        StartCoroutine(TakeScreenshot());

        RenderTexture.active = null;
    }


    IEnumerator TakeScreenshot() // take screenshot of the current background and label as Texture2D
    {
        yield return new WaitForEndOfFrame();
        screenshotToSend = ScreenCapture.CaptureScreenshotAsTexture();
        if (screenshotToSend != null)
        {
            StartCoroutine(PostScreeshot(screenshotToSend));
        }

        UnityEngine.Object.Destroy(screenshotToSend);
    }


    IEnumerator PostScreeshot(Texture2D screenshot) // Post the screenshot taken by TakeScreenshot() to the server
    {
        // Initiate screenshotContainer
 		ScreenshotData screenshotContainer = new ScreenshotData();

        // Convert screen to a string so that it can be attached to the container
        //screenshot.Compress(false); // compress the screenshot if latency is caused
        byte[] bytes = screenshot.EncodeToJPG();
        string rgb_base64 = Convert.ToBase64String(bytes);

        // Attach the string created above to screenshotContainer
        screenshotContainer.rgb_base64 = rgb_base64;
        
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
