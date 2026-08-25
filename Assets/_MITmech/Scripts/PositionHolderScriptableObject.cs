using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;

[CreateAssetMenu(fileName ="Position Holder",menuName = "Machine Attributes/Position Holder")]
public class PositionHolderScriptableObject : ScriptableObject
{
    public Transform objectToFollow;

    public AxisPositionValue xPosition;
    public AxisPositionValue yPosition;
    public AxisPositionValue zPosition;

    public void SetStartPosition(Transform objectToFollow)
    {
        this.objectToFollow = objectToFollow;

        xPosition.Reset();
        yPosition.Reset();
        zPosition.Reset();

        xPosition.axisPosition = objectToFollow.position.x;
        yPosition.axisPosition = objectToFollow.position.y;
        zPosition.axisPosition = objectToFollow.position.z;
    }

    public void SetOffset(Axis axis,float addedOffset)
    {
        Debug.Log(addedOffset);
        if (axis == Axis.X)
        {
            xPosition.userAddedoffset = addedOffset;
            xPosition.Offset = objectToFollow.position.x;
        }
        else if (axis == Axis.Y)
        {
            yPosition.userAddedoffset = addedOffset;
            yPosition.Offset = objectToFollow.position.z;
        }
        else if (axis == Axis.Z)
        {
            zPosition.userAddedoffset = addedOffset;
            zPosition.Offset = objectToFollow.position.y;
        }
    }
}

[System.Serializable]
public class AxisPositionValue
{
    public UnityEvent<float> valueChange;

    public float Offset
    {
        get { return offset; }
        set { 
            offset = value;
            CalculateValue();
        }
    }
    public float offset = 0;

    public float UserAddedOffset
    {
        get { return userAddedoffset; }
        set
        {
            userAddedoffset = value;
            CalculateValue();
        }
    }
    public float userAddedoffset = 0;

    public void Reset()
    {
        offset = 0;
        userAddedoffset = 0;
    }

    public float AxisPosition
    {
        get
        {
            return axisPosition;
        }
        set
        {
            axisPosition = value;
            CalculateValue();
        }
    }

    void CalculateValue()
    {
        float calculatedOffset = axisPosition - offset + userAddedoffset;
        valueChange.Invoke(calculatedOffset);
    }
    public float axisPosition;
}
