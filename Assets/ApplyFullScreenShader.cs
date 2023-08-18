using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ApplyFullScreenShader : MonoBehaviour
{
    public Shader screenShader = null;
    private Material m_renderMaterial;
    public Cubemap cubemapLabel;
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
        m_renderMaterial.SetTexture("_LabelCubeMap", cubemapLabel);

        ScreenshotCamera = gameObject.GetComponent<Camera>(); 
    }


    void OnRenderImage(RenderTexture source, RenderTexture destination) // Used for post-processing. Only run after the camera finishes rendering
    {
        
        Graphics.Blit(screenTexture, destination, m_renderMaterial);


        Debug.Log("on render image called");
    }

 
}
