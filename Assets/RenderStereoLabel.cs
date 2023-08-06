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

    public ComputeShader cShader;
    public Shader surface_shader;
    ComputeBuffer cBuffer;
    int r_sum;

  
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

        // Access the screenshot camera
        ScreenshotCamera = gameObject.GetComponent<Camera>(); 

                //sum_all
        Material material = new Material(surface_shader);
        get_sum();
        material.SetBuffer ("sum_all_results", cBuffer);



    }

    void Update()
    {
        // Only render elements on the UI layer (black sphere + white label + magenta shadow + blue billboard)
        ScreenshotCamera.cullingMask &= (1 << LayerMask.NameToLayer("UI"));
        // ScreenshotCamera.targetTexture = renderTexture;
        // RenderTexture.active = renderTexture;

        // // Vector3 currentAngle = player.transform.eulerAngles;// shouldn't I be updating the currentAngle based on the deviation from the starting position?
        // // Quaternion adjustedAngle = Quaternion.Euler(initialRotation[0]+currentAngle[0], initialRotation[1]+currentAngle[1], initialRotation[2]+currentAngle[2]);
        // // m = Matrix4x4.TRS(Vector3.zero, initialRotation, new Vector3(1,1,1) );
        // Graphics.Blit(ScreenshotCamera.targetTexture, renderTexture);

        // // backgroundAndLabelSphereMaterial.SetTexture("_LabelCubeMap", renderTexture); // Extract render texture directly from UICamera, which renders the white label and the black background, along with blue billboard and red shadow 
        // // backgroundAndLabelSphereMaterial.SetTexture("_BillboardCubeMap", renderTexture);
        // // backgroundAndLabelSphereMaterial.SetTexture("_ShadowCubeMap", renderTexture);


        // RenderTexture.active = null;
        //Graphics.Blit(ScreenshotCamera.targetTexture, renderTexture);

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

        get_sum();
    }

    //bind to compute shader
    void get_sum(){
    r_sum = cShader.FindKernel("CSMain");
    cBuffer = new ComputeBuffer(1, sizeof(int));

    cShader.SetBuffer(r_sum, "ResultBuffer", cBuffer);
    cShader.Dispatch(r_sum, 16, 16, 1);

    cBuffer.Release();
    cBuffer = null;

    }

}
