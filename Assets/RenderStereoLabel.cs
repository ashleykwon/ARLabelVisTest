using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RenderStereoLabel : MonoBehaviour
{
    public Camera ScreenshotCamera;
    public GameObject backgroundAndLabelSphere;
    public GameObject labelSphere;
    // public Cubemap cubemapBackground;
    // public Cubemap cubemapLabel;
    // public Cubemap cubemapTest; // For debugging purposes only
    public GameObject player;
    public Material backgroundAndLabelSphereMaterial;
    public Material labelSphereMaterial;
    public RenderTexture renderTexture;

  
    // Start is called before the first frame update
    void Start()
    {

        //int cubemapSize = 1024; // this can change for a better resolution            

        // Define cube-shaped render texture for cubemap
        // renderTexture = new RenderTexture(cubemapSize, cubemapSize, 16);
        renderTexture.dimension = UnityEngine.Rendering.TextureDimension.Cube;
        // renderTexture.height = cubemapSize;
        // renderTexture.width = cubemapSize;

        // Set the label map to be the render texture of the label screenshot camera
        labelSphere.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_CubeMap", renderTexture);

        backgroundAndLabelSphereMaterial = backgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial;

       
        // Access the screenshot camera
        ScreenshotCamera = gameObject.GetComponent<Camera>(); 

    }

    void Update()
    {
        // Only render elements on the UI layer (black sphere + white label)
        ScreenshotCamera.cullingMask &= (1 << LayerMask.NameToLayer("UI"));

        // Move and rotate the sphere with the player
        //ScreenshotCamera.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);

        // // float newXAngle = player.transform.eulerAngles.x;
        // // float newYAngle = player.transform.eulerAngles.y;
        //ScreenshotCamera.transform.Rotate(player.transform.rotation[0], player.transform.rotation[1], player.transform.rotation[2]);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        // Take a screenshot and render it to a cubemap
        ScreenshotCamera.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;
        
        ScreenshotCamera.RenderToCubemap(renderTexture, 63); 

        // backgroundAndLabelSphereMaterial.SetTexture("_LabelCubeMap", renderTexture);

        //Debug.Log(backgroundAndLabelSphereMaterial.GetTexture("_LabelCubeMap"));
        RenderTexture.active = null;
    }
}
