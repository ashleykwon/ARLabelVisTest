using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RenderStereoLabel : MonoBehaviour
{
    public Camera LabelScreenshotCamera;
    public GameObject backgroundAndLabelSphere;
    public GameObject labelSphere;
    public GameObject player;
    public Material backgroundAndLabelSphereMaterial;
    RenderTexture labelRenderTexture;
    Quaternion initialRotation;
    Matrix4x4 m;

    // Shader surface_shader;
    private ComputeBuffer rotation_matrix_buffer;

    // public ComputeShader cShader;
    // private ComputeBuffer sumBuffer;
    // private int kernelID;

  
    // Start is called before the first frame update
    void Start()
    {
        int cubemapSize = 2048; // this can change for a better resolution            

        // Define a cube-shaped render texture for the white label + black background (default where alpha = 0)
        labelRenderTexture = new RenderTexture(cubemapSize, cubemapSize, 16); 
        labelRenderTexture.dimension = UnityEngine.Rendering.TextureDimension.Cube;

        // To prevent antialiasing
        labelRenderTexture.autoGenerateMips = false;
        labelRenderTexture.useMipMap = false;
        labelRenderTexture.filterMode = FilterMode.Point;


        backgroundAndLabelSphereMaterial = backgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial;

        initialRotation = Quaternion.Euler (180f, 270f, 0f); //Should place the label in the middle of the view
       
        //this is a 16 float array
        // float[] rotation_matrix_array = {1.0F,1.0F,1.0F};

        // Material material = new Material(surface_shader);
        // int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(float));
        // rotation_matrix_buffer = new ComputeBuffer(16, stride, ComputeBufferType.Default);
        // rotation_matrix_buffer.SetData(rotation_matrix_array);
        // material.SetBuffer("rotation_matrix", rotation_matrix_buffer);

        // Access the screenshot camera
        LabelScreenshotCamera = gameObject.GetComponent<Camera>(); 

        LabelScreenshotCamera.RenderToCubemap(labelRenderTexture, 63);


        //sum_all
        // Material material = new Material(surface_shader);
        // get_sum();
        // SetUp_getSum();
        // material.SetBuffer ("sum_all_results", sumBuffer);



    }

    void Update()
    {
        // Only render elements on the UI layer (black sphere + white label + magenta shadow + blue billboard)
        LabelScreenshotCamera.cullingMask &= (1 << LayerMask.NameToLayer("UI"));
       
    //     // Move and rotate the sphere with the player
    //     //backgroundAndLabelSphere.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);

    //     // // float newXAngle = player.transform.eulerAngles.x;
    //     // // float newYAngle = player.transform.eulerAngles.y;
    //     //LabelScreenshotCamera.transform.Rotate(player.transform.rotation[0], player.transform.rotation[1], player.transform.rotation[2]);
    }

    // Update is called once per frame
    void LateUpdate() // This part causes the double-rendering
    {

        // // Take a screenshot (white label + black background + blue billboard) and render it to billboard and label cubemaps (these two maps are initially simialr)
        LabelScreenshotCamera.targetTexture = labelRenderTexture;
        RenderTexture.active = labelRenderTexture;

        LabelScreenshotCamera.RenderToCubemap(labelRenderTexture, 63);


        backgroundAndLabelSphereMaterial.SetTexture("_LabelCubeMap", labelRenderTexture); // Extract render texture directly from UICamera, which renders the white label and the black background, along with blue billboard and red shadow 
        backgroundAndLabelSphereMaterial.SetTexture("_BillboardCubeMap", labelRenderTexture);
        backgroundAndLabelSphereMaterial.SetTexture("_ShadowCubeMap", labelRenderTexture);


        RenderTexture.active = null;

        // get_sum();
    }

    // private void SetUp_getSum(){
    //     kernelID = cShader.FindKernel("CSMain");
    //     cShader.SetBuffer(kernelID, "_SumBuffer", sumBuffer);
    // }

    // private void Update_getSum(){
    //     cShader.Dispatch(kernelID, 16, 1, 1);
    // }


}
