using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sum_all : MonoBehaviour
{
    public ComputeShader cShader;
    ComputeBuffer cBuffer;
    int[] analysisResult;
    int r_sum;

    public Texture2D inputTexture;   


    // Start is called before the first frame update
    void Start()
    {
    if (null == cShader) 
      {
         Debug.Log("Shader missing.");
         return;
      }
    Debug.Log("sucess loading");
      get_sum();
    }

    void get_sum(){
        r_sum = cShader.FindKernel("CSMain");
        cBuffer = new ComputeBuffer(1, sizeof(int));

        cShader.SetBuffer(r_sum, "ResultBuffer", cBuffer);
        cShader.Dispatch(r_sum, 16, 16, 1);

        cBuffer.Release();
        cBuffer = null;

    }

    // Update is called once per frame
    void Update()
    {
        get_sum();

    }
}
