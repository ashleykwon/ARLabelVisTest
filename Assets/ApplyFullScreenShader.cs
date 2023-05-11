using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplyFullScreenShader : MonoBehaviour
{
    public Shader screenShader = null;
    private Material m_renderMaterial;
    public Texture2D LabelTex;
    
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
    }
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, m_renderMaterial);
    }
}
