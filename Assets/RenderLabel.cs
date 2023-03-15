
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

   
   
   
   public string selectMethod;
   public enum contrastMethods {Palette, HSV};

   public contrastMethods selectedMethod;
   public Dictionary<contrastMethods, Func<int[], Texture2D, int, Color>> assignColors;
   // Start is called before the first frame update
   void Start()
   {
      assignColors = new Dictionary<contrastMethods, Func<int[], Texture2D, int, Color>>();
      assignColors[contrastMethods.Palette] = AssignColor_usingPalette;
      assignColors[contrastMethods.HSV] = AssignColor_usingHSV;
      selectedMethod = contrastMethods.Palette;

      ScreenshotCamera = gameObject.GetComponent<Camera>(); 
      
      // Access labelPlane's material
      labelPlaneMaterial = labelPlane.GetComponent<MeshRenderer>().sharedMaterial;

      // Set labelPlaneMaterial's initial texture to a transparent layer
      labelPlaneMaterial.SetTexture("_MainTex", transparentLayer);

      for (int i = 0; i <= label.width; i++)
      {
         for (int j = 0; j <= label.height; j++)
         {
               if (label.GetPixel(i,j) == Color.white)
               {
                  labelX.Add(i);
                  labelY.Add(j);
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
      //yield return new WaitForEndOfFrame();

      // Clear out pixels in labelPlane by setting its texture to be transparent
      // labelPlaneMaterial.SetTexture("_MainTex", transparentLayer);

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

      // for (int i = 0; i < Screen.width; i++)
      // {
      //    for (int j = 0; j < Screen.height; j++)
      //    {
      //       Color transparentPxl = new Color(0f, 0f, 0f, 0f);
      //       Screenshot.SetPixel(i, j, transparentPxl);
      //    }
      // }

      // Read pixels on the screen into Screenshot. The pixels on the screen should be those from screenTexture
      Screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
      Screenshot.filterMode = FilterMode.Point;

      // Read whatever that's in the scene before the labels are added to the renderedLabel texture
      Texture2D renderedLabel = new Texture2D(Screen.width, Screen.height);
      renderedLabel.filterMode = FilterMode.Point;

      // Iterate through label pixel locations and change pixel colors. renderedLabel is the texture onto which the screenshot + the label are rendered 
      renderedLabel.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); 

      // for (int i = 0; i < Screen.width; i++)
      // {
      //    for (int j = 0; j < Screen.height; j++)
      //    {
      //       Color transparentPxl = new Color(0f, 0f, 0f, 0f);
      //       renderedLabel.SetPixel(i, j, transparentPxl);
      //    }
      // }

      // Set label pixel colors
      for (int i = 0; i < labelX.Count; i++)
      {
         int[] labelPixelCoord = new int[] {labelX[i], labelY[i]};
         Color newColor = assignColors[selectedMethod](labelPixelCoord, Screenshot, 4); // Modify this line to use a different color assignment model, 4 is the neighborhood size from which background pixels are sampled. This can change 
         renderedLabel.SetPixel(labelX[i], labelY[i], newColor);
      }

      // Render the new label colors on renderedLabel.
      renderedLabel.Apply();

      // Set labelPlaneMaterial's _MainTex to generated label + background
      labelPlaneMaterial.SetTexture("_MainTex", renderedLabel);
      
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

   // AssignColor samples neighboring pixels' values in a neighborhood of size neighborhoodSize+1 by neighborhoodSize+1 
   // and assigns a color to a label pixel at labelPixelCoord based on colors of pixels in the neigborhood
   public Color AssignColor_usingPalette(int[] labelPixelCoord, Texture2D backgroundImage, int neighborhoodSize)
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

   // AssignColor samples neighboring pixels' values in a neighborhood of size neighborhoodSize+1 by neighborhoodSize+1 
   // and assigns a color to a label pixel at labelPixelCoord based on colors of pixels in the neigborhood
   public Color AssignColor_usingHSV(int[] labelPixelCoord, Texture2D backgroundImage, int neighborhoodSize)
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

      //calculate color intensity
      double total_intensity = 0;
      float r = 0;
      float g = 0;
      float b = 0;


      for (int j = 0; j < neighboringPixelColors.Count; j++){
            total_intensity += neighboringPixelColors[j][0] * 0.2126 + neighboringPixelColors[j][1] * 0.7152 + neighboringPixelColors[j][2] * 0.0722;
            r += neighboringPixelColors[j][0];
            g += neighboringPixelColors[j][0];
            b += neighboringPixelColors[j][0];
      }
      
      Color average_color = new Color(r,g,b,1);

      double r_inverse = (float)1.0 - r/ neighboringPixelColors.Count;
      double g_inverse = (float)1.0 - g/ neighboringPixelColors.Count;
      double b_inverse = (float)1.0 - b/ neighboringPixelColors.Count;

      double intensity = total_intensity / neighboringPixelColors.Count;
      // Debug.Log("intensity" + intensity);

      //using intensity
      // Color palette_col = palette[distances.IndexOf(distances.Max())];
      // Color resulting_col = new Color(palette_col[0] * (float)intensity, palette_col[1] * (float)intensity, palette_col[2] * (float)intensity, 1);
      // labelColor = resulting_col;

      //using blur inverse
      // labelColor =  new Color((float)r_inverse, (float)g_inverse, (float)b_inverse, 1);

      //using 1-intensity
      // labelColor =  new Color((float)1.0 - (float)intensity, (float)1.0 - (float)intensity, (float)1.0 - (float)intensity, 1);

      //use HSV inverse
      float H, S, V;
      Color.RGBToHSV(average_color, out H, out S, out V);
      H = (H + (float)0.5) % (float)1;
      labelColor = Color.HSVToRGB(H, S, V);

      //use 1-HSV inverse
      Color HSV = Color.HSVToRGB(H, S, V);
      // labelColor = new Color((float)1.0 - HSV[0],(float)1.0 - HSV[1], (float)1.0 - HSV[2] );

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

