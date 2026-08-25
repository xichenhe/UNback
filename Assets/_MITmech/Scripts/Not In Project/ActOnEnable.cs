using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActOnEnable : MonoBehaviour
{
    public SnapRestrictions sr;

    private void Start()
    {
        
    }

    private void OnDisable()
    {
        sr.MakeRemovable();
        Debug.Log("act on disabled");
    }

    private void OnEnable()
    {
        sr.MakeUnremovable();
        Debug.Log("act on enabled");
    }
}
