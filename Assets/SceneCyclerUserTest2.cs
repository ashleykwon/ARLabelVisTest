using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OVR;
using TMPro;

public class SceneCyclerUserTest2 : MonoBehaviour
{   
    // public bool randomize = true;
    
    // private int qIdx = -1;
    // private List<int> qMap = new List<int>();

    // private Dictionary<string, int> sceneToIdx = new Dictionary<string, int>();

    // private Dictionary<int, SceneQuestion> sceneQuestions = new Dictionary<int, SceneQuestion>();
    // int aIdx = 0;


    // public GameObject labelSphere;
    // public GameObject backgroundAndLabelSphere;
    // public GameObject CamerasContainer;
    // Material labelSphereMaterial;
    // Material backgroundAndLabelSphereMaterial;

    // // private GameObject sceneContainer;
    // private bool rIndexTriggerHeld = false;
    // private bool lIndexTriggerHeld = false;
    // private bool lHandTriggerHeld = false;
    // int currentVisModeIdx;
    // RenderStereoBackgroundforAreaLabel CurrentScript;
    // int currentQuestionIdx;
    // public TMP_Text modeID;
    // public TMP_Text confirmationMessage;
    // List<int[]> modePreferences;
    // List<int[]> allComparisons;
    // List<int[]> comparisonsToUse;
    // bool turnOffLabel = false;
    // int currentMode; // currently displayed mode
    // bool triggerLeft;
    // System.Random randomIdx;
    // public int numComparisons;
    // bool preferenceChosen;
    // Vector2 stickInput;
    // int[] currentComparison;
    // int currentComparisonPairIdx; // index in numComparisons


    // private List<string> ParseName(string sceneName)
    // {
    //     List<string> tokens = new List<string>();
    //     string[] splitTokens = sceneName.Split('_');
    //     tokens.AddRange(splitTokens);

    //     return tokens;
    // }

    // private void ParseQuestions()
    // {
    //     string filePath = Path.Combine(Application.dataPath, "Resources/UserTesting/questions_test2.json");
    //     string json = File.ReadAllText(filePath);

    //     SceneQuestionsList questionsList = JsonUtility.FromJson<SceneQuestionsList>(json);
    //     int i = 0;

    //     foreach (SceneQuestion question in questionsList.sceneQuestions)
    //     {
    //         sceneQuestions[i] = question;
    //         qMap.Add(i);
    //         i++;
    //     }

    //     // if (randomize)
    //     // {
    //     //     qMap = CreateAndShuffleList(qMap.Count);
    //     // }

    //     Dictionary<string, List<int>> uniqScntoi = new Dictionary<string, List<int>>();

    //     for (int j = 0; j < qMap.Count; j++)
    //     {
    //         // int SceneID = j;
    //         SceneQuestion cur = sceneQuestions[qMap[j]];
    //         if (!uniqScntoi.ContainsKey(cur.sceneName))
    //         {
    //             uniqScntoi[cur.sceneName] = new List<int>();
    //         }
    //         uniqScntoi[cur.sceneName].Add(qMap[j]);
    //     }

    //     qMap = new List<int>();

    //     foreach (var kvp in uniqScntoi)
    //     {
    //         foreach (int idx in kvp.Value)
    //         {
    //             qMap.Add(idx);
    //         }
    //     }
    // }

    // void Start()
    // {

    //     for (int i = 1; i < SceneManager.sceneCountInBuildSettings; i++)
    //     {
            
    //         string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
    //         string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
    //         List<string> tokens = ParseName(sceneName);
    //         sceneToIdx.Add(tokens[0], i);
    //     }

    //     // Set the first label display mode
    //     labelSphereMaterial = labelSphere.GetComponent<Renderer>().material;
    //     backgroundAndLabelSphereMaterial = backgroundAndLabelSphere.GetComponent<Renderer>().material;
    //     currentVisModeIdx = 0;

    //     ParseQuestions();
    //     // UnityEngine.Debug.Log(sceneQuestions.Count);

