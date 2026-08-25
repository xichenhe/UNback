using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialStepCompleteOnStatus : MonoBehaviour
{

    TutorialStep step;
    TutorialStateChecker stateChecker;
    bool previousFrameValue;
    public bool disableCheckForSteps;
    public TutorialStep[] stepsToStopChecking;
    public TutorialStep[] stepsToRecheckOnUncompletion;

    public GameObject statusObject;
    public int index = 1;

    // Start is called before the first frame update
    void Start()
    {

            step = GetComponent<TutorialStep>();
            stateChecker = GetComponentInParent<TutorialStateChecker>();
            previousFrameValue = step.isCurrentlyComplete;

    }

    // Update is called once per frame
    void Update()
    {
        if (index == 1)
        {
            if (statusObject.GetComponent<MouseRotate>().isLocked) {
                //Debug.Log(gameObject.name + " has been completed. Previous frame value was" + previousFrameValue);
                step.isCurrentlyComplete = true;
                if (previousFrameValue == false)
                {
                    if (disableCheckForSteps)
                    {
                        foreach (TutorialStep uncheckStep in stepsToStopChecking)
                        {
                            uncheckStep.canBeIgnoredInCheck = true;
                            Debug.Log(this.name + " disabling " + uncheckStep.name);
                        }
                    }
                    //Debug.Log("previous frame value was false");
                    stateChecker.UpdateSteps();
                    //buttonpressed = false;
                }
                previousFrameValue = true; }

        }
        else
        {
            step.isCurrentlyComplete = false;
            if (previousFrameValue == true)
            {
                if (disableCheckForSteps)
                {
                    foreach (TutorialStep uncheckStep in stepsToRecheckOnUncompletion)
                        uncheckStep.canBeIgnoredInCheck = false;
                }
                stateChecker.UpdateSteps();
            }
            previousFrameValue = false;
        }
    }
}
