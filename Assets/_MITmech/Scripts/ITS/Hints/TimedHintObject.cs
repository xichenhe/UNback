using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MITmech.ITS
{
[CreateAssetMenu(fileName = "Timed Hint", menuName = "ScriptableObjects/ITS/Hints/Timed Hint", order = 1)]

public class TimedHintObject : HintObject
{
    public HintContainer hintContainer;
    public int index;
    public float waitTime = 10f;

    public void Initialize(HintContainer hintContainer,int index)
    {
        this.hintContainer = hintContainer;
        this.index = index;
    }

    public void ReclaimTime()
    {
        hintContainer.subTask.ReclaimHintTime(waitTime);
        hintContainer.hintIndex = index;
    }
}

}
