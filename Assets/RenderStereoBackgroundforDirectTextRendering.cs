using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RenderStereoBackgroundforDirectTextRendering : MonoBehaviour
{
    public Camera ScreenshotCamera;
    public Camera LabelScreenshotCamera;
    public GameObject backgroundAndLabelSphere;
    // public Cubemap cubemapBackground;
    public RenderTexture cubemapLabel; // From render texture from left eye camera (the one that screenshots label + background)
    public GameObject player;
    public Material backgroundAndLabelSphereMaterial;
    RenderTexture renderTexture;

  
    // Start is called before the first frame update
    void Start()
    {

        int cubemapSize = 1024; // this can change for a better resolution
        
        //backgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_LabelCubeMap", cubemapLabel);

        backgroundAndLabelSphereMaterial = backgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial;
        //Debug.Log(backgroundAndLabelSphereMaterial.GetTexture("_LabelCubeMap"));

        // Define cube-shaped render texture for cubemap
        renderTexture = new RenderTexture(cubemapSize, cubemapSize, 16);
        renderTexture.dimension = UnityEngine.Rendering.TextureDimension.Cube;

        // cubemapLabel.height = cubemapSize;
        // cubemapLabel.width = cubemapSize;
       
        // Access the screenshot camera
        ScreenshotCamera = gameObject.GetComponent<Camera>(); 

        //Debug.Log(LabelScreenshotCamera.targetTexture);
    }

    void Update()
    {
        // Block out the layer that contains the label and the black background behind the label (it's a plane object that has a material + shader)
        ScreenshotCamera.cullingMask &=  ~(1 << LayerMask.NameToLayer("Label"));
        ScreenshotCamera.cullingMask &=  ~(1 << LayerMask.NameToLayer("UI"));

        // Move and rotate the sphere with the player
        backgroundAndLabelSphere.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
        
        // // float newXAngle = player.transform.eulerAngles.x;
        // // float newYAngle = player.transform.eulerAngles.y;
        // backgroundAndLabelSphere.transform.Rotate(player.transform.rotation[0], player.transform.rotation[1], player.transform.rotation[2]);
    }

    // Update is called once per frame
    void LateUpdate()
    {     
        // Take a screenshot and render it to a cubemap
        
        backgroundAndLabelSphereMaterial.SetTexture("_CubeMap", renderTexture);
        backgroundAndLabelSphereMaterial.SetTexture("_LabelCubeMap", cubemapLabel); //Somehow this cubemapLabel doesn't update well
        
        ScreenshotCamera.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;
        
        ScreenshotCamera.RenderToCubemap(renderTexture, 63); 
        RenderTexture.active = null;
    }
}