    //     // Add all available comparisons to the int array of modeIDs
    //     allComparisons = new List<int[]>();
    //     // Debug.Log("Beginning to add comparisons");
    //     for (int j = 0; j <= 6; j++){
    //         for (int k = 1; k <= 7; k++){
    //             if (k > j){
    //                 int[] comparison = new int[2];
    //                 comparison[0] = j;
    //                 comparison[1] = k;
    //                 allComparisons.Add(comparison);
    //             }
    //         }
    //     }

    //     // Randomly select comparisons to use
    //     randomIdx = new System.Random();
    //     comparisonsToUse = new List<int[]>();
    //     for (int i = 0; i < numComparisons; i++){
    //         int currentPairIdx = randomIdx.Next(0, allComparisons.Count-1);
    //         comparisonsToUse.Add(allComparisons[currentPairIdx]);
    //         allComparisons.RemoveAt(currentPairIdx);
    //     }

    //     // For debugging purposes only
    //     // comparisonsToUse[0][0] = 5;


    //     // Set default values for label mode visualization 
    //     currentMode = 0; // 0 by default
    //     triggerLeft = false;
    //     preferenceChosen = false;
    //     currentComparisonPairIdx = 0;
    //     currentComparison = new int[2];
    //     currentComparison[0] = comparisonsToUse[currentComparisonPairIdx][0]; // set the initial comparison
    //     currentComparison[1] = comparisonsToUse[currentComparisonPairIdx][1]; // set the initial comparison
    //     displayMode(currentComparison[0]); // set the initial display
    //     modePreferences = new List<int[]>();
    //     confirmationMessage.text = "";

    //     currentQuestionIdx = 0;
    //     LoadNext(true);
    // }


    // public async Task WriteResponses()
    // {
        
    // }

    // public void RecordResponse()
    // {
    //     SceneQuestion cur = sceneQuestions[currentQuestionIdx];
    //     if (!cur.responded)
    //     {
    //         cur.response = aIdx.ToString();
    //         cur.labelMode = allVisualizationModes[currentVisModeIdx];
    //         cur.detectionTime = detectionTime;
    //         cur.responseTime = responseTime;
    //         cur.responded = true;
    //         UnityEngine.Debug.Log("Recorded response " + cur.answers[aIdx]);
    //         UnityEngine.Debug.Log(responseTime);
    //     }
    // }

    // public void ManualResponse()
    // {
    //     UserTestingMovePlayer instance = FindObjectOfType<UserTestingMovePlayer>();
    //     SceneQuestion cur = sceneQuestions[currentQuestionIdx];
    //     cur.response = aIdx.ToString();
    //     cur.labelMode = allVisualizationModes[currentVisModeIdx];
    //     cur.responded = true;
    // }


    // public bool Responded()
    // {
    //     return sceneQuestions[currentQuestionIdx].responded;
    // }

    // public string SceneName()
    // {
    //     return sceneQuestions[qMap[qIdx]].sceneName;
    // }
    
    // public void ShowQuestion()
    // {
    //     detectionSW.Stop();
    //     responseSW.Reset();
    //     responseSW.Start();
    //     questionUI.SetActive(true);
    // }

    // // false - task 1 (polygons), true - task 2 (optimal label)
    // public bool Mode()
    // {
    //     return sceneQuestions[currentQuestionIdx].answers.Count == 0;
    // }

    // public void UpdateMask(SceneQuestion currentQuestion)
    // {
    //     Cubemap newLabelCubemap = Resources.Load("Materials/" + currentQuestion.mask+"3D", typeof(Cubemap)) as Cubemap;
    //     labelSphere.GetComponent<Renderer>().material.SetTexture("_CubeMap", newLabelCubemap);
    //     // UnityEngine.Debug.Log(currentQuestion.mask);
        
    //     // Load new 2D masks for average background value calculation
    //     CurrentScript = CamerasContainer.GetComponent<RenderStereoBackgroundforAreaLabel>(); 
    //     CurrentScript.LabelMask = Resources.Load<Texture2D>("Materials/" + currentQuestion.mask+"2D");
    //     CurrentScript.BackgroundMask = Resources.Load<Texture2D>("Materials/" + currentQuestion.mask+"2D_BG");

