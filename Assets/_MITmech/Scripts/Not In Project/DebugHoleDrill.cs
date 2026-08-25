using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugHoleDrill : MonoBehaviour
{
    public Transform rotatingWorldZero;
    public Drillable drillable;
    DrillBit bit;

    // Start is called before the first frame update
    void Start()
    {
        bit = GetComponent<DrillBit>();

    }

    public void DebugAHole()
    {
        Debug.Log("Debugging Hole.");
        Vector3 inverseTransform = rotatingWorldZero.InverseTransformPoint(transform.position);
        Debug.Log(inverseTransform.x + ", " + inverseTransform.y + ", " + inverseTransform.z);
        drillable.NewHole(.1f, rotatingWorldZero.InverseTransformPoint(transform.position), true);
        drillable.UpdateHoleDepth(rotatingWorldZero.InverseTransformPoint(transform.position), bit, true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
