using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MITmech.ITS
{

[CreateAssetMenu(fileName = "ITSTaskCompletedEvent", menuName = "ScriptableObjects/ITS/ITSTaskCompletedEvent", order = 1)]
public class UserCompletedTaskAction : UserAction
{
    public override void InvokeAction()
    {
        base.InvokeAction();
        ITSManager.Instance.interfaceModel.AddCompletedTask(this);
    }
}

}
