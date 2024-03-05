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
    public GameObject LabelContainer;
    // public TMP_Text labelColorMode;
    // public TMP_Text opacityLevel;
    // public TMP_Text granularitymode;
    public TMP_Text modeID;
    Material labelSphereMaterial;
    private Dictionary<int, List<int>> modesPreferredOver = new Dictionary<int, List<int>>();
    private int[] confirmedPreference;
    int[] sortedModes; // stores all available modeIDs
    bool turnOffLabel = false;
    int currentMode; // currently displayed mode
    bool preferredModeChosen;
    
    // Start is called before the first frame update
    void Start()
    {
        labelSphereMaterial = BackgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial;

        labelSphereMaterial.SetInt("_ColorMethod", 5);
        labelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
        modeID.text = "Mode ID: 0";
        confirmedPreference = new int[2];
        sortedModes = new int[8];
        for (int i = 0; i < 8; i++){
            sortedModes[i] = i;
            modesPreferredOver[i] = new List<int>();
        }
        currentMode = 0; // 0 by default
        preferredModeChosen = false;
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
    // void mergeSort(int []arr, int n)
    // {
    //     // For current size of subarrays to be merged curr_size varies from 1 to n/2
    //     int curr_size; 
                      
    //     // For picking starting index of left subarray to be merged
    //     int left_start;
                          
          
    //     // Merge subarrays in bottom up manner. First merge subarrays of size 1 to create sorted 
    //     // subarrays of size 2, then merge subarrays of size 2 to create sorted subarrays of size 4, and so on.
    //     for (curr_size = 1; curr_size <= n-1; curr_size = 2*curr_size)
    //     { 
    //         // Pick starting point of different subarrays of current size
    //         for (left_start = 0; left_start < n-1; left_start += 2*curr_size)
    //         {
    //             // Find ending point of left subarray. mid+1 is starting point of right
    //             int mid = Math.Min(left_start + curr_size - 1,n-1);
          
    //             int right_end = Math.Min(left_start + 2*curr_size - 1, n-1);
          
    //             // Merge Subarrays arr[left_start...mid] & arr[mid+1...right_end]
    //             merge(arr, left_start, mid, right_end);
    //         }
    //     }

    //     Debug.Log("Done");
    // }
      
    // /* Function to merge the two haves arr[l..m] and
    // arr[m+1..r] of array arr[] */
    // void merge(int []arr, int l, int m, int r)
    // {
    //     int i, j, k;
    //     int n1 = m - l + 1;
    //     int n2 = r - m;
      
    //     /* create temp arrays */
    //     int []L = new int[n1];
    //     int []R = new int[n2];
      
    //     /* Copy data to temp arrays L[] and R[] */
    //     for (i = 0; i < n1; i++)
    //         L[i] = arr[l + i];
    //     for (j = 0; j < n2; j++)
    //         R[j] = arr[m + 1+ j];
      
    //     /* Merge the temp arrays back into arr[l..r]*/
    //     i = 0;
    //     j = 0;
    //     k = l;
    //     while (i < n1 && j < n2)
    //     {
    //         // Compare the two current modes
    //         if (!preferredModeChosen){
    //             CompareModes(L[i], R[j]);
                
    //         }
    //         else{
    //             if (modesPreferredOver[R[j]].Contains(L[i])) // L[i] is preferred over R[j]
    //             {
    //                 arr[k] = L[i];
    //                 i++;
    //             }
    //             else
    //             {
    //                 arr[k] = R[j];
    //                 j++;
    //             }
    //             preferredModeChosen = false;
    //             k++;
    //         }
            
    //     }
      
    //     /* Copy the remaining elements of L[], if there are any */
    //     while (i < n1)
    //     {
    //         arr[k] = L[i];
    //         i++;
    //         k++;
    //     }
      
    //     /* Copy the remaining elements of R[], if there are any */
    //     while (j < n2)
    //     {
    //         arr[k] = R[j];
    //         j++;
    //         k++;
    //     }
    // }

    /* l is for left index and r is right index of the sub-array of arr to be sorted */
    void mergeSort(int[] arr, int l, int r)
    {
        if (l < r)
        { 
            // Same as (l+r)/2 but avoids overflow for large l & h
            int m = l + (r - l) / 2; 
            mergeSort(arr, l, m);
            mergeSort(arr, m+1, r);
            merge(arr, l, m, r);
       }
    }
 
    /* Function to merge the two haves arr[l..m] and arr[m+1..r] of array arr[] */
    void merge(int[] arr, int l, int m, int r)
    {
        int i, j, k;
        int n1 = m - l + 1;
        int n2 = r - m;
     
        /* create temp arrays */
        int []L = new int[n1];
        int []R = new int[n2];
     
        /* Copy data to temp arrays
        L[] and R[] */
        for (i = 0; i < n1; i++)
            L[i] = arr[l + i];
        for (j = 0; j < n2; j++)
            R[j] = arr[m + 1+ j];
     
        /* Merge the temp arrays back into arr[l..r]*/
        i = 0;
        j = 0;
        k = l;
        while (i < n1 && j < n2)
        {
            CompareModes(L[i], R[j]);
            if (modesPreferredOver[R[j]].Contains(L[i]))
            {
                arr[k] = L[i];
                i++;
            }
            else
            {
                arr[k] = R[j];
                j++;
            }
            k++;
        }
     
        /* Copy the remaining elements of
        L[], if there are any */
        while (i < n1)
        {
            arr[k] = L[i];
            i++;
            k++;
        }
     
        /* Copy the remaining elements of
        R[], if there are any */
        while (j < n2)
        {
            arr[k] = R[j];
            j++;
            k++;
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

                // Update the preference information in the modesPreferredOver dictionary
                modesPreferredOver[sortedModes[rightIdx]].Add(sortedModes[leftIdx]);
            }
            else{
                confirmedPreference[0] = sortedModes[rightIdx];
                confirmedPreference[1] = sortedModes[leftIdx];

                // Update the preference information in the modesPreferredOver dictionary
                modesPreferredOver[sortedModes[leftIdx]].Add(sortedModes[rightIdx]); // the mode at rightIdx is preferred over that at leftIdx
            }

            // Change the global variable value to ensure that there's only one pair being compared at a time
            preferredModeChosen = true;
            // Print the preferred mode for debugging purposes
            Debug.Log("Preferred mode chosen!");
            Debug.Log(confirmedPreference[0]);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Run the comparison with the MergeSort class 
        // mergeSort(sortedModes, sortedModes.Length);
        mergeSort(sortedModes, 0, sortedModes.Length-1);
    }
}
