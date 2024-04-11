using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SceneComparison
{
    public string sceneName;
    public string mask;
    public List<CompResponse> responses = new List<CompResponse>();
}

[System.Serializable]
public class CompResponse
{
    public List<int> pair = new List<int>();
    public string reason = "";
}

[System.Serializable]
public class SceneComparisonList
{
    public List<SceneComparison> sceneComparisons = new List<SceneComparison>();
}