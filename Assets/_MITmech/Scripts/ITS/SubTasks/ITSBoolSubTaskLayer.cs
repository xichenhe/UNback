using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MITmech.ITS
{

public class ITSBoolSubTaskLayer : ITSSubTaskLayer
{
    public bool trueBool = true;
    public void SetCompletetion(bool completed)
    {
        if(completed == trueBool)
            subTask.CompleteSubtask();
        else
            subTask.RevertSubTask();
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

}
