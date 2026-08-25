using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RedDot : MonoBehaviour
{
    public Transform raycastStartObject;
    RaycastHit hit;

    // Update is called once per frame
    void Update()
    {
        Debug.DrawRay(raycastStartObject.position, Vector3.down);
        if(Physics.Raycast(raycastStartObject.position, Vector3.down, out hit, 5f))
        {
            transform.position = hit.point;
        }
    }
}
