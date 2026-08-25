using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialStepJogup : MonoBehaviour
{
    public MouseRotate mr;
    public float desiredJogAmount; // 0-1
    float jogRange;

    TutorialStep step;
    TutorialStateChecker stateChecker;
    bool previousFrameValue;
    public bool disableCheckForSteps;
    public TutorialStep[] stepsToStopChecking;
    public TutorialStep[] stepsToRecheckOnUncompletion;
    public bool checkIfPreviousStepcompletedBeforeResetting;
    public TutorialStep stepToCheckForCompletion;

    public bool jogdown = false;

    // Start is called before the first frame update
    void Start()
    {
        step = GetComponent<TutorialStep>();
        stateChecker = GetComponentInParent<TutorialStateChecker>();
        previousFrameValue = step.isCurrentlyComplete;

        jogRange = mr.minAngle + ((mr.maxAngle - mr.minAngle) * desiredJogAmount);
        //Debug.Log("Debugging on " + name + " and we're doing the calculation " + mr.maxAngle + " " + mr.minAngle + " " + desiredJogAmount + " and getting " + jogRange);
    }

    // Update is called once per frame
    void Update()
    {
        if (CheckJog())
        {
            //Debug.Log(gameObject.name + " has been completed. Previous frame value was" + previousFrameValue);
            step.isCurrentlyComplete = true;
            if (previousFrameValue == false)
            {
                if (disableCheckForSteps && step.shown)
                {
                    if (checkIfPreviousStepcompletedBeforeResetting)
                    {
                        if (stepToCheckForCompletion.isCurrentlyComplete)
                        {
                            foreach (TutorialStep uncheckStep in stepsToStopChecking)
                            {

                                uncheckStep.canBeIgnoredInCheck = true;
                                Debug.Log(this.name + " disabling " + uncheckStep.name);

                            }
                        }
                    }
                    else
                    {
                        foreach (TutorialStep uncheckStep in stepsToStopChecking)
                        {
                            uncheckStep.canBeIgnoredInCheck = true;
                            Debug.Log(this.name + " disabling " + uncheckStep.name);

                        }

                    }

                }
                //Debug.Log("previous frame value was false");
                stateChecker.UpdateSteps();

            }
            previousFrameValue = true;
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

    bool CheckJog() {
        if (jogdown) {
            return mr.storedRotation <= jogRange;
        } else {
            return mr.storedRotation >= jogRange;
        }
    }
}
