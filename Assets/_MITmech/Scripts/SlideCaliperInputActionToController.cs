using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;


// TODO CHANGE PRIMARY BUTTON INPUT AXIS TO GRIP BUTTON FOR SLIDING CONTROLLER AND TEST ON HEADSET

public class SlideCaliperInputActionToController : MonoBehaviour
{
    //Input Actions from both controllers that bind to grip buttons for "grabbing"
    //the adjustment jaws of the caliper without actually using the XR interactable
    //The controller that is not gripping the calipers just has to be within 
    //a distance threshold next to the calipers display and it has to have its
    //grip pressed as well to slide the calipers
    public InputActionReference slideLeftGripButtonReference;
    public InputActionReference slideRightGripButtonReference;
    
    //Constants to change if needed
    private const float zeroJawSlidePosition = 0.0f;    //The value of the caliper slide axis position of the adjustment jaw that closes the calipers
    public const float maxJawSlidePosition = 0.15f;    //Maximum extension of the calipers (This will be negated later because the slide axis points in oppostie direction)
    public bool adjustmentJawSnappedToMeasurement = false;
    
    public TextMeshProUGUI measurementTextMeshProObj;   //component that gets modified for caliper measurement display
    public GameObject inchesToMillimetersButton;

    public Transform grabPointTransformFixed;   //This transform is a child of the calipers, but not the adjustment jaws, so it doesn't move with the adj jaws
    public Transform grabPointTransformSliding;     //This transform is a child of the adjustment jaws, used so that you can check if the other controller is close enough
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable caliperInteractable;
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor leftXRControllerDirectInteractor;
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor rightXRControllerDirectInteractor;
    
    [SerializeField] private float mmToInchesConversionFactor = 0.0393700787f;
    public float distanceBetweenGrabPointAndOtherControllerThreshold = 0.2f; //Threshold for when to track adjustment jaw with other controller if it grips(I think these constants are in meters for all intents and purposes hopefully no scaling issues occur)

    private float slidingGrabPointToControllerDistance;
    private float snapSlidingGrabPointToControllerDistance;

    public float GetCaliperMeasurementMeters(){
        return Mathf.Abs(slidingGrabPointToControllerDistance);
    }

    public bool SetCaliperSnapMeasurementMeters(float setDistance){
        adjustmentJawSnappedToMeasurement = true;
        snapSlidingGrabPointToControllerDistance = -Mathf.Abs(setDistance);
        //returns a check to see that adjustment jaw has actually snapped to the correct position
        return (Mathf.Clamp(snapSlidingGrabPointToControllerDistance, -Mathf.Abs(maxJawSlidePosition), zeroJawSlidePosition) == transform.localPosition.x);
    }

    void Update()
    {
        
        Vector3 vectorFromSlidingGrabPointToLeftController = (grabPointTransformFixed.InverseTransformPoint(leftXRControllerDirectInteractor.transform.position));
        Vector3 vectorFromSlidingGrabPointToRightController = (grabPointTransformFixed.InverseTransformPoint(rightXRControllerDirectInteractor.transform.position));

        bool leftGripButtonValue = (slideLeftGripButtonReference.action.ReadValue<float>() > 0f ? true : false);
        bool rightGripButtonValue = (slideRightGripButtonReference.action.ReadValue<float>() > 0f ? true : false);

        bool leftControllerCloseToSlidingGrabPoint = (grabPointTransformSliding.InverseTransformPoint(leftXRControllerDirectInteractor.transform.position)).magnitude < distanceBetweenGrabPointAndOtherControllerThreshold;
        bool rightControllerCloseToSlidningGrabPoint = (grabPointTransformSliding.InverseTransformPoint(rightXRControllerDirectInteractor.transform.position)).magnitude < distanceBetweenGrabPointAndOtherControllerThreshold;

        //Check if there is an interactor selecting and choose the other controller to slide calipers if conditions are met
        if(caliperInteractable.interactorsSelecting.Count > 0){
            if(leftGripButtonValue && leftControllerCloseToSlidingGrabPoint && caliperInteractable.interactorsSelecting[0].transform.name == rightXRControllerDirectInteractor.name){
                slidingGrabPointToControllerDistance = Mathf.Clamp(vectorFromSlidingGrabPointToLeftController.x, -Mathf.Abs(maxJawSlidePosition), zeroJawSlidePosition);
            }else if(rightGripButtonValue && rightControllerCloseToSlidningGrabPoint && caliperInteractable.interactorsSelecting[0].transform.name == leftXRControllerDirectInteractor.name){
                slidingGrabPointToControllerDistance = Mathf.Clamp(vectorFromSlidingGrabPointToRightController.x, -Mathf.Abs(maxJawSlidePosition), zeroJawSlidePosition);
            }
        }
        //adjustment jaw snapped variable tells whether the adjustment jaw is snapped to a measurement of a hole dimeter, hole position, or TODO: hole depth
        if(!adjustmentJawSnappedToMeasurement){
            //Use the input action from the non selecting controller for sliding
            if(Mathf.Abs(snapSlidingGrabPointToControllerDistance - slidingGrabPointToControllerDistance) < 0.01f){
                snapSlidingGrabPointToControllerDistance = slidingGrabPointToControllerDistance;
                transform.localPosition = new Vector3(slidingGrabPointToControllerDistance, transform.localPosition.y, transform.localPosition.z);
            }else{
                transform.localPosition = new Vector3(Mathf.Clamp(snapSlidingGrabPointToControllerDistance, -Mathf.Abs(maxJawSlidePosition), zeroJawSlidePosition), transform.localPosition.y, transform.localPosition.z);
            }
        }else{
            //SetCaliperSnapMeasurementMeters with another class or event to fix the adjustment jaws into a measurement (threshold for when to stop snapping must be
            //controlled by the script that executes SetCaliperSnapMeasurementMeters) and to stop snapping to a measurement set adjustmentJawSnappedToMeasurement to false in another script
            transform.localPosition = new Vector3(Mathf.Clamp(snapSlidingGrabPointToControllerDistance, -Mathf.Abs(maxJawSlidePosition), zeroJawSlidePosition), transform.localPosition.y, transform.localPosition.z);
        }
    
        //Sets the value of the measurement DRO to inches or mm
        if(inchesToMillimetersButton.activeInHierarchy){
            measurementTextMeshProObj.text = (mmToInchesConversionFactor*1000f*Mathf.Abs(transform.localPosition.x)).ToString(((inchesToMillimetersButton.activeInHierarchy) ? "F3" : "F2"));
        }else{
            measurementTextMeshProObj.text = (1000f*Mathf.Abs(transform.localPosition.x)).ToString(((inchesToMillimetersButton.activeInHierarchy) ? "F3" : "F2"));
        }
    }
}