using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEngine;
using TMPro;
using System;

public class SceneTimer : MonoBehaviour
{
    [SerializeField] private static bool appStarted = false;
    [SerializeField] private static Dictionary<string, float> sceneNamesAndSceneChangeAccumulatedTime;
    [SerializeField] private static List<float> sceneTimerValues;
    [SerializeField] private static float timeAtNewSceneStart;
    public float timerTextFontSize = 0.03f;

    private TextMeshProUGUI timerText;

    private void AddNewSceneInfo(string sceneName, float newTimeStamp){
        sceneNamesAndSceneChangeAccumulatedTime.Add(SceneManager.GetActiveScene().name, newTimeStamp);
    }

    private string ToMinutesSecondsString(float timeInput){
        TimeSpan timeInputSpan = TimeSpan.FromSeconds(timeInput);
        return string.Format("{0:00}:{1:00}", (int) timeInputSpan.TotalMinutes, (int) timeInputSpan.Seconds);
    }

    void Start(){
        timerText = GetComponent<TextMeshProUGUI>();
        timeAtNewSceneStart = Time.time;

        if(!appStarted){
            timerText.fontSize = timerTextFontSize;
            timerText.fontStyle = FontStyles.Bold;
            timerText.verticalAlignment = VerticalAlignmentOptions.Middle;
            timerText.horizontalAlignment = HorizontalAlignmentOptions.Left;
            timerText.color = Color.black;
            sceneNamesAndSceneChangeAccumulatedTime = new Dictionary<string, float>();
            sceneTimerValues = new List<float>();
            appStarted = true;
        }
        timerText.text = "Total Time: " + ToMinutesSecondsString(Time.time) + "\n";
        
        if(!(sceneNamesAndSceneChangeAccumulatedTime.ContainsKey(SceneManager.GetActiveScene().name))){
            AddNewSceneInfo(SceneManager.GetActiveScene().name, 0f);
            sceneTimerValues.Add(0f);
        }

        List<string> sceneNames = new List<string>(sceneNamesAndSceneChangeAccumulatedTime.Keys);

        for(int i = 0;i < sceneNames.Count;i++){
            sceneNamesAndSceneChangeAccumulatedTime[sceneNames[i]] = sceneTimerValues[i];

            timerText.text += "Time in " + sceneNames[i].ToString() + ": " + ToMinutesSecondsString(sceneTimerValues[i]);

            if(i < (sceneNamesAndSceneChangeAccumulatedTime.Count - 1)){
                timerText.text += "\n";
            }
        }

        //Debug.Log(timerText.text);
    }

    void OnApplicationFocus(bool focus){
        if(!focus){
            List<string> sceneNames = new List<string>(sceneNamesAndSceneChangeAccumulatedTime.Keys);
            timerText.text = "Total Time: " + ToMinutesSecondsString(Time.time)  + "\n";
            for(int i = 0;i < sceneNames.Count;i++){
                timerText.text += "Time in " + sceneNames[i].ToString() + ": " + ToMinutesSecondsString(sceneTimerValues[i]) + "\n";

                /* if(i < (sceneNamesAndSceneChangeAccumulatedTime.Count - 1)){
                    timerText.text += "\n";
                } */
            }
            Debug.Log(timerText.text);
        }
    }

    void Update(){

        if(!(sceneNamesAndSceneChangeAccumulatedTime.ContainsKey(SceneManager.GetActiveScene().name))){
            AddNewSceneInfo(SceneManager.GetActiveScene().name, 0f);
            sceneTimerValues.Add(0f);
        }

        int i = 0;
        timerText.text = "Total Time: " + ToMinutesSecondsString(Time.time) + "\n";
        foreach(var sceneInfo in sceneNamesAndSceneChangeAccumulatedTime){
            if(sceneInfo.Key == SceneManager.GetActiveScene().name){
                sceneTimerValues[i] = sceneInfo.Value + (Time.time - timeAtNewSceneStart);
            }
            
            timerText.text += "Time in " + sceneInfo.Key.ToString() + ": " + ToMinutesSecondsString(sceneTimerValues[i]);

            if(i < (sceneNamesAndSceneChangeAccumulatedTime.Count - 1)){
                timerText.text += "\n";
            }
            i++;
        }
        
    }
}
