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

    private Dictionary<int, SceneQuestion> sceneQuestions = new Dictionary<int, SceneQuestion>();
    int aIdx = 0;

    Stopwatch detectionSW = new Stopwatch();
    Stopwatch responseSW = new Stopwatch();

    // public GameObject questionUI;
    // public TMP_Text questionText;
    public List<Image> answerImgs;
    public List<Image> answerPanels;
    private Color unselected = new Color(1.0f, 215 / 255f, 215 / 255f);

    private TMP_Text activeCatsText;
    private TMP_Text curCatText;
    private float vanishTime = 5.0f;
    // private GameObject sceneContainer;
    private bool rIndexTriggerHeld = false;
    private bool lIndexTriggerHeld = false;
    private bool lHandTriggerHeld = false;

    private OVRInput.Controller leftController = OVRInput.Controller.LTouch;
    private OVRInput.Controller rightController = OVRInput.Controller.RTouch;

    int currentScene;


    void Start()
    {
        SceneManager.LoadScene(1);
        currentScene = 1;
    }

    //Update is called once per frame
    void Update()
    {
        //bool triggerRight = OVRInput.Get(OVRInput.Button.Two);

        float rIndexTrigger = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);
        float lIndexTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);
        float lHandTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger);
        

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
            if (currentScene == 1){
                SceneManager.LoadScene(2);
                currentScene = 2;
            }
            else{
                SceneManager.LoadScene(1);
            }
        }
      
    }
}
