using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SnapZoneManager;

public class InteractableZone : MonoBehaviour
{
    public UnityEngine.Events.UnityEvent OnInteractionSuccess;
    public SnapObjectType[] interactableZoneInventory;
    Renderer rend;
    public bool throwErrorIfSpinning;

    public bool CheckIfInInventory(SnapObjectType potentialInteractingObject)
    {
        //check if the thing attempting to interact with us is something we respond to
        bool snapAvailable = false;
        foreach (SnapObjectType objectType in interactableZoneInventory)
        {
            if (potentialInteractingObject == objectType)
            {
                if(throwErrorIfSpinning && Spinning.Instance.isSpinning)
                {
                    FailureState.Instance.SystemFailure("You can't interact with this while it's spinning.");
                }
                else
                {
                    //we found the snap object type in the inventory of our interactable zone, meaning we can now Invoke our successful interaction event
                    snapAvailable = true;
                    OnInteractionSuccess?.Invoke();
                }
            }
        }
        return snapAvailable;
    }

    // Start is called before the first frame update
    void Start()
    {
        rend = GetComponent<Renderer>();
        SnapZoneManager.Instance.Hovering += OnHover;
    }

    void OnHover(bool isHovering)
    {
        if (gameObject.activeSelf)
        {
            rend.enabled = isHovering;
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
