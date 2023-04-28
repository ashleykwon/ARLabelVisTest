
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; 
using System.Resources;
using TMPro;
using UnityEngine.UI;
using System.Linq;



public class RenderLabel : MonoBehaviour
{
   public Camera ScreenshotCamera;
   public string path = "";
   public GameObject labelPlane;

   public Texture2D label;
   public Texture2D transparentLayer;

   private List<(int, int, bool)> labelCoords = new List<(int, int, bool)>();  // (x, y, isEdge)
   public Material labelPlaneMaterial;

   public int neighborhoodSize;
   
   public enum contrastMethods {Palette, HSV, LAB};
   public bool addOutline;


   public contrastMethods selectedMethod;

   public Dictionary<contrastMethods, Func<int[], Texture2D, int, Color>> assignColors;
   // Start is called before the first frame update
   void Start()
   {
      assignColors = new Dictionary<contrastMethods, Func<int[], Texture2D, int, Color>>();
      assignColors[contrastMethods.Palette] = ColorAssignment.AssignColor_usingPalette;
      assignColors[contrastMethods.HSV] = ColorAssignment.AssignColor_usingHSV;
      assignColors[contrastMethods.LAB] = ColorAssignment.AssignColor_usingCIELAB;

      ScreenshotCamera = gameObject.GetComponent<Camera>(); 
      
      // Access labelPlane's material
      labelPlaneMaterial = labelPlane.GetComponent<MeshRenderer>().sharedMaterial;

      // Set labelPlaneMaterial's initial texture to a transparent layer
      labelPlaneMaterial.SetTexture("_MainTex", transparentLayer);
      // labelPlaneMaterial.SetTexture("_TextMatte", transparentLayer);

      Debug.Log(label.width);
      

      for (int i = 0; i <= label.width ; i++)
      {
         for (int j = 0; j <= label.height; j++)
         {
               int x = (int)Math.Round(i * (double)label.width / Screen.width);
               int y = (int)Math.Round(j * (double)label.height /Screen.height);

               if (label.GetPixel(x,y) == Color.white)
               {
                  bool is_edge = label.GetPixel(x + 1, y)[0] == 0 || label.GetPixel(x, y + 1)[0] == 0 || label.GetPixel(x - 1, y)[0] == 0 || label.GetPixel(x, y - 1)[0] == 0;
                  labelCoords.Add((i, j, is_edge));
               }   
         }
      }
   }


   void Update()
   {
      // Update labelPlane's position and rotation according to ScreenshotCamera's rotation and position
      float newZ = ScreenshotCamera.transform.position[2] + 8.5f; //This 8.5 may vary depending on the distance between the Screenshot camera and the label plane, which should remain consistent
      labelPlane.transform.position = new Vector3(ScreenshotCamera.transform.position.x, ScreenshotCamera.transform.position.y, newZ);

      float newXAngle = ScreenshotCamera.transform.eulerAngles.x;
      float newYAngle = ScreenshotCamera.transform.eulerAngles.y;
      labelPlane.transform.Rotate(newXAngle, newYAngle, ScreenshotCamera.transform.rotation[2]);

   }

   
   
   // // LateUpdate calls the function that extracts frames from ScreenshotCamera and generates label colors
   void LateUpdate()
   {           
      TakeScreenShot();
   }



     // Return file name for saved screenshot 
     // (this isn't necessary if I'm not saving frames from ScreenshotCamera for testing purposes)
   string fileName(int width, int height)
   {
      return string.Format("screen_{0}x{1}_{2}.png",
                           width, height,
                           System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
   }
 

     // Extract frames from ScreenshotCamera and generate label colors based on the frames
   public void TakeScreenShot()
   {

      // Block out the layer that contains the label (it's a plane object that has a material + shader)
      ScreenshotCamera.cullingMask &=  ~(1 << LayerMask.NameToLayer("Label"));
      
      // Create a new render texture for the current frame
      RenderTexture screenTexture = new RenderTexture(Screen.width, Screen.height, 16);

      // screenTexture settings to prevent antialiasing
      screenTexture.autoGenerateMips = false;
      screenTexture.filterMode = FilterMode.Point;

      // Set ScreenshotCamera's target texture (the texture onto which the current scene is rendered) to screenTexture 
      // and manually render the camera
      ScreenshotCamera.targetTexture = screenTexture;
      RenderTexture.active = screenTexture;
      ScreenshotCamera.Render();

      // Screenshot is the background without the label
      Texture2D Screenshot = new Texture2D(Screen.width, Screen.height);

      // Read pixels on the screen into Screenshot. The pixels on the screen should be those from screenTexture
      Screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
      Screenshot.filterMode = FilterMode.Point;

      // Read whatever that's in the scene before the labels are added to the renderedLabel texture
      Texture2D renderedLabel = new Texture2D(Screen.width, Screen.height);
      renderedLabel.filterMode = FilterMode.Point;

      // Iterate through label pixel locations and change pixel colors. renderedLabel is the texture onto which the screenshot + the label are rendered 
      renderedLabel.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);

      // Set label pixel colors
      Texture2D textMatte = new Texture2D(renderedLabel.width, renderedLabel.height);
      double _lambda = 0.8;
      for (int i = 0; i < labelCoords.Count; i++)
      {
         (int labelX, int labelY, bool isEdge) = labelCoords[i];
         Color newColor = Color.white;

         // if (!addOutline || !isEdge)
         // {
         //    newColor = assignColors[selectedMethod](new int[] {labelX, labelY}, Screenshot, neighborhoodSize); // Modify this line to use a different color assignment model, 4 is the neighborhood size from which background pixels are sampled. This can change 
         // }

         // if (UnityEngine.Random.Range(0.0f, 1.0f) > _lambda){
         //    newColor = new Color(1.0f,0.0f,0.0f);
         // }
         renderedLabel.SetPixel(labelX, labelY, newColor);
         textMatte.SetPixel(labelX, labelY, newColor);
      }
        


      // Render the new label colors on renderedLabel.
      renderedLabel.Apply();

      // 
      // 

      // Set labelPlaneMaterial's _MainTex to generated label + background
      // labelPlaneMaterial.SetTexture("_MainTex", blurred);
      labelPlaneMaterial.SetTexture("_MainTex", renderedLabel);
      // labelPlaneMaterial.SetTexture("_TextMatte", textMatte);
      
      // Set RenderTexture to null for rendering the next frame
      RenderTexture.active = null;

      // Save renderedTexture (only for debugging purposes)
      //   byte[] byteArray = Screenshot.EncodeToPNG();
      //   string filename = fileName(Convert.ToInt32(Screenshot.width), Convert.ToInt32(Screenshot.height));
      //   path = Application.dataPath + filename;  
      //   System.IO.File.WriteAllBytes(path, byteArray);

        
      // Source: https://docs.unity3d.com/ScriptReference/Material.SetTexture.html
      // Source: https://gamedevbeginner.com/how-to-capture-the-screen-in-unity-3-methods/ 
   }
    
}

