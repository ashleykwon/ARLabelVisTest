using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OVR;
using TMPro;

public class UserTestingMovePlayer : MonoBehaviour
{
    public Rigidbody player;
    public GameObject BackgroundAndLabelSphere;
    public GameObject LabelContainer;
    public TMP_Text labelColorMode;
    public TMP_Text opacityLevel;
    public TMP_Text granularitymode;
    public TMP_Text modeID;
    Material labelSphereMaterial; // Doesn't need to be specified at Start
    public int currentLabelDisplayMode;

    // Start is called before the first frame update
    void Start()
    { 
        labelSphereMaterial = BackgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial;

        currentLabelDisplayMode = 0;

        labelSphereMaterial.SetInt("_ColorMethod", 5);
        labelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
        labelColorMode.text = "Label Color: Green";
        granularitymode.text = "Granularity Mode: N/A";
        opacityLevel.text = "Opacity Level: 0.4";
        modeID.text = "Mode ID: 0";
}


    // Update is called once per frame
    void Update()
    {
        bool triggerLeft = OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger);

        
        if (triggerLeft) // Change color assignment algorithm on left trigger
        {
            Debug.Log("triggerLeft pressed");
            currentLabelDisplayMode += 1;
            if (currentLabelDisplayMode >= 8){
                currentLabelDisplayMode = 0;
            }
            if (currentLabelDisplayMode == 0){ // Baseline + 40% opacity
                labelSphereMaterial.SetInt("_ColorMethod", 5);
                labelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
                labelColorMode.text = "Label Color: Green";
                granularitymode.text = "Granularity Mode: N/A";
                opacityLevel.text = "Opacity Level: 0.4";
                modeID.text = "Mode ID: 0";
            }
            else if (currentLabelDisplayMode == 1){ // Baseline + 70% opacity
                labelSphereMaterial.SetInt("_ColorMethod", 5);
                labelSphereMaterial.SetFloat("_OpacityLevel", 0.7f);
                labelColorMode.text = "Label Color: Green";
                granularitymode.text = "Granularity Mode: N/A";
                opacityLevel.text = "Opacity Level: 0.7";
                modeID.text = "Mode ID: 1";
            }
            else if (currentLabelDisplayMode == 2){ // CIELAB + Per-pixel + 40% opacity
                labelSphereMaterial.SetInt("_ColorMethod", 4);
                labelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
                labelSphereMaterial.SetInt("_GranularityMethod", 0);
                labelColorMode.text = "Label Color: CIELAB-based";
                granularitymode.text = "Granularity Mode: Per-pixel";
                opacityLevel.text = "Opacity Level: 0.4";
                modeID.text = "Mode ID: 2";
            }
            else if (currentLabelDisplayMode == 3){ // CIELAB + Per-area + 40% opacity
                labelSphereMaterial.SetInt("_ColorMethod", 4);
                labelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
                labelSphereMaterial.SetInt("_GranularityMethod", 1);
                labelColorMode.text = "Label Color: CIELAB-based";
                granularitymode.text = "Granularity Mode: Per-area";
                opacityLevel.text = "Opacity Level: 0.4";
                modeID.text = "Mode ID: 3";
            }
            else if (currentLabelDisplayMode == 4){ // CIELAB + Per-background + 30% opacity
                labelSphereMaterial.SetInt("_ColorMethod", 4);
                labelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
                labelSphereMaterial.SetInt("_GranularityMethod", 2);
                labelColorMode.text = "Label Color: CIELAB-based";
                granularitymode.text = "Granularity Mode: Per-background";
                opacityLevel.text = "Opacity Level: 0.4";
                modeID.text = "Mode ID: 4";
            }
            else if (currentLabelDisplayMode == 5){ // CIELAB + Per-background + 70% opacity
                labelSphereMaterial.SetInt("_ColorMethod", 4);
                labelSphereMaterial.SetFloat("_OpacityLevel", 0.7f);
                labelSphereMaterial.SetInt("_GranularityMethod", 0);
                labelColorMode.text = "Label Color: CIELAB-based";
                granularitymode.text = "Granularity Mode: Per-pixel";
                opacityLevel.text = "Opacity Level: 0.7";
                modeID.text = "Mode ID: 5";
            }
            else if (currentLabelDisplayMode == 6){ // CIELAB + Per-background + 70% opacity
                labelSphereMaterial.SetInt("_ColorMethod", 4);
                labelSphereMaterial.SetFloat("_OpacityLevel", 0.7f);
                labelSphereMaterial.SetInt("_GranularityMethod", 1);
                labelColorMode.text = "Label Color: CIELAB-based";
                granularitymode.text = "Granularity Mode: Per-area";
                opacityLevel.text = "Opacity Level: 0.7";
                modeID.text = "Mode ID: 6";
            }
            else if (currentLabelDisplayMode == 7){ // CIELAB + Per-background + 70% opacity
                labelSphereMaterial.SetInt("_ColorMethod", 4);
                labelSphereMaterial.SetFloat("_OpacityLevel", 0.7f);
                labelSphereMaterial.SetInt("_GranularityMethod", 2);
                labelColorMode.text = "Label Color: CIELAB-based";
                granularitymode.text = "Granularity Mode: Per-background";
                opacityLevel.text = "Opacity Level: 0.7";
                modeID.text = "Mode ID: 7";
            }
        }
    }
}
