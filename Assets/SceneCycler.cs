using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OVR;
using TMPro;

public class SceneCycler : MonoBehaviour
{   
    private Dictionary<string, List<int>> categoriesDict = new Dictionary<string, List<int>>();
    private Dictionary<int, string> sceneNames = new Dictionary<int, string>();
    private List<string> categories;
    private int categoryIdx = 0;
    private List<string> activeCategories = new List<string>();
    private List<int> allScenes = new List<int>();
    private List<int> activeScenes = new List<int>();
    private int sceneIdx = 0;
    private TMP_Text activeCatsText;
    private TMP_Text curCatText;
    private float vanishTime = 3.0f;
    private GameObject sceneContainer;

    private OVRInput.Controller leftController = OVRInput.Controller.LTouch;
    private OVRInput.Controller rightController = OVRInput.Controller.RTouch;

    private List<string> ParseName(string sceneName)
    {
        List<string> tokens = new List<string>();
        string[] splitTokens = sceneName.Split('_');
        tokens.AddRange(splitTokens);

        return tokens;
    }
    void Start()
    {
        
        for (int i = 1; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            Debug.Log(sceneName);
            List<string> tokens = ParseName(sceneName);
            sceneNames.Add(i, tokens[0]);
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

        SceneManager.LoadScene(activeScenes[sceneIdx]);
    }

    public void HideSceneContainer()
    {
        sceneContainer.SetActive(false);
    }

    public void LoadNext()
    {
        sceneIdx = (sceneIdx + 1) % activeScenes.Count;
        SceneManager.LoadScene(activeScenes[sceneIdx]);
    }

    //Update is called once per frame
    void Update()
    {
        //bool triggerRight = OVRInput.Get(OVRInput.Button.Two);

        if (OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger)) 
        {   
            CancelInvoke();
            sceneContainer.SetActive(true); 
            Invoke("HideSceneContainer", vanishTime);
            if (activeScenes.Count > 0)
            {
                LoadNext();
            }
        }
        else if (OVRInput.Get(OVRInput.Button.Three))
        {
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
        else if (OVRInput.Get(OVRInput.Button.Four))
        {
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
