using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePlayer : MonoBehaviour
{
    public Rigidbody player;
    float speed = 0.5f;
    // Start is called before the first frame update
    void Start()
    {
        player.useGravity = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.UpArrow)) // use while key down
        {
           player.transform.Translate(Vector3.forward*speed);
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            player.transform.Translate(Vector3.back*speed);
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            player.transform.Translate(Vector3.left*speed);
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            player.transform.Translate(Vector3.right*speed);
        }
    }
}
