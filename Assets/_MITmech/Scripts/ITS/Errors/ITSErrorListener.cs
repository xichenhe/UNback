using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MITmech.ITS
{

public class ITSErrorListener : MonoBehaviour
{
    public UnityEvent errorEventTriggered;
    public List<ErrorObject> errorObjects;

    private void Start()
    {
        foreach( ErrorObject errorObject in errorObjects)
        {
            errorObject.TriggerErrorEvent.AddListener(ErrorEventTriggered);
        }
    }

    public void ErrorEventTriggered()
    {
        print("ErrorTriggered");
        errorEventTriggered.Invoke();
    }
}

}
