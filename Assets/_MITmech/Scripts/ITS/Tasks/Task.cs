using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MITmech.ITS
{

[CreateAssetMenu(fileName = "Task", menuName = "ScriptableObjects/ITS/Task", order = 1)]

public class Task : ScriptableObject
{
    public Action associatedAction;
    public bool completed;
    public List<SubTask> subTasksCompleted = new List<SubTask>();   
    public List<SubTask> subTasks = new List<SubTask>();
    [SerializeField]
    public List<SubTask> optimalPaths = new List<SubTask>();
    // Start is called before the first frame update

    public void Initialize()
    {
        subTasksCompleted.Clear();
        subTasks = optimalPaths.ToList();

        if (subTasks.Count > 0)
        {
            completed = false;
        }

        int subtaskIndex = 0;

        foreach(SubTask subTask in subTasks)
        {
            subTask.Initialize(subtaskIndex);
            subTask.CompletedEvent.AddListener(AddCompletedTask);
            subtaskIndex++;
        }
    }
    public void Reset()
    {
        foreach(SubTask subTask in subTasks)
        {
            subTask.ResetObject();
        }
    }
    public void AddCompletedTask(SubTask task)
    {
        if (!subTasksCompleted.Contains(task))
            subTasksCompleted.Add(task);

        if (subTasks.Contains(task))
            subTasks.Remove(task);

        if (subTasks.Count == 0)
        {
            completed = true;
            associatedAction.AddTaskCompleted(this);
        }
    }
    public void RevertSubtask(SubTask subTask)
    {
        if (subTasksCompleted.Contains(subTask))
        {
            subTasksCompleted.Remove(subTask);
            subTasks.Add(subTask);

            subTasks.Sort((x, y) => x.subTaskIndex.CompareTo(y.subTaskIndex));
        }
/*        else
            Debug.Log("task was never completed");*/
    }
    public SubTask GetCurrentSubTask()
    {
        if(subTasks.Count == 0)
            return null;

        return subTasks[0];
    }
}

}
