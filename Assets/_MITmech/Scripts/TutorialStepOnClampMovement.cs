using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialStepOnClampMovement : MonoBehaviour
{
    public ClampMovement clamp;
    public ClampMovement.ClampStatus desiredClampStatus;
    TutorialStep step;
    TutorialStateChecker stateChecker;
    bool previousFrameValue;
    public bool disableCheckForSteps;
    public TutorialStep[] stepsToStopChecking;
    public TutorialStep[] stepsToRecheckOnUncompletion;

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
        if (clamp.currentClampStatus == desiredClampStatus)
        {
            //Debug.Log(gameObject.name + " has been completed. Previous frame value was" + previousFrameValue);
            step.isCurrentlyComplete = true;
            if (previousFrameValue == false)
            { // only check if currently on the step this script is attached to
                if (disableCheckForSteps && step.shown)
                { 
                    foreach (TutorialStep uncheckStep in stepsToStopChecking)
                    {
                        
                        uncheckStep.canBeIgnoredInCheck = true;
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
}
