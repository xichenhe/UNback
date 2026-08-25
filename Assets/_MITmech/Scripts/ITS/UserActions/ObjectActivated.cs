using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MITmech.ITS
{

[CreateAssetMenu(fileName = "ITSObjectActivatedEvent", menuName = "ScriptableObjects/ITS/ITSObjectActivatedEvent", order = 1)]
public class ObjectActivated : UserAction
{
    public bool isActive;

    public void SetValues(bool isActive)
    {

    }
}

}
