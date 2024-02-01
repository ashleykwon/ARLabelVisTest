using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OVR;
using TMPro;

public class MovePlayer : MonoBehaviour
{
    public Rigidbody player;
    public float speed;
    public GameObject BackgroundAndLabelSphere;
    public GameObject LabelContainer;
    public TMP_Text labelColorMode;
    public TMP_Text billboardColorMode;
    public TMP_Text granularitymode;
    int CurrentColorAssignmentAlgo; // Doesn't need to be specified at Start
    int CurrentBillboardColorAssignmentAlgo; // Doesn't need to be specified at Start
    // int shadowIntensityIdx;
    public int currentLabelMovementMode;
    //public Vector3 DefaultLabelPosition;
    Material labelSphereMaterial; // Doesn't need to be specified at Start
    string[] labelColorModesList;
    string[] billboardColorModesList;
    string[] granularityModesList;
    


    // Start is called before the first frame update
    void Start()
    {
        CurrentColorAssignmentAlgo = 1;
        CurrentBillboardColorAssignmentAlgo = 0;
        // shadowIntensityIdx = 0;
        currentLabelMovementMode = 0;
        labelSphereMaterial = BackgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial;
        labelColorModesList = new string[] {"Palette", "RGBA reversed", "HSV-based", "CIELAB-based"};
        billboardColorModesList = new string[] {"None", "Blue", "HSV-based"};
        granularityModesList = new string[] {"Per-pixel", "Background"};
    }


    // Update is called once per frame
    void Update()
    {
        // Debug.Log("right joystick triggered");
        
        var joystickAxis = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick, OVRInput.Controller.RTouch);
        float fixedY = player.position.y;

        player.position += (transform.right * joystickAxis.x + transform.forward * joystickAxis.y) * Time.deltaTime * speed;
        player.position = new Vector3(player.position.x, 0, player.position.z);

        bool AButtonPressed = OVRInput.GetDown(OVRInput.RawButton.A);
        bool BButtonPressed = OVRInput.GetDown(OVRInput.RawButton.B);
        bool YButtonPressed = OVRInput.GetDown(OVRInput.RawButton.Y);
        bool XButtonPressed = OVRInput.GetDown(OVRInput.RawButton.X);
        bool joystickPressed = OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick);
        bool rightJoystickPressed = OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick);

        // float triggerLeft = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);
        // float triggerRight = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);



        if (XButtonPressed) // Change color assignment algorithm on left trigger
        {
            Debug.Log("X button pressed");
            CurrentColorAssignmentAlgo += 1;
            if (CurrentColorAssignmentAlgo > 4)
            {
                CurrentColorAssignmentAlgo = 1;
            }
            ChangeColorAssignmentAlgo(CurrentColorAssignmentAlgo);
        }

        if (YButtonPressed) // Change billboard color assignment on right trigger 
        {
            Debug.Log("Y button pressed");
            CurrentBillboardColorAssignmentAlgo += 1;
            if (CurrentBillboardColorAssignmentAlgo > 2)
            {
                CurrentBillboardColorAssignmentAlgo = 0;
            }
            billboardColorMode.text = "Billboard Color: " +  billboardColorModesList[CurrentBillboardColorAssignmentAlgo];
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

        if (joystickPressed) // Change granularity
        {
            int currentInt = labelSphereMaterial.GetInt("_GranularityMethod");
            if (currentInt == 1)
            {
                labelSphereMaterial.SetInt("_GranularityMethod", 0);
                granularitymode.text = "Granularity Mode: "  + granularityModesList[0];
            }
            else
            {
                labelSphereMaterial.SetInt("_GranularityMethod", 1);
                granularitymode.text = "Granularity Mode: "  + granularityModesList[1];
            }
            
        }

        if (rightJoystickPressed)
        {
            // Debug.Log("rightJoystickPressed");
            // currentLabelMovementMode += 1;
            if (currentLabelMovementMode == 0) // default movement with the user's head
            { 
                LabelContainer.transform.localPosition = new Vector3(0,0,17);
                currentLabelMovementMode = 1;
            }
            else// random x and y position assignment but within the user's field of view
            {
                float newX = Random.Range(-5,5);
                float newY = Random.Range(-5,5);
                LabelContainer.transform.localPosition= new Vector3(newX, newY, 17);
                //LabelContainer.transform.rotation = player.rotation;
                currentLabelMovementMode = 0;
            }
        }

        // if (BButtonPressed) // Toggle shadow
        // {
        //     Debug.Log("B button pressed");
        //     int shadowEnabled = labelSphereMaterial.GetInt("_EnableShadow");

        //     if (shadowEnabled == 0)
        //     {
        //         labelSphereMaterial.SetInt("_EnableShadow", 1);
        //     }
        //     if (shadowEnabled == 1)
        //     {
        //         shadowIntensityIdx += 1;
        //         if (shadowIntensityIdx == 4)
        //         {
        //             labelSphereMaterial.SetInt("_EnableShadow", 0);
        //             shadowIntensityIdx = 0; 
        //         }
        //         else 
        //         {
        //             labelSphereMaterial.SetFloat("_ShadowMultiplier", shadowIntensityIdx*0.5f);
        //         }
                
        //     }     
        // }
    }


    public void ChangeColorAssignmentAlgo(int CurrentColorAssignmentAlgo)
    {
        Debug.Log("Color assignment algorithm changed!");
        labelSphereMaterial.SetInt("_ColorMethod", CurrentColorAssignmentAlgo);
        labelColorMode.text = "Label Color: " + labelColorModesList[CurrentColorAssignmentAlgo-1];
    }
}
