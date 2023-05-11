using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OVR;

public class MovePlayer : MonoBehaviour
{
    public Rigidbody player;
    public float speed;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Debug.Log("right joystick triggered");
        
        var joystickAxis = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick, OVRInput.Controller.RTouch);
        float fixedY = player.position.y;

        player.position += (transform.right * joystickAxis.x + transform.forward * joystickAxis.y) * Time.deltaTime * speed;
        player.position = new Vector3(player.position.x, 0, player.position.z);
    }
}
