using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MITmech.ITS
{

[CreateAssetMenu(fileName = "ITSMessage", menuName = "ScriptableObjects/ITS/ITSMessage", order = 1)]
public class ITSMessage : ScriptableObject
{
    public string message;
    public Severity severity;
    public void SendMessage()
    {
        //ITSManager.Instance.errorAgent.AddMessage(this);
    }
}

}
