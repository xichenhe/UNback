using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MITmech.ITS
{

[CreateAssetMenu(fileName = "ReactionObject", menuName = "ScriptableObjects/ITS/ReactionObject", order = 1)]
public class ReactionObject : ScriptableObject
{
    public HintContainer hintContainer;

    public void Initialize()
    {
        //hintContainer.Initialize();
    }

    public void GiveHint()
    {
        hintContainer.GiveHint();
    }
}

}
