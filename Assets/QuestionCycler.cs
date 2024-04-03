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

public class QuestionCycler : MonoBehaviour
{   
    public bool randomize = true;
    
    private int qIdx = -1;
    private List<int> qMap = new List<int>();

    private Dictionary<string, int> sceneToIdx = new Dictionary<string, int>();

    private Dictionary<int, SceneQuestion> sceneQuestions = new Dictionary<int, SceneQuestion>();
    int aIdx = 0;

    Stopwatch detectionSW = new Stopwatch();
    Stopwatch responseSW = new Stopwatch();

    public GameObject questionUI;
    public TMP_Text questionText;
    public GameObject answerContainer;
    public List<Image> answerImgs;
    public List<Image> answerPanels;
    private Color unselected = new Color(1.0f, 215 / 255f, 215 / 255f);
    public GameObject labelSphere;
    public GameObject backgroundAndLabelSphere;
    public GameObject CamerasContainer;
    Material labelSphereMaterial;
    Material backgroundAndLabelSphereMaterial;

    // private GameObject sceneContainer;
    private bool rIndexTriggerHeld = false;
    private bool lIndexTriggerHeld = false;
    private bool lHandTriggerHeld = false;
    int currentVisModeIdx;
    RenderStereoBackgroundforAreaLabel CurrentScript;
    List<int> allVisualizationModes;
    int currentQuestionIdx;


    private List<string> ParseName(string sceneName)
    {
        List<string> tokens = new List<string>();
        string[] splitTokens = sceneName.Split('_');
        tokens.AddRange(splitTokens);

        return tokens;
    }

    private List<int> CreateAndShuffleList(int n)
    {
        List<int> list = new List<int>();
        for (int i = 0; i < n; i++)
        {
            list.Add(i);
        }

        ShuffleList(list);
        return list;
    }

