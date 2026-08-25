using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPosCalculator : MonoBehaviour
{
    public Transform centerPos;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Rotated position is: " + centerPos.InverseTransformPoint(transform.position));
    }
}
