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

        bool AButtonPressed =  OVRInput.Get(OVRInput.Button.One);
        bool BButtonPressed =  OVRInput.Get(OVRInput.Button.Two);

        float triggerLeft = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);
        float triggerRight = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);



        if (triggerLeft > 0.3f) // Change color assignment algorithm on left trigger
        {
            Debug.Log("Left trigger pressed");
            CurrentColorAssignmentAlgo += 1;
            if (CurrentColorAssignmentAlgo > 4)
            {
                CurrentColorAssignmentAlgo = 1;
            }
            ChangeColorAssignmentAlgo(CurrentColorAssignmentAlgo);
        }

        if (triggerRight > 0.3f) // Change billboard color assignment on right trigger 
        {
            Debug.Log("Right trigger pressed");
            CurrentBillboardColorAssignmentAlgo += 1;
            if (CurrentBillboardColorAssignmentAlgo > 2)
            {
                CurrentBillboardColorAssignmentAlgo = 0;
            }
            labelSphereMaterial.SetInt("_BillboardColorMethod", CurrentBillboardColorAssignmentAlgo);
        }

        if (AButtonPressed) // Toggle outline
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

        if (BButtonPressed) // Toggle shadow
        {
            Debug.Log("B button pressed");
            int shadowEnabled = labelSphereMaterial.GetInt("_EnableShadow");
            if (shadowEnabled == 0)
            {
                labelSphereMaterial.SetInt("_EnableShadow", 1);
            }
            else
            {
                labelSphereMaterial.SetInt("_EnableShadow", 0);
            }            
        }
    }


    public void ChangeColorAssignmentAlgo(int CurrentColorAssignmentAlgo)
    {
        Debug.Log("Color assignment algorithm changed!");
        labelSphereMaterial.SetInt("_ColorMethod", CurrentColorAssignmentAlgo);
    }
}
