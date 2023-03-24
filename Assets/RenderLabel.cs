
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

   
   
   public enum contrastMethods {Palette, HSV, LAB};
   public bool addOutline;

   public contrastMethods selectedMethod;

   public Dictionary<contrastMethods, Func<int[], Texture2D, int, Color>> assignColors;
   // Start is called before the first frame update
   void Start()
   {
      assignColors = new Dictionary<contrastMethods, Func<int[], Texture2D, int, Color>>();
      assignColors[contrastMethods.Palette] = AssignColor_usingPalette;
      assignColors[contrastMethods.HSV] = AssignColor_usingHSV;
      assignColors[contrastMethods.LAB] = AssignColor_usingCIELAB;

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
                  bool is_edge = label.GetPixel(i + 1, j)[0] == 0 || label.GetPixel(i, j + 1)[0] == 0 || label.GetPixel(i - 1, j)[0] == 0 || label.GetPixel(i, j - 1)[0] == 0;
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
      for (int i = 0; i < labelCoords.Count; i++)
      {
         (int labelX, int labelY, bool isEdge) = labelCoords[i];
         Color newColor = Color.white;

         if (!addOutline || !isEdge)
         {
            newColor = assignColors[selectedMethod](new int[] {labelX, labelY}, Screenshot, 4); // Modify this line to use a different color assignment model, 4 is the neighborhood size from which background pixels are sampled. This can change 
         }
         renderedLabel.SetPixel(labelX, labelY, newColor);
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

      float r = 0;
      float g = 0;
      float b = 0;
      for (int j = 0; j < neighboringPixelColors.Count; j++){
            r += neighboringPixelColors[j][0] / neighboringPixelColors.Count;
            g += neighboringPixelColors[j][1] / neighboringPixelColors.Count;
            b += neighboringPixelColors[j][2]/ neighboringPixelColors.Count;
      }
      Color average_color = new Color(r,g,b,1);

      // labelColor = method_three(average_color);

      return labelColor;
   }

   public Color method_one(Color myCol){
      //Saturation and value set to 1
      float H, S, V;
      Color.RGBToHSV(myCol, out H, out S, out V);
      H = (float)1.0;
      V = (float)1.0;
      Color labelColor = Color.HSVToRGB(H, S, V);
      return labelColor;
   }

   public Color method_two(Color myCol){
      //Value = 0 if background_hsv.value > 0.5, Value = 1 if background_hsv.value <= 0.5
      float H, S, V;
      Color.RGBToHSV(myCol, out H, out S, out V);
      if(V> 0.5){
         V = (float)0.0;
      }else{
         V = (float)1.0;
      }
      Color labelColor = Color.HSVToRGB(H, S, V);
      return labelColor;
   }

   public Color method_three(Color myCol){
   //If saturation is larger than 0.5, invert the background’s hue and saturation in the label and set its value to 1
   //Else, invert the value
      float H, S, V;
      Color.RGBToHSV(myCol, out H, out S, out V);
      if(S > 0.5){
         H = (float)1.0 - H;
         V = (float)1.0 - V;
         S = (float)1.0;
      }else{
         S = (float)1.0 - S;
      }
      Color labelColor = Color.HSVToRGB(H, S, V);
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
      // float H, S, V;
      // Color.RGBToHSV(average_color, out H, out S, out V);
      // H = (float)1.0;
      // V = (float)1.0;
      // labelColor = Color.HSVToRGB(H, S, V);

      // //use 1-HSV inverse
      // Color HSV = Color.HSVToRGB(H, S, V);
      // labelColor = new Color((float)1.0 - HSV[0],(float)1.0 - HSV[1], (float)1.0 - HSV[2]);

      return labelColor;
   }

   // Naive method using thresholding
   public Color AssignColor_usingCIELAB(int[] labelPixelCoord, Texture2D backgroundImage, int neighborhoodSize) 
   {
      List<Color> neighboringPixelColors = SamplePixelColors(labelPixelCoord, backgroundImage, neighborhoodSize);
      float R = 0, G = 0, B = 0;
      foreach (Color c in neighboringPixelColors){
         R += c[0];
         G += c[1];
         B += c[2];
      }

      R /= (float) neighboringPixelColors.Count;
      G /= (float) neighboringPixelColors.Count;
      B /= (float) neighboringPixelColors.Count;

      
      // Color bgCol = backgroundImage.GetPixel(labelPixelCoord[0], labelPixelCoord[1]);
      Color bgCol = new Color(R, G, B);
      
      Vector3 bgLAB = RGB_to_LAB(bgCol);

      float L = bgLAB[0];
      float newL = 100 - L;

      if (25 >= L && L <= 50){
         newL += 25;
      }

      if (50 < L && L <= 25) {
         newL -= 25;
      }

      bgLAB[0] = newL;

      return LAB_TO_RGB(bgLAB); // replace
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

   // code based of http://www.easyrgb.com/en/math.php
   public Vector3 RGB_to_LAB(Color RGB) {
      double R = RGB[0];
      double G = RGB[1];
      double B = RGB[2];

      // reference values, D65/2°
      double Xr = 95.047;  
      double Yr = 100.0;
      double Zr = 108.883;

      double var_R = R; //(R / 255.0);
      double var_G = G; //(G / 255.0);
      double var_B = B; //(B / 255.0);

      if (var_R > 0.04045) 
         var_R = Math.Pow(((var_R + 0.055) / 1.055), 2.4);
      else
         var_R = var_R / 12.92;
      if (var_G > 0.04045)
         var_G = Math.Pow(((var_G + 0.055) / 1.055), 2.4);
      else
         var_G = var_G / 12.92;
      if (var_B > 0.04045)
         var_B = Math.Pow(((var_B + 0.055) / 1.055), 2.4);
      else
         var_B = var_B / 12.92;

      var_R *= 100;
      var_G *= 100;
      var_B *= 100;

      double X = var_R * 0.4124 + var_G * 0.3576 + var_B * 0.1805;
      double Y = var_R * 0.2126 + var_G * 0.7152 + var_B * 0.0722;
      double Z = var_R * 0.0193 + var_G * 0.1192 + var_B * 0.9505;

      // now convert from XYZ to LAB

      double var_X = X / Xr;
      double var_Y = Y / Yr;
      double var_Z = Z / Zr;

      if (var_X > 0.008856)
         var_X = Math.Pow(var_X, 1/3.0);
      else
         var_X = (7.787 * var_X) + (16 / 116);
      if (var_Y > 0.008856)
         var_Y = Math.Pow(var_Y, 1/3.0);
      else
         var_Y = (7.787 * var_Y) + (16 / 116);
      if (var_Z > 0.008856)
         var_Z = Math.Pow(var_Z, 1/3.0);
      else
         var_Z = (7.787 * var_Z) + (16 / 116);

      Vector3 LAB = new Vector3();

      LAB[0] = (float) ((116 * var_Y) - 16);
      LAB[1] = (float) (500 * (var_X - var_Y));
      LAB[2] = (float) (200 * (var_Y - var_Z)); // Not sure why this was originally LAB[3]

      return LAB;
   } 

   // based off of http://www.easyrgb.com/en/math.php
   public Color LAB_TO_RGB(Vector3 LAB){
      double L = LAB[0];
      double A = LAB[1];
      double B = LAB[2];

      // reference values, D65/2°
      double Xr = 95.047;  
      double Yr = 100.0;
      double Zr = 108.883;

      // first convert LAB to XYZ
      double var_Y = (L + 16.0) / 116.0;
      double var_X = A / 500 + var_Y;
      double var_Z = var_Y - B / 200;

      if (Math.Pow(var_Y, 3)  > 0.008856) 
         var_Y = Math.Pow(var_Y, 3);
      else
         var_Y = (var_Y - 16 / 116) / 7.787;
      if (Math.Pow(var_X, 3)  > 0.008856)
         var_X = Math.Pow(var_X, 3);
      else
         var_X = (var_X - 16 / 116) / 7.787;
      if (Math.Pow(var_Z, 3)  > 0.008856) 
         var_Z = Math.Pow(var_Z, 3);
      else
         var_Z = (var_Z - 16 / 116) / 7.787;

      double X = var_X * Xr;
      double Y = var_Y * Yr;
      double Z = var_Z * Zr;

      // now convert XYZ to RGB

      X /= 100.0;
      Y /= 100.0;
      Z /= 100.0;

      double var_R = var_X *  3.2406 + var_Y * -1.5372 + var_Z * -0.4986;
      double var_G = var_X * -0.9689 + var_Y *  1.8758 + var_Z *  0.0415;
      double var_B = var_X *  0.0557 + var_Y * -0.2040 + var_Z *  1.0570;

      if (var_R > 0.0031308) 
         var_R = 1.055 * (Math.Pow(var_R, (1 / 2.4))) - 0.055;
      else
         var_R = 12.92 * var_R;
      if (var_G > 0.0031308) 
         var_G = 1.055 * (Math.Pow(var_G, (1 / 2.4))) - 0.055;
      else
         var_G = 12.92 * var_G;
      if (var_B > 0.0031308) 
         var_B = 1.055 * (Math.Pow(var_B, (1 / 2.4))) - 0.055;
      else
         var_B = 12.92 * var_B;

      // Color RGB = new Color((float)var_R * 255, (float)var_G * 255, (float)var_B * 255);
      Color RGB = new Color((float)var_R, (float)var_G, (float)var_B);

      return RGB;
   }

    
}

