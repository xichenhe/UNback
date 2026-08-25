using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorSequencerController : MonoBehaviour
{
    public List<AnimationSequencer> animationSequencers;

    public void PlaySequence(int index)
    {
        animationSequencers[index].PlaySequence();
    }
}
