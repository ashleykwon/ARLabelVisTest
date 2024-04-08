using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using UnityEngine;
using OVR;
using TMPro;
using System;
using System.IO;

public class PairwiseComparison : MonoBehaviour
{
    public string comparisonJson;
    private List<SceneComparison> sceneComparisons = new List<SceneComparison>();
    private int numSceneComparisons;
    private int scIdx = 0;
    private Dictionary<string, int> sceneToIdx = new Dictionary<string, int>();

    public Rigidbody player;
    public GameObject labelSphere;
    public GameObject BackgroundAndLabelSphere;
    public GameObject OVRCameraRig;
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
    string outputFilePath;
    // private Dictionary<string, int> sceneToIdx = new Dictionary<string, int>();
    StreamWriter writer;
    public string SceneName;


    // Start is called before the first frame update
    void Start()
    {

        for (int i = 1; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneFile = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            string sceneName = sceneFile.Split('_')[0];
            sceneToIdx.Add(sceneName, i);
        }

        labelSphereMaterial = BackgroundAndLabelSphere.GetComponent<MeshRenderer>().sharedMaterial;

        numComparisons = 8; // default number

        ParseComparisonJson();
        UpdateScene();

        // // Add all available comparisons to the int array of modeIDs
        // allComparisons = new List<int[]>();
        // // Debug.Log("Beginning to add comparisons");
        // for (int j = 0; j <= 6; j++){
        //     for (int k = 1; k <= 7; k++){
        //         if (k > j){
        //             int[] comparison = new int[2];
        //             comparison[0] = j;
        //             comparison[1] = k;
        //             allComparisons.Add(comparison);
        //         }
        //     }
        // }

        // // Randomly select comparisons to use
        // randomIdx = new System.Random();
        // comparisonsToUse = new List<int[]>();
        // for (int i = 0; i < numComparisons; i++){
        //     int currentPairIdx = randomIdx.Next(0, allComparisons.Count-1);
        //     comparisonsToUse.Add(allComparisons[currentPairIdx]);
        //     allComparisons.RemoveAt(currentPairIdx);
        // }

        // // For debugging purposes only
        // // comparisonsToUse[0][0] = 5;


        // // Set default values for label mode visualization 
        // currentMode = 0; // 0 by default
        // triggerLeft = false;
        // preferenceChosen = false;
        // currentComparisonPairIdx = 0;
        // currentComparison = new int[2];
        // currentComparison[0] = comparisonsToUse[currentComparisonPairIdx][0]; // set the initial comparison
        // currentComparison[1] = comparisonsToUse[currentComparisonPairIdx][1]; // set the initial comparison
        // displayMode(currentComparison[0]); // set the initial display
        // modePreferences = new List<int[]>();
        // confirmationMessage.text = "";

        // Start a text file to write answers
        // string dateString = DateTime.Now.ToString("yyyyMMdd_HHmm");
        // outputFilePath = Path.Combine(Application.dataPath, SceneName+$"UserResponse_Test2_{dateString}.txt");
        // writer = new StreamWriter(outputFilePath, true);
    }

