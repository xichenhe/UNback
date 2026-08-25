using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialStepCorrectSpinSpeedRange : MonoBehaviour
{
    Spinning spin;
    public float minSpinRPM, maxSpinRPM;
    TutorialStep step;
    TutorialStateChecker stateChecker;
    bool previousFrameValue;
    public bool disableCheckForSteps;
    public TutorialStep[] stepsToStopChecking;
    public TutorialStep[] stepsToRecheckOnUncompletion;

    // Start is called before the first frame update
    void Start()
    {
        spin = Spinning.Instance;
        step = GetComponent<TutorialStep>();
        stateChecker = GetComponentInParent<TutorialStateChecker>();
        previousFrameValue = step.isCurrentlyComplete;
    }

    // Update is called once per frame
    void Update()
    {
        if (spin.SpinSpeed > minSpinRPM && spin.SpinSpeed < maxSpinRPM)
        {
            //Debug.Log(gameObject.name + " has been completed. Previous frame value was" + previousFrameValue);
            step.isCurrentlyComplete = true;
            if (previousFrameValue == false)
            {
                //Debug.Log("previous frame value was false");
                stateChecker.UpdateSteps();
                if (disableCheckForSteps)
                {
                    foreach (TutorialStep uncheckStep in stepsToStopChecking)
                        uncheckStep.canBeIgnoredInCheck = true;
                }
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
}
