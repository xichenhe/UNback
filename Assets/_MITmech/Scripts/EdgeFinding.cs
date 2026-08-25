using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static SnapZoneManager;

public class EdgeFinding : MonoBehaviour
{
    public Transform parentTrans;
    public GameObject notSpinning;
    public Transform spinningSource;
    public float maximumOffset;
    public bool isPopped;
    public float sensitivityMultiplier;
    public float popOutDistance; // not used anymore
    Vector3 centerPos;
    Vector3 notSpinningPos;

    public UnityEvent popOutEvent;
    public UnityEvent resetEvent;

    public float minRPM, maxRPM;

    //public RemoveFromSnap removeFromSnap;
    public SnapZone sz;
    bool lastFrameSnapped;

    //public GameObject resetButton;

    private GameObject objTouched;
    public bool warnedBlockInEdgefinder; // this warning should only show up once per this mistake

    bool issetup = false;

    // Start is called before the first frame update
    void Start()
    {
        warnedBlockInEdgefinder = false;
        isPopped = false;
        centerPos = transform.localPosition;
        Debug.Log("Center position is: " + centerPos.x + " " +centerPos.y+ " " +centerPos.z);
         issetup = true;
        //ResetPop();


    }

    private void OnTriggerStay(Collider other)
    {
        if(other.name != "DrillableTriggerBox")
            return;
        // only if not yet popped 
        if (!isPopped)
        {
            //Debug.Log("Woah we're inside " + other.gameObject.name);
            if (other.CompareTag("Drillable"))
            {
                if (Spinning.Instance.SpinSpeed < minRPM-1)
                {
                    FailureState.Instance.DisplayWarning("Increase RPM to at least " + minRPM + " RPM. The edgefinder may run over the stock.");
                }
                else if (Spinning.Instance.SpinSpeed > maxRPM+1)
                {
                    FailureState.Instance.DisplayWarning("Reduce RPM to at least " + maxRPM + " RPM. Tool may break");
                }

                //Debug.Log("transform x is: " + transform.localPosition.x + " while we're subrtracting: " + Time.deltaTime * sensitivityMultiplier);
                float newX = transform.localPosition.x - Time.deltaTime * sensitivityMultiplier;

                if (newX < 0f)
                {
                    Debug.Log("We're gonna pop out.");
                    newX = 0f;
                    //temporary, just to make sure things are working right
                    //other.GetComponent<Drillable>().NewHole(0, transform.position, true);
                    objTouched = other.gameObject;
                    PopOut();
                }
                else
                {
                    //Debug.Log("New X is: " + newX);
                    transform.localPosition = new Vector3(newX, transform.localPosition.y, transform.localPosition.z);
                    //Debug.Log("Currently interacting with drillable object! New offset is: " + newX);
                }

            }
        }

        if (other.CompareTag("Drillable"))
        {
            if (!Spinning.Instance.isSpinning && !warnedBlockInEdgefinder)
            {
                FailureState.Instance.DisplayWarning("Do not move the block into the edge finder, it may break.");
                warnedBlockInEdgefinder = true;
            }
        }
    }

    public void PopOut()
    {

        Debug.Log("Yeah, that's close enough, we're popping out.");
        Debug.Log("popout old x: " + transform.localPosition.x);

        notSpinning.SetActive(true);
        isPopped = true;
        lastFrameSnapped = true;
        // hide current mesh
        gameObject.GetComponent<Renderer>().enabled = false;
        //gameObject.GetComponent<CapsuleCollider>().enabled = false;

        popOutEvent.Invoke();

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Controller"))
        {
            if (Spinning.Instance.isSpinning)
            {
                FailureState.Instance.SystemFailure("You touched the spinning edgefinder with your hands.");
            }
        }
        if (other.gameObject.CompareTag("Drillable"))
        {
            if (Spinning.Instance.isSpinning && !Spinning.Instance.IsLocked)
            {
                FailureState.Instance.SystemFailure("You tried to edge find before locking the quill. This may lead to inaccurate results.");
            }
        }

    }

    public void ResetPop()
    {
        if (issetup)
        {
            //resetButton.SetActive(false);
            transform.localPosition = new Vector3(centerPos.x + maximumOffset, centerPos.y, centerPos.z); //hardcoded, might have to be changed after scale adjust
            isPopped = false;
            gameObject.GetComponent<Renderer>().enabled = true;
            warnedBlockInEdgefinder = false;
            //gameObject.GetComponent<CapsuleCollider>().enabled = true;

            resetEvent.Invoke();
        }
    }

    private void Update()
    {
        if (sz.currentlySnapped != SnapObjectType.EdgeFinder && lastFrameSnapped)
        {
            notSpinning.GetComponent<EdgeFinderPopOut>().Reset();
            lastFrameSnapped = false;
            Debug.Log("reset");
        }
        }

   

    private void OnEnable()
    {
        ResetPop();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Drillable") && !Spinning.Instance.isSpinning && !isPopped)
        {
            warnedBlockInEdgefinder = false;
        }
    }
}
