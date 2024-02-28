using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCameraView : MonoBehaviour
{
    public Camera MainCamera;
    
    // Start is called before the first frame update
    void Start()
    {
        // Only show the sphere that contains the label
        // MainCamera.cullingMask &=  (1 << LayerMask.NameToLayer("BackgroundAndLabel"));
        MainCamera.cullingMask &= (1 << LayerMask.NameToLayer("PolygonUI")) | (1 << LayerMask.NameToLayer("BackgroundAndLabel"));

    }

    // Update is called once per frame
    void Update()
    {
        // Only include the layer that contains the label (it's a plane object that has a material + shader)
        //MainCamera.cullingMask &=  (1 << LayerMask.NameToLayer("Label"));
    }
}
