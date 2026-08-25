using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EdgefinderReset : MonoBehaviour
{
    public EdgeFinding edgeFinderTipReference;
    public EdgeFinderPopOut edgeFindingPopOutReference;

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Drillable") && edgeFinderTipReference.isPopped)
        {
            edgeFindingPopOutReference.Reset();
        }
    }
}
