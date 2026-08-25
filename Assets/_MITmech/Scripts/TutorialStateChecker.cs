using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
//using UnityEngine.Video;

public class TutorialStateChecker : MonoBehaviour
{
    //New Project

    public TutorialStep[] tutorialSteps; //Reference to TutorialStep.cs
    public TextMeshProUGUI currentStepText;
    int currentStep;
    public GameObject EnableOnComplete;

    public bool inTutorial = true;

    // Public accessors for EventLogger 
    public int CurrentStep => currentStep;
    public string CurrentStepName => (tutorialSteps != null && currentStep < tutorialSteps.Length)
        ? tutorialSteps[currentStep].gameObject.name
        : "Unknown";

    //public VideoPlayer videoPlayer;


    // Start is called before the first frame update
    void Start()
    {

    }
    public void Update()
    {
        UpdateSteps();
    }

    public void UpdateSteps()
    {
        if (inTutorial)
        {
            //Debug.Log("Updating Steps.");

            for (int i = 0; i < tutorialSteps.Length; i++)
            {
                TutorialStep step = tutorialSteps[i];
                if (!step.isCurrentlyComplete && !step.canBeIgnoredInCheck)
                {
                    //Debug.Log("currently showing: " + step.gameObject.name);
                    currentStepText.text = /*"Next Step: " + "\n" +*/ step.promptText;
                    step.shown = true;

                    // showing for each step
                    if (currentStep != i)
                    {
                        if (tutorialSteps[currentStep].showGameObject)
                        {
                            tutorialSteps[currentStep].showOnStep.SetActive(false);

                            //tutorialSteps[currentStep].activateVideoPlayer.SetActive(false);

                        }

                        int previousStep = currentStep; // store before overwriting
                        currentStep = i;

                        if (tutorialSteps[currentStep].showGameObject)
                        {
                            tutorialSteps[currentStep].showOnStep.SetActive(true);

                        }
                        /*
                        if (tutorialSteps[currentStep].showVideo) {
                            tutorialSteps[currentStep].activateVideoPlayer.SetActive(true);
                            //videoPlayer.clip = tutorialSteps[currentStep].tutorialVideoStep;

                        }
                        */

                        // Log step change 
                        EventLogger.Instance?.Log(
                            "step_changed",
                            null,
                            new Dictionary<string, object> {
                                { "from_step",  previousStep },
                                { "to_step",    currentStep },
                                { "step_name",  tutorialSteps[currentStep].gameObject.name },
                                { "prompt",     tutorialSteps[currentStep].promptText }
                            }
                        );
                        
                        
                        // 
                    }

                    // showing on complete
                    if (currentStep == (tutorialSteps.Length - 1))
                    {
                        Debug.Log("showing enable on complete");
                        //EnableOnComplete.SetActive(true);
                        inTutorial = false;
                    }
                    return;
                }
            }
        }

    }

    public bool lastStepComplete(string s)
    {

        for (int i = 0; i < tutorialSteps.Length; i++)
        {
            if (s == tutorialSteps[i].gameObject.name)
            {
                return i == currentStep;
            }
        }
        //Debug.Log("This message should not be reached");
        return false;
    }



    // Update is called once per frame
    public void RestartTutorial()
    {
        //  Log tutorial restart 
        EventLogger.Instance?.Log(
            "tutorial_restart",
            null,
            new Dictionary<string, object> {
                { "restarted_from_step", currentStep }
            }
        );
        // 

        inTutorial = true;
        for (int i = 1; i < tutorialSteps.Length; i++) // do not reset step 0
        {
            TutorialStep step = tutorialSteps[i];
            step.shown = false;
            step.canBeIgnoredInCheck = false;
            //step.isCurrentlyComplete = false;
        }
    }

    public void StartTutorial()
    {
        RestartTutorial();
        // find the earliest incomplete step
        for (int i = 0; i < tutorialSteps.Length; i++)
        {
            TutorialStep step = tutorialSteps[i];
            if (!step.isCurrentlyComplete && !step.canBeIgnoredInCheck)
            {
                Debug.Log("currently showing: " + step.gameObject.name);
                currentStepText.text = /*"Next Step: " + "\n" +*/ step.promptText;
                step.shown = true;
                currentStep = i;

                if (tutorialSteps[currentStep].showGameObject)
                {
                    tutorialSteps[currentStep].showOnStep.SetActive(true);

                }

                // showing on complete
                if (currentStep == (tutorialSteps.Length - 1))
                {
                    Debug.Log("showing enable on complete");
                    EnableOnComplete.SetActive(true);
                    inTutorial = false;
                }

                return;
            }
        }
    }
}