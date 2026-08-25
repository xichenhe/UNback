using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MITmech.ITS
{

public class ITSReactionBasedLayer : MonoBehaviour
{
    public ReactionObject reactionObject;
    private void Start()
    {
        reactionObject.Initialize();
    }
    public void GiveHint()
    {
        reactionObject.GiveHint();
    }

}

}
