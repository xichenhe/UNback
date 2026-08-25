using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MouseRotate : MonoBehaviour, IDragHandler
{
    Camera mainCamera;
    Vector3 previousRay;
    public float storedRotation;
    public float angleSensitivity;
    float startX, startY;
    public float angleBounds;
    public float minAngle, maxAngle;

    public Transform buttonTransform;
    public float modelRotationMultiplier = 1;

    // for selecting which rotation functions
    public bool isXY;
    public bool isZ;
    public bool adjustSpindleSpeed;

    public bool manualSetRotation = false;

    bool pauseModelRotation;
    public bool isRotatedRight;

    Transform parentTransform;
    Vector3 buttonStartRotation;

    // for chuck
    public bool chuckInstall = false;
    public QuillMover qm;

    // for locking and unlocking rotation
    public bool isLocked = false;
    public GameObject modelUnlocked;
    public GameObject modelLocked;

    // for spindle speed
    public GameObject disk;
    public GameObject spindleTextContainer;
    public TextMeshProUGUI spindleText;
    int spindleTextOn = 0;

    //public float storedRotationMultiplier;

    // Start is called before the first frame update
    void Start()
    {
        buttonStartRotation = buttonTransform.rotation.eulerAngles;
        mainCamera = Camera.main;
        previousRay = new Vector3(0f, 0f, 0f);
        storedRotation = 0f;
        startY = buttonTransform.localRotation.eulerAngles.y;
        startX = buttonTransform.localRotation.eulerAngles.x;
        parentTransform = GetComponentInParent<Transform>();
        
        if (isXY) {
            minAngle = -angleBounds;
            maxAngle = angleBounds;
        }
        pauseModelRotation = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (spindleTextOn != 0) {
            spindleTextOn --;
        } if (adjustSpindleSpeed && spindleTextOn == 0) {
            spindleTextContainer.SetActive(false);
        }
        //transform.LookAt(mainCamera.transform);
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {     
        if (adjustSpindleSpeed && !Spinning.Instance.isSpinning) {
            
            FailureState.Instance.SystemFailure("You may only adjust the spin speed when the spindle is on.");
        }
        if (!isLocked)
        {
            Vector3 newRay = Input.mousePosition - mainCamera.WorldToScreenPoint(transform.position);
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
        if(Vector3.Angle(parentTransform.forward, cross) > 90)
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
                //Debug.Log(storedRotation + " is greater than angle bounds of: " + angleBounds);
                storedRotation = maxAngle;
                //Debug.Log("rotation was greater than angle bounds. Now: " + storedRotation);
            }
            else if (storedRotation < minAngle)
            {
                //Debug.Log(storedRotation + " is less than angle bounds of: " + -angleBounds);
                storedRotation = minAngle;
                //Debug.Log("rotation was less than angle bounds. Now: " + storedRotation);
            }

            if (adjustSpindleSpeed)
            {
                disk.transform.localRotation = Quaternion.Euler(disk.transform.rotation.x, (Spinning.Instance.SpinSpeed * (130 / 2500)) + 2, disk.transform.rotation.z);
                Spinning.Instance.SpinSpeed = 500f + (storedRotation * 500);
            }

            //now we can rotate using the stored rotation
            buttonTransform.localRotation = Quaternion.Euler(startX, startY, storedRotation * modelRotationMultiplier);

        //reset previous ray for the next frame of dragging
        previousRay = newRay;
        }
        else
        {
            FailureState.Instance.DisplayWarning("You attemped to move the quill while its locked");
        }
    }

    public void GrabRotate(Transform rotatingControllerTransform)
    {
        if (adjustSpindleSpeed && !Spinning.Instance.isSpinning)
        {

            FailureState.Instance.SystemFailure("You may only adjust the spin speed when the spindle is on.");
        }
        else if (!isLocked)
        {
            Vector3 newRay = rotatingControllerTransform.position - transform.position;
            //Debug.Log("I'm being dragged, new Ray is " + newRay);

            //find angle between the last frame's mouse position and new mouse position
            float angle = Vector3.Angle(previousRay, newRay) * angleSensitivity;
            //Debug.Log("angle transfom is " + angle);
            Debug.DrawRay(transform.position, previousRay, Color.green);
            Debug.DrawRay(transform.position, newRay, Color.green);
            //Debug.Log("Change in angle is " + angle);

            //we will use the cross product to determine if we're going clockwise or counterclockwise
            Vector3 cross = Vector3.Cross(previousRay, newRay);
            //Debug.Log("Cross product is: " + cross);
            Debug.DrawRay(transform.position, cross, Color.blue);
            //Debug.Log("Angle between forwards and cross is: " + Vector3.Angle(transform.forward, cross));

            if (manualSetRotation && angle >=0.2f) {
                angle = 0.2f;
            }
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

            bool isAtMaxAngle = false;
            //make sure stored rotation value is clamped
            //storedRotation = Mathf.Clamp(storedRotation, -angleBounds, angleBounds);
            /*if (storedRotation > maxAngle && isZ)
            {
                storedRotation += angle;
                if (storedRotation > maxAngle + 0.1f)
                {
                    // cycle chuck tighten status
                    qm.CycleChuck();
                    storedRotation = maxAngle - 0.5f;
                }
            }*/
            if (storedRotation > maxAngle)
            {
                StopAllCoroutines();
                StartCoroutine(PauseModelRotation());
                //Debug.Log(storedRotation + " is greater than angle bounds of: " + angleBounds);
                storedRotation = maxAngle;
                isAtMaxAngle = true;
                //Debug.Log("rotation was greater than angle bounds. Now: " + storedRotation);
            }
            else if (storedRotation < minAngle)
            {
                StopAllCoroutines();
                StartCoroutine(PauseModelRotation());
                //Debug.Log(storedRotation + " is less than angle bounds of: " + -angleBounds);
                storedRotation = minAngle;
                isAtMaxAngle = true;
                //Debug.Log("rotation was less than angle bounds. Now: " + storedRotation);
            }
            if (!isAtMaxAngle && !pauseModelRotation)
            {
                Vector3 rotatedNewRay = Quaternion.AngleAxis(buttonStartRotation.y, Vector3.up) * newRay;
                Vector2 flattenedHandPosition = new Vector2(rotatedNewRay.x, rotatedNewRay.y);
                //Debug.Log(flattenedHandPosition);
                float handleAngle = Vector2.Angle(transform.up, flattenedHandPosition);
                if (flattenedHandPosition.x > 0)
                {
                    if (isRotatedRight)
                    {
                        if (manualSetRotation) {
                            buttonTransform.rotation = Quaternion.Euler(new Vector3(buttonStartRotation.x, buttonStartRotation.y, 50 - (storedRotation * 22f)));

                        } else {
                            buttonTransform.rotation = Quaternion.Euler(new Vector3(buttonStartRotation.x, buttonStartRotation.y, buttonStartRotation.z + handleAngle));
                        }
                    }
                    else
                    {
                        if (manualSetRotation)
                        {
                            buttonTransform.rotation = Quaternion.Euler(new Vector3(buttonStartRotation.x, buttonStartRotation.y, 50 - (storedRotation * 22f)));
                        }
                        else
                        {
                            buttonTransform.rotation = Quaternion.Euler(new Vector3(buttonStartRotation.x, buttonStartRotation.y, buttonStartRotation.z - handleAngle));
                        }
                    }
                }
                else
                {
                    if (isRotatedRight)
                    {
                        if (manualSetRotation)
                        {
                            buttonTransform.rotation = Quaternion.Euler(new Vector3(buttonStartRotation.x, buttonStartRotation.y, 50 - (storedRotation * 22f)));
                        }
                        else
                        {
                            buttonTransform.rotation = Quaternion.Euler(new Vector3(buttonStartRotation.x, buttonStartRotation.y, buttonStartRotation.z - handleAngle));
                        }
                    }
                    else
                    {
                        if (manualSetRotation)
                        {
                            buttonTransform.rotation = Quaternion.Euler(new Vector3(buttonStartRotation.x, buttonStartRotation.y, 50 - (storedRotation * 22f)));
                        }
                        else
                        {
                            buttonTransform.rotation = Quaternion.Euler(new Vector3(buttonStartRotation.x, buttonStartRotation.y, buttonStartRotation.z + handleAngle));
                        }
                    }
                }
            }
            if (adjustSpindleSpeed)
            {
                spindleTextContainer.SetActive(true);
                Spinning.Instance.SpinSpeed = 500f + (storedRotation * 500);
                float y = 5f - (storedRotation * 52);
                disk.transform.localRotation = Quaternion.Euler(disk.transform.rotation.x, y, disk.transform.rotation.z);
                spindleText.text = Mathf.Round(Spinning.Instance.SpinSpeed).ToString();
                spindleTextOn = 50;
            }

            //reset previous ray for the next frame of dragging
            previousRay = newRay;
        }
        else {
            FailureState.Instance.DisplayWarning("You attemped to move the quill while its locked");
        }
        }

    IEnumerator PauseModelRotation()
    {
        //Debug.Log("Pausing model rotation temporarily.");
        pauseModelRotation = true;
        yield return new WaitForSeconds(.3f);
        pauseModelRotation = false;
    }

    public void Jog(float jogSpeed)
    {
        storedRotation += jogSpeed;
        if(storedRotation > maxAngle || storedRotation < minAngle)
        {
            if (isXY)
            {
                Jogging.Instance.EndXYSounds();
            }
            else if (isZ)
            {
                Jogging.Instance.EndZSounds();
            }

            //Debug.Log("Do we need an error message here?");
        }
        //make sure stored rotation value is clamped
        if (storedRotation > maxAngle)
        {
            storedRotation = maxAngle;
        }
        else if (storedRotation < minAngle)
        {
            storedRotation = minAngle;
        }

        //now we can rotate using the stored rotation
        buttonTransform.localRotation = Quaternion.Euler(startX, startY, storedRotation * modelRotationMultiplier);
    }

    public void toggleLock() {
        Spinning.Instance.IsLocked = !isLocked;
        isLocked = !isLocked;
        modelUnlocked.SetActive(!isLocked);
        modelLocked.SetActive(isLocked);
    }
}
