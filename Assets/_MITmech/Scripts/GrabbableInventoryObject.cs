using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;
using UnityEngine.Events;

public class GrabbableInventoryObject : MonoBehaviour
{

    [Header("Tool Snapping")]
    public SnapZoneManager.SnapObjectType inventoryObjectType;
    public Transform corkboardOriginalPosition;

    SnapZone mostRecentSnapZone;
    public bool hasRecentZone = false;
    HoleManager mostRecentHole;

    public SnapZone desiredSnapZone;
    public InteractableZone desiredInteractableZone;
    SnapRestrictions mostRecentSnapRestrictions;
    MeshRenderer[] rend;
    public bool teleportToCorkboard;
    bool isSnapped;
    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteract;
    Transform copyTransform;
    public GameObject addedText;
    public CycleTextObject cto;

    [Header("Interaction Managment")]
    // for gravity managing
    public bool hasGravity = false;

    // for tutorial
    public ParallelsReveal pr;
    public MetalShavingManager msm;
    public TutorialStepButtonPressed bp;

    bool interactionbuffer = false;

    [Header("Tool Animation")]
    public Animator anim;
    public Transform animationAnchor;
    public bool isPlayingAnimation = false;

    public bool matchRotation = true;

    // only for playing animations on release
    public bool dropInteractable = false;
    bool interactWhenRelease = false;


    // Start is called before the first frame update

    public UnityEvent toolPickedUp;
    public UnityEvent toolSnapped;

    public void ToolSelected()
    {
        toolPickedUp?.Invoke();
    }
    void Start()
    {
        rend = GetComponentsInChildren<MeshRenderer>();
        grabInteract = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        //anim = GetComponent<Animator>();
        teleportToCorkboard = true;
        if(addedText != null)
        {
            addedText.SetActive(true);
        }

    }

