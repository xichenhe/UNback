using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationSequencer : MonoBehaviour
{
    public int repetitions = 1;
    public List<Animator> animators;

    public void PlaySequence()
    {
        StartCoroutine(PlayEntireSequence());
    }
    public IEnumerator PlayEntireSequence()
    {
        for (int i = 0; i < repetitions; i++)
            for (int j = 0;j < animators.Count;j++)
            yield return StartCoroutine(PlayAnimation(j));

        print("finished sequence");
    }
    public IEnumerator PlayAnimation(int index)
    {
        animators[index].gameObject.SetActive(true);
        print(animators[index].GetCurrentAnimatorStateInfo(0).length);
        yield return new WaitForSeconds(animators[index].GetCurrentAnimatorStateInfo(0).length);
        animators[index].gameObject.SetActive(false);
    }
}
