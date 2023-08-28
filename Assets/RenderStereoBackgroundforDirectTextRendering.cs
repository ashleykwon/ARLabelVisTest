using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RenderStereoBackgroundforDirectTextRendering : MonoBehaviour
{
    public Camera ScreenshotCamera;
    public GameObject backgroundAndLabelSphere;
    public GameObject labelSphere;
    public GameObject player;
    public Material backgroundAndLabelSphereMaterial;
    RenderTexture renderTexture;

    public ComputeShader cShader;
    private ComputeBuffer sumBuffer;
    private int kernelID_main;
    private int kernelID_init;

    // Start is called before the first frame update
    void Start()
    {
        int cubemapSize = 2048; // this can change for a better resolution
        
        backgroundAndLabelSphereMaterial = backgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial;
     
        // Define a cube-shaped render texture for the background
        renderTexture = new RenderTexture(cubemapSize, cubemapSize, 16);
        renderTexture.dimension = UnityEngine.Rendering.TextureDimension.Cube;

         // To prevent antialiasing
        renderTexture.autoGenerateMips = false;
        renderTexture.useMipMap = false;
        renderTexture.filterMode = FilterMode.Point;

        // Access the screenshot camera
        ScreenshotCamera = gameObject.GetComponent<Camera>(); 

        sumBuffer = new ComputeBuffer(4, 16);

        //sum_all
        Debug.Log("SetUp get_sum");
        SetUp_getSum();
        backgroundAndLabelSphereMaterial.SetBuffer("sum_all_results", sumBuffer);
    }

    void Update()
    {
        // Block out the layer that contains the label and the black background behind the label (it's a plane object that has a material + shader)
        ScreenshotCamera.cullingMask &=  ~(1 << LayerMask.NameToLayer("BackgroundAndLabel"));
        ScreenshotCamera.cullingMask &=  ~(1 << LayerMask.NameToLayer("UI"));

        // Move and rotate the sphere with the player
        backgroundAndLabelSphere.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
        labelSphere.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
    }

    // Update is called once per frame
    void LateUpdate()
    {     
        // Take a screenshot and render it to a cubemap
       
        backgroundAndLabelSphereMaterial.SetTexture("_CubeMap", renderTexture);
        
        ScreenshotCamera.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;
        
        // Render the background and the label
        ScreenshotCamera.RenderToCubemap(renderTexture, 63); 
        RenderTexture.active = null;

        Update_getSum();
    }


    private void SetUp_getSum(){
        kernelID_main = cShader.FindKernel("CSMain");
        kernelID_init = cShader.FindKernel("CSInit");

        cShader.SetTexture(kernelID_main, "InputCubeMap", renderTexture);
        cShader.SetTexture(kernelID_init, "InputCubeMap", renderTexture);

        cShader.SetBuffer(kernelID_main, "_SumBuffer", sumBuffer); //sumBuffer is null somehow
        cShader.SetBuffer(kernelID_init, "_SumBuffer", sumBuffer);

        // cShader.Dispatch(kernelID_init, 1, 1, 1);
    }

    private void Update_getSum(){  
        
        // Debug.Log("Update get_sum");
        cShader.Dispatch(kernelID_main, 16, 1, 1);
        int[] results = new int[4];
        sumBuffer.GetData(results);

    }

}
