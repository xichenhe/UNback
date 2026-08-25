using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ClampMovement : MonoBehaviour
{
    public static ClampMovement Instance;

    public enum ClampStatus { Loose, NeedsAWack, NeedsATestJiggle, FullySecure };
    public ClampStatus currentClampStatus;

    public MouseRotate viseRotator;
    public float rotationMultiplier;
    Vector3 zeroPosition;

    public Transform farPoint, nearPoint;
    public float yThickness;
    public bool isClamped;
    bool justExitedClamp;

    public bool needsAWack;
    public bool needsATestJiggle;
    public bool fullySecure;
    bool distanceAlreadyClamped;

    int totalJiggles;
    bool needAJiggle = false;
    bool wacked = false;
    bool isSecure = false;

    public SnapRestrictions parallelSnapRestrictions;
    private SnapRestrictions blockSnapRestrictions;

    float originalMouseRotateAngleBounds;

    public GameObject hammerWackInteractableZone;

    public GameObject box; // hardcoded used to set parallel removability, change when more blocks added
    public ParallelsReveal pr;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        zeroPosition = transform.localPosition;
        //turning off for 
        //isClamped = false;
        originalMouseRotateAngleBounds = viseRotator.angleBounds;
        distanceAlreadyClamped = false;
        totalJiggles = 0;
    }

    // Update is called once per frame
    void Update()
    {

        //Debug.Log("Clamp distance is: " + (farPoint.position - nearPoint.position).magnitude);
        //Debug.Log("Clamp distance is y: " + yThickness);
        if ((farPoint.position - nearPoint.position).magnitude < 0.113f && blockSnapRestrictions != null)// yThickness)
        {

            //Debug.Log("We are clamped now.");
            isClamped = true;
            parallelSnapRestrictions.removable = false;
            blockSnapRestrictions.removable = false;
            hammerWackInteractableZone.SetActive(true);
            if (!distanceAlreadyClamped)
            {
                /*
                 //back when we used anglebounds for limiting
                if(viseRotator.storedRotation >= 0f)
                {
                    viseRotator.angleBounds = viseRotator.storedRotation;
                }
                else
                {
                    viseRotator.angleBounds = -viseRotator.storedRotation;
                }
                */
                viseRotator.minAngle = viseRotator.storedRotation;
                distanceAlreadyClamped = true;
            }




            if (fullySecure)
            {
                if (parallelSnapRestrictions != null)
                {
                    parallelSnapRestrictions.removable = false;
                }
                if (blockSnapRestrictions != null)
                {
                    blockSnapRestrictions.removable = false;
                }
                hammerWackInteractableZone.SetActive(false);
            }
            else if (!needsATestJiggle)
            {
                currentClampStatus = ClampStatus.NeedsAWack;
                needsAWack = true;
            }

        }
        else
        {
            //Debug.Log("You can keep pushing if you want.");
            if (isClamped == true)
            {
                StopAllCoroutines();
                StartCoroutine(JustExitedClamp());
            }
            distanceAlreadyClamped = false;
            isClamped = false;
            needsATestJiggle = false;
            fullySecure = false;
            needsAWack = false;
            currentClampStatus = ClampStatus.Loose;
            hammerWackInteractableZone.SetActive(false);
            viseRotator.minAngle = -viseRotator.angleBounds;
            if (parallelSnapRestrictions != null)
            {
                if (!box.activeSelf)
                {
                    parallelSnapRestrictions.removable = true;
                }
                else
                {
                    parallelSnapRestrictions.removable = false;
                }

            }
            if (blockSnapRestrictions != null)
            {
                blockSnapRestrictions.removable = true;
            }

        }
        Vector3 newPos = new Vector3(viseRotator.storedRotation * rotationMultiplier, 0f, 0f);
        //Debug.Log(newPos);
        if ((zeroPosition + newPos).x < transform.localPosition.x && isClamped)
        {
            //Debug.Log("Tried to move forwards while clamped. Didn't. " + (zeroPosition+newPos).x + " compared to " + transform.localPosition.x);
        }
        else
        {
            transform.localPosition = zeroPosition + newPos;

        }

    }

    private void OnTriggerStay(Collider other)
    {
        //Debug.Log("Well, somethings inside me.");
        if (other.gameObject.CompareTag("Drillable"))
        {
            //in old implementation restricted removal of block
            //parallelSnapRestrictions = other.transform.parent.gameObject.GetComponent<SnapRestrictions>();
            blockSnapRestrictions = other.transform.parent.gameObject.GetComponent<SnapRestrictions>();
            //Debug.Log("Trying to push the drillable object.");
            yThickness = other.GetComponent<Drillable>().yDimensions;
            if (!isClamped && !justExitedClamp)
            {
                //Debug.Log("We aren't clamped, so we're gonna try to push.");
                other.transform.parent.position += Vector3.forward * Time.deltaTime * 0.1f;
            }
        }
    }

    IEnumerator JustExitedClamp()
    {
        totalJiggles = 0;
        justExitedClamp = true;
        yield return new WaitForSeconds(.1f);
        justExitedClamp = false;
    }

    public void DoTestJiggle()
    {
        if (needAJiggle) {
            needAJiggle = false;
            StartCoroutine(ReverseParallel());
        }   
    }

    public void DoHammerWack()
    {
        if (!wacked) {
            totalJiggles++;
            if (totalJiggles < 3)
            {
                float randomFloat = Random.Range(0f, 2f);
                isSecure = randomFloat > 1f;
            }
            else
            {
                isSecure = true;
            }
            wacked = true;
        }
        currentClampStatus = ClampStatus.NeedsATestJiggle;
        needsAWack = false;
        needsATestJiggle = true;

        if (isSecure)
        {
            pr.SlideSecuredParallel();
        }
        else {
            pr.SlideParallel();
        }

        needAJiggle = true;
    }

    IEnumerator ReverseParallel()
    {
        wacked = false;
        yield return new WaitForSeconds(1f);
        pr.ReverseParallel();
        //Debug.Log("Random float was: " + randomFloat);
        if (isSecure)
        {
            fullySecure = true;
            needsATestJiggle = false;
            needsAWack = false;
            currentClampStatus = ClampStatus.FullySecure;
        }
        else
        {
            fullySecure = false;
            needsATestJiggle = false;
            needsAWack = true;
            currentClampStatus = ClampStatus.NeedsAWack;

        }
        yield return null;
    }
}
