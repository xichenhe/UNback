using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CurrentStatusBool : MonoBehaviour
{
    [Header("Current Status")]
    public bool interactedState;

    [Header("Status Text")]
    [Tooltip("Variable UI Text for the machine componant status")]
    public TextMeshProUGUI statusText;

    [Header("Status Color")]
    [Tooltip("Reference to the UI Image component which will change based on the status")]
    public Image statusColor;
    public Color looseColor = Color.red; // State Color
    public Color tightColor = Color.green; // State Color


    private void Start()
    {
        //statusText.enabled = true;
        interactedState = false;    
        UpdateStatus();
        //Debug.Log("current status bool " + name); // What is is Debug?
    }

    public void CycleInteracted()
    {
        interactedState = !interactedState;
        UpdateStatus();
    }

    public void SetTightened()
    {
        interactedState = true;
        UpdateStatus();
        Debug.Log("current status bool tightened");
    }

    public void SetLoosened()
    {
        interactedState = false;
        UpdateStatus();
        Debug.Log("current status bool loosened");
    }

    // Update the text and color based on the current state
    void UpdateStatus()
    {
        if (interactedState)
        {
            statusText.text = "Tight";
            statusColor.color = tightColor;
        }
        else
        {
            statusText.text = "Loose";
            statusColor.color = looseColor;
        }
    }

    /*
    public TextMeshProUGUI defaultStateText;
    public TextMeshProUGUI interactedStateText;
    public bool interactedState;

    private void Start()
    {
        interactedState = false;
        defaultStateText.enabled = true;
        interactedStateText.enabled = false;
        Debug.Log("current status bool " + name);
    }

    public void CycleInteracted()
    {
        interactedState = !interactedState;
        defaultStateText.enabled = !interactedState;
        interactedStateText.enabled = interactedState;
    }

    public void SetTightened()
    {
        Debug.Log("current status bool tightened");
        interactedState = true;
        defaultStateText.enabled = false;
        interactedStateText.enabled = true;
    }

    public void SetLoosened()
    {
        Debug.Log("current status bool loosened");
        interactedState = false;
        defaultStateText.enabled = true;
        interactedStateText.enabled = false;
    }
    */
}
