using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCameraView : MonoBehaviour
{
    public Camera MainCamera;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Block out the layer that contains the label (it's a plane object that has a material + shader)
        MainCamera.cullingMask &=  (1 << LayerMask.NameToLayer("Label"));
    }
}
