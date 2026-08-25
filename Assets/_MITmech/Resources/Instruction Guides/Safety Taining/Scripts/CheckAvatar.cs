using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CheckAvatar : MonoBehaviour
{
    public GenerateRandom[] checkFixed;
    public Draggable2D[] checkDragged;

    public GameObject successMessage;
    public GameObject failureMessage;

    public bool checkAvatar()
    {

        foreach (GenerateRandom g in checkFixed)
        {
            if (!g.isFixed)
            {
                return false;
            }
        }

        foreach (Draggable2D d in checkDragged)
        {
            if (!d.snapped)
            {
                return false;
            }
        }

        return true;
    }

    public void displayMessage()
    {
        if (checkAvatar())
        {
            successMessage.SetActive(true);
        }
        else { failureMessage.SetActive(true); }
    }
}
