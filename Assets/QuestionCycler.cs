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

    // private GameObject sceneContainer;
    private bool rIndexTriggerHeld = false;
    private bool lIndexTriggerHeld = false;
    private bool lHandTriggerHeld = false;


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

        ParseQuestions();

        if (randomize)
        {
            qMap = CreateAndShuffleList(qMap.Count);
        }

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
        string outpath = Path.Combine(Application.persistentDataPath, $"UserResponse_{dateString}.json");
        UnityEngine.Debug.Log(outpath);
        using (StreamWriter writer = new StreamWriter(outpath, false))
        {
            await writer.WriteAsync(json);
        }
    }

    public void RecordResponse()
    {
        SceneQuestion cur = sceneQuestions[qMap[qIdx]];
        if (!cur.responded)
        {

            UserTestingMovePlayer instance = FindObjectOfType<UserTestingMovePlayer>();
            responseSW.Stop();
            long detectionTime = detectionSW.ElapsedMilliseconds;
            long responseTime = responseSW.ElapsedMilliseconds;
            cur.response = aIdx.ToString();
            cur.labelMode = instance.currentLabelDisplayMode;
            cur.detectionTime = detectionTime;
            cur.responseTime = responseTime;
            cur.responded = true;
            UnityEngine.Debug.Log("Recorded response " + cur.answers[aIdx]);
            UnityEngine.Debug.Log(responseTime);
        }
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
        return sceneQuestions[qMap[qIdx]].responded;
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
        return sceneQuestions[qMap[qIdx]].answers.Count == 0;
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

    public void UpdateMask(string name)
    {
        Cubemap newLabelCubemap = Resources.Load("Materials/" + name, typeof(Cubemap)) as Cubemap;
        labelSphere.GetComponent<Renderer>().material.SetTexture("_CubeMap", newLabelCubemap);
    }

    // Updates active question based on current state of qIdx
    public void UpdateQuestion()
    {
        
        SceneQuestion curQ = sceneQuestions[qMap[qIdx]];
        questionText.SetText(curQ.question);

        UpdateMask(curQ.mask);

        for (int i = 0; i < answerImgs.Count; i++)
        {
            if (i < curQ.answers.Count)
            {
                Sprite sprite = Resources.Load<Sprite>(curQ.answers[i]);
                UnityEngine.Debug.Log(curQ.answers[i]);
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

            qIdx = (qIdx + 1) % qMap.Count;
            string nextScn = sceneQuestions[qMap[qIdx]].sceneName;
            SceneManager.LoadScene(sceneToIdx[nextScn]);
            HideQuestion();
            UpdateQuestion();
            detectionSW.Reset();
            detectionSW.Start();
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

        if(OVRInput.GetUp(OVRInput.Button.SecondaryThumbstick))
        {
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
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            WriteResponses();
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
            // CancelInvoke();
            // sceneContainer.SetActive(true); 
            // Invoke("HideSceneContainer", vanishTime);
            LoadNext();
        }
   
    }
}
