using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OVR;
using TMPro;

public class SceneCycler : MonoBehaviour
{   
    public bool randomize = true;
    
    private int sceneIdx = 0;
    private List<int> sceneMap = new List<int>();
    private List<int> allScenes = new List<int>();
    private List<int> activeScenes = new List<int>();

    private int categoryIdx = 0;
    private List<string> categories;
    private List<string> activeCategories = new List<string>();

    private Dictionary<string, List<int>> categoriesDict = new Dictionary<string, List<int>>();
    private Dictionary<string, int> sceneToIdx = new Dictionary<string, int>();

    private Dictionary<int, List<SceneQuestion>> sceneQuestions = new Dictionary<int, List<SceneQuestion>>();
    int qIdx = 0;

    public TMP_Text questionText;
    public List<TMP_Text> answerTexts;

    private TMP_Text activeCatsText;
    private TMP_Text curCatText;
    private float vanishTime = 5.0f;
    private GameObject sceneContainer;
    private bool rIndexTriggerHeld = false;
    private bool lIndexTriggerHeld = false;
    private bool lHandTriggerHeld = false;

    private OVRInput.Controller leftController = OVRInput.Controller.LTouch;
    private OVRInput.Controller rightController = OVRInput.Controller.RTouch;

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
        for (int i = 1; i < n; i++)
        {
            list.Add(i);
        }

        ShuffleList(list);
        list.Insert(0,0);
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
        string filePath = Path.Combine(Application.dataPath, "UserStudy/questions.json");
        string json = File.ReadAllText(filePath);

        SceneQuestionsList questionsList = JsonUtility.FromJson<SceneQuestionsList>(json);
        
