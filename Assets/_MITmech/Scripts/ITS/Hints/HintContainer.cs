using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MITmech.ITS
{

[CreateAssetMenu(fileName = "HintContainer", menuName = "ScriptableObjects/ITS/Hints/HintContainer", order = 1)]
public class HintContainer : ScriptableObject
{
    public HintObject lastGivenHint;
    public int hintIndex = 0;
    public SubTask subTask;
    public List<TimedHintObject> hints = new List<TimedHintObject>();
    public CompletionHintObject completionHint;
    public SubTaskUndidHintObject subtaskUndidHint;

    public void Initialize(SubTask subTask)
    {
        lastGivenHint = null;

        this.subTask = subTask;

        int index = 0;
        foreach (TimedHintObject hint in hints)
        {
            hint.Initialize(this,index);
            index++;
        }

        hintIndex = 0;
    }
    public void Reset()
    {
        hintIndex = 0;
    }
    public void GiveHint()
    {

        if (hintIndex >= hints.Count || hintIndex > (int)ITSManager.Instance.interfaceModel.feedbackLevel)
        {
            return;
        }
        hints[hintIndex].GiveHint();
        lastGivenHint = hints[hintIndex];

        hintIndex++;

    }
    public void SubtaskUndid()
    {
        if (subtaskUndidHint)
        {
            subtaskUndidHint.GiveHint();
            lastGivenHint = subtaskUndidHint;
        }
    }
    public void SetComplete()
    {
        if (completionHint && lastGivenHint!=null)
        {
            completionHint.GiveHint();
            lastGivenHint = null;
        }

    }
    public TimedHintObject GetCurrentTimedHint()
    {
        if(hintIndex >= hints.Count)
            return hints[hints.Count-1];
        else
            return hints[hintIndex];
    }
}

}
