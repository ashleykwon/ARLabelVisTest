using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OVR;
using TMPro;

public class PairwiseComparison : MonoBehaviour
{
    public Rigidbody player;
    public GameObject BackgroundAndLabelSphere;
    public GameObject LabelContainer;
    // public TMP_Text labelColorMode;
    // public TMP_Text opacityLevel;
    // public TMP_Text granularitymode;
    public TMP_Text modeID;
    Material labelSphereMaterial;
    private Dictionary<int, List<int>> modesPreferredOver = new Dictionary<int, List<int>>();
    private int[] confirmedPreference;
    List<int> sortedModes = new List<int>(); // stores all available modeIDs
    bool turnOffLabel = false;
    int currentMode; // currently displayed mode
    
    // Start is called before the first frame update
    void Start()
    {
        labelSphereMaterial = BackgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial;

        labelSphereMaterial.SetInt("_ColorMethod", 5);
        labelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
        // labelColorMode.text = "Label Color: Green";
        // granularitymode.text = "Granularity Mode: N/A";
        // opacityLevel.text = "Opacity Level: 0.4";
        modeID.text = "Mode ID: 0";
        confirmedPreference = new int[2];
        for (int i = 0; i < 8; i++){
            sortedModes.Add(i);
        }
        currentMode = 0; // 0 by default
    }

    void displayMode(int currentLabelDisplayMode)
    {
        if (currentLabelDisplayMode == 0){ // Baseline + 40% opacity
            labelSphereMaterial.SetInt("_ColorMethod", 5);
            labelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
            // labelColorMode.text = "Label Color: Green";
            // granularitymode.text = "Granularity Mode: N/A";
            // opacityLevel.text = "Opacity Level: 0.4";
            modeID.text = "Mode ID: 0";
        }
        else if (currentLabelDisplayMode == 1){ // Baseline + 70% opacity
            labelSphereMaterial.SetInt("_ColorMethod", 5);
            labelSphereMaterial.SetFloat("_OpacityLevel", 0.7f);
            // labelColorMode.text = "Label Color: Green";
            // granularitymode.text = "Granularity Mode: N/A";
            // opacityLevel.text = "Opacity Level: 0.7";
            modeID.text = "Mode ID: 1";
        }
        else if (currentLabelDisplayMode == 2){ // CIELAB + Per-pixel + 40% opacity
            labelSphereMaterial.SetInt("_ColorMethod", 4);
            labelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
            labelSphereMaterial.SetInt("_GranularityMethod", 0);
            // labelColorMode.text = "Label Color: CIELAB-based";
            // granularitymode.text = "Granularity Mode: Per-pixel";
            // opacityLevel.text = "Opacity Level: 0.4";
            modeID.text = "Mode ID: 2";
        }
        else if (currentLabelDisplayMode == 3){ // CIELAB + Per-area + 40% opacity
            labelSphereMaterial.SetInt("_ColorMethod", 4);
            labelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
            labelSphereMaterial.SetInt("_GranularityMethod", 1);
            // labelColorMode.text = "Label Color: CIELAB-based";
            // granularitymode.text = "Granularity Mode: Per-area";
            // opacityLevel.text = "Opacity Level: 0.4";
            modeID.text = "Mode ID: 3";
        }
        else if (currentLabelDisplayMode == 4){ // CIELAB + Per-background + 30% opacity
            labelSphereMaterial.SetInt("_ColorMethod", 4);
            labelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
            labelSphereMaterial.SetInt("_GranularityMethod", 2);
            // labelColorMode.text = "Label Color: CIELAB-based";
            // granularitymode.text = "Granularity Mode: Per-background";
            // opacityLevel.text = "Opacity Level: 0.4";
            modeID.text = "Mode ID: 4";
        }
        else if (currentLabelDisplayMode == 5){ // CIELAB + Per-background + 70% opacity
            labelSphereMaterial.SetInt("_ColorMethod", 4);
            labelSphereMaterial.SetFloat("_OpacityLevel", 0.7f);
            labelSphereMaterial.SetInt("_GranularityMethod", 0);
            // labelColorMode.text = "Label Color: CIELAB-based";
            // granularitymode.text = "Granularity Mode: Per-pixel";
            // opacityLevel.text = "Opacity Level: 0.7";
            modeID.text = "Mode ID: 5";
        }
        else if (currentLabelDisplayMode == 6){ // CIELAB + Per-background + 70% opacity
            labelSphereMaterial.SetInt("_ColorMethod", 4);
            labelSphereMaterial.SetFloat("_OpacityLevel", 0.7f);
            labelSphereMaterial.SetInt("_GranularityMethod", 1);
            // labelColorMode.text = "Label Color: CIELAB-based";
            // granularitymode.text = "Granularity Mode: Per-area";
            // opacityLevel.text = "Opacity Level: 0.7";
            modeID.text = "Mode ID: 6";
        }
        else if (currentLabelDisplayMode == 7){ // CIELAB + Per-background + 70% opacity
            labelSphereMaterial.SetInt("_ColorMethod", 4);
            labelSphereMaterial.SetFloat("_OpacityLevel", 0.7f);
            labelSphereMaterial.SetInt("_GranularityMethod", 2);
            // labelColorMode.text = "Label Color: CIELAB-based";
            // granularitymode.text = "Granularity Mode: Per-background";
            // opacityLevel.text = "Opacity Level: 0.7";
            modeID.text = "Mode ID: 7";
        }
        else if (currentLabelDisplayMode == 8){ // CIELAB + Per-background + 70% opacity
            labelSphereMaterial.SetInt("_ColorMethod", 6);
            labelSphereMaterial.SetFloat("_OpacityLevel", 0.0f);
            // labelColorMode.text = "Label Color: N/A";
            // granularitymode.text = "Granularity Mode: N/A";
            // opacityLevel.text = "Opacity Level: N/A";
            modeID.text = "Mode ID: No label";
        }
    }

    void CompareModes(int leftIdx, int rightIdx)
    {
        bool triggerLeft = OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger);
        Vector2 stickInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
        // currentMode = sortedModes[leftIdx];

        if (triggerLeft) 
        {
            turnOffLabel = !turnOffLabel;
            if (turnOffLabel){
                // Turn off the label display
                displayMode(8);
            }
            else{
                displayMode(currentMode);
            }  
        }

        if (!turnOffLabel){
            if (stickInput.magnitude > 0.8f)
            {
                if (stickInput.x < 0) // tilt to the right
                {
                    // Set the current mode
                    currentMode = sortedModes[rightIdx];

                    // Display the current mode
                    displayMode(sortedModes[rightIdx]);
                    
                }    
                else if (stickInput.x >= 0) // tilt to the left
                {
                    // Set the current mode
                    currentMode = sortedModes[leftIdx];

                    // Display the current mode
                    displayMode(sortedModes[leftIdx]);
                }
            }
        }

        if(OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick))
        {
            if (currentMode == sortedModes[leftIdx]){
                confirmedPreference[0] = sortedModes[leftIdx];
                confirmedPreference[1] = sortedModes[rightIdx];
            }
            else{
                confirmedPreference[0] = sortedModes[rightIdx];
                confirmedPreference[1] = sortedModes[leftIdx];
            }

            // Print the preferred mode for debugging purposes
            Debug.Log("Preferred mode chosen!");
            Debug.Log(confirmedPreference[0]);
        }

    }

    // Update is called once per frame
    void Update()
    {
        // Run the comparison with the MergeSort class 
        CompareModes(0, 1);
    }
}
