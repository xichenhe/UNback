using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableController : MonoBehaviour
{
    public PositionHolderScriptableObject positionHolder;

    private void Awake()
    {
        positionHolder.SetStartPosition(transform);
    }

    private void Update()
    {
        positionHolder.xPosition.AxisPosition = transform.position.x;
        positionHolder.yPosition.AxisPosition = transform.position.z;
        positionHolder.zPosition.AxisPosition = transform.position.y;
    }
}