    //     // Recalculate average background values 
    //     CurrentScript.backgroundOrLableChanged = true;
    // }


    // public void UpdateBackground(SceneQuestion currentQuestion)
    // {
    //     // Load new Equirectangular background 
    //     CurrentScript = CamerasContainer.GetComponent<RenderStereoBackgroundforAreaLabel>();
    //     CurrentScript.EquirectangularBackground = Resources.Load<Texture2D>("Materials/" + currentQuestion.sceneName+"2D");

    //     // Recalculate average background values 
    //     CurrentScript.backgroundOrLableChanged = true;
    // }

    // // Updates active question based on current state of qIdx
    // public void UpdateQuestion()
    // {
        
    //     SceneQuestion curQ = sceneQuestions[currentQuestionIdx];

    //     UpdateMask(curQ);
    //     UpdateBackground(curQ);
    // }

    // public void LoadNext(bool bypass = false)
    // {
    //     if (bypass || Responded())
    //     {   
    //         // qIdx = (qIdx + 1) % qMap.Count;
    //         currentQuestionIdx = currentQuestionIdx % qMap.Count;
    //         // string nextScn = sceneQuestions[qMap[qIdx]].sceneName;
    //         string nextScn = sceneQuestions[currentQuestionIdx].sceneName;
    //         SceneManager.LoadScene(sceneToIdx[nextScn]);
    //         UpdateQuestion();
    //     }
    // }

    // int mod(int x, int m) {
    //     return (x%m + m)%m;
    // }



    // void UpdateDisplayMode(int currentLabelDisplayMode)
    // {
    //     if (currentLabelDisplayMode == 0){ // Baseline + 40% opacity
    //         backgroundAndLabelSphereMaterial.SetInt("_ColorMethod", 5);
    //         backgroundAndLabelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
    //         modeID.text = "Mode ID: 0";
    //     }
    //     else if (currentLabelDisplayMode == 1){ // Baseline + 70% opacity
    //         backgroundAndLabelSphereMaterial.SetInt("_ColorMethod", 5);
    //         backgroundAndLabelSphereMaterial.SetFloat("_OpacityLevel", 0.7f);
    //         modeID.text = "Mode ID: 1";
    //     }
    //     else if (currentLabelDisplayMode == 2){ // CIELAB + Per-pixel + 40% opacity
    //         backgroundAndLabelSphereMaterial.SetInt("_ColorMethod", 4);
    //         backgroundAndLabelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
    //         backgroundAndLabelSphereMaterial.SetInt("_GranularityMethod", 0);
    //         modeID.text = "Mode ID: 2";
    //     }
    //     else if (currentLabelDisplayMode == 3){ // CIELAB + Per-area + 40% opacity
    //         backgroundAndLabelSphereMaterial.SetInt("_ColorMethod", 4);
    //         backgroundAndLabelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
    //         backgroundAndLabelSphereMaterial.SetInt("_GranularityMethod", 1);
    //         modeID.text = "Mode ID: 3";
    //     }
    //     else if (currentLabelDisplayMode == 4){ // CIELAB + Per-background + 30% opacity
    //         backgroundAndLabelSphereMaterial.SetInt("_ColorMethod", 4);
    //         backgroundAndLabelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
    //         backgroundAndLabelSphereMaterial.SetInt("_GranularityMethod", 2);
    //         modeID.text = "Mode ID: 4";
    //     }
    //     else if (currentLabelDisplayMode == 5){ // CIELAB + Per-pixel + 70% opacity
    //         backgroundAndLabelSphereMaterial.SetInt("_ColorMethod", 4);
    //         backgroundAndLabelSphereMaterial.SetFloat("_OpacityLevel", 0.7f);
    //         backgroundAndLabelSphereMaterial.SetInt("_GranularityMethod", 0);
    //         modeID.text = "Mode ID: 5";
    //     }
    //     else if (currentLabelDisplayMode == 6){ // CIELAB + Per-area + 70% opacity
    //         backgroundAndLabelSphereMaterial.SetInt("_ColorMethod", 4);
    //         backgroundAndLabelSphereMaterial.SetFloat("_OpacityLevel", 0.7f);
    //         backgroundAndLabelSphereMaterial.SetInt("_GranularityMethod", 1);
    //         modeID.text = "Mode ID: 6";
    //     }
    //     else if (currentLabelDisplayMode == 7){ // CIELAB + Per-background + 70% opacity
    //         backgroundAndLabelSphereMaterial.SetInt("_ColorMethod", 4);
    //         backgroundAndLabelSphereMaterial.SetFloat("_OpacityLevel", 0.7f);
    //         backgroundAndLabelSphereMaterial.SetInt("_GranularityMethod", 2);
    //         modeID.text = "Mode ID: 7";
    //     }
    //     else if (currentLabelDisplayMode == 8){ // No label
    //         backgroundAndLabelSphereMaterial.SetInt("_ColorMethod", 6);
    //         backgroundAndLabelSphereMaterial.SetFloat("_OpacityLevel", 0.0f);
    //         modeID.text = "Mode ID: No label";
    //     }
    // }

