using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugBlockSize : MonoBehaviour
{
    public Transform referenceZero;
    public Transform rotatingWorldZero;
    
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("sent contact location was " + transform.position);
        Debug.Log("0,0 is at location of: " + rotatingWorldZero.InverseTransformPoint(referenceZero.position));
        Vector3 zeroedReferencePos = transform.position - rotatingWorldZero.InverseTransformPoint(referenceZero.position);
        zeroedReferencePos = new Vector3(zeroedReferencePos.x, 0, zeroedReferencePos.z);
        Debug.Log("zeroed out contact location is: " + zeroedReferencePos);
        Debug.Log("zeroed out contact location x is: " + zeroedReferencePos.x);
        Debug.Log("zeroed out contact location z is: " + zeroedReferencePos.z);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
