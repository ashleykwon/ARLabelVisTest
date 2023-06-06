using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OVR;

public class MovePlayer : MonoBehaviour
{
    public Rigidbody player;
    public float speed;
    public GameObject labelSphere;
    int CurrentColorAssignmentAlgo; // Doesn't need to be specified at Start
    Material labelSphereMaterial; // Doesn't need to be specified at Start

    // Start is called before the first frame update
    void Start()
    {
        CurrentColorAssignmentAlgo = 1;
        labelSphereMaterial = labelSphere.GetComponent<MeshRenderer>().sharedMaterial;

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

        // Change color assignment algorithm on trigger
        if (triggerRight > 0.5f)
        {
            CurrentColorAssignmentAlgo += 1;
            if (CurrentColorAssignmentAlgo > 4)
            {
                CurrentColorAssignmentAlgo = 1;
            }
            ChangeColorAssignmentAlgo(CurrentColorAssignmentAlgo);

        }
    }


    public void ChangeColorAssignmentAlgo(int CurrentColorAssignmentAlgo)
    {
        Debug.Log("Color assignment algorithm changed!");
        labelSphereMaterial.SetInt("_ColorMethod", CurrentColorAssignmentAlgo);
    }
}