    // //Update is called once per frame
    // void Update()
    // {
    //     //bool triggerRight = OVRInput.Get(OVRInput.Button.Two);

    //     stickInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
    //     triggerLeft = OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger);

    //     // Run the comparison 
    //     if (triggerLeft) 
    //     {
    //         turnOffLabel = !turnOffLabel;
    //         if (turnOffLabel){
    //             // Turn off the label display
    //             displayMode(8);
    //         }
    //         else{
    //             displayMode(currentMode);
    //         }  
    //     }

    //     if (!turnOffLabel){
    //         if (stickInput.magnitude > 0.8f)
    //         {
    //             confirmationMessage.text = "";
    //             if (stickInput.x < 0) // tilt to the left
    //             {
    //                 // Set the current mode<
    //                 currentMode = currentComparison[0];

    //                 // Display the current mode
    //                 displayMode(currentComparison[0]);
                    
    //             }    
    //             else if (stickInput.x >= 0) // tilt to the right
    //             {
    //                 // Set the current mode
    //                 currentMode = currentComparison[1];

    //                 // Display the current mode
    //                 displayMode(currentComparison[1]);
    //             }
    //         }
    //     }

    //     if(OVRInput.GetUp(OVRInput.Button.PrimaryThumbstick))
    //     {
    //         int[] preference = new int[2];
    //         if (currentMode == currentComparison[0]){
    //             preference[0] = currentComparison[0];
    //             preference[1] = currentComparison[1];
    //         }
    //         else{
    //             preference[0] = currentComparison[1];
    //             preference[1] = currentComparison[0];
    //         }
    //         if (currentComparisonPairIdx < numComparisons){
    //             // Add the chosen preference to modePreferences
    //             modePreferences.Add(preference);
    //             string chosenModeAsString = preference[0].ToString();
    //             confirmationMessage.text = "Mode " + chosenModeAsString + " chosen!";
    //             // Move on to the next comparison
    //             currentComparisonPairIdx += 1;  
    //             currentComparison = comparisonsToUse[currentComparisonPairIdx];
    //             displayMode(currentComparison[0]);
    //             // if (currentComparisonPairIdx == numComparisons-1){
    //             //     Debug.Log("Done!");
    //             //     modeID.text = "End of all comparisons!";
    //             // }

    //         }
    //         else{
    //             Debug.Log("Done!");
    //             modeID.text = "End of all comparisons!";
    //         }
    //     }

    //     if (((rIndexTrigger > 0) && !rIndexTriggerHeld) || Input.GetKeyDown(KeyCode.N)) 
    //     {   
    //         rIndexTriggerHeld = true;
    //         LoadNext();
    //     }
   
    // }

    // void OnDestroy()
    // {
    //     WriteResponses();
    // }
}