        foreach (SceneQuestion question in questionsList.sceneQuestions)
        {
            int sceneQIdx = sceneToIdx[question.sceneName];

            if (!sceneQuestions.ContainsKey(sceneQIdx))
            {
                sceneQuestions[sceneQIdx] = new List<SceneQuestion>();
            }
            sceneQuestions[sceneQIdx].Add(question);
        }
    }

    void Start()
    {

        List<int> randIndices = CreateAndShuffleList(SceneManager.sceneCountInBuildSettings);

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++) 
        {
            sceneMap.Add(i);
        }

        if (randomize)
        {
            sceneMap = CreateAndShuffleList(SceneManager.sceneCountInBuildSettings);
        }

        for (int i = 1; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            
            string scenePath = SceneUtility.GetScenePathByBuildIndex(sceneMap[i]);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            List<string> tokens = ParseName(sceneName);
            sceneToIdx.Add(tokens[0], i);
            allScenes.Add(i);
            for (int j = 1; j < tokens.Count; j++)
            {
                if (!categoriesDict.ContainsKey(tokens[j]))
                {
                    categoriesDict[tokens[j]] = new List<int>();
                }
               categoriesDict[tokens[j]].Add(i);
            }
        }

        ParseQuestions();
        
        categories = new List<string>(categoriesDict.Keys);
        activeScenes.AddRange(allScenes);

        sceneContainer = GameObject.Find("SceneContainer");
        sceneContainer.SetActive(true);
        Invoke("HideSceneContainer", vanishTime);

        activeCatsText = GameObject.Find("ActiveCategories").GetComponent<TextMeshPro>();
        activeCatsText.SetText("Active: ALL" + " (" + activeScenes.Count.ToString() + ")");

        curCatText = GameObject.Find("CurrentCategory").GetComponent<TextMeshPro>();
        curCatText.SetText("Cat: " + categories[categoryIdx]);
        //curCatText.color = Color.red;


        SceneManager.LoadScene(sceneMap[activeScenes[sceneIdx]]);
        qIdx = 0;
        UpdateQuestion();
    }

    public void HideSceneContainer()
    {
        sceneContainer.SetActive(false);
    }

    public async Task WriteResponses()
    {
        SceneQuestionsList responses = new SceneQuestionsList();

        foreach (KeyValuePair<int, List<SceneQuestion>> entry in sceneQuestions)
        {
            foreach (SceneQuestion question in entry.Value)
            {
                responses.sceneQuestions.Add(question);
            }
        }

        string json = JsonUtility.ToJson(responses, true);
        string dateString = DateTime.Now.ToString("yyyyMMdd_HHmm");
        string outpath = Path.Combine(Application.persistentDataPath, $"UserResponse_{dateString}.json");
        Debug.Log(outpath);
        using (StreamWriter writer = new StreamWriter(outpath, false))
        {
            await writer.WriteAsync(json);
        }
    }

    public void RecordResponse(int idx)
    {
        sceneQuestions[activeScenes[sceneIdx]][qIdx].response = sceneQuestions[activeScenes[sceneIdx]][qIdx].answers[idx];
        Debug.Log("Recorded response " + sceneQuestions[activeScenes[sceneIdx]][qIdx].answers[idx]);
    }

    public void UpdateQuestion()
    {
        SceneQuestion curQ = sceneQuestions[activeScenes[sceneIdx]][qIdx];
        questionText.SetText(curQ.question);

        for (int i = 0; i < answerTexts.Count; i++)
        {
            if (i < curQ.answers.Count)
            {
                answerTexts[i].SetText(curQ.answers[i]);
            }
            else
            {
                answerTexts[i].SetText("");
            }
        }
    }

    public void LoadNext()
    {
        sceneIdx = (sceneIdx + 1) % activeScenes.Count;
        SceneManager.LoadScene(sceneMap[activeScenes[sceneIdx]]);

        qIdx = 0;
        UpdateQuestion();
    }

    //Update is called once per frame
    void Update()
    {
        //bool triggerRight = OVRInput.Get(OVRInput.Button.Two);

        float rIndexTrigger = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);
        float lIndexTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);
        float lHandTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger);
        

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            RecordResponse(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            RecordResponse(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            RecordResponse(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            RecordResponse(3);
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
            CancelInvoke();
            sceneContainer.SetActive(true); 
            Invoke("HideSceneContainer", vanishTime);
            if (activeScenes.Count > 0)
            {
                LoadNext();
            }
        }
        else if ((lIndexTrigger > 0) && !lIndexTriggerHeld)
        {
            lIndexTriggerHeld = true;
            CancelInvoke();
            sceneContainer.SetActive(true); 
            Invoke("HideSceneContainer", vanishTime);
            string curCategory = categories[categoryIdx];
            if (activeCategories.Contains(curCategory))
            {
                activeCategories.Remove(curCategory);
                // curCatText.color = Color.red;
            }
            else
            {
                activeCategories.Add(curCategory);
                // curCatText.color = Color.green;
            }

            activeScenes.Clear();


            if (activeCategories.Count > 0) 
            {
                for (int i = 0; i < allScenes.Count; i++)
                {
                    bool inIntersection = true;

                    for (int j = 0; j < activeCategories.Count; j++)
                    {
                        if (!categoriesDict[activeCategories[j]].Contains(allScenes[i])) 
                        {
                            inIntersection = false;
                            break;
                        }
                    }

                    if (inIntersection)
                    {
                        activeScenes.Add(allScenes[i]);
                    }
                }

                string updatedCatsText = "Active: ";
                for (int i = 0; i < activeCategories.Count; i++)
                {
                    if (i > 0)
                    {
                        updatedCatsText += ", " + activeCategories[i];
                    }
                    else
                    {
                        updatedCatsText += activeCategories[i];
                    }
                }

                activeCatsText.SetText(updatedCatsText + " (" + activeScenes.Count.ToString() + ")");
            }
            else
            {
                activeScenes.AddRange(allScenes);
                activeCatsText.SetText("Active: ALL" + " (" + activeScenes.Count.ToString() + ")");
            }

            sceneIdx = 0;
            if (activeScenes.Count > 0)
            {
                SceneManager.LoadScene(activeScenes[sceneIdx]);
            }
        }
        else if ((lHandTrigger > 0) && !lHandTriggerHeld)
        {
            lHandTriggerHeld = true;
            CancelInvoke();
            sceneContainer.SetActive(true); 
            Invoke("HideSceneContainer", vanishTime);
            categoryIdx = (categoryIdx + 1) % categories.Count;
            curCatText.SetText("Cat: " + categories[categoryIdx]);
            if (activeCategories.Contains(categories[categoryIdx]))
            {
                // curCatText.color = Color.green;
            }
            else
            {
                // curCatText.color = Color.red;
            }
        }
    }
}
