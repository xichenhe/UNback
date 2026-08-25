using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static ClampMovement;

public class BlockStatusText : MonoBehaviour
{
    [Header("Current Status")]
    SnapRestrictions snapRestrictions; // Needs more detail about how this is used throughout the application 

    [Header("Status Text")]
    [Tooltip("Variable UI Text for the machine part status")]
    public TextMeshProUGUI statusText;

    [Tooltip("Reference to the UI Image component which will change based on the status")]
    public Image statusColor;
    [Tooltip("Select Color for component")]
    public Color looseColor = Color.red; // State Color
    [Tooltip("Select Color for component")]
    public Color tightColor = Color.green; // State Color

    // Start is called before the first frame update
    void Start()
    {
        snapRestrictions = GetComponent<SnapRestrictions>();
        statusText.text = "Loose"; // Update the statusText based on the current state
        statusColor.color = looseColor; // Update the color of the statusColor based on the current state

        Debug.Log("Clamp Status " + ClampMovement.Instance.currentClampStatus);
    }

    // Update is called once per frame
    void Update()
    {
        if (ClampMovement.Instance.currentClampStatus == ClampStatus.Loose)
        {
            statusText.text = "Loose"; // Update the statusText based on the current state
            statusColor.color = looseColor; // Update the color of the statusColor based on the current state
        }
        else if (ClampMovement.Instance.currentClampStatus == ClampStatus.NeedsAWack)
        {
            statusText.text = "Loose"; // Update the statusText based on the current state
            statusColor.color = looseColor; // Update the color of the statusColor based on the current state
        }
        else if (ClampMovement.Instance.currentClampStatus == ClampStatus.NeedsATestJiggle)
        {
            statusText.text = "Loose"; // Update the statusText based on the current state
            statusColor.color = looseColor; // Update the color of the statusColor based on the current state
        }
        else if (ClampMovement.Instance.currentClampStatus == ClampStatus.FullySecure)
        {
            statusText.text = "Tight"; // Update the statusText based on the current state
            statusColor.color = tightColor; // Update the color of the statusColor based on the current state
        }     
    }
}
