using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SnapZoneManager;

public class TutorialStepCompleteOnSnapzone : MonoBehaviour
{
    public SnapZone zoneToCheck;
    public SnapObjectType[] desiredSnapObjects;
    TutorialStep step;
    TutorialStateChecker stateChecker;
    bool previousFrameValue;

    public bool disableCheckForSteps;
    public TutorialStep[] stepsToStopChecking;

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
        bool validObjectSnapped = false;
        foreach(SnapObjectType objectType in desiredSnapObjects)
        {
            if (zoneToCheck.currentlySnapped == objectType)
            {
                validObjectSnapped = true;
            }
        }
        if (validObjectSnapped)
        {
            step.isCurrentlyComplete = true;
            if (previousFrameValue == false)
            {
                stateChecker.UpdateSteps();
            }

            if (disableCheckForSteps && step.shown)
            {
                foreach (TutorialStep uncheckStep in stepsToStopChecking)
                {

                    uncheckStep.canBeIgnoredInCheck = true;
                    Debug.Log(this.name + " disabling " + uncheckStep.name);

                }
            }

            previousFrameValue = true;
        }
        else
        {
            step.isCurrentlyComplete = false;
            if (previousFrameValue == true)
            {
                stateChecker.UpdateSteps();
            }
            previousFrameValue = false;
        }
    }
}



