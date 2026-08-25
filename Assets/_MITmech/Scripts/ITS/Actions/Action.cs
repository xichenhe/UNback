using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MITmech.ITS
{
[CreateAssetMenu(fileName = "Action", menuName = "ScriptableObjects/ITS/Action", order = 1)]
public class Action : ScriptableObject
{
    public List<Task> tasksCompleted = new List<Task>();
    public List<Task> tasksList = new List<Task>();
    public List<Task> optimalPath = new List<Task>();
    // Start is called before the first frame update

    public void Initialize()
    {
        tasksList = optimalPath.ToList();
        tasksCompleted.Clear();
        foreach (Task task in tasksList)
        {
            Debug.Log(task.name);
            task.Initialize();
        }
    }

    public void AddTaskCompleted(Task task)
    {
        if (!tasksCompleted.Contains(task)) 
        tasksCompleted.Add(task);

        if(tasksList.Contains(task))
        tasksList.Remove(task);

        if(tasksList.Count == 0)
            InterfaceModel.Instance.CompleteAction(this);
            
    }

    public Task GetOptimalTask() 
    {
        if(tasksList.Count > 0)
        return tasksList[0];
        else return null;
    }
}

}