    private void OnTriggerEnter(Collider other)
    {    
        if (other.gameObject.CompareTag("SnapZone"))
        {
            //find the new snap zone.
            SnapZone newSnapZone = other.gameObject.GetComponent<SnapZone>();

            //check if our object can even snap into this snap zone.{
            bool replaceSnapZone = false;
            foreach(var dictionaryItem in newSnapZone.inventoryObjectModelDictionary)
            {
                if(dictionaryItem.Key == inventoryObjectType)
                {
                    replaceSnapZone = true;
                }
            }

            if (replaceSnapZone)
            {
                //disable the renderer on the old snap zone
                if (mostRecentSnapZone != null && mostRecentSnapZone != newSnapZone)
                {
                    mostRecentSnapZone.OnHover(false);
                }

                mostRecentSnapZone = newSnapZone;
                // if most recent snapzone is the chuck snapzone and nothing is snapped, set current object to snaptochuck in spinning instance
                if (Spinning.Instance.snappedToChuck == null && newSnapZone == Spinning.Instance.chuckSnapZone) {
                    Spinning.Instance.snappedToChuck = this;
                }
                hasRecentZone = true;
                Debug.Log("Found the snap zone on: " + mostRecentSnapZone.gameObject.name);
                //if we're grabbing an object, we should enable the renderer on the snap zone we're selecting
                if (grabInteract.isSelected)
                {
                    mostRecentSnapZone.OnHover(true);
                    Debug.Log("Called on hover on " + mostRecentSnapZone);
                }
            }
        }
        else if (other.gameObject.CompareTag("InteractableZone") && !dropInteractable)
        {
            
            if (other.gameObject.GetComponent<InteractableZone>().CheckIfInInventory(inventoryObjectType))
            {
                Debug.Log("successful interaction!");

            }
        }
        else if (other.gameObject.CompareTag("InteractableZone"))
        {
            //find the new snap zone.
            InteractableZone newInteractableZone = other.gameObject.GetComponent<InteractableZone>();
            if (newInteractableZone = desiredInteractableZone)
            {
                Debug.Log(name+" setting renderer to true");
                other.gameObject.GetComponent<Renderer>().enabled = true;
                interactWhenRelease = true;
            }
        }
        else if (other.gameObject.CompareTag("Hole")) {
            if (inventoryObjectType == SnapZoneManager.SnapObjectType.DeburringTool) {
                mostRecentHole = other.gameObject.GetComponent<HoleManager>();
                mostRecentHole.Select();
                interactWhenRelease = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("SnapZone"))
        {
            if (other.gameObject.GetComponent<SnapZone>() == mostRecentSnapZone)
            {
                mostRecentSnapZone.OnHover(false);
                Debug.Log("Unassigning snap zone from " + mostRecentSnapZone.gameObject.name);
                mostRecentSnapZone = null;

                if (desiredSnapZone == Spinning.Instance.chuckSnapZone)
                {
                    Spinning.Instance.snappedToChuck = null;
                }

                hasRecentZone = false;
            }
        } else if (other.gameObject.CompareTag("InteractableZone") && dropInteractable) {
            Debug.Log("setting renderer to false");
            other.gameObject.GetComponent<Renderer>().enabled = false;
            interactWhenRelease = false;
        }
        else if (other.gameObject.CompareTag("Hole"))
        {
            if (inventoryObjectType == SnapZoneManager.SnapObjectType.DeburringTool)
            {
                interactWhenRelease = false;
                mostRecentHole.Unselect();
                mostRecentHole = null;
            }
        }
    }


    // trigger when release
    // on objects that should only snap if additional operations needed
    public void GravityWhenDrop()
    {
        // debur tool and chuck key
        if (dropInteractable && interactWhenRelease) {
            if (inventoryObjectType == SnapZoneManager.SnapObjectType.ChuckKey && !interactionbuffer)
            {
                if (desiredInteractableZone.GetComponent<InteractableZone>().CheckIfInInventory(inventoryObjectType))
                {
                    Debug.Log("successful interaction!");
                    isPlayingAnimation = true;
                    Quaternion tempRotation = anim.gameObject.transform.rotation;
                    if (!cto.isTight)
                    {
                        StartCoroutine(TurnCounterClockwise());
                        if (Spinning.Instance.snappedToChuck != null)
                        {
                            Spinning.Instance.snappedToChuck.RemoveIfSnapped();
                        }
                        Spinning.Instance.snappedToChuck = null;

                    }
                    else
                    {
                        StartCoroutine(TurnClockwise());
                        //Debug.Log("Snapping...");
                        if (Spinning.Instance.snappedToChuck != null)
                        {
                            //Debug.Log("Snapping..." + Spinning.Instance.snappedToChuck.name);
                            Spinning.Instance.snappedToChuck.SnapWhenInteract();
                        }
                    }
                    anim.gameObject.transform.rotation = tempRotation;

                }
                teleportToCorkboard = true;
            }
            else if (inventoryObjectType == SnapZoneManager.SnapObjectType.ChuckKey) {
                StartCoroutine(Gravity());
            } else if (inventoryObjectType == SnapZoneManager.SnapObjectType.DeburringTool)
            {  // play deburring animation        
                if (mostRecentHole != null)
                {
                    Debug.Log("successful interaction!");
                    isPlayingAnimation = true;
                    Quaternion tempRotation = anim.gameObject.transform.rotation;
                    Vector3 tempPosition = anim.gameObject.transform.position;
                    animationAnchor = mostRecentHole.gameObject.transform;

                    StartCoroutine(Debur());

                    anim.gameObject.transform.rotation = tempRotation;
                    anim.gameObject.transform.position = tempPosition;
                    mostRecentHole.UpdateHoleDebur();
                }
                teleportToCorkboard = true;
            }
        }
        else if (dropInteractable) {
            Debug.Log("we are not in the interactable zone.");
            // start gravity coroutine
            StartCoroutine(Gravity());
        }
        // block and parallel 
        else if (inventoryObjectType == SnapZoneManager.SnapObjectType.SixByFourSteelBlock || inventoryObjectType == SnapZoneManager.SnapObjectType.Parallels) {

            Debug.Log("Checking if in snapzone.");
            mostRecentSnapZone?.OnHover(false);
            if (mostRecentSnapZone != null)
            {
                // add snap restrictions here
                // if currently snapping block, and parallel havent been snapped in, return error
                if (inventoryObjectType == SnapZoneManager.SnapObjectType.SixByFourSteelBlock)
                {
                    if (!pr.revealed)
                    { // temp
                        teleportToCorkboard = true;
                        FailureState.Instance.SystemFailure("Did you forget the parallels? Not using the parallels will lead to inaccurate results.");
                        return;
                    }
                }
                if (inventoryObjectType == SnapZoneManager.SnapObjectType.Parallels)
                {
                    if (msm.hasShaving)
                    { // temp
                        teleportToCorkboard = true;
                        FailureState.Instance.SystemFailure("Remeber to use the brush and clean the metal shavings first, or your drilling may be inaccurate.");
                        return;
                    }
                }
                SnapWhenInteract();
            }
            else {
                Debug.Log("We don't have a most recent snap zone.");
                // start gravity coroutine
                StartCoroutine(Gravity());
            }
            
                
        }
        // snappable tools and mallet and brush
        else {
            Debug.Log("Checking if in snapzone.");
            mostRecentSnapZone?.OnHover(false);
            if (!isSnapped)
            {
                Debug.Log("We don't have a most recent snap zone.");
                // start gravity coroutine
                StartCoroutine(Gravity());
            }
        }
        
    }

    // perform additional operatio on objects to snap them
    public void SnapWhenInteract()
    {
        toolSnapped.Invoke();
    
        Debug.Log("Most recent snap zone is: " + mostRecentSnapZone);
        WarnOnEnable isWarnedOnEnable = mostRecentSnapZone.inventoryObjectModelDictionary[inventoryObjectType].transform.GetComponent<WarnOnEnable>();
        bool isSnappable;
        isSnappable = mostRecentSnapZone.CheckIfInInventory(inventoryObjectType);
    
        if (isWarnedOnEnable == null)
        {
            Debug.Log("There's no warning on the potential snap.");
        }
        else
        {
            Debug.Log("We found a warning on the potential snap.");
            isSnappable = false;
        }

        Debug.Log("isSnappable is: " + isSnappable);

        if (isSnappable)
        {
            teleportToCorkboard = false;
            Debug.Log("Snapping in the: " + inventoryObjectType.ToString());
        
            foreach (Renderer arend in rend)
            {
                arend.enabled = false;
            }
        
            copyTransform = mostRecentSnapZone.inventoryObjectModelDictionary[inventoryObjectType].transform;
            mostRecentSnapRestrictions = mostRecentSnapZone.inventoryObjectModelDictionary[inventoryObjectType].GetComponent<SnapRestrictions>();
            isSnapped = true;
        
            if (addedText != null)
            {
                addedText.SetActive(false);
            }

            // ============= LOGGING ADDED HERE =============
            EventLogger.Instance?.Log("tool_snapped", 
                $"tool_{inventoryObjectType}|zone_{mostRecentSnapZone.gameObject.name}|success_true");
            // ============================================
        }
        else
        {
            Debug.Log("We were not snappable, teleporting back to corkboard.");
            StartCoroutine(Gravity());

            // ============= LOGGING ADDED HERE =============
            EventLogger.Instance?.Log("tool_snap_failed", 
                $"tool_{inventoryObjectType}|zone_{mostRecentSnapZone?.gameObject.name ?? "none"}|success_false");
            // ============================================
        }
    }


    // on objects that should snap when it enters the snapzone
    // to add gravity, repleace all releport to corkboard = true to  StartCoroutine(Gravity());
    /*
    public void CheckIfInSnapzone()
    {
        if (dropInteractable && interactWhenRelease) {
            // play chuck key animation
            
            if (inventoryObjectType == SnapZoneManager.SnapObjectType.ChuckKey) {
                if (desiredInteractableZone.GetComponent<InteractableZone>().CheckIfInInventory(inventoryObjectType))
                {
                    Debug.Log("successful interaction!");
                    isPlayingAnimation = true;
                    Quaternion tempRotation = anim.gameObject.transform.rotation;
                    if (!cto.isTight)
                    {
                        StartCoroutine(TurnCounterClockwise());
                    }
                    else {
                        StartCoroutine(TurnClockwise());
                    }
                    anim.gameObject.transform.rotation = tempRotation;
                    teleportToCorkboard = true;
                }
            } 
            else if (inventoryObjectType == SnapZoneManager.SnapObjectType.DeburringTool) {  // play deburring animation        
                if (mostRecentHole != null) {
                    Debug.Log("successful interaction!");
                    isPlayingAnimation = true;
                    Quaternion tempRotation = anim.gameObject.transform.rotation;
                    Vector3 tempPosition = anim.gameObject.transform.position;
                    animationAnchor = mostRecentHole.gameObject.transform;
                    
                    StartCoroutine(Debur());

                    anim.gameObject.transform.rotation = tempRotation;
                    anim.gameObject.transform.position = tempPosition;
                    mostRecentHole.UpdateHoleDebur();
                }
                teleportToCorkboard = true;
            }
        }
        else
        {
            Debug.Log("Checking if in snapzone.");
            mostRecentSnapZone?.OnHover(false);
            if (mostRecentSnapZone != null)
            {
                // add snap restrictions here
                // if currently snapping block, and parallel havent been snapped in, return error
                if (inventoryObjectType == SnapZoneManager.SnapObjectType.SixByFourSteelBlock)
                {
                    if (!pr.revealed)
                    { // temp
                        teleportToCorkboard = true;
                        FailureState.Instance.SystemFailure("Did you forget the parallels? Not using the parallels will lead to inaccurate results.");
                        return;
                    }
                }
                if (inventoryObjectType == SnapZoneManager.SnapObjectType.Parallels)
                {
                    if (msm.hasShaving)
                    { // temp
                        teleportToCorkboard = true;
                        FailureState.Instance.SystemFailure("Remeber to use the brush and clean the metal shavings first, or your drilling may be inaccurate.");
                        return;
                    }
                }
                Debug.Log("Most recent snap zone is: " + mostRecentSnapZone);
                WarnOnEnable isWarnedOnEnable = mostRecentSnapZone.inventoryObjectModelDictionary[inventoryObjectType].transform.GetComponent<WarnOnEnable>();
                //Debug.Log("Detected warnOnEnable on: " + isWarnedOnEnable);
                bool isSnappable;
                isSnappable = mostRecentSnapZone.CheckIfInInventory(inventoryObjectType);
                //checking for if theres actually a warning on the object we're enabling
                if (isWarnedOnEnable == null)
                {
                    Debug.Log("There's no warning on the potential snap.");

                }
                else
                {
                    Debug.Log("We found a warning on the potential snap.");
                    isSnappable = false;
                }

                Debug.Log("isSnappable is: " + isSnappable);

                if (isSnappable)
                {
                    teleportToCorkboard = false;
                    Debug.Log("Snapping in the: " + inventoryObjectType.ToString());
                    foreach (Renderer arend in rend)
                    {
                        arend.enabled = false;
                    }
                    copyTransform = mostRecentSnapZone.inventoryObjectModelDictionary[inventoryObjectType].transform;
                    mostRecentSnapRestrictions = mostRecentSnapZone.inventoryObjectModelDictionary[inventoryObjectType].GetComponent<SnapRestrictions>();
                    isSnapped = true;
                    if (addedText != null)
                    {
                        addedText.SetActive(false);
                    }

                }
                else
                {
                    Debug.Log("We were not snappable, teleporting back to corkboard.");
                    teleportToCorkboard = true;
                }
            }
            else
            {
                Debug.Log("We don't have a most recent snap zone.");
                teleportToCorkboard = true;
            }
        }

    }*/

    public void RemoveIfSnapped()
    {
        OnHoverEnd();
        teleportToCorkboard = false;
    
        if(addedText != null)
        {
            addedText.SetActive(true);
        }

        if (hasGravity) 
        {
            StopCoroutine(Gravity());
            hasGravity = false;
            GetComponent<Rigidbody>().isKinematic = true;
            GetComponent<Rigidbody>().useGravity = false;
            teleportToCorkboard = false;
        }

        if (isSnapped) 
        {
            mostRecentSnapZone = desiredSnapZone;
        }

        if (mostRecentSnapZone != null)
        {
            if (mostRecentSnapZone.inventoryObjectModelDictionary[inventoryObjectType].GetComponent<SnapRestrictions>().removable)
            {
                mostRecentSnapZone.CheckIfInInventory(SnapZoneManager.SnapObjectType.Empty);
            
                foreach (Renderer arend in rend)
                {
                    arend.enabled = true;
                }
            
                isSnapped = false;
                mostRecentSnapRestrictions = null;
                mostRecentSnapZone = null;
                teleportToCorkboard = true;

                // ============= LOGGING ADDED HERE =============
                EventLogger.Instance?.Log("tool_removed", 
                    $"tool_{inventoryObjectType}|removed_successfully_true");
                // ============================================
            }
            else
            {
                Debug.Log("You can't remove that yet.");
            
                // ============= LOGGING ADDED HERE =============
                EventLogger.Instance?.Log("tool_remove_failed", 
                    $"tool_{inventoryObjectType}|reason_not_removable");
                // ============================================
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isSnapped)
        {
            //check if we should still be snapped by checking that snap zone
            if (mostRecentSnapZone != null)
            {
                if (desiredSnapZone.currentlySnapped == inventoryObjectType)
                {
                    teleportToCorkboard = false;
                    mostRecentSnapZone = desiredSnapZone;
                    //Debug.Log("We are snapped, match position, set grab interactable to: " + mostRecentSnapRestrictions.removable);
                    grabInteract.enabled = mostRecentSnapRestrictions.removable;

                    transform.position = copyTransform.position;
                    Debug.Log("copying transformation: " + copyTransform.rotation);
                    if (matchRotation) {
                        transform.rotation = copyTransform.rotation;
                    }
                }
                else
                {

                    isSnapped = false;
                    //RemoveIfSnapped();
                    foreach (Renderer arend in rend)
                    {
                        arend.enabled = true;
                    }
                    teleportToCorkboard = true;
                    mostRecentSnapRestrictions = null;
                }
            }
        }
        else if (isPlayingAnimation) {
            transform.position = animationAnchor.transform.position;
            transform.rotation = animationAnchor.transform.rotation;
        }
        else if (teleportToCorkboard)
        {
            isSnapped = false;
            grabInteract.enabled = true;
            transform.position = corkboardOriginalPosition.position;
            transform.rotation = corkboardOriginalPosition.rotation;
        }
        /*
        if (!grabInteract.isSelected)
        {
            mostRecentSnapZone.OnHover(false);
        }
        */
        //debugtext.text += "end reached"


    }

    public void OnHoverStart()
    {
        if (teleportToCorkboard)
        {
            anim?.SetBool("IsHovered", true);
        }
    }

    public void OnHoverEnd()
    {
        anim?.SetBool("IsHovered", false);
    }

    IEnumerator TurnClockwise()
    {
        while (true)
        {
            interactionbuffer = true;
            anim.Play("Clockwise Turn");
            yield return new WaitForSeconds(1f);
            //anim.Play("Empty");
            isPlayingAnimation = false;
            yield return new WaitForSeconds(2.5f);
            interactionbuffer = false;
            yield break;
        }
    }
    IEnumerator TurnCounterClockwise()
    {
        while (true)
        {
            interactionbuffer = true;
            anim.Play("Counter Clockwise Turn");
            yield return new WaitForSeconds(1f);
            //anim.Play("Empty");
            isPlayingAnimation = false;
            yield return new WaitForSeconds(2.5f);
            interactionbuffer = false;
            yield break;
        }
    }

    IEnumerator Debur()
    {
        while (true)
        {
            anim.Play("Debur");
            yield return new WaitForSeconds(1f);
            //anim.Play("Empty");
            isPlayingAnimation = false;
            bp.LongPress();
            yield break;
        }
    }

    IEnumerator Gravity(){
        hasGravity = true;
        GetComponent<Rigidbody>().isKinematic = false;
        GetComponent<Rigidbody>().useGravity = true;

        yield return new WaitForSeconds(1f);

        hasGravity = false;
        GetComponent<Rigidbody>().isKinematic = true;
        GetComponent<Rigidbody>().useGravity = false;

        teleportToCorkboard = true;

        yield break;
    }

    IEnumerator InteractionBuffer() {
        interactionbuffer = true;
        yield return new WaitForSeconds(1f);
        interactionbuffer = false;
        yield break;

    }
}
