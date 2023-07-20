using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RenderStereoLabel : MonoBehaviour
{
    public Camera ScreenshotCamera;
    public GameObject backgroundAndLabelSphere;
    public GameObject labelSphere;
    public GameObject player;
    public Material backgroundAndLabelSphereMaterial;
    public Material labelSphereMaterial;
    RenderTexture renderTexture;

  
    // Start is called before the first frame update
    void Start()
    {
        int cubemapSize = 1024; // this can change for a better resolution            

        // Define a cube-shaped render texture for the white label + black background (default where alpha = 0)
        renderTexture = new RenderTexture(cubemapSize, cubemapSize, 16); 
        renderTexture.dimension = UnityEngine.Rendering.TextureDimension.Cube;

        // To prevent antialiasing
        renderTexture.autoGenerateMips = false;
        renderTexture.useMipMap = false;
        renderTexture.filterMode = FilterMode.Point;


        backgroundAndLabelSphereMaterial = backgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial;

       
        // Access the screenshot camera
        ScreenshotCamera = gameObject.GetComponent<Camera>(); 

    }

    void Update()
    {
        // Only render elements on the UI layer (black sphere + white label)
        ScreenshotCamera.cullingMask &= (1 << LayerMask.NameToLayer("UI"));

    //     // Move and rotate the sphere with the player
    //     //backgroundAndLabelSphere.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);

    //     // // float newXAngle = player.transform.eulerAngles.x;
    //     // // float newYAngle = player.transform.eulerAngles.y;
    //     //ScreenshotCamera.transform.Rotate(player.transform.rotation[0], player.transform.rotation[1], player.transform.rotation[2]);
    }

    // Update is called once per frame
    void LateUpdate() // This part causes the double-rendering
    {
        // Take a screenshot (white label + black background + blue billboard) and render it to billboard and label cubemaps (these two maps are initially simialr)
        ScreenshotCamera.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;
        
        ScreenshotCamera.RenderToCubemap(renderTexture, 63); 

        backgroundAndLabelSphereMaterial.SetTexture("_LabelCubeMap", renderTexture); // Extract render texture directly from UICamera, which renders the white label and the black background, along with blue billboard and red shadow 
        backgroundAndLabelSphereMaterial.SetTexture("_BillboardCubeMap", renderTexture);
        backgroundAndLabelSphereMaterial.SetTexture("_ShadowCubeMap", renderTexture);


        RenderTexture.active = null;
    }
}
