using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OVR;
using TMPro;
using System;

public class PairwiseComparison : MonoBehaviour
{
    public Rigidbody player;
    public GameObject BackgroundAndLabelSphere;
    // public TMP_Text labelColorMode;
    // public TMP_Text opacityLevel;
    // public TMP_Text granularitymode;
    public TMP_Text modeID;
    public TMP_Text confirmationMessage;
    Material labelSphereMaterial;
    List<int[]> modePreferences;
    List<int[]> allComparisons;
    List<int[]> comparisonsToUse;
    bool turnOffLabel = false;
    int currentMode; // currently displayed mode
    bool triggerLeft;
    System.Random randomIdx;
    public int numComparisons;
    bool preferenceChosen;
    Vector2 stickInput;
    int[] currentComparison;
    int currentComparisonPairIdx; // index in numComparisons


    // Start is called before the first frame update
    void Start()
    {
        labelSphereMaterial = BackgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial;

        numComparisons = 4; // default number

        // Add all available comparisons to the int array of modeIDs
        allComparisons = new List<int[]>();
        // Debug.Log("Beginning to add comparisons");
        for (int j = 0; j <= 6; j++){
            for (int k = 1; k <= 7; k++){
                if (k > j){
                    int[] comparison = new int[2];
                    comparison[0] = j;
                    comparison[1] = k;
                    allComparisons.Add(comparison);
                }
            }
        }

        // Randomly select comparisons to use
        randomIdx = new System.Random();
        comparisonsToUse = new List<int[]>();
        for (int i = 0; i < numComparisons; i++){
            int currentPairIdx = randomIdx.Next(0, allComparisons.Count-1);
            comparisonsToUse.Add(allComparisons[currentPairIdx]);
            allComparisons.RemoveAt(currentPairIdx);
        }


        // Set default values for label mode visualization 
        currentMode = 0; // 0 by default
        triggerLeft = false;
        preferenceChosen = false;
        currentComparisonPairIdx = 0;
        currentComparison = new int[2];
        currentComparison[0] = comparisonsToUse[currentComparisonPairIdx][0]; // set the initial comparison
        currentComparison[1] = comparisonsToUse[currentComparisonPairIdx][1]; // set the initial comparison
        displayMode(currentComparison[0]); // set the initial display
        modePreferences = new List<int[]>();
        confirmationMessage.text = "";
    }

    void displayMode(int currentLabelDisplayMode)
    {
        if (currentLabelDisplayMode == 0){ // Baseline + 40% opacity
            labelSphereMaterial.SetInt("_ColorMethod", 5);
            labelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
            modeID.text = "Mode ID: 0";
        }
        else if (currentLabelDisplayMode == 1){ // Baseline + 70% opacity
            labelSphereMaterial.SetInt("_ColorMethod", 5);
            labelSphereMaterial.SetFloat("_OpacityLevel", 0.7f);
            modeID.text = "Mode ID: 1";
        }
        else if (currentLabelDisplayMode == 2){ // CIELAB + Per-pixel + 40% opacity
            labelSphereMaterial.SetInt("_ColorMethod", 4);
            labelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
            labelSphereMaterial.SetInt("_GranularityMethod", 0);
            modeID.text = "Mode ID: 2";
        }
        else if (currentLabelDisplayMode == 3){ // CIELAB + Per-area + 40% opacity
            labelSphereMaterial.SetInt("_ColorMethod", 4);
            labelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
            labelSphereMaterial.SetInt("_GranularityMethod", 1);
            modeID.text = "Mode ID: 3";
        }
        else if (currentLabelDisplayMode == 4){ // CIELAB + Per-background + 30% opacity
            labelSphereMaterial.SetInt("_ColorMethod", 4);
            labelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
            labelSphereMaterial.SetInt("_GranularityMethod", 2);
            modeID.text = "Mode ID: 4";
        }
        else if (currentLabelDisplayMode == 5){ // CIELAB + Per-background + 70% opacity
            labelSphereMaterial.SetInt("_ColorMethod", 4);
            labelSphereMaterial.SetFloat("_OpacityLevel", 0.7f);
            labelSphereMaterial.SetInt("_GranularityMethod", 0);
            modeID.text = "Mode ID: 5";
        }
        else if (currentLabelDisplayMode == 6){ // CIELAB + Per-background + 70% opacity
            labelSphereMaterial.SetInt("_ColorMethod", 4);
            labelSphereMaterial.SetFloat("_OpacityLevel", 0.7f);
            labelSphereMaterial.SetInt("_GranularityMethod", 1);
            modeID.text = "Mode ID: 6";
        }
        else if (currentLabelDisplayMode == 7){ // CIELAB + Per-background + 70% opacity
            labelSphereMaterial.SetInt("_ColorMethod", 4);
            labelSphereMaterial.SetFloat("_OpacityLevel", 0.7f);
            labelSphereMaterial.SetInt("_GranularityMethod", 2);
            modeID.text = "Mode ID: 7";
        }
        else if (currentLabelDisplayMode == 8){ // CIELAB + Per-background + 70% opacity
            labelSphereMaterial.SetInt("_ColorMethod", 6);
            labelSphereMaterial.SetFloat("_OpacityLevel", 0.0f);
            modeID.text = "Mode ID: No label";
        }
    }
   

    // Update is called once per frame
    void Update()
    {
        stickInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
        triggerLeft = OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger);

        // Run the comparison 
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
                confirmationMessage.text = "";
                if (stickInput.x < 0) // tilt to the left
                {
                    // Set the current mode<
                    currentMode = currentComparison[0];

                    // Display the current mode
                    displayMode(currentComparison[0]);
                    
                }    
                else if (stickInput.x >= 0) // tilt to the right
                {
                    // Set the current mode
                    currentMode = currentComparison[1];

                    // Display the current mode
                    displayMode(currentComparison[1]);
                }
            }
        }

        if(OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick))
        {
            int[] preference = new int[2];
            if (currentMode == currentComparison[0]){
                preference[0] = currentComparison[0];
                preference[1] = currentComparison[1];
            }
            else{
                preference[0] = currentComparison[1];
                preference[1] = currentComparison[0];
            }
            if (currentComparisonPairIdx < numComparisons){
                // Add the chosen preference to modePreferences
                modePreferences.Add(preference);
                string chosenModeAsString = preference[0].ToString();
                confirmationMessage.text = "Mode " + chosenModeAsString + " chosen!";
                // Move on to the next comparison
                currentComparisonPairIdx += 1;  
                currentComparison = comparisonsToUse[currentComparisonPairIdx];
                displayMode(currentComparison[0]);

            }
            else{
                Debug.Log("Done!");
                modeID.text = "End of all comparisons!";
            }
        }
    }
}

