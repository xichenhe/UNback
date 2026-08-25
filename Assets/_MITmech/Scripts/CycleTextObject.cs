using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static Spinning;

public class CycleTextObject : MonoBehaviour
{
    //NEW
    [Header("Current Status")]
    public bool isTight;
    public SnapZone quillSnapZone;

    [Header("Status Text")]
    [Tooltip("Variable UI Text for the machine componant status")]
    public TextMeshProUGUI statusText;

    [Tooltip("Reference to the UI Image component which will change based on the status")]
    public Image statusColor;
    public Color looseColor = Color.red; // State Color
    public Color tightColor = Color.green; // State Color

    private void Start()
    {
        Spinning.SpinningEvent += CheckIfTight;
        statusText.text = "Tight";
        statusColor.color = tightColor; // Update the color of the statusColor based on the current state
    }

    public void CycleText()
    {
        isTight = !isTight;

        if (isTight)
        {
            statusText.text = "Tight"; // Update the statusText based on the current state
            statusColor.color = tightColor; // Update the color of the statusColor based on the current state
        }

        if (!isTight)
        {
            statusText.text = "Loose"; // Update the statusText based on the current state
            statusColor.color = looseColor; // Update the color of the statusColor based on the current state
        }

    }

    void CheckIfTight(bool isSpinning)
    {
        if (isSpinning)
        {
            if (!isTight && quillSnapZone.currentlySnapped == SnapZoneManager.SnapObjectType.Chuck)
            {
                FailureState.Instance.SystemFailure("You started spinning before the chuck was tightened.");
            }
        }
    }
}
