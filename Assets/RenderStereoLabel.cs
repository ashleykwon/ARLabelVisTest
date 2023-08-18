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
    Quaternion initialRotation;
    Matrix4x4 m;

    public Shader surface_shader;
    private ComputeBuffer rotation_matrix_buffer;

    public ComputeShader cShader;
    private ComputeBuffer sumBuffer;
    private int kernelID_main;
    private int kernelID_init;

  
    // Start is called before the first frame update
    void Start()
    {
        int cubemapSize = 2048; // this can change for a better resolution            

        // Define a cube-shaped render texture for the white label + black background (default where alpha = 0)
        renderTexture = new RenderTexture(cubemapSize, cubemapSize, 16); 
        renderTexture.dimension = UnityEngine.Rendering.TextureDimension.Cube;

        // To prevent antialiasing
        renderTexture.autoGenerateMips = false;
        renderTexture.useMipMap = false;
        renderTexture.filterMode = FilterMode.Point;


        backgroundAndLabelSphereMaterial = backgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial;

        initialRotation = Quaternion.Euler (180f, 270f, 0f); //Should place the label in the middle of the view
        // Matrix4x4 m = Matrix4x4.TRS(Vector3.zero, initialRotation, new Vector3(1,1,1) );
        // backgroundAndLabelSphereMaterial.SetVector("_LabelRotationMatrixRow1", m.GetRow(0));
        // backgroundAndLabelSphereMaterial.SetVector("_LabelRotationMatrixRow2", m.GetRow(1));
        // backgroundAndLabelSphereMaterial.SetVector("_LabelRotationMatrixRow3", m.GetRow(2));
        // backgroundAndLabelSphereMaterial.SetVector("_LabelRotationMatrixRow4", m.GetRow(3));

        //rotation_matrix

        //this is a 16 float array
        float[] rotation_matrix_array = {1.0F,1.0F,1.0F};

        Material material = new Material(surface_shader);
        int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(float));
        rotation_matrix_buffer = new ComputeBuffer(16, stride, ComputeBufferType.Default);
        rotation_matrix_buffer.SetData(rotation_matrix_array);
        material.SetBuffer("rotation_matrix", rotation_matrix_buffer);

        // Access the screenshot camera
        ScreenshotCamera = gameObject.GetComponent<Camera>(); 

        ScreenshotCamera.RenderToCubemap(renderTexture, 63);


        //sum_all
        Debug.Log("SetUp get_sum");
        SetUp_getSum();
        material.SetBuffer ("sum_all_results", sumBuffer);



    }

    void Update()
    {
        // Only render elements on the UI layer (black sphere + white label + magenta shadow + blue billboard)
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

        // // Take a screenshot (white label + black background + blue billboard) and render it to billboard and label cubemaps (these two maps are initially simialr)
        ScreenshotCamera.targetTexture = renderTexture;
        RenderTexture.active = renderTexture;

        // // Graphics.Blit(ScreenshotCamera.targetTexture, renderTexture);

        
        ScreenshotCamera.RenderToCubemap(renderTexture, 63);

        // // backgroundAndLabelSphereMaterial.SetVector("_LabelRotationMatrixRow1", m.GetRow(0));
        // // backgroundAndLabelSphereMaterial.SetVector("_LabelRotationMatrixRow2", m.GetRow(1));
        // // backgroundAndLabelSphereMaterial.SetVector("_LabelRotationMatrixRow3", m.GetRow(2));
        // // backgroundAndLabelSphereMaterial.SetVector("_LabelRotationMatrixRow4", m.GetRow(3)); 
        // // Graphics.Blit(ScreenshotCamera.targetTexture, renderTexture);

        backgroundAndLabelSphereMaterial.SetTexture("_LabelCubeMap", renderTexture); // Extract render texture directly from UICamera, which renders the white label and the black background, along with blue billboard and red shadow 
        backgroundAndLabelSphereMaterial.SetTexture("_BillboardCubeMap", renderTexture);
        backgroundAndLabelSphereMaterial.SetTexture("_ShadowCubeMap", renderTexture);


        RenderTexture.active = null;

        Update_getSum();

    }

    private void SetUp_getSum(){
        kernelID_main = cShader.FindKernel("CSMain");
        kernelID_init = cShader.FindKernel("CSInit");

        cShader.SetTexture(kernelID_main, "InputImage", renderTexture);
        cShader.SetTexture(kernelID_init, "InputImage", renderTexture);

        cShader.SetBuffer(kernelID_main, "_SumBuffer", sumBuffer);
        cShader.SetBuffer(kernelID_init, "_SumBuffer", sumBuffer);

        cShader.Dispatch(kernelID_init, 1, 1, 1);
    }

    private void Update_getSum(){  
        
        Debug.Log("Update get_sum");
        cShader.Dispatch(kernelID_main, 16, 1, 1);
        int[] results = new int[4];
        sumBuffer.GetData(results);

    }


}
