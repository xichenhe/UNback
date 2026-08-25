using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace MITmech.ITS
{

[CreateAssetMenu(fileName = "SubTask", menuName = "ScriptableObjects/ITS/SubTask", order = 1)]

public class SubTask : ScriptableObject
{
    public UnityEvent<SubTask> CompletedEvent;
    public UnityEvent<SubTask> RevertedEvent;

    public bool completed;
    public int subTaskIndex = 0;
    public Task associatedTask;
    public HintContainer hintContainer;
    public float nextHintInterval;
    public float timeOnTask;

    public List<SubTask> dependantSubTasks;
    public List<SubTask> optimizedDependantSubTasks = new List<SubTask>();
    public List<SubTask> completedDependantSubTasks = new List<SubTask>();


    public void Initialize(int index)
    {
        completedDependantSubTasks.Clear();
        dependantSubTasks.Clear();
        dependantSubTasks = optimizedDependantSubTasks.ToList();

        int dependantIndex = 0;
        foreach (SubTask dependantSubTask in dependantSubTasks)
        {
            dependantSubTask.Initialize(dependantIndex);
            dependantSubTask.CompletedEvent.AddListener(DependantCompleted);
            dependantSubTask.RevertedEvent.AddListener(DependantReverted);
            dependantIndex++;
        }

        completed = false;

        subTaskIndex = index;

        timeOnTask = 0;
        if (hintContainer != null)
        {
            hintContainer.Initialize(this);

            nextHintInterval = hintContainer.GetCurrentTimedHint().waitTime;
        }
        else
        {
            Debug.Log(name + " missing hint container");
        }



    }

    public void ReclaimHintTime(float time)
    {
        timeOnTask -= time;
        nextHintInterval -= time;
    }

    public void SetCompletetion(bool completed)
    {
        if (completed)
            CompleteSubtask();
        else
        {
            RevertSubTask();
        }
    }
    public void UpdateHintTime(float deltaTime)
    {
        if (dependantSubTasks.Count > 0)
        {
            dependantSubTasks[0].UpdateHintTime(deltaTime);
            return;
        }

        if (hintContainer.hintIndex > hintContainer.hints.Count - 1)
        {
            return;
        }
        timeOnTask += deltaTime;

        if (timeOnTask > nextHintInterval)
        {
            Debug.Log("hint given");
            GiveHint();

            nextHintInterval += hintContainer.GetCurrentTimedHint().waitTime;
        }
    }

    public void GiveHint()
    {
        hintContainer.GiveHint();
    }
    public void CompleteSubtask()
    {
        if (associatedTask.completed)
            return;

        completed = true;

        //this is now handled by the event
        //associatedTask.AddCompletedTask(this);

        hintContainer.SetComplete();

        CompletedEvent.Invoke(this);
    }
    public void RevertSubTask()
    {
        completed = false;

        associatedTask.RevertSubtask(this);

        RevertedEvent.Invoke(this);

        if (InterfaceModel.Instance.currentOptimalAction.GetOptimalTask().GetCurrentSubTask().QuerySubtaskChain(this))
            hintContainer.SubtaskUndid();

        //Debug.Log(name);

    }
    public bool QuerySubtaskChain(SubTask subTask)
    {
        bool found = false;

        if (this == subTask)
        {
            return true;

        }

        foreach (SubTask dependantSubTask in dependantSubTasks)
        {
            found = dependantSubTask.QuerySubtaskChain(subTask);
            if (found == true)
            {
                return found;
            }
        }

        return found;
    }
    public void CheckIfTaskWasCompleted()
    {
        if (associatedTask.subTasksCompleted.Contains(this))
        {
            return;
        }
        else
            ResetSubTasks();
    }

    public void DependantCompleted(SubTask subTask)
    {
        Debug.Log(subTask.name);
        if (dependantSubTasks.Contains(subTask))
            dependantSubTasks.Remove(subTask);
        if (!completedDependantSubTasks.Contains(subTask))
            completedDependantSubTasks.Add(subTask);
    }

    public void DependantReverted(SubTask subTask)
    {
        completedDependantSubTasks.Remove(subTask);

        if (!dependantSubTasks.Contains(subTask))
            dependantSubTasks.Add(subTask);
        dependantSubTasks.OrderBy(x => x.subTaskIndex);
    }

    public void ResetSubTasks()
    {
        ResetObject();

        Debug.Log("found");
        foreach (SubTask subTask in dependantSubTasks)
        {
            if (!associatedTask.subTasks.Contains(subTask))
            {
                associatedTask.subTasks.Insert(associatedTask.subTasks.IndexOf(this), subTask);
                subTask.ResetObject();
                associatedTask.subTasksCompleted.Remove(subTask);
            }
        }
    }
    public void ResetObject()
    {
        completed = false;
        timeOnTask = 0;
        hintContainer.Reset();
        nextHintInterval = hintContainer.GetCurrentTimedHint().waitTime;

    }
}

}
