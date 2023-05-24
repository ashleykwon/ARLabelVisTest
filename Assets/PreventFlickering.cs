using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreventFlickering : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        OVRManager.display.displayFrequency = 72.0f;
        //XRSettings.eyeTextureResolutionScale = 1.4f;   
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
