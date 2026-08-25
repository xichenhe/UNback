using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CinemachineRotator : MonoBehaviour
{
    public float maxDegrees;
    public float maxDegreesVertical;
    public bool canRotate = true;
    public float rotateOutSpeed;
    //public float returnToCenterSpeed;
    public bool isRotating;
    public int framesToZero;

    public RectTransform[] rotationCanvases;
    public Transform sceneObjects;

    public float scrollZoomMultiplier;
    public float maxZoomScale, minZoomScale;
    [SerializeField] float scaleFactor;
    bool returningToZero;

    public bool scrollActive;

    public bool rightClickToDrag = true;
    private bool rightDrag = false;
    private float previousX;
    private float previousY;

    private Vector3 originalRotation;
    private Vector3 originalPosition;


    private void Start()
    {
        isRotating = false;
        scaleFactor = 1;
        returningToZero = false;
        originalRotation = transform.localEulerAngles;
        originalPosition = transform.position;
    }

    public void RotateLeft()
    {
        //transform.Rotate(new Vector3(0f, rotateOutSpeed * Time.deltaTime, 0f));
        if (canRotate)
        {
            Vector3 rot = transform.localEulerAngles;
            rot.y += rotateOutSpeed * Time.deltaTime;
            //Debug.Log("left rot.y is "+ rot.y);
            if (rot.y < maxDegrees || rot.y > 360 - maxDegrees)
            {                
                transform.localEulerAngles = rot;
            }
        }
        /*
        sceneObjects.Rotate(new Vector3(0f, -rotateOutSpeed * Time.deltaTime, 0f));
        float currentYRotation = transform.rotation.eulerAngles.y;
        if(currentYRotation > 180)
        {
            currentYRotation -= 360;
        }
        if (currentYRotation > maxDegrees)
        {
            //Debug.Log("A little too far left.");
            transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        }
        else
        {
            foreach (RectTransform rect in rotationCanvases)
            {
                rect.Rotate(new Vector3(0f, rotateOutSpeed * Time.deltaTime, 0f));
            }
        }*/

    }

    public void RotateRight()
    {
        //transform.Rotate(new Vector3(0f, -rotateOutSpeed * Time.deltaTime, 0f));
        if (canRotate)
        {
           
            Vector3 rot = transform.localEulerAngles;
            rot.y -= rotateOutSpeed * Time.deltaTime;
            //Debug.Log("right rot.y is " + rot.y);
            if (rot.y < maxDegrees || rot.y > 360 - maxDegrees)
            {
                transform.localEulerAngles = rot;
            }
        }
        /*
        sceneObjects.Rotate(new Vector3(0f, rotateOutSpeed * Time.deltaTime, 0f));
        float currentYRotation = transform.rotation.eulerAngles.y;
        if (currentYRotation > 180)
        {
            currentYRotation -= 360;
        }
        if (currentYRotation < -maxDegrees)
        {
            //Debug.Log("A little too far right.");
            transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        }
        else
        {
            foreach (RectTransform rect in rotationCanvases)
            {
                rect.Rotate(new Vector3(0f, -rotateOutSpeed * Time.deltaTime, 0f));
            }
        }*/
    }

    //***
    public void RotateUp()
    {
        //transform.Rotate(new Vector3(rotateOutSpeed * Time.deltaTime, 0f, 0f));
        if (canRotate)
        {
            Vector3 rot = transform.localEulerAngles;
            rot.x += rotateOutSpeed * Time.deltaTime;
            //Debug.Log("left rot.x is " + rot.x);
            if (rot.x < maxDegreesVertical || rot.x > 360 - maxDegreesVertical)
            {
                transform.localEulerAngles = rot;
            }
        }

        /*
        sceneObjects.Rotate(new Vector3(rotateOutSpeed * Time.deltaTime, 0f, 0f));
        float currentXRotation = transform.rotation.eulerAngles.y;
        if (currentXRotation > 180)
        {
            currentXRotation -= 360;
        }
        if (currentXRotation < -maxDegrees)
        {
            //Debug.Log("A little too far right.");
            transform.rotation = Quaternion.Euler(0f, 0f, -90f);
        }
        else
        {
            foreach (RectTransform rect in rotationCanvases)
            {
                rect.Rotate(new Vector3(-rotateOutSpeed * Time.deltaTime, 0f, 0f));
            }
        }*/
    }

    public void RotateDown()
    {
        //transform.Rotate(new Vector3(-rotateOutSpeed * Time.deltaTime, 0f, 0f));
        if (canRotate)
        {
            Vector3 rot = transform.localEulerAngles;
            rot.x -= rotateOutSpeed * Time.deltaTime;
            //Debug.Log("up rot.x is " + rot.x);
            if (rot.x < maxDegreesVertical || rot.x > 360 - maxDegreesVertical)
            {
                transform.localEulerAngles = rot;
            }
        }
        /*
        sceneObjects.Rotate(new Vector3(-rotateOutSpeed * Time.deltaTime, 0f, 0f));
        float currentXRotation = transform.rotation.eulerAngles.y;
        if (currentXRotation > 180)
        {
            currentXRotation -= 360;
        }
        if (currentXRotation < -maxDegrees)
        {
            //Debug.Log("A little too far right.");
            transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        }
        else
        {
            foreach (RectTransform rect in rotationCanvases)
            {
                rect.Rotate(new Vector3(rotateOutSpeed * Time.deltaTime, 0f, 0f));
            }
        }*/
    }
    //****

    public void SwtichLeft() 
    {
        Vector3 rot = transform.localEulerAngles;
        rot.y += 90f;
        if (rot.y < maxDegrees || rot.y > 360 - maxDegrees)
        {
            transform.localEulerAngles = rot;
        }
    }

    public void SwtichRight()
    {
        Vector3 rot = transform.localEulerAngles;
        rot.y -= 90f;
        if (rot.y < maxDegrees || rot.y > 360 - maxDegrees)
        {
            transform.localEulerAngles = rot;
        }
    }   

    public void IsRotating(bool areWeRotating)
    {
        isRotating = areWeRotating;
    }

    public void SetCanRotate(bool b)
    {
        canRotate = b;
    } 

    public void ReturnToZero()
    {
        StopAllCoroutines();
        StartCoroutine(ReturnToZeroCoroutine());
    }

    IEnumerator ReturnToZeroCoroutine()
    {
        /*
        returningToZero = true;
        Quaternion startRotation = sceneObjects.rotation;
        Vector3 startScale = transform.localScale;
        for(int i = 0; i <= framesToZero; i++)
        {
            float lerp = (float)i / (float)framesToZero;
            //Debug.Log(lerp);
            sceneObjects.rotation = Quaternion.Lerp(startRotation, Quaternion.identity, lerp);
            foreach (RectTransform rect in rotationCanvases)
            {
                Quaternion currentRotation = rect.rotation;
                rect.rotation = Quaternion.Lerp(currentRotation, Quaternion.identity, lerp);
            }
            transform.localScale = Vector3.Lerp(startScale, new Vector3(1f, 1f, 1f), lerp);
            yield return null;
        }
        returningToZero = false;*/
        returningToZero = true;
        transform.localScale = new Vector3(1f, 1f, 1f);
        transform.localEulerAngles = originalRotation;
        transform.position = originalPosition;
        yield return null;
        returningToZero = false;
    }

    private void Update()
    {
        /*
        if (!isRotating)
        {
            Vector3 rotationVector = transform.rotation.eulerAngles;
            if(rotationVector.y > 180)
            {
                rotationVector.y -= 360;
            }
            transform.rotation = Quaternion.Euler(Vector3.RotateTowards(rotationVector, Vector3.zero, returnToCenterSpeed, 1));
        }
        */
        bool isScreenView = CameraSwitchTrigger.Instance.currentCameraView == CameraSwitchTrigger.CameraView.ScreenView 
            || CameraSwitchTrigger.Instance.currentCameraView == CameraSwitchTrigger.CameraView.BlockSideView;


        if (Input.mouseScrollDelta.magnitude > 0.1 && !returningToZero && !isScreenView)
        {
            scaleFactor -= Input.mouseScrollDelta.y * Time.deltaTime * scrollZoomMultiplier;
            scaleFactor = Mathf.Clamp(scaleFactor, minZoomScale, maxZoomScale);
            transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
        }

        if (Input.GetMouseButton(1) && rightClickToDrag) {
            Debug.Log("Pressed right click.");
            if (rightDrag) {
                //if (Input.GetKey(KeyCode.E))
                //{
                    if (previousY < Input.mousePosition.y)
                    {
                        RotateDown();
                    }
                    else if (previousY > Input.mousePosition.y)
                    {
                        RotateUp();
                    }
                //}
                //else {
                    if (previousX < Input.mousePosition.x)
                    {
                        RotateLeft();
                    }
                    else if (previousX > Input.mousePosition.x)
                    {
                        RotateRight();
                    }
                //
                //
                //
                //
                //
                //
                //} 
            }
            previousX = Input.mousePosition.x;
            previousY = Input.mousePosition.y;
            rightDrag = true;
        } else {
            rightDrag = false;
        
        }

    }

    public void SetScreenViewScale() {
        transform.position = new Vector3(originalPosition.x, originalPosition.y, -1.7f);
    }

}
