using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MITmech.ITS
{

public enum Severity { Normal,Warning,Error,Complete}
public class ErrorAgent : MonoBehaviour
{
    public delegate void ErrorAgentDelegate(string message,Severity severity);
    public event ErrorAgentDelegate ErrorAgentEvent;

    public HintObject lastHint;
    public HintObject currentHint;

    public UnityEvent<string,Severity> HintGiven;

    public UnityEvent FatalErrorTriggered;
    
    public UserAction wrongToolUsedInZone;

    public List<HintContainer> hintContainers;
    public List<ErrorObject> errorObjects;

    public HintObject completionHint;

    private void Start()
    {
        foreach (HintContainer container in hintContainers)
        {
            foreach(HintObject hintObject in container.hints)
            {
                hintObject.ActivateHintEvent.AddListener(GiveHint);
            }
            container.subtaskUndidHint?.ActivateHintEvent.AddListener(GiveHint);
            container.completionHint?.ActivateHintEvent.AddListener(GiveHint);
        }
        foreach(ErrorObject errorObject in errorObjects)
        {
            errorObject.TriggerErrorEvent.AddListener(TriggerError);
        }
    }
    private void OnDestroy()
    {
        foreach (HintContainer container in hintContainers)
        {
            foreach (HintObject hintObject in container.hints)
                hintObject.ActivateHintEvent.RemoveListener(GiveHint);

            container.subtaskUndidHint?.ActivateHintEvent.AddListener(GiveHint);
            container.completionHint?.ActivateHintEvent.AddListener(GiveHint);

        }

        foreach (ErrorObject errorObject in errorObjects)
        {
            errorObject.TriggerErrorEvent.RemoveListener(TriggerError);
        }
    }
    void GiveHint(string hintText,HintObject hintObject)
    {
        currentHint = hintObject;
        if (lastHint != null)
        {
            lastHint.DisableHint();

            if (currentHint?.GetType() != typeof(TimedHintObject))
            {
                if (lastHint?.GetType() == typeof(TimedHintObject))
                {
                    (lastHint as TimedHintObject).ReclaimTime();
                    print(currentHint.hintText + "\n" + lastHint.hintText);
                }
            }
        }
       
        HintGiven.Invoke(hintText,currentHint.severity);

        lastHint = currentHint;

    }

    public void Completed()
    {
        HintGiven.Invoke(completionHint.hintText, Severity.Normal);

    }
    public void TriggerError()
    {
        FatalErrorTriggered.Invoke();
    }
}

}
