using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SceneQuestion
{
    public string sceneName;
    public string question;
    public List<string> answers;
    public string response = "";
}

[System.Serializable]
public class SceneQuestionsList
{
    public List<SceneQuestion> sceneQuestions = new List<SceneQuestion>();
}