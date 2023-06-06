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
    public Material labelSphereMaterial;
    RenderTexture renderTexture;

  
    // Start is called before the first frame update
    void Start()
    {

        int cubemapSize = 1024; // this can change for a better resolution
        
        labelSphere.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_LabelCubeMap", cubemapLabel);

        labelSphereMaterial = labelSphere.GetComponent<MeshRenderer>().sharedMaterial;

        // Define cube-shaped render texture for cubemap
        renderTexture = new RenderTexture(cubemapSize, cubemapSize, 16);
        renderTexture.dimension = UnityEngine.Rendering.TextureDimension.Cube;
       
        // Access the screenshot camera
        ScreenshotCamera = gameObject.GetComponent<Camera>(); 
    }

    void Update()
    {
        // Block out the layer that contains the label (it's a plane object that has a material + shader)
        ScreenshotCamera.cullingMask &=  ~(1 << LayerMask.NameToLayer("Label"));

        // Move and rotate the sphere with the player
        labelSphere.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);

        float newXAngle = player.transform.eulerAngles.x;
        float newYAngle = player.transform.eulerAngles.y;
        labelSphere.transform.Rotate(player.transform.rotation[0], player.transform.rotation[1], player.transform.rotation[2]);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        // Take a screenshot and render it to a cubemap
        labelSphereMaterial.SetTexture("_CubeMap", renderTexture);
        
        ScreenshotCamera.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;
        
        ScreenshotCamera.RenderToCubemap(renderTexture, 63); 
        RenderTexture.active = null;
    }
}
