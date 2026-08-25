using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapRestrictions : MonoBehaviour
{

    public bool removable;
    public Collider collider;

    public void MakeRemovable()
    {
        removable = true;
    }

    public void MakeUnremovable()
    {
        removable = false;
    }

    public void CycleRemovablity()
    {
        removable = !removable;
    }
}
