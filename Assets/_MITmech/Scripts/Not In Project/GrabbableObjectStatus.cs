using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabbableObjectStatus : MonoBehaviour
{
    public bool isSnapped = true;
    public List<MonoBehaviour> activateWhenSnapped;
    public List<MonoBehaviour> activateWhenUnSnapped;

    public Transform snapAnchor;
    public Rigidbody connected;

    FixedJoint fj;
    Rigidbody rb;

    float size = 0;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        changeStatus(isSnapped);
        if (GetComponent<DrillBit>() != null) {
            size = (GetComponent<DrillBit>().drillThickness / 0.125f) * 0.01f;
        }
        
    }
   
    private void Update()
    {
        // make sure there are only 1 joint, update kinematic status
        if (!isSnapped && GetComponent<FixedJoint>() != null) {
            Destroy(GetComponent<FixedJoint>());
            rb.isKinematic = true;
            rb.useGravity = false;
        } 
    }
    public void changeStatus(bool snap) {
        if (!snap)
        {
            Destroy(GetComponent<FixedJoint>());
            rb.constraints = RigidbodyConstraints.None;
            rb.isKinematic = true;
            rb.useGravity = false;
            if (fj != null)
            {
                fj.connectedBody = null;
            }
            isSnapped = false;
            foreach (MonoBehaviour s in activateWhenUnSnapped)
            {
                s.enabled = true;
            }
            foreach (MonoBehaviour s in activateWhenSnapped)
            {
                s.enabled = false;
            }
        }
        else {
            SnapIn();
            isSnapped = true;
            foreach (MonoBehaviour s in activateWhenUnSnapped)
            {
                s.enabled = false;
            }
            foreach (MonoBehaviour s in activateWhenSnapped)
            {
                s.enabled = true;
            }
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezePositionX;
        }
    }

    void SnapIn() {
        rb.isKinematic = false;
        rb.useGravity = true;
        fj = gameObject.AddComponent<FixedJoint>();
        Vector3 transformby = snapAnchor.position;
        transformby.y += size;
        transform.position = transformby;
        transform.rotation = snapAnchor.rotation;
        fj.connectedBody = connected;
    }

}
