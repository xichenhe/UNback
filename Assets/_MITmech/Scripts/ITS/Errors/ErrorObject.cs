using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MITmech.ITS
{
[CreateAssetMenu(fileName = "ErrorObject", menuName = "ScriptableObjects/ITS/Hints/ErrorObject", order = 1)]

public class ErrorObject : ScriptableObject
{

    public UnityEvent TriggerErrorEvent;

    public void TriggerError()
    {
        TriggerErrorEvent.Invoke();
    }
}

}
