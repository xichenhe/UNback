using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Spinning;
using static ClampMovement;
using static FailureState;
using UnityEngine.Events;

public class DrillBit : MonoBehaviour
{
    public UnityEvent CenterHoleDrilledEvent;
    public UnityEvent<bool> PartActiveEvent;

    public Transform rotatingWorldZero;
    public Transform entryPoint;
    public float drillThickness;
    Drillable drill;

    public MouseRotate zRotator;
    float storedAngleBounds;

    public bool isCenterPointDrill;
    public float minDepthToCenterPoint = -.1f;
    public float depthTooDangerous = -10f;

    public AudioSource drillingIntoMachineSource;

    bool newHoleStarted;
    bool justSnappedIn;

    public bool hasSpace;
    public float scaleAdjuster = 1;


    // Start is called before the first frame update
    void Start()
    {
        //justSnappedIn = false;
        newHoleStarted = false;
        drillThickness = drillThickness * scaleAdjuster;
        minDepthToCenterPoint = minDepthToCenterPoint * scaleAdjuster;
        depthTooDangerous = depthTooDangerous * scaleAdjuster;
        if (zRotator == null)
        {
            GameObject quillRotateManager = GameObject.Find("Quill Rotate [Manager]");
            if (quillRotateManager != null)
            {
                zRotator = quillRotateManager.GetComponent<MouseRotate>()
                    ?? quillRotateManager.GetComponentInChildren<MouseRotate>(true);
            }
        }

        if (zRotator == null)
        {
            Debug.LogWarning($"{nameof(DrillBit)} on {name} could not find a quill rotation controller. Angle reset bookkeeping will be skipped.");
        }

    }

    bool TryGetEntryPointInMachineSpace(out Vector3 machineSpaceEntryPoint)
    {
        machineSpaceEntryPoint = default;

        if (rotatingWorldZero == null || entryPoint == null)
        {
            Debug.LogError($"{nameof(DrillBit)} on {name} is missing a rotating world zero or entry point reference.");
            return false;
        }

        machineSpaceEntryPoint = rotatingWorldZero.InverseTransformPoint(entryPoint.position);
        return true;
    }

    void RemoveCurrentDrillFromSnap()
    {
        GrabbableInventoryObject snappedDrill = GetComponentInParent<GrabbableInventoryObject>();
        if (snappedDrill != null)
        {
            snappedDrill.RemoveIfSnapped();
            return;
        }

        RemoveFromSnap removeFromSnap = GameObject.Find("DrillBitRemoveButton")?.GetComponent<RemoveFromSnap>();
        if (removeFromSnap != null)
        {
            removeFromSnap.RemoveCurrentObject();
            return;
        }

        Debug.LogWarning($"{nameof(DrillBit)} on {name} could not find a way to remove the snapped drill.");
    }

