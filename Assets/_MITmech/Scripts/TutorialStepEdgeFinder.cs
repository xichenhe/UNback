using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialStepEdgeFinder : MonoBehaviour
{
    TutorialStep step;
    TutorialStateChecker stateChecker;
    bool previousFrameValue;
    public bool disableCheckForSteps;
    public TutorialStep[] stepsToStopChecking;
    public TutorialStep[] stepsToRecheckOnUncompletion;

    public bool checkPop; // else, check for reset
    bool stepcompleted = false;
    public EdgeFinding ef;

    // Start is called before the first frame update
    void Start()
    {
        step = GetComponent<TutorialStep>();
        stateChecker = GetComponentInParent<TutorialStateChecker>();
        previousFrameValue = step.isCurrentlyComplete;
    }

    public bool AttemptToComplete()
    {
        if (stateChecker.lastStepComplete(this.name)) { // if last step of this step is completed, 
                    Debug.Log("EF Checked");
            if (checkPop) {
                return ef.isPopped;
            } else {
                return !ef.isPopped;
            }
        }          

        return false;

    }


    // Update is called once per frame
    void Update()
    {
        if (AttemptToComplete())
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

                stateChecker.UpdateSteps();
                //buttonpressed = false;
            }
            previousFrameValue = true;

        }
        else
        {
            //step.isCurrentlyComplete = false;
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
