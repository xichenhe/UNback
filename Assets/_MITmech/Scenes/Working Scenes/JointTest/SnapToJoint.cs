using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapToJoint : MonoBehaviour
{

    private FixedJoint joint;
    void Start()
    {
        joint = GetComponent<FixedJoint>();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("JointSnapZone"))
        {
            JointSnapPoint jsp = other.gameObject.GetComponent<JointSnapPoint>();
            foreach (SnapToJoint stj in jsp.snapList)
            {
                if (stj == this)
                {
                    // snap it
                    
                    joint.connectedBody = jsp.GetComponent<Rigidbody>();
                    GetComponent<Rigidbody>().isKinematic = false;
                    break;
                }

            }

        }
    }
}
