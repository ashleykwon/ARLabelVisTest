using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ApplyFullScreenShader : MonoBehaviour
{
    public Shader screenShader = null;
    private Material m_renderMaterial;
    public Texture2D LabelTex;
    public Camera ScreenshotCamera;
    // public Texture2D Screenshot;
    public RenderTexture screenTexture;
    string path;
    
    void Start()
    {
        if (screenShader == null)
        {
            Debug.LogError("no shader");
            m_renderMaterial = null;
            return;
        }

        

        m_renderMaterial = new Material(screenShader);
        m_renderMaterial.SetTexture("_LabelTex", LabelTex);
        //m_renderMaterial.SetTexture("_MainTex", screenTexture);
        m_renderMaterial.SetTexture("_CurrentFrameTex", screenTexture);


        ScreenshotCamera = gameObject.GetComponent<Camera>(); 
        // Screenshot = new Texture2D(Screen.width, Screen.height);
        // screenTexture = new RenderTexture(Screen.width, Screen.height, 16);
    }
    // void Update()
    // {
    //     ScreenshotCamera.targetTexture = screenTexture;
    //     RenderTexture.active = screenTexture;
    //     ScreenshotCamera.Render();
    // // // //     Screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
    // // // //     RenderTexture.active = null;
    // }

    void OnRenderImage(RenderTexture source, RenderTexture destination) // Used for post-processing. Only run after the camera finishes rendering
    {
        
        // Somehow RenderTexture source is blank in the headset??
        //Screenshot.filterMode = FilterMode.Point;
        // m_renderMaterial.SetTexture("_CurrentFrameTex", Screenshot);

        // Show the render texture captured from RightEyeAnchor
        //m_renderMaterial.SetTexture("_CurrentFrameTex", screenTexture);
        Graphics.Blit(screenTexture, destination, m_renderMaterial);

        // The line below should take source -> apply m_renderMaterial to source -> render the result to destination
        //Graphics.Blit(source, destination, m_renderMaterial);
        
        Debug.Log("on render image called");
    }

 
}