    private void ShuffleList(List<int> list)
    {
        System.Random rand = new System.Random();
        int n = list.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            int temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private void ParseQuestions()
    {
        string filePath = Path.Combine(Application.dataPath, "Resources/UserTesting/questions.json");
        string json = File.ReadAllText(filePath);

        SceneQuestionsList questionsList = JsonUtility.FromJson<SceneQuestionsList>(json);
        int i = 0;

        foreach (SceneQuestion question in questionsList.sceneQuestions)
        {
            sceneQuestions[i] = question;
            qMap.Add(i);
            i++;
        }

        if (randomize)
        {
            qMap = CreateAndShuffleList(qMap.Count);
        }

        Dictionary<string, List<int>> uniqScntoi = new Dictionary<string, List<int>>();

        for (int j = 0; j < qMap.Count; j++)
        {
            // int SceneID = j;
            SceneQuestion cur = sceneQuestions[qMap[j]];
            if (!uniqScntoi.ContainsKey(cur.sceneName))
            {
                uniqScntoi[cur.sceneName] = new List<int>();
            }
            uniqScntoi[cur.sceneName].Add(qMap[j]);
        }

        qMap = new List<int>();

        foreach (var kvp in uniqScntoi)
        {
            foreach (int idx in kvp.Value)
            {
                qMap.Add(idx);
            }
        }
    }

    void Start()
    {

        for (int i = 1; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            List<string> tokens = ParseName(sceneName);
            sceneToIdx.Add(tokens[0], i);
        }

        // Set the first label display mode
        labelSphereMaterial = labelSphere.GetComponent<Renderer>().material;
        backgroundAndLabelSphereMaterial = backgroundAndLabelSphere.GetComponent<Renderer>().material;
        currentVisModeIdx = 0;

        // Add all visualization modes to the list and randomize them
        allVisualizationModes = CreateAndShuffleList(8);

        ParseQuestions();
        // UnityEngine.Debug.Log(sceneQuestions.Count);
        currentQuestionIdx = 0;
        LoadNext(true);
    }


    public async Task WriteResponses()
    {
        SceneQuestionsList responses = new SceneQuestionsList();

        foreach (KeyValuePair<int, SceneQuestion> entry in sceneQuestions)
        {
            responses.sceneQuestions.Add(entry.Value);
        }

        string json = JsonUtility.ToJson(responses, true);
        string dateString = DateTime.Now.ToString("yyyyMMdd_HHmm");
        string outpath = Path.Combine(Application.dataPath, $"UserResponse_{dateString}.json");
        UnityEngine.Debug.Log(outpath);
        using (StreamWriter writer = new StreamWriter(outpath, false))
        {
            await writer.WriteAsync(json);
        }
    }

    public void RecordResponse()
    {
        SceneQuestion cur = sceneQuestions[currentQuestionIdx];
        if (!cur.responded)
        {
            UserTestingMovePlayer instance = FindObjectOfType<UserTestingMovePlayer>();
            responseSW.Stop();
            long detectionTime = detectionSW.ElapsedMilliseconds;
            long responseTime = responseSW.ElapsedMilliseconds;
            cur.response = aIdx.ToString();
            cur.labelMode = allVisualizationModes[currentVisModeIdx];
            cur.detectionTime = detectionTime;
            cur.responseTime = responseTime;
            cur.responded = true;
            UnityEngine.Debug.Log("Recorded response " + cur.answers[aIdx]);
            UnityEngine.Debug.Log(responseTime);
        }
    }

    public void ManualResponse()
    {
        UserTestingMovePlayer instance = FindObjectOfType<UserTestingMovePlayer>();
        SceneQuestion cur = sceneQuestions[currentQuestionIdx];
        cur.response = aIdx.ToString();
        cur.labelMode = allVisualizationModes[currentVisModeIdx];
        cur.responded = true;
    }

    public void UpdateResponse(int idx)
    {
        aIdx = idx;
        for (int i = 0; i < answerPanels.Count; i++)
        {
            if (i == aIdx)
            {
                answerPanels[i].color = Color.green;
            }
            else
            {
                answerPanels[i].color = unselected;
            }

        }
    }

    public bool Responded()
    {
        return sceneQuestions[currentQuestionIdx].responded;
    }

    public string SceneName()
    {
        return sceneQuestions[qMap[qIdx]].sceneName;
    }
    
    public void ShowQuestion()
    {
        detectionSW.Stop();
        responseSW.Reset();
        responseSW.Start();
        questionUI.SetActive(true);
    }

    // false - task 1 (polygons), true - task 2 (optimal label)
    public bool Mode()
    {
        return sceneQuestions[currentQuestionIdx].answers.Count == 0;
    }

    public void ShowPanels()
    {
        answerContainer.SetActive(true);
    }

    public void HidePanels()
    {
        answerContainer.SetActive(false);
    }

    public void HideQuestion()
    {
        questionUI.SetActive(false);
    }

    public void UpdateMask(SceneQuestion currentQuestion)
    {
        Cubemap newLabelCubemap = Resources.Load("Materials/" + currentQuestion.mask+"3D", typeof(Cubemap)) as Cubemap;
        labelSphere.GetComponent<Renderer>().material.SetTexture("_CubeMap", newLabelCubemap);
        // UnityEngine.Debug.Log(currentQuestion.mask);

        currentVisModeIdx += 1;
        if (currentVisModeIdx >= 8){
            currentVisModeIdx = 0;
        }

        UpdateDisplayMode(allVisualizationModes[currentVisModeIdx]);
        

        // Load new 2D masks for average background value calculation
        CurrentScript = CamerasContainer.GetComponent<RenderStereoBackgroundforAreaLabel>(); 
        CurrentScript.LabelMask = Resources.Load<Texture2D>("Materials/" + currentQuestion.mask+"2D");
        CurrentScript.BackgroundMask = Resources.Load<Texture2D>("Materials/" + currentQuestion.mask+"2D_BG");

        // Recalculate average background values 
        CurrentScript.backgroundOrLableChanged = true;
    }


    public void UpdateBackground(SceneQuestion currentQuestion)
    {
        // Load new Equirectangular background 
        CurrentScript = CamerasContainer.GetComponent<RenderStereoBackgroundforAreaLabel>();
        CurrentScript.EquirectangularBackground = Resources.Load<Texture2D>("Materials/" + currentQuestion.sceneName+"2D");

        // Recalculate average background values 
        CurrentScript.backgroundOrLableChanged = true;
    }

    // Updates active question based on current state of qIdx
    public void UpdateQuestion()
    {
        
        SceneQuestion curQ = sceneQuestions[currentQuestionIdx];
        questionText.SetText(curQ.question);

        UpdateMask(curQ);
        UpdateBackground(curQ);

        for (int i = 0; i < answerImgs.Count; i++)
        {
            if (i < curQ.answers.Count)
            {
                Sprite sprite = Resources.Load<Sprite>(curQ.answers[i]);
                // UnityEngine.Debug.Log(curQ.answers[i]);
                answerImgs[i].sprite = sprite;
            }
        }

        if (Mode())
        {
            ShowQuestion();
            HidePanels();
        }
    }

    public void LoadNext(bool bypass = false)
    {
        if (bypass || Responded())
        {   
            ShowPanels();

            // qIdx = (qIdx + 1) % qMap.Count;
            currentQuestionIdx = (currentQuestionIdx+1) % qMap.Count; // maybe this line needs to be fixed
            // string nextScn = sceneQuestions[qMap[qIdx]].sceneName;
            string nextScn = sceneQuestions[currentQuestionIdx].sceneName;
            SceneManager.LoadScene(sceneToIdx[nextScn]);
            HideQuestion();
            UpdateQuestion();
            detectionSW.Reset();
            detectionSW.Start();
        }
    }

    int mod(int x, int m) {
        return (x%m + m)%m;
    }

    public void LoadPrev(bool bypass = false)
    {
        if (bypass || Responded())
        {   
            ShowPanels();

            qIdx = mod(qIdx - 1, qMap.Count);
            UnityEngine.Debug.Log(-1 );
            UnityEngine.Debug.Log(qMap[qIdx]);
            string nextScn = sceneQuestions[qMap[qIdx]].sceneName;
            SceneManager.LoadScene(sceneToIdx[nextScn]);
            HideQuestion();
            UpdateQuestion();
            detectionSW.Reset();
            detectionSW.Start();
        }


    }


    void UpdateDisplayMode(int currentLabelDisplayMode)
    {
        if (currentLabelDisplayMode == 0){ // Baseline + 40% opacity
            backgroundAndLabelSphereMaterial.SetInt("_ColorMethod", 5);
            backgroundAndLabelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
            // modeID.text = "Mode ID: 0";
        }
        else if (currentLabelDisplayMode == 1){ // Baseline + 70% opacity
            backgroundAndLabelSphereMaterial.SetInt("_ColorMethod", 5);
            backgroundAndLabelSphereMaterial.SetFloat("_OpacityLevel", 0.7f);
            // modeID.text = "Mode ID: 1";
        }
        else if (currentLabelDisplayMode == 2){ // CIELAB + Per-pixel + 40% opacity
            backgroundAndLabelSphereMaterial.SetInt("_ColorMethod", 4);
            backgroundAndLabelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
            backgroundAndLabelSphereMaterial.SetInt("_GranularityMethod", 0);
            // modeID.text = "Mode ID: 2";
        }
        else if (currentLabelDisplayMode == 3){ // CIELAB + Per-area + 40% opacity
            backgroundAndLabelSphereMaterial.SetInt("_ColorMethod", 4);
            backgroundAndLabelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
            backgroundAndLabelSphereMaterial.SetInt("_GranularityMethod", 1);
            // modeID.text = "Mode ID: 3";
        }
        else if (currentLabelDisplayMode == 4){ // CIELAB + Per-background + 30% opacity
            backgroundAndLabelSphereMaterial.SetInt("_ColorMethod", 4);
            backgroundAndLabelSphereMaterial.SetFloat("_OpacityLevel", 0.4f);
            backgroundAndLabelSphereMaterial.SetInt("_GranularityMethod", 2);
            // modeID.text = "Mode ID: 4";
        }
        else if (currentLabelDisplayMode == 5){ // CIELAB + Per-pixel + 70% opacity
            backgroundAndLabelSphereMaterial.SetInt("_ColorMethod", 4);
            backgroundAndLabelSphereMaterial.SetFloat("_OpacityLevel", 0.7f);
            backgroundAndLabelSphereMaterial.SetInt("_GranularityMethod", 0);
            // modeID.text = "Mode ID: 5";
        }
        else if (currentLabelDisplayMode == 6){ // CIELAB + Per-area + 70% opacity
            backgroundAndLabelSphereMaterial.SetInt("_ColorMethod", 4);
            backgroundAndLabelSphereMaterial.SetFloat("_OpacityLevel", 0.7f);
            backgroundAndLabelSphereMaterial.SetInt("_GranularityMethod", 1);
            // modeID.text = "Mode ID: 6";
        }
        else if (currentLabelDisplayMode == 7){ // CIELAB + Per-background + 70% opacity
            backgroundAndLabelSphereMaterial.SetInt("_ColorMethod", 4);
            backgroundAndLabelSphereMaterial.SetFloat("_OpacityLevel", 0.7f);
            backgroundAndLabelSphereMaterial.SetInt("_GranularityMethod", 2);
            // modeID.text = "Mode ID: 7";
        }
        else if (currentLabelDisplayMode == 8){ // No label
            backgroundAndLabelSphereMaterial.SetInt("_ColorMethod", 6);
            backgroundAndLabelSphereMaterial.SetFloat("_OpacityLevel", 0.0f);
            // modeID.text = "Mode ID: No label";
        }
    }

    //Update is called once per frame
    void Update()
    {
        //bool triggerRight = OVRInput.Get(OVRInput.Button.Two);

        float rIndexTrigger = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);
        float lIndexTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);
        float lHandTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger);
        

        Vector2 stickInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);

        if (stickInput.magnitude > 0.8f)
        {
            UnityEngine.Debug.Log("joystick input detected");
            if (stickInput.x < 0 && stickInput.y >= 0)
            {
                UpdateResponse(0);
            }
            else if (stickInput.x >= 0 && stickInput.y >= 0)
            {
                UpdateResponse(1);
            }
            else if (stickInput.x < 0 && stickInput.y < 0)
            {
                UpdateResponse(2);
            }
            else if (stickInput.x >= 0 && stickInput.y < 0)
            {
                UpdateResponse(3);
            }
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            WriteResponses();
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            ManualResponse();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            LoadPrev(true);
            UnityEngine.Debug.Log($"Question manually updated to question {qIdx} ({SceneName()}) from question.json");
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            LoadNext(true);
            UnityEngine.Debug.Log($"Question manually updated to question {qIdx} ({SceneName()}) from question.json");
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            ShowQuestion();
            if (Mode())
            {
                HidePanels();
            }
        }

        if (rIndexTrigger == 0) {
            rIndexTriggerHeld = false;
        }

        if (lIndexTrigger == 0) {
            lIndexTriggerHeld = false;
        }

        if (lHandTrigger == 0) {
            lHandTriggerHeld = false;
        }

        if (((rIndexTrigger > 0) && !rIndexTriggerHeld) || Input.GetKeyDown(KeyCode.N)) 
        {   
            rIndexTriggerHeld = true;
            if (!Responded())
            {
                if (!questionUI.activeSelf)
                {
                    ShowQuestion();
                }
                else
                {
                    RecordResponse();
                    HideQuestion();
                }
            }
            LoadNext();
        }
   
    }

    void OnDestroy()
    {
        WriteResponses();
    }
}
