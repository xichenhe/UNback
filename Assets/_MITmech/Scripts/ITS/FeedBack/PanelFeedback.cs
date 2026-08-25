using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MITmech.ITS
{

public class PanelFeedback : MonoBehaviour
{
    public Notification notification;
    public TMP_Text feedbackText;

    private void Start()
    {
        ITSManager.Instance.errorAgent.ErrorAgentEvent += AddToLog;
    }

    public void AddToLog(string feedback,Severity severity)
    { 

        feedbackText.text += feedback + "\n";
    }
    public void GiveHint(string feedback, Severity severity)
    {

        feedbackText.text = feedback;
        if(severity == Severity.Normal)
        notification.Notify();
        else if(severity == Severity.Complete)
            notification.Complete();
    }
}

}
