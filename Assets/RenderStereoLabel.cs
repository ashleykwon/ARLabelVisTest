using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro.Examples; 
using TMPro;

public class RenderStereoLabel : MonoBehaviour
{
    public Camera LabelScreenshotCamera;
    public GameObject backgroundAndLabelSphere;
    public GameObject labelSphere;
    public GameObject player;
    public Material backgroundAndLabelSphereMaterial;
    RenderTexture labelRenderTexture;
    // Quaternion initialRotation;
    // Matrix4x4 m;

    public Shader surface_shader;
    //public Cubemap backgroundCubeMap; 
    // private ComputeBuffer rotation_matrix_buffer;

    // public ComputeShader cShader;
    // private ComputeBuffer sumBuffer;
    // private int kernelID_main;
    // private int kernelID_init;
  
    //sum a character
    private TMP_Text m_TextComponent;
    // Cubemap newLabelCubemap;

    // Start is called before the first frame update
    void Start()
    {
        // newLabelCubemap = Resources.Load("Materials/Test", typeof(Cubemap)) as Cubemap;
        // labelSphere.GetComponent<Renderer>().material.SetTexture("_CubeMap", newLabelCubemap);
        
        int cubemapSize = 2048; // this can change for a better resolution            

        // Define a cube-shaped render texture for the white label + black background (default where alpha = 0)
        labelRenderTexture = new RenderTexture(cubemapSize, cubemapSize, 16); 
        labelRenderTexture.dimension = UnityEngine.Rendering.TextureDimension.Cube;

        // To prevent antialiasing
        labelRenderTexture.autoGenerateMips = false;
        labelRenderTexture.useMipMap = false;
        labelRenderTexture.filterMode = FilterMode.Point;


        backgroundAndLabelSphereMaterial = backgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial;

        // Access the screenshot camera
        LabelScreenshotCamera = gameObject.GetComponent<Camera>(); 

        LabelScreenshotCamera.RenderToCubemap(labelRenderTexture, 63);
    }

    void Update()
    {
        // Only render elements on the UI layer (black sphere + white label + magenta shadow + blue billboard)
        LabelScreenshotCamera.cullingMask &= (1 << LayerMask.NameToLayer("UI"));
    }

    // Update is called once per frame
    void LateUpdate() // This part causes the double-rendering
    {

        // Take a screenshot (white label + black background + blue billboard) and render it to billboard and label cubemaps (these two maps are initially simialr)
        LabelScreenshotCamera.targetTexture = labelRenderTexture;
        RenderTexture.active = labelRenderTexture;

        LabelScreenshotCamera.RenderToCubemap(labelRenderTexture, 63);


        backgroundAndLabelSphereMaterial.SetTexture("_LabelCubeMap", labelRenderTexture); // Extract render texture directly from UICamera, which renders the white label and the black background, along with blue billboard and red shadow 
        backgroundAndLabelSphereMaterial.SetTexture("_BillboardCubeMap", labelRenderTexture);
        backgroundAndLabelSphereMaterial.SetTexture("_ModeCubeMap", labelRenderTexture);


        RenderTexture.active = null;

    }

}