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

   public List<int> labelX = new List<int>();
   public List<int> labelY = new List<int>();
   public Material labelPlaneMaterial;

   // Start is called before the first frame update
   void Start()
   {
      ScreenshotCamera = gameObject.GetComponent<Camera>(); 
      
      // Access labelPlane's material
      labelPlaneMaterial = labelPlane.GetComponent<MeshRenderer>().sharedMaterial;

      // Set labelPlaneMaterial's initial texture to a transparent layer
      labelPlaneMaterial.SetTexture("_MainTex", transparentLayer);

      for (int i = 0; i <= label.width; i++)
      {
         for (int j = 0; j <= label.height; j++)
         {
               if ((label.GetPixel(i,j)[0] == 1) && (label.GetPixel(i,j)[1] == 1) && (label.GetPixel(i,j)[2] == 1))
               {
                  labelX.Add(i);
                  labelY.Add(j);
               }   
         }
      }
   }

   
   
   // LateUpdate calls the function that extracts frames from ScreenshotCamera and generates label colors
   void LateUpdate()
   {           
      StartCoroutine(TakeScreenShot());
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
   public IEnumerator TakeScreenShot()
   {
      yield return new WaitForEndOfFrame();

      // Clear out pixels in labelPlane by setting its texture to be transparent
      //labelPlaneMaterial.SetTexture("_MainTex", transparentLayer);

      // Block out the layer that contains the label (it's a plane object that has a material + shader)
      ScreenshotCamera.cullingMask &=  ~(1 << LayerMask.NameToLayer("Label"));
      
      // Update labelPlane's position and rotation according to ScreenshotCamera's rotation and position
      float newZ = ScreenshotCamera.transform.position[2] + 8.5f; //This 8.5 may vary depending on the distance between the screenshot camera and the label plane, which should remain consistent
      labelPlane.transform.position = new Vector3(ScreenshotCamera.transform.position[0], ScreenshotCamera.transform.position[1], newZ);
      
      float newXAngle = ScreenshotCamera.transform.eulerAngles.x;
      float newYAngle = ScreenshotCamera.transform.eulerAngles.y;
      labelPlane.transform.Rotate(newXAngle, newYAngle, ScreenshotCamera.transform.rotation[2]);
      
      // Extract the current frame from ScreenshotCamera and copy its pixel values into Screenshot
      RenderTexture screenTexture = new RenderTexture(Screen.width, Screen.height, 16);
      screenTexture.autoGenerateMips = false;
      screenTexture.filterMode = FilterMode.Point;
      ScreenshotCamera.targetTexture = screenTexture;
      RenderTexture.active = screenTexture;
      ScreenshotCamera.Render();

      // Screenshot is the background without the label
      Texture2D Screenshot = new Texture2D(Screen.width, Screen.height);

      // Read pixels on the screen into Screenshot
      Screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
      Screenshot.filterMode = FilterMode.Point;

      // Read whatever that's in the scene before the labels are added to the renderedLabel texture
      Texture2D renderedLabel = new Texture2D(Screen.width, Screen.height);
      renderedLabel.filterMode = FilterMode.Point;
      
      // Iterate through label pixel locations and change pixel colors. renderedLabel is the texture onto which the screenshot + the label are rendered 
      //renderedLabel.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); 

      // Clear out all pixel values that are already in renderedLabel (those from the previous frame)
      for (int i = 0; i < Screen.width; i++)
      {
         for (int j = 0; j < Screen.height; j++)
         {
            Color transparentPixelVal = new Color(0f, 0f, 0f, 0f);
            renderedLabel.SetPixel(i, j, transparentPixelVal);
         } 
      }
      for (int i = 0; i < labelX.Count; i++)
      {
         int[] labelPixelCoord = new int[] {labelX[i], labelY[i]};
         Color newColor = AssignColor(labelPixelCoord, Screenshot, 4); // 6 is the neighborhood size from which background pixels are sampled. This can change 
         renderedLabel.SetPixel(labelX[i], labelY[i], newColor);
      }

      // Render the new label colors on renderedLabel.
      renderedLabel.Apply();

      

      // Set labelPlaneMaterial's _MainTex to generated label + background
      labelPlaneMaterial.SetTexture("_MainTex", renderedLabel);
      
      // Source: https://docs.unity3d.com/ScriptReference/Material.SetTexture.html
      

      RenderTexture.active = null;

      // Save renderedTexture (only for debugging purposes)
      //   byte[] byteArray = renderedLabel.EncodeToPNG();
      //   string filename = fileName(Convert.ToInt32(renderedLabel.width), Convert.ToInt32(renderedLabel.height));
      //   path = Application.dataPath + filename;  
      //   System.IO.File.WriteAllBytes(path, byteArray);

        

      // Source: https://gamedevbeginner.com/how-to-capture-the-screen-in-unity-3-methods/ 
   }

   // AssignColor samples neighboring pixels' values in a neighborhood of size neighborhoodSize+1 by neighborhoodSize+1 
   // and assigns a color to a label pixel at labelPixelCoord based on colors of pixels in the neigborhood
   public Color AssignColor(int[] labelPixelCoord, Texture2D backgroundImage, int neighborhoodSize)
   {
      Color[] palette = { new Color(0, 0, 0, 1), new Color(1, 0, 0, 1), new Color(1, 1, 0, 1), new Color(0, 0, 1, 1), new Color(1, 0, 1, 1),  new Color(0, 1, 1, 1), new Color(1, 1, 1, 1)};
      Color labelColor = palette[0]; // Default labelColor. May or may not change
      List<double> distances = new List<double>();
      List<Color> neighboringPixelColors = SamplePixelColors(labelPixelCoord, backgroundImage, neighborhoodSize);
      for (int i = 0; i < palette.Length; i++)
      {
         double dist = 0;
         for (int j = 0; j < neighboringPixelColors.Count; j++){
            dist += CalculateDistance(palette[i], neighboringPixelColors[j]);
         }
         distances.Add(dist);
      }
      labelColor = palette[distances.IndexOf(distances.Max())];
      return labelColor;
   }



   // Samples neighboring pixels of labelPixelCoord in backgroundImage 
   public List<Color> SamplePixelColors(int[] labelPixelCoord, Texture2D backgroundImage, int neighborhoodSize)
   {
      List<Color> neighbors = new List<Color>();
      for (int i = -1 * (neighborhoodSize/2); i <= neighborhoodSize/2; i++){
         for (int j = -1 * (neighborhoodSize/2); j <= neighborhoodSize/2; j++){
            neighbors.Add(backgroundImage.GetPixel(labelPixelCoord[0]+i,labelPixelCoord[1]+j));
         }
      }
      return neighbors;
   }
   


   // Calculates the distance between two Color objects
   public double CalculateDistance(Color labelPixel, Color neighborPixel)
   {
      double distance = Math.Pow((labelPixel[0] - neighborPixel[0]), 2) + Math.Pow((labelPixel[1] - neighborPixel[1]), 2) + Math.Pow((labelPixel[2] - neighborPixel[2]), 2); 
      return Math.Sqrt(distance);
   }
}
