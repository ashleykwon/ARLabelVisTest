using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadColorAssignmentAlgo : MonoBehaviour
{
    public GameObject labelPlane;
    public Material labelPlaneMaterial;
    //public TMPro.TMP_Dropdown colorAlgoDropdown;
    public int selectedAlgoID;
    // Start is called before the first frame update
    void Start()
    {
        // Access labelPlane's material
        labelPlaneMaterial = labelPlane.GetComponent<MeshRenderer>().sharedMaterial;
        selectedAlgoID = 1; // default to initial method
        // int currentAlgo = labelPlaneMaterial.GetInt("_ColorMethod");
        // Debug.Log(currentAlgo);
        // Debug.Log(colorAlgoDropdown.value);
    }

    public void Update()
    {
        float triggerRight = OVRInput.Get(OVRInput.RawAxis1D.RIndexTrigger);

        if (triggerRight > 0.9f)
        {
            selectedAlgoID += 1;
            SetColorAssignmentAlgo(selectedAlgoID);
        }
    }

    public void SetColorAssignmentAlgo (int selectedAlgoID)
    {
        //int selectedAlgoID = colorAlgoDropdown.value;
        labelPlaneMaterial.SetInt("_ColorMethod", selectedAlgoID);
        Debug.Log("Color assignment algorithm changed!");
    }
}
