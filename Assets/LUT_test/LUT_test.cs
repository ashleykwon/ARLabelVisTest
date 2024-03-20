using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LUT_test : MonoBehaviour
{

    public GameObject cubeObj;
    Material cubeObjSphereMaterial;
    Texture3D LookupTable;
    Texture2D MainTexture;

    // Start is called before the first frame update
    void Start()
    {
        cubeObjSphereMaterial = cubeObj.GetComponent<MeshRenderer>().sharedMaterial;
        LookupTable = new Texture3D(256, 256, 256, TextureFormat.RGBAFloat, false);
        MainTexture = new Texture2D(300, 300);
        for (int r = 0; r <= 255; r++){
            for (int g = 0; g <= 255; g++){
                for (int b = 0; b <= 255; b++){
                    Color32 Red = new Color32(255, 0, 0, 255);
                    LookupTable.SetPixel(r, g, b, Red);
                }
            }
        }
        LookupTable.Apply();

        for (int x = 0; x <= 300; x++){
            for (int y = 0; y <= 300; y++){
                Color32 Green = new Color32(0, 255, 0, 255);
                MainTexture.SetPixel(x, y, Green);
            }
        }
        MainTexture.Apply();

        cubeObjSphereMaterial.SetTexture("_LUT", LookupTable);   
        cubeObjSphereMaterial.SetTexture("_MainTex", MainTexture); 
    }

    // Update is called once per frame
    void Update()
    {
    }
}
