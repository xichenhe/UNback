using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Schematics : MonoBehaviour
{
    public float xPos;
    public float yPos;
    bool displayed = false;

    public bool GetDisplayed()
    {
        return displayed;
    }

    public void SetDisplayed(bool b) {
        displayed = b;
    }
}
