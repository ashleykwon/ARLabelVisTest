using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; 
// using static AndroidExtensions;
// using UnityEngine.AndroidJavaClass;

public class TakeScreenshot : MonoBehaviour
{
    //public RenderTexture overviewTexture;
    //GameObject OVcamera;
    public string path = "";

    public Camera camOV;

     void Start()
     {
        camOV = gameObject.GetComponent<Camera>();  
      //   if (camOV.targetTexture == null){
      //       camOV.targetTexture = new RenderTexture(Screen.width, Screen.height, 24);
      //   } // this line causes the screen to turn completely black when I run this on headset. 
     }
 
   //   void LateUpdate()
   //   {           
   //      StartCoroutine(TakeScreenShot());  
   //   }
 
     // return file name
   //   string fileName(int width, int height)
   //   {
   //      return string.Format("screen_{0}x{1}_{2}.png",
   //                            width, height,
   //                            System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
   //   }
 
   //   public IEnumerator TakeScreenShot()
   //   {
   //       yield return new WaitForEndOfFrame();
 
   //       camOV = gameObject.GetComponent<Camera>();  
   //       RenderTexture currentRT = RenderTexture.active;    
   //       RenderTexture.active = camOV.targetTexture;
   //       camOV.Render();

   //       // Define an empty texture called imageOverview
   //       Texture2D imageOverview = new Texture2D(camOV.targetTexture.width, camOV.targetTexture.height, TextureFormat.RGB24, false);
   //       imageOverview.ReadPixels(new Rect(0, 0, camOV.targetTexture.width, camOV.targetTexture.height), 0, 0);
   //       imageOverview.Apply();
   //       RenderTexture.active = currentRT;    
         
   //      var appName = Application.identifier;
   //      var fileName = $"{appName}-{DateTime.Now:yyMMdd-hhmmss}";
   //      // #if UNITY_EDITOR
   //      //         // MiscExtensions.SaveScreenShotInternally(imageOverview, fileName);
   //      // #elif UNITY_ANDROID
   //      //         AndroidExtensions.SaveImageToGallery(imageOverview, fileName, "Some description");
   //      // #endif

   //      // Encode texture into PNG
   //      byte[] bytes = imageOverview.EncodeToPNG();
         
   //      // Try to see if the screenshot is null or not
   //      Debug.Log("Screenshot pixels");
   //      //Debug.Log(imageOverview.GetPixel(0,0));

   //      // save in memory
   //      // string filename = fileName(Convert.ToInt32(imageOverview.width), Convert.ToInt32(imageOverview.height));
   //      //path = Application.dataPath + filename;    
   //      // path = GetAndroidExternalStoragePath()+"/"+filename; 
   //      //Debug.Log("Screenshot taken"); 
   //      //Debug.Log(fileName); 
   //      // System.IO.File.WriteAllBytes(path, bytes);
   //   }

    //  private string GetAndroidExternalStoragePath()
    // {
    //     if (Application.platform != RuntimePlatform.Android)
    //         return Application.persistentDataPath;

    //     var jc = new AndroidJavaClass("android.os.Environment");
    //     var path = jc.CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory", 
    //         jc.GetStatic<string>("DIRECTORY_DCIM"))
    //         .Call<string>("getAbsolutePath");
    //     return path;
    // }
}
