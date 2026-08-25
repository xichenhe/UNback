using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HoleManager : MonoBehaviour
{
    public GameObject bottomhole;
    public GameObject counterTophole;
    public GameObject counterBottomhole;
    public bool isCounter = false;
    bool drilledThrough = false;
    public bool deburred = false;

    public Material holeMaterial;
    public Material deburredHoleMaterial;
    public Material selectHoleMaterial;

    public TutorialStepButtonPressed p;
    // adjust hole debur status


    private void Start()
    {
        if (p != null)
        {
            return;
        }

        TutorialStepButtonPressed[] tutorialButtons = FindObjectsByType<TutorialStepButtonPressed>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (TutorialStepButtonPressed tutorialButton in tutorialButtons)
        {
            if (tutorialButton.name == "Step 38" || HasTutorialStepLabel(tutorialButton.gameObject, "Step 38"))
            {
                p = tutorialButton;
                break;
            }
        }

        if (p == null)
        {
            Debug.LogWarning($"{nameof(HoleManager)} on {name} could not find tutorial step 'Step 38'. Deburring will still work, but it will not auto-advance the tutorial.");
        }
    }

    static bool HasTutorialStepLabel(GameObject stepObject, string expectedLabel)
    {
        TMP_Text[] tmpTexts = stepObject.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text tmpText in tmpTexts)
        {
            if (tmpText.text == expectedLabel)
            {
                return true;
            }
        }

        Text[] uiTexts = stepObject.GetComponentsInChildren<Text>(true);
        foreach (Text uiText in uiTexts)
        {
            if (uiText.text == expectedLabel)
            {
                return true;
            }
        }

        return false;
    }

    public void UpdateHoleDebur()
    {
        if (!deburred) {
            p?.Press();
            deburred = true;
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null && deburredHoleMaterial != null)
            {
                renderer.material = deburredHoleMaterial;
            }
            if (isCounter)
            {
                Renderer counterTopRenderer = counterTophole != null ? counterTophole.GetComponent<Renderer>() : null;
                if (counterTopRenderer != null && deburredHoleMaterial != null)
                {
                    counterTopRenderer.material = deburredHoleMaterial;
                }
            }
            if (drilledThrough)
            {
                Renderer bottomRenderer = bottomhole != null ? bottomhole.GetComponent<Renderer>() : null;
                if (bottomRenderer != null && deburredHoleMaterial != null)
                {
                    bottomRenderer.material = deburredHoleMaterial;
                }
                if (isCounter)
                {
                    Renderer counterBottomRenderer = counterBottomhole != null ? counterBottomhole.GetComponent<Renderer>() : null;
                    if (counterBottomRenderer != null && deburredHoleMaterial != null)
                    {
                        counterBottomRenderer.material = deburredHoleMaterial;
                    }
                }
            }
            CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
            if (capsuleCollider != null)
            {
                Destroy(capsuleCollider);
            }
        }
    }
    // adjust hole dimension
    public void adjustDimension(float d) {
        transform.localScale *= d;
        if (isCounter)
        {
            // do the ame for counter top hole
            if (counterTophole != null)
            {
                counterTophole.transform.localScale *= d;
            }
        }

        //do the same for bottom hole
        if (drilledThrough)
        {
            if (bottomhole != null)
            {
                bottomhole.transform.localScale *= d;
            }

            if (isCounter)
            {
                // do the same for counter bottom hole
                if (counterBottomhole != null)
                {
                    counterBottomhole.transform.localScale *= d;
                }
            }
        }
    }

    public void DrillThrough() {
        drilledThrough = true;
        if (deburred) {
            Renderer bottomRenderer = bottomhole != null ? bottomhole.GetComponent<Renderer>() : null;
            if (bottomRenderer != null && deburredHoleMaterial != null)
            {
                bottomRenderer.material = deburredHoleMaterial;
            }
            if (isCounter)
            {
                Renderer counterBottomRenderer = counterBottomhole != null ? counterBottomhole.GetComponent<Renderer>() : null;
                if (counterBottomRenderer != null && deburredHoleMaterial != null)
                {
                    counterBottomRenderer.material = deburredHoleMaterial;
                }
            }
        }
    }

    public void Select() {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && selectHoleMaterial != null)
        {
            renderer.material = selectHoleMaterial;
        }
        if (isCounter)
        {
            Renderer counterTopRenderer = counterTophole != null ? counterTophole.GetComponent<Renderer>() : null;
            if (counterTopRenderer != null && selectHoleMaterial != null)
            {
                counterTopRenderer.material = selectHoleMaterial;
            }
        }
    }

    public void Unselect() {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && holeMaterial != null)
        {
            renderer.material = holeMaterial;
        }
        if (isCounter)
        {
            Renderer counterTopRenderer = counterTophole != null ? counterTophole.GetComponent<Renderer>() : null;
            if (counterTopRenderer != null && holeMaterial != null)
            {
                counterTopRenderer.material = holeMaterial;
            }
        }
    }
}
