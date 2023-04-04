using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; 
using System.Resources;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class ImageProcessing 
{
   public static Texture2D conv2D(Texture2D image, float[,] filter)
   {
      Texture2D res = new Texture2D(image.width, image.height);
      for (int x = 0; x < image.width; x++){
         for (int y = 0; y < image.height; y++){
            float r = 0;
            float g = 0;
            float b = 0;
            for (int j = 0; j < filter.GetLength(1); j++){
               for (int i = 0; i < filter.GetLength(0); i++){
                  if (x + i >= image.width || y + j >= image.height){
                     continue;
                  }
                  Color c = image.GetPixel(x + i, y + j);
                  r += c[0] * filter[j, i];
                  g += c[1] * filter[j, i];
                  b += c[2] * filter[j, i];
               }
            }
            res.SetPixel(x, y, new Color(r, g, b));
         }
      }
      res.Apply();
      return res;
   }

   public static float[,] boxBlur(int sz)
   {
      float[,] filter = new float[sz, sz];
      for (int i = 0; i < sz; i++){
         for (int j = 0; j < sz; j++){
            filter[i, j] = 1f / (sz * sz);
         } 
      }
      return filter;
   }

   public static float[,] gaussianBlur(int sz, float sigma)
   {
      float[,] filter = new float[sz, sz];
      float rescale = (float) Math.Sqrt(2 * Math.PI);
      for (int i = 0; i < sz; i++){
         for (int j = 0; j < sz; j++){
            double x = j / (float) sz - 0.5f;
            double y = i / (float) sz - 0.5f;
            double rsq = x * x + y * y;
            filter[i, j] = (float) Math.Exp(1 / 2f * rsq) / rescale;
         } 
      }
      return filter;
   }
}