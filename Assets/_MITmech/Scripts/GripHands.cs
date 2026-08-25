using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GripHands : MonoBehaviour
{
    public InputActionReference[] gripReferences;
    Animator anim;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        foreach(InputActionReference gripReference in gripReferences)
        {
            gripReference.action.started += GripStarted;
            gripReference.action.canceled += GripCancelled;
        }
    }

    private void OnDestroy()
    {
        foreach (InputActionReference gripReference in gripReferences)
        {
            gripReference.action.started -= GripStarted;
            gripReference.action.canceled -= GripCancelled;
        }
    }

    void GripStarted(InputAction.CallbackContext ctx)
    {
        anim.SetBool("GripPressed", true);
    }

    void GripCancelled(InputAction.CallbackContext ctx)
    {
        anim.SetBool("GripPressed", false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
