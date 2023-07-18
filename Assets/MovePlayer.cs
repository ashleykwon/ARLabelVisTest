using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OVR;

public class MovePlayer : MonoBehaviour
{
    public Rigidbody player;
    public float speed;
    public GameObject BackgroundAndLabelSphere;
    int CurrentColorAssignmentAlgo; // Doesn't need to be specified at Start
    int CurrentBillboardColorAssignmentAlgo; // Doesn't need to be specified at Start
    Material labelSphereMaterial; // Doesn't need to be specified at Start


    // Start is called before the first frame update
    void Start()
    {
        CurrentColorAssignmentAlgo = 1;
        CurrentBillboardColorAssignmentAlgo = 0;
        labelSphereMaterial = BackgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial;

    }

    // Update is called once per frame
    void Update()
    {
        // Debug.Log("right joystick triggered");
        
        var joystickAxis = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick, OVRInput.Controller.RTouch);
        float fixedY = player.position.y;

        player.position += (transform.right * joystickAxis.x + transform.forward * joystickAxis.y) * Time.deltaTime * speed;
        player.position = new Vector3(player.position.x, 0, player.position.z);

        float triggerRight = OVRInput.Get(OVRInput.RawAxis1D.RIndexTrigger);
        bool AButtonPressed =  OVRInput.Get(OVRInput.Button.One);
        bool BButtonPressed =  OVRInput.Get(OVRInput.Button.Two);


        // Change color assignment algorithm on trigger
        if (triggerRight > 0.5f)
        {
            Debug.Log("Right joystick triggered");
            CurrentColorAssignmentAlgo += 1;
            if (CurrentColorAssignmentAlgo > 4)
            {
                CurrentColorAssignmentAlgo = 1;
            }
            ChangeColorAssignmentAlgo(CurrentColorAssignmentAlgo);
        }

        if (AButtonPressed)
        {
            Debug.Log("A button pressed");
            int currentInt = labelSphereMaterial.GetInt("_EnableOutline");
            if (currentInt == 0)
            {
                labelSphereMaterial.SetInt("_EnableOutline", 1);
            }
            else if (currentInt == 1)
            {
                labelSphereMaterial.SetInt("_EnableOutline", 0);
            }  
        }

        if (BButtonPressed)
        {
            Debug.Log("B button pressed");
            // int currentInt = labelSphereMaterial.GetInt("_EnableShadow");
            // if (currentInt == 0)
            // {
            //     labelSphereMaterial.SetInt("_EnableShadow", 1);
            // }
            // else if (currentInt == 1)
            // {
            //     labelSphereMaterial.SetInt("_EnableShadow", 0);
            // }
            CurrentBillboardColorAssignmentAlgo += 1;
            if (CurrentBillboardColorAssignmentAlgo > 2)
            {
                CurrentBillboardColorAssignmentAlgo = 0;
            }
            labelSphereMaterial.SetInt("_BillboardColorMethod", CurrentBillboardColorAssignmentAlgo);
        }
    }


    public void ChangeColorAssignmentAlgo(int CurrentColorAssignmentAlgo)
    {
        Debug.Log("Color assignment algorithm changed!");
        labelSphereMaterial.SetInt("_ColorMethod", CurrentColorAssignmentAlgo);
    }
}