    private void ParseComparisonJson()
    {
        string filePath = Path.Combine(Application.dataPath, $"Resources/UserTesting/{comparisonJson}.json");
        string json = File.ReadAllText(filePath);

        SceneComparisonList comparisonsList = JsonUtility.FromJson<SceneComparisonList>(json);
        sceneComparisons = comparisonsList.sceneComparisons;
        numSceneComparisons = comparisonsList.sceneComparisons.Count;
        Debug.Log($"num scene comps: {numSceneComparisons}");

        for (int i = 0; i < numSceneComparisons; i++)
        {
            for (int j = 0; j < numComparisons; j++)
            {
                sceneComparisons[i].responses.Add(new CompResponse());
            }
        }

        // foreach (SceneComparison comp in comparisonsList.sceneComparisons)
        // {
        //     sceneComparisons.Add(comp);
        // }
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
        else if (currentLabelDisplayMode == 5){ // CIELAB + Per-pixel + 70% opacity
            labelSphereMaterial.SetInt("_ColorMethod", 4);
            labelSphereMaterial.SetFloat("_OpacityLevel", 0.7f);
            labelSphereMaterial.SetInt("_GranularityMethod", 0);
            modeID.text = "Mode ID: 5";
        }
        else if (currentLabelDisplayMode == 6){ // CIELAB + Per-area + 70% opacity
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
        else if (currentLabelDisplayMode == 8){ // No label
            labelSphereMaterial.SetInt("_ColorMethod", 6);
            labelSphereMaterial.SetFloat("_OpacityLevel", 0.0f);
            modeID.text = "Mode ID: No label";
        }
    }

    public async Task WriteResponses()
    {
        SceneComparisonList scResponses = new SceneComparisonList();
        scResponses.sceneComparisons = sceneComparisons;
        
        string json = JsonUtility.ToJson(scResponses, true);
        string dateString = DateTime.Now.ToString("yyyyMMdd_HHmm");
        string outpath = Path.Combine(Application.dataPath, $"Test2_Results/UserResponse_{dateString}.json");
        UnityEngine.Debug.Log(outpath);
        using (StreamWriter writer = new StreamWriter(outpath, false))
        {
            await writer.WriteAsync(json);
        }
    }

    void UpdateScene()
    {
        SceneComparison currentSC = sceneComparisons[scIdx];

        // Update scene
        SceneManager.LoadScene(sceneToIdx[currentSC.sceneName]);

        Cubemap newLabelCubemap = Resources.Load("Materials/" + currentSC.mask+"3D", typeof(Cubemap)) as Cubemap;
        labelSphere.GetComponent<Renderer>().material.SetTexture("_CubeMap", newLabelCubemap);

        // Load new 2D masks for average background value calculation
        RenderStereoBackgroundforAreaLabel CurrentScript = OVRCameraRig.GetComponent<RenderStereoBackgroundforAreaLabel>(); 
        CurrentScript.LabelMask = Resources.Load<Texture2D>("Materials/" + currentSC.mask+"2D");
        CurrentScript.BackgroundMask = Resources.Load<Texture2D>("Materials/" + currentSC.mask+"2D_BG");
        CurrentScript.EquirectangularBackground = Resources.Load<Texture2D>("Materials/" + currentSC.sceneName);

        // Recalculate average background values 
        CurrentScript.backgroundOrLableChanged = true;

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

        // For debugging purposes only
        // comparisonsToUse[0][0] = 5;


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

                // Write the 2 modes that were compared and the mode that was chosen as a line in a txt file
                // writer.WriteLine(currentComparison[0].ToString() + ", " + currentComparison[1].ToString() +", " + chosenModeAsString);

                SceneComparison curSC = sceneComparisons[scIdx];
                CompResponse compRes = new CompResponse();
                compRes.pair = new List<int>() {preference[0], preference[1]};
                curSC.responses[currentComparisonPairIdx] = compRes;

                // Move on to the next comparison
                currentComparisonPairIdx += 1;  
                currentComparison = comparisonsToUse[currentComparisonPairIdx];
                displayMode(currentComparison[0]);
                // if (currentComparisonPairIdx == numComparisons-1){
                //     Debug.Log("Done!");
                //     modeID.text = "End of all comparisons!";
                // }

            }
            else{
                Debug.Log("Done!");
                modeID.text = "End of all comparisons!";
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            scIdx -= 1;
            if (scIdx == -1)
            {
                scIdx = numSceneComparisons - 1;
            }
            UpdateScene();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            scIdx = (scIdx + 1) % numSceneComparisons;
            UpdateScene();
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            WriteResponses();
        }
    }

    void OnDestroy()
    {
        // Save the text file at the end of the comparison
        WriteResponses();
    }
}