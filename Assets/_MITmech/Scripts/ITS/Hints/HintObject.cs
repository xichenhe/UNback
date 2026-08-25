using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MITmech.ITS
{
[CreateAssetMenu(fileName = "Hint", menuName = "ScriptableObjects/ITS/Hints/Hint", order = 1)]

public class HintObject : ScriptableObject
{
    public Severity severity = Severity.Normal;
    public string hintText;
    public UnityEvent<string,HintObject> ActivateHintEvent;
    public UnityEvent DisableHintEvent;

    public HintObject GiveHint()
    {
        if (ActivateHintEvent != null)
        {
            ActivateHintEvent.Invoke(hintText,this);
        }
        return this;
    }
    public void DisableHint()
    {
        DisableHintEvent.Invoke();
    }
/*    public void Reset()
    {
        ActivateHintEvent.RemoveAllListeners();
        DisableHintEvent.RemoveAllListeners();
    }*/
}

}
