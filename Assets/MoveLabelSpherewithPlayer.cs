using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveLabelSpherewithPlayer : MonoBehaviour
{
    public GameObject player;
    public GameObject LabelSphere;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        LabelSphere.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
    }
}
