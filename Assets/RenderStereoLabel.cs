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

    public Shader surface_shader;
    public Cubemap backgroundCubeMap; 
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
        labelRenderTexture = new RenderTexture(cubemapSize, cubemapSize, 16); 
        labelRenderTexture.dimension = UnityEngine.Rendering.TextureDimension.Cube;

        // To prevent antialiasing
        labelRenderTexture.autoGenerateMips = false;
        labelRenderTexture.useMipMap = false;
        labelRenderTexture.filterMode = FilterMode.Point;


        backgroundAndLabelSphereMaterial = backgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial;

        initialRotation = Quaternion.Euler (180f, 270f, 0f); //Should place the label in the middle of the view
       
        //this is a 16 float array
        float[] rotation_matrix_array = {1.0F,1.0F,1.0F};

        Material material = new Material(surface_shader);
        int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(float));
        rotation_matrix_buffer = new ComputeBuffer(16, stride, ComputeBufferType.Default);
        rotation_matrix_buffer.SetData(rotation_matrix_array);
        material.SetBuffer("rotation_matrix", rotation_matrix_buffer);

        // Access the screenshot camera
        LabelScreenshotCamera = gameObject.GetComponent<Camera>(); 

        LabelScreenshotCamera.RenderToCubemap(labelRenderTexture, 63);

        sumBuffer = new ComputeBuffer(4, 16);



        //sum_all
        Debug.Log("SetUp get_sum");
        SetUp_getSum();
        material.SetBuffer("sum_all_results", sumBuffer);

        //Sum_Single_Letter();

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
        //backgroundAndLabelSphereMaterial.SetTexture("_ShadowCubeMap", labelRenderTexture);


        RenderTexture.active = null;

        Update_getSum();
    }

    private void SetUp_getSum(){
        kernelID_main = cShader.FindKernel("CSMain");
        kernelID_init = cShader.FindKernel("CSInit");

        cShader.SetTexture(kernelID_main, "InputImage", labelRenderTexture);
        cShader.SetTexture(kernelID_init, "InputImage", labelRenderTexture);
        cShader.SetTexture(kernelID_main, "InputCubeMap", backgroundCubeMap);
        cShader.SetTexture(kernelID_init, "InputCubeMap", backgroundCubeMap);

        Debug.Log(sumBuffer);

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


    //method 3: sum only from a letter
    private void Sum_Single_Letter(){

        Debug.Log("method3!");
        float sum_r = 0.0F;
        float sum_g = 0.0F;
        float sum_b = 0.0F;

        Texture2D label_texture = getRTPixels(labelRenderTexture);

        int x = 200;
        int y = 200;
        Coordination start_coords = new Coordination(x, y);
        Result_color letter_sum = dfs(label_texture, start_coords);

        sum_r = letter_sum.r;
        sum_g = letter_sum.g;
        sum_b = letter_sum.b;

    }

    //method 4: sum a kernel,  which is in the label
    private void Sum_A_Kernel(int kernel_size){
        float sum_r = 0.0F;
        float sum_g = 0.0F;
        float sum_b = 0.0F;

        int x = 200;
        int y = 200;
        Texture2D label_texture = getRTPixels(labelRenderTexture);

        for(int i = -kernel_size/2; i<kernel_size/2; i++){
            for(int j = -kernel_size/2; j<kernel_size/2; j++){
                int cur_x = x + i;
                int cur_y = y + j;

                sum_r += label_texture.GetPixel(cur_x, cur_y).r;
                sum_g += label_texture.GetPixel(cur_x, cur_y).g;
                sum_b += label_texture.GetPixel(cur_x, cur_y).b;

            }
        }
    }


    //this function gets pixel values from renderTexture
    //this is a helper function
    private Texture2D getRTPixels(RenderTexture rt){
        RenderTexture currentActiveRT = RenderTexture.active;
        RenderTexture.active = rt;

        // Create a new Texture2D and read the RenderTexture image into it
        Texture2D tex = new Texture2D(rt.width, rt.height);
        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);

        // Restorie previously active render texture
        RenderTexture.active = currentActiveRT;
        return tex;

    }

    private Result_color dfs(Texture2D label_texture, Coordination start_coords){
        float r = 0.0F;
        float g = 0.0F;
        float b = 0.0F;

        Stack<Coordination> myStack = new Stack<Coordination>();
        myStack.Push(start_coords);

        while(myStack.Count != 0){
            Coordination cur = myStack.Pop();

            int cur_x = cur.x;
            int cur_y = cur.y;

            float cur_r = label_texture.GetPixel(cur.x, cur.y).r;
            float cur_g = label_texture.GetPixel(cur.x, cur.y).g;
            float cur_b = label_texture.GetPixel(cur.x, cur.y).b;
            r += cur_r;
            g += cur_g;
            b += cur_b;

            if (cur_r!=0 || cur_g!=0 || cur_g!=0){
                //if this pixel is a label pixel, continue the search
                Coordination top = new Coordination(cur_x, cur_y+1);
                Coordination bot = new Coordination(cur_x, cur_y-1);
                Coordination left = new Coordination(cur_x-1, cur_y);
                Coordination right = new Coordination(cur_x+1, cur_y);
                myStack.Push(top);
                myStack.Push(bot);
                myStack.Push(left);
                myStack.Push(right);
            }

        }

        return new Result_color(r,g,b);

    }

}

public struct Coordination{
        public int x;
        public int y;

        public Coordination(int x, int y){
            this.x = x;
            this.y = y;
        }
    }


public struct Result_color{
        public float r;
        public float g;
        public float b;

        public Result_color(float r, float g, float b){
            this.r = r;
            this.g = g;
            this.b = b;
        }
    }