    void LogAngleReset()
    {
        if (zRotator != null)
        {
            Debug.Log("Resetting angle bounds to " + -zRotator.angleBounds);
        }
        else
        {
            Debug.Log("Resetting angle bounds, but no z rotator is assigned.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Drillable"))
        {
            drill = other.gameObject.GetComponent<Drillable>();
            if (drill == null)
            {
                Debug.LogWarning($"{other.gameObject.name} is tagged Drillable but has no {nameof(Drillable)} component.");
                return;
            }
            //if (ClampMovement.Instance.isClamped)
            //{
                if (Spinning.Instance != null && Spinning.Instance.isSpinning)
                {
                    if (Spinning.Instance.SpinSpeed < drill.minSpinSpeed)
                    {
                        FailureState.Instance?.SystemFailure("Your drill was spinning too slowly!");
                    }
                    else if (Spinning.Instance.SpinSpeed > drill.maxSpinSpeed)
                    {
                        FailureState.Instance?.SystemFailure("Your drill was spinning too quickly!");
                    }
                    else
                    {
                        if (TryGetEntryPointInMachineSpace(out Vector3 machineSpaceEntryPoint))
                        {
                            drill.NewHole(drillThickness, machineSpaceEntryPoint, isCenterPointDrill);
                            newHoleStarted = true;
                        }
                    }

                }
                else if (Jogging.Instance != null && Jogging.Instance.isCurrentJogging)
                {
                    FailureState.Instance?.SystemFailure("Do not use the jogging controls to center your drill, you can break the machine.");
                }
                else if (justSnappedIn)
                {
                    // remove the drill!
                    string parentName = other.transform.parent != null ? other.transform.parent.gameObject.name : "(no parent)";
                    Debug.Log("other object is " + other.name + " of " + parentName);
                    RemoveCurrentDrillFromSnap();
                    FailureState.Instance?.SystemFailure("You didn't have enough space to put this drill in.");
                }
                else
                {
                    Debug.Log("Just snapped in is: " + justSnappedIn);
                    Debug.Log("Locking z rotation");
                    //storedAngleBounds = zRotator.minAngle;
                    //zRotator.minAngle = zRotator.storedRotation;
                    //Debug.Log("YOUR DRILL IS NOW BROKEN.");
                    //FailureState.Instance.SystemFailure("Your drill wasn't spinning when it touched the drillable piece.");
                }
            }
            //else
            //{
                //FailureState.Instance.SystemFailure("Your block wasn't clamped when you touched it with your drill.");
            //}

        //}
        else if (other.gameObject.CompareTag("Untagged"))
        {
            if (Spinning.Instance != null && Spinning.Instance.isSpinning)
            {
                Debug.Log("You should not be drilling that! Stop it now! It was the: " + other.gameObject.name);
                //FailureState.Instance.SystemFailure("Your spinning drill touched something it shouldn't. Did you forget the parallels?");
                drillingIntoMachineSource?.Play();
            }
            else if (justSnappedIn)
            {
                // remove the drill!
                string parentName = other.transform.parent != null ? other.transform.parent.gameObject.name : "(no parent)";
                Debug.Log("other object is " + other.name + " of "+ parentName);
                RemoveCurrentDrillFromSnap();

                FailureState.Instance?.SystemFailure("You didn't have enough space to put this drill in.");
            }
            else
            {
                //Debug.Log("Locking z rotation");
                //storedAngleBounds = zRotator.minAngle;
                //zRotator.minAngle = zRotator.storedRotation;
                Debug.Log("Your drill bit hit: " + other.gameObject.name);
                FailureState.Instance?.DisplayWarning("Your drill bit hit something, be careful.");
            }

        }
        else if (other.gameObject.CompareTag("Controller"))
        {
            if (Spinning.Instance != null && Spinning.Instance.isSpinning)
            {
                FailureState.Instance?.SystemFailure("You touched a spinning drill with your hands.");
            }
        }
    }

    private void OnEnable()
    {
        StartCoroutine(WeJustSnappedIn());
        PartActiveEvent?.Invoke(true);

    }
    private void OnDisable()
    {
        PartActiveEvent?.Invoke(false);
        print($"{gameObject.name} : disabled");

    }
    IEnumerator WeJustSnappedIn()
    {


        justSnappedIn = true;
        Debug.Log("We just snapped in!" + justSnappedIn);
        yield return new WaitForSeconds(1f);
        justSnappedIn = false;
        Debug.Log("The time has passed." + justSnappedIn);
    }
    

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Drillable"))
        {
            if (Spinning.Instance != null && Spinning.Instance.isSpinning)
            {      
                Debug.LogWarning("Drilled");
                CenterHoleDrilledEvent?.Invoke();
                if (drill == null)
                {
                    drill = other.gameObject.GetComponent<Drillable>();
                }

                if (drill == null)
                {
                    Debug.LogWarning($"{other.gameObject.name} is tagged Drillable but has no {nameof(Drillable)} component.");
                    return;
                }

                if (!TryGetEntryPointInMachineSpace(out Vector3 machineSpaceEntryPoint))
                {
                    return;
                }

                if (!newHoleStarted)
                {   
                    drill.NewHole(drillThickness, machineSpaceEntryPoint, isCenterPointDrill);
                }
                drill.UpdateHoleDepth(machineSpaceEntryPoint, this, isCenterPointDrill);
            }
            /*
            else
            {
                Debug.Log("Locking z rotation");
                storedAngleBounds = zRotator.angleBounds;
                zRotator.angleBounds = Mathf.Abs(zRotator.storedRotation);
            }
            */

        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Drillable"))
        {
            newHoleStarted = false;
            Drillable drillable = other.gameObject.GetComponent<Drillable>();
            if (Spinning.Instance != null && Spinning.Instance.isSpinning)
            {
                drillable?.DisplayHoleData();
                drillable?.SilenceSound();
                //Just in case, because of a bug
                LogAngleReset();
                //zRotator.minAngle = -zRotator.angleBounds;
            }
            else
            {
                LogAngleReset();
                //zRotator.minAngle = -zRotator.angleBounds;
                drillable?.SilenceSound();
            }

        }
        else if (other.gameObject.CompareTag("Untagged"))
        {
            LogAngleReset();
            //zRotator.minAngle = -zRotator.angleBounds;
        }

    }

    // Update is called once per frame
    void Update()
    {
            //Debug.Log("just snapped in is: " + justSnappedIn);
    }
}
