using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class GrabRotate : MonoBehaviour
{
    public enum WhichHand { Left, Right, Neither};
    public WhichHand whichHandRotating;

    public MouseRotate mouseRotate;
    public InputActionReference leftGripReference, rightGripReference;
    public Transform leftHandTransform, rightHandTransform;
    bool leftGripping, rightGripping, leftInRange, rightInRange;
    UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor leftRay, rightRay;

    // Start is called before the first frame update
    void Start()
    {
        leftGripping = false;
        rightGripping = false;
        leftInRange = false;
        rightInRange = false;

        rightRay = rightHandTransform.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
        leftRay = leftHandTransform.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();

        Debug.Log("Left ray is: " + leftRay + " and right ray is: " + rightRay);

        whichHandRotating = WhichHand.Neither;

        leftGripReference.action.started += LeftGripStarted;
        leftGripReference.action.canceled += LeftGripCanceled;
        rightGripReference.action.started += RightGripStarted;
        rightGripReference.action.canceled += RightGripCanceled;

        
    }

    private void OnDestroy()
    {
        leftGripReference.action.started -= LeftGripStarted;
        leftGripReference.action.canceled -= LeftGripCanceled;
        rightGripReference.action.started -= RightGripStarted;
        rightGripReference.action.canceled -= RightGripCanceled;
    }

    void LeftGripStarted(InputAction.CallbackContext ctx)
    {
        leftGripping = true;
        if (leftInRange)
        {
            if (whichHandRotating == WhichHand.Neither)
            {
                whichHandRotating = WhichHand.Left;
                leftRay.enabled = false;
            }
        }

    }

    void LeftGripCanceled(InputAction.CallbackContext ctx)
    {
        leftGripping = false;
        leftRay.enabled = true;
        if (whichHandRotating == WhichHand.Left)
        {
            whichHandRotating = WhichHand.Neither;
        }
    }

    void RightGripStarted(InputAction.CallbackContext ctx)
    {
        rightGripping = true;
        if (rightInRange)
        {
            if (whichHandRotating == WhichHand.Neither)
            {
                whichHandRotating = WhichHand.Right;
                rightRay.enabled = false;
            }
        }

    }

    void RightGripCanceled(InputAction.CallbackContext ctx)
    {
        rightGripping = false;
        rightRay.enabled = true;
        if (whichHandRotating == WhichHand.Right)
        {
            whichHandRotating = WhichHand.Neither;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.transform == rightHandTransform)
        {
            rightInRange = true;
        }
        else if(other.transform == leftHandTransform)
        {
            leftInRange = true;
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform == rightHandTransform)
        {
            rightInRange = false;
        }
        else if (other.transform == leftHandTransform)
        {
            leftInRange = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(whichHandRotating == WhichHand.Left)
        {
            //Debug.Log("Left hand rotating.");
            mouseRotate.GrabRotate(leftHandTransform);
        }
        else if(whichHandRotating == WhichHand.Right)
        {
            //Debug.Log("Right hand rotating.");
            mouseRotate.GrabRotate(rightHandTransform);
        }
    }
}
