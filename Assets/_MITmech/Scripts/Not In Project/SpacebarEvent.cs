using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class SpacebarEvent : MonoBehaviour
{
    public UnityEvent invokeEvent;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SpacebarPressed(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Context is performed.");
            invokeEvent?.Invoke();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
