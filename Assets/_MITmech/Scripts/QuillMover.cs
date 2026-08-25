using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuillMover : MonoBehaviour
{
    //public Slider zAxisSlider;
    public MouseRotate zRotator;
    Vector3 startPos;
    Vector3 startZRot;
    public float angleNeededToManualLockDegrees = 10f;
    private bool resetLockingOrUnlockingQuill = true;

    public GrabbableInventoryObject gio;

    //public GameObject zQuillLever;
    public float movementMultiplier;
    public CurrentStatusBool csb;


    // Start is called before the first frame update
    void Start()
    {
        //startZRot = zQuillLever.transform.rotation.eulerAngles;
        startPos = transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateZPosition();
    }

    public void UpdateZPosition()
    {
        //Vector3 newZRot = startZRot + new Vector3(1f, 0f, 0f) * rotationMultiplier * zRotator.storedRotation;
        //zQuillLever.transform.rotation = Quaternion.Euler(newZRot);

        //Added conditions for manually locking the quill to replace pneumatics
        //tighten the chuck
        //if (gio.hasRecentZone)
        //{
            if (zRotator.storedRotation > 2.5f && resetLockingOrUnlockingQuill)
            {
                gio.SnapWhenInteract();
                GetComponent<QuillManualLock>().Tighten();
                csb.SetTightened();
                //GetComponentInChildren<SnapZone>().inventoryObjectModelDictionary[SnapZoneManager.SnapObjectType.Chuck].transform.Translate(-Vector3.forward*GetComponentInChildren<SnapZone>().unlockedChuckOffsetMeters, Space.Self);
                resetLockingOrUnlockingQuill = false;
                zRotator.storedRotation = 0f;
            }
        //}
        // loosen the chuck
        //else if (GetComponentInChildren<SnapZone>().currentlySnapped == SnapZoneManager.SnapObjectType.Chuck)
        //{
/*            if (zRotator.storedRotation > 2.5f && resetLockingOrUnlockingQuill)
            {
                GetComponent<QuillManualLock>().Loosen();
                StartCoroutine(Untighten());
                csb.SetLoosened();
                //GetComponentInChildren<SnapZone>().inventoryObjectModelDictionary[SnapZoneManager.SnapObjectType.Chuck].transform.Translate(Vector3.forward*GetComponentInChildren<SnapZone>().unlockedChuckOffsetMeters, Space.Self);
                resetLockingOrUnlockingQuill = false;
            }*/
        //}

        //only lowers the quill if the handle MouseRotate rotation is less than 0, so greater than 0 is reserved for spring loaded locking
        if (zRotator.storedRotation <= 0f)
        {
            StartCoroutine(WaitForChange());
            transform.localPosition = startPos + Vector3.up * zRotator.storedRotation * movementMultiplier;
        }
        else
        {
            transform.localPosition = startPos;
        }
    }

    IEnumerator WaitForChange()
    {
        yield return new WaitForSeconds(1f);
        resetLockingOrUnlockingQuill = true;
        yield break;
    }
    IEnumerator Untighten()
    {
        Debug.Log("untightening....");
        yield return new WaitForSeconds(0.5f);
        //anim.Play("Empty");
        gio.RemoveIfSnapped();
        yield break;
    }
}

