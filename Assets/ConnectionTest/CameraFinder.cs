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

public class CameraFinder : MonoBehaviour
{
    Camera[] allCameras;
    private readonly string url = "http://127.0.0.1:8000/predict"; 
    public GameObject player;
    public TMP_Text label;
    RenderTexture rt;
    Texture2D screenShot;
    public class ScreenshotData{
        public string background_and_label_rgb_base64;
        public string background_rgb_base64;
        public string label_mask_rgb_base64;
    }

    bool serverOutputReceived = true;

    // Start is called before the first frame update
    void Start()
    {
        // Locate all three cameras (left, right, center eye anchors)
        allCameras = FindObjectsOfType<Camera>();

        
        // Right eye anchor: Make sure that the background screenshot camera doesn't capture the black sphere and the label 
        allCameras[0].cullingMask &=  ~(1 << LayerMask.NameToLayer("UI"));
        allCameras[0].cullingMask &=  ~(1 << LayerMask.NameToLayer("Label"));

        // Center eye anchor: 
        allCameras[1].cullingMask &=  ~(1 << LayerMask.NameToLayer("Label"));
        
        // Left eye anchor: Occlude all layers except for the label layer (black background and white label) for the label screenshot camera
        allCameras[2].cullingMask &= (1 << LayerMask.NameToLayer("Label"));
        
        // initiate render texture
        int resWidth = allCameras[0].pixelWidth;
        int resHeight = allCameras[0].pixelHeight;
        rt = new RenderTexture(resWidth, resHeight, 24);
        screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
    }

    

    // Update is called once per frame
    void Update()
    {
        StartCoroutine(PostScreenshots());
    }

    IEnumerator PostScreenshots(){
        // Wait until the current frame is fully rendered
        yield return new WaitForEndOfFrame();

        ScreenshotData screenshotContainer = takePics();

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

    string ScreenShotName(int width, int height, int cameraIdx) // for debugging purposes only
    {
        Debug.Log(Application.dataPath + "/" + cameraIdx.ToString() + ".jpg");
        return Application.dataPath + "/" + cameraIdx.ToString() + ".jpg";
        
    }

    ScreenshotData takePics()
    {
        ScreenshotData screenshotContainer = new ScreenshotData();

        for (int i = 0; i < allCameras.Length; i++)
        {
            // get the current camera
            Camera currentCamera = allCameras[i].GetComponent<Camera>();

            
            
            currentCamera.targetTexture = rt;

            // Render the currentCamera's view onto the render texture defined above
            currentCamera.Render();
            
            RenderTexture.active = rt;

            // Read pixels from the render texture onto a texture 2D object
            screenShot.ReadPixels(currentCamera.pixelRect, 0, 0);
            screenShot.Apply();

            // Encode the screenshot as a jpg file
            byte[] bytes = screenShot.EncodeToJPG();
            string screenshotAsString = Convert.ToBase64String(bytes);
            if (i == 0){
                screenshotContainer.background_and_label_rgb_base64 = screenshotAsString;
            }
            else if (i == 1){
                screenshotContainer.label_mask_rgb_base64 = screenshotAsString;
            }
            else{
                screenshotContainer.background_rgb_base64 = screenshotAsString;
            }

            // Save the screenshot (for debugging purposes only)
            // string filename = ScreenShotName(resWidth, resHeight, i);
            // System.IO.File.WriteAllBytes(filename, bytes);

            // Revert the currentCamera's target texture and currently active render texture to be able to take a screenshot from the next camera
            currentCamera.targetTexture = null;
            RenderTexture.active = null;
            rt.Release();
        }
        
        return screenshotContainer;
    }
}
