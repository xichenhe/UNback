using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MITmech.ITS
{

public class ITSSubTaskLayer : MonoBehaviour
{
    public SubTask subTask;

    public void CompleteSubTask()
    {
        subTask.CompleteSubtask();
    }
    public void CheckTaskForDependancies()
    {
        subTask.CheckIfTaskWasCompleted();
    }
}

}
