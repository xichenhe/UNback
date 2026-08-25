using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EdgeFinderPopOut : MonoBehaviour
{
    public GameObject resetButton;
    public GameObject ogEdgeFinder;
    public GameObject resetter;
    public Transform block;

    public float popOutDistance;

    private Vector3 ogPos;
    public SnapZone edgefinderSnapZone;

    private void Start()
    {
        /*Debug.Log("debugging edgefinder popout: original position: " + transform.position);
        ogPos = transform.position;
        Debug.Log("debugging edgefinder popout: ogPos " + ogPos);*/
    }

    private void OnEnable()
    {
        Debug.Log("debugging edgefinder popout: popping out!");
        PopOut();
        resetter.SetActive(true);
    }

    private void PopOut() {
        ogPos = transform.position;
        Vector3 moveDir = transform.position - block.position;
        moveDir.y = 0f;
        transform.position += (Vector3.Normalize(moveDir) * popOutDistance);
    }

    public void Reset()
    {
        Debug.Log("debugging edgefinder popout: resetting!");
        ogEdgeFinder.GetComponent<EdgeFinding>().ResetPop();
        transform.position = ogPos;
        Debug.Log("debugging edgefinder popout: new position of ef after resetting: " + transform.position);
        resetter.SetActive(false);
        gameObject.SetActive(false);
    }

    public void Update() {
        // reset when unsnapped
        if (edgefinderSnapZone.currentlySnapped != SnapZoneManager.SnapObjectType.EdgeFinder)
        {
            Reset();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Drillable"))
        {
            FailureState.Instance.DisplayWarning("Do not move the block into the edge finder, it may break.");
            ogEdgeFinder.GetComponent<EdgeFinding>().warnedBlockInEdgefinder = true;
        }
    }
}
