using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseRotateSpoof : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public MouseRotate spoofingMouseRotate;
    Camera mainCamera;
    Vector3 previousRay;
    public float storedRotation;
    public float angleSensitivity;
    float startX, startY;
    float angleBounds;
    float minAngle, maxAngle;
    public Transform debugButtonImage;
    public float modelRotationMultiplier;

    bool pauseModelRotation;

    public bool isRotatedRight;
    bool isRotating;

    Transform parentTransform;

    Vector3 buttonStartRotation;

    public bool makeNegative = false;

    //public float storedRotationMultiplier;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
        previousRay = new Vector3(0f, 0f, 0f);
        storedRotation = 0f;
        parentTransform = GetComponentInParent<Transform>();
        pauseModelRotation = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isRotating)
        {
            storedRotation = spoofingMouseRotate.storedRotation;
        }
        maxAngle = spoofingMouseRotate.maxAngle;
        minAngle = spoofingMouseRotate.minAngle;

        //transform.LookAt(mainCamera.transform);
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        isRotating = true;
        Vector3 newRay = Input.mousePosition - transform.position;
        //Debug.Log("I'm being dragged, new Ray is " + newRay);

        //find angle between the last frame's mouse position and new mouse position
        float angle = Vector3.Angle(previousRay, newRay) * angleSensitivity;
        Debug.DrawRay(transform.position, previousRay, Color.green);
        Debug.DrawRay(transform.position, newRay, Color.green);
        //Debug.Log("Change in angle is " + angle);

        //we will use the cross product to determine if we're going clockwise or counterclockwise
        Vector3 cross = Vector3.Cross(previousRay, newRay);
        //Debug.Log("Cross product is: " + cross);
        Debug.DrawRay(transform.position, cross, Color.blue);
        //Debug.Log("Angle between forwards and cross is: " + Vector3.Angle(transform.forward, cross));
        if (Vector3.Angle(parentTransform.forward, cross) > 90)
        {
            storedRotation += angle;
            //Debug.Log("Rotating clockwise! Stored rotation is: " + storedRotation);
        }
        else
        {
            storedRotation -= angle;
            //Debug.Log("Rotating counterclockwise! Stored rotation is: " + storedRotation);
        }

        //make sure stored rotation value is clamped
        //storedRotation = Mathf.Clamp(storedRotation, -angleBounds, angleBounds);
        if (storedRotation > maxAngle)
        {
            //Debug.Log(storedRotation + " is greater than angle bounds of: " + maxAngle);
            storedRotation = maxAngle;
            //Debug.Log("rotation was greater than angle bounds. Now: " + storedRotation);
        }
        else if (storedRotation < minAngle)
        {
            //Debug.Log(storedRotation + " is less than angle bounds of: " + minAngle);
            storedRotation = minAngle;
            //Debug.Log("rotation was less than angle bounds. Now: " + storedRotation);
        }
        //Debug.Log("stored rotation on fake rotator is: " + storedRotation);
        spoofingMouseRotate.storedRotation = storedRotation;

        //now we can rotate using the stored rotation
        debugButtonImage.localRotation = Quaternion.Euler(startX, startY, storedRotation * modelRotationMultiplier);

        //reset previous ray for the next frame of dragging
        previousRay = newRay;
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        isRotating = false;
    }
}
