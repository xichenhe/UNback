using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

namespace MITmech.ITS
{

public enum FeedbackLevel { Level1,Level2,Level3,Level4}
public class InterfaceModel : MonoBehaviour
{
    public FeedbackLevel feedbackLevel;
    public static InterfaceModel Instance { get; private set; }
    public Action currentOptimalAction;
    public List<Action> performedActionList = new List<Action>();
    public VideoPlayer videoPlayer;
    public List<Action> actionList = new List<Action>();
    public List<Action> optimalActionList = new List<Action>();

    public UnityEvent NonOptimalPathTaken;
    // Start is called before the first frame update
    void Start()
    {
        Instance = this;

        actionList = optimalActionList.ToList();

        foreach (Action action in actionList)
        {
            action.Initialize();
        }

        currentOptimalAction = actionList[0];
    }

    // Update is called once per frame
    void Update()
    {

        if (currentOptimalAction.tasksList.Count == 0)
        {
            actionList.Remove(currentOptimalAction);

            if (currentOptimalAction == null)
            ITSManager.Instance.errorAgent.Completed();

            if (actionList.Count > 0)
            {
                currentOptimalAction = actionList[0];
            }
        }


        if (currentOptimalAction.GetOptimalTask().GetCurrentSubTask() != null && !videoPlayer.isPlaying)
            currentOptimalAction.GetOptimalTask().GetCurrentSubTask().UpdateHintTime(Time.deltaTime);

    }

    public void AddCompletedTask(UserCompletedTaskAction newAction)
    {
        //actionList.Add(newAction);

        //if (actionList[actionList.Count-1] != optimalActionList[actionList.Count - 1])
            NonOptimalPathTaken?.Invoke();


    }

    public void CompleteAction(Action action)
    {
        if (!actionList.Contains(action))
        actionList.Remove(action);
    }
}

}
