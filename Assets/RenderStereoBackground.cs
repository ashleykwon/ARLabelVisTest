using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RenderStereoBackground : MonoBehaviour
{
    public Camera ScreenshotCamera;
    public GameObject labelSphere;
    public Cubemap cubemapBackground;
    public Cubemap cubemapLabel;
    public GameObject player;

  
    // Start is called before the first frame update
    void Start()
    {
        // Render to all 6 faces on cubemap
        // UpdateCubemap(63);
        labelSphere.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_LabelCubeMap", cubemapLabel);
        
    }

    // Update is called once per frame
    void Update()
    {
        // Block out the layer that contains the label (it's a plane object that has a material + shader)
        ScreenshotCamera.cullingMask &=  ~(1 << LayerMask.NameToLayer("Label"));

        // Move and rotate the sphere with the player
        labelSphere.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);

        float newXAngle = player.transform.eulerAngles.x;
        float newYAngle = player.transform.eulerAngles.y;
        labelSphere.transform.Rotate(player.transform.rotation[0], player.transform.rotation[1], player.transform.rotation[2]);

        // Take a screenshot and render it to a cubemap
        ScreenshotCamera.RenderToCubemap(cubemapBackground);
        
    }
}
