using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class CoordinateSetter : MonoBehaviour
{
    public PositionHolderScriptableObject positionHolder;

    public Axis axis = Axis.X;

    private void Start()
    {
        axis = Axis.X;
    }

    public void SetXAxis()
    { 
        axis = Axis.X; 
    }
    public void SetYAxis()
    {
        axis = Axis.Y;
    }
    public void SetZAxis()
    {
        axis = Axis.Z;
    }

    public void SetOffset(float userInput)
    {
        positionHolder.SetOffset(axis,userInput);
    }
}
