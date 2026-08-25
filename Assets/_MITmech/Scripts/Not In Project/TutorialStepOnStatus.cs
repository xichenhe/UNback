using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialStepOnStatus : MonoBehaviour
{
    public bool isStatusSender = false;

    public TutorialStepOnStatus sendTo; 

    TutorialStep step;
    TutorialStateChecker stateChecker;
    bool previousFrameValue;
    public bool disableCheckForSteps;
    public TutorialStep[] stepsToStopChecking;
    public TutorialStep[] stepsToRecheckOnUncompletion;

    public UnityEngine.UI.Button[] b; // if pressed, this step is over.
    bool buttonpressed = false;

    // Start is called before the first frame update
    void Start()
    {
        if (!isStatusSender)
        {
            step = GetComponent<TutorialStep>();
            stateChecker = GetComponentInParent<TutorialStateChecker>();
            previousFrameValue = step.isCurrentlyComplete;
        }
    }

    public void Press()
    {
        buttonpressed = stateChecker.lastStepComplete(this.name); //simplefied version of last step complete && button is pressed
        Debug.Log(this.name + " button pressed: " + buttonpressed);

    }


    // Update is called once per frame
    void Update()
    {
        if (buttonpressed)
        {
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
