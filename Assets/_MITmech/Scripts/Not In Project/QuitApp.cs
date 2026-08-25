using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitApp : MonoBehaviour
{
    public void QuitThatApp()
    {
        Debug.Log("Quitting App.");
        Application.Quit();
    }
}
