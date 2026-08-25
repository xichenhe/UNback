using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class XRTerra_PlayerController3DOF : MonoBehaviour
{
    public Transform mainCamera;
    public float xSensitivity, ySensitivity;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void MouseX(InputAction.CallbackContext context)
    {
        float mouseXDelta = context.ReadValue<float>() * Time.deltaTime * xSensitivity;
        transform.Rotate(0f, mouseXDelta, 0f);
    }

    public void MouseY(InputAction.CallbackContext context)
    {
        float mouseYDelta = context.ReadValue<float>() * Time.deltaTime * ySensitivity;
        Vector3 desiredRotation = mainCamera.eulerAngles + new Vector3(-mouseYDelta, 0f, 0f);

        if (desiredRotation.x > 180) 
        {
            desiredRotation.x -= 360; 
        }
        desiredRotation.x = Mathf.Clamp(desiredRotation.x, -85f, 85f);
        mainCamera.rotation = Quaternion.Euler(desiredRotation);
    }

}
