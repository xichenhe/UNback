using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MITmech.ITS
{

[System.Serializable]
[CreateAssetMenu(fileName = "ITSEvent", menuName = "ScriptableObjects/ITS/ITSEvent", order = 1)]
public class UserAction:ScriptableObject
{
    public delegate void ActionOccuredDelegate();
    public event ActionOccuredDelegate ActionOccured;

    public virtual void InvokeAction()
    {
        if(ActionOccured != null)
        ActionOccured.Invoke();
    }
}

}
