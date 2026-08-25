using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Spinning;
using UnityEngine.Events;

public class CoordinateDisplay : MonoBehaviour
{
    //public Slider xSlider, ySlider, zSlider;
    public MouseRotate xRotator, yRotator, zRotator, millHeadRotator;
    MouseRotate currentlySelectedRotator;

    public UnityEvent<bool> xSelected;
    public UnityEvent<bool> ySelected;
    
    public UnityEvent<bool> xOffsetCorrect;
    public UnityEvent<bool> yOffsetCorrect;

    public TextMeshProUGUI xText, yText, zText;
    public TextMeshProUGUI spinSpeedText;
    public GameObject spinSpeedButton;

    public Vector3 zeroPosition;
    public Vector3 currentCoordinates;

    public bool isJogging;
    public Jogging jogging;

    public CoordinateDisplay otherCoordDisplay;

    public bool enteringSpinSpeed;

    bool valueUnselected;
    public bool valueEntered; // made public for tutorial
    string enteredValue;

    bool enterValueRoutineStarted;

    public int maxStringLength;

    public bool isExtraScreen;
    public bool useExtraScreen = true;

    float adjustFactor = 0.851f;

    // Start is called before the first frame update
    void Start()
    {
        if (!isExtraScreen)
        {
            isJogging = false;
            enteringSpinSpeed = false;
            valueUnselected = true;
            valueEntered = false;
            enterValueRoutineStarted = false;
            ResetZero();
        }


    }

    private void OnEnable()
    {
        if (isExtraScreen && useExtraScreen)
        {
            currentCoordinates = otherCoordDisplay.currentCoordinates;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!isExtraScreen)
        {
            float xCoord = (xRotator.storedRotation - zeroPosition.x);
            float yCoord = (yRotator.storedRotation - zeroPosition.y);
            float zCoord = (zRotator.storedRotation + millHeadRotator.storedRotation - zeroPosition.z);
            if (zCoord < .01f && zCoord > -.01f)
            {
                zCoord = 0f;
            }
            //Debug.Log(zCoord + " is the z coordinate");

            if (valueUnselected)
            {
                string xTextTruncated = "X: " + Math.Round((Double)xCoord, 4);
                if (xTextTruncated.Length > maxStringLength)
                {
                    xTextTruncated = xTextTruncated.Substring(0, maxStringLength);
                }
                string yTextTruncated = "Y: " + Math.Round((Double)yCoord, 4);
                if (yTextTruncated.Length > maxStringLength)
                {
                    yTextTruncated = yTextTruncated.Substring(0, maxStringLength);
                }
                string zTextTruncated = "Z: " + Math.Round((Double)zCoord, 4);
                if (zTextTruncated.Length > maxStringLength)
                {
                    zTextTruncated = zTextTruncated.Substring(0, maxStringLength);
                }
                xText.text = xTextTruncated;
                yText.text = yTextTruncated;
                zText.text = zTextTruncated;
                currentCoordinates = new Vector3(xCoord, yCoord, zCoord);
            }
        }
        else
        {
            if (useExtraScreen)
            {
                xText.text = otherCoordDisplay.xText.text;
                yText.text = otherCoordDisplay.yText.text;
                zText.text = otherCoordDisplay.zText.text;
                currentCoordinates = otherCoordDisplay.currentCoordinates;
            }
        }




        //Debug.Log(currentCoordinates + " on " + gameObject.GetInstanceID());
    }


    public void ResetZero()
    {
        Debug.Log("resetting zero");
        zeroPosition = new Vector3(xRotator.storedRotation, yRotator.storedRotation, zRotator.storedRotation + millHeadRotator.storedRotation);
    }

    public void CycleJogStatus()
    {
        Debug.Log("Cycling jog status.");
        isJogging = !isJogging;
    }

    public void UnselectAxis()
    {
        valueUnselected = true;
        enteringSpinSpeed = false; //if they actually clicked the spin speed button, this should fire first before it gets set back to true
    }

    public void SubmitValue()
    {
        float parsedNumber;
        if (float.TryParse(enteredValue, out parsedNumber))
        {
            valueEntered = true;
            UnselectAxis();
        }
        else if (enteredValue == null)
        {
            enteredValue = "0";
            valueEntered = true;
            UnselectAxis();
        }
        else
        {
            Debug.Log("Failure was cuz the value was: " + enteredValue + " on " + gameObject.name);
            FailureState.Instance.SystemFailure("You did not input a valid value.");
            UnselectAxis();
        }



    }

    public void DeleteDigit()
    {
        if (enteredValue != null)
        {
            if (enteredValue.Length > 0)
            {
                enteredValue = enteredValue.Substring(0, enteredValue.Length - 1);
                if (enteredValue.Length == 0)
                {
                    enteredValue = null;
                }
            }
        }
        else
        {
            Debug.Log("enteredValue is already null");
        }


    }

    public void StartInputtingSpinSpeed()
    {
        if (!Spinning.Instance.isSpinning)
        {
            FailureState.Instance.SystemFailure("You may only adjust the spin speed when the spindle is on.");
            return;
        }
        else if (gameObject.activeSelf)
        {
            UnselectAxis();
            enteringSpinSpeed = true;
            StartCoroutine(EnterSpinSpeedValue());
        }

    }

    public void XButtonHeld()
    {
        if (isJogging)
        {
            jogging.JogX();
        }
        else
        {
            if (currentlySelectedRotator != xRotator)
            {
                UnselectAxis();
                StartCoroutine(EnterValue(xRotator));
            }
            else if (!enterValueRoutineStarted)
            {
                StartCoroutine(EnterValue(xRotator));
            }
        }
    }

    public void YButtonHeld()
    {
        if (isJogging)
        {
            jogging.JogY();
        }
        else
        {
            if (currentlySelectedRotator != yRotator)
            {
                UnselectAxis();
                StartCoroutine(EnterValue(yRotator));
            }
            else if (!enterValueRoutineStarted)
            {
                StartCoroutine(EnterValue(yRotator));
            }
        }
    }

    public void ZButtonHeld()
    {
        if (isJogging)
        {
            jogging.JogZ();
        }
        else
        {
            if (currentlySelectedRotator != zRotator)
            {
                UnselectAxis();
                StartCoroutine(EnterValue(zRotator));
            }
            else if (!enterValueRoutineStarted)
            {
                StartCoroutine(EnterValue(zRotator));
            }
        }
    }

    IEnumerator EnterValue(MouseRotate whichAxis)
    {
        xSelected.Invoke(false);
        ySelected.Invoke(false);


        enterValueRoutineStarted = true;
        enteredValue = null;
        Debug.Log("Ready to enter value!");
        TextMeshProUGUI whichText;
        if (whichAxis == xRotator)
        {
            xSelected.Invoke(true);
            whichText = xText;
        }
        else if (whichAxis == yRotator)
        {
            ySelected.Invoke(true);
            whichText = yText;
        }
        else if (whichAxis == zRotator)
        {
            whichText = zText;
        }
        else
        {
            whichText = xText;
            Debug.Log("You should not have been able to get here!");
        }
        valueEntered = false;

        //  GetComponent<TextMesh>().fontStyle = FontStyle.Bold;
        string previousValue = whichText.text;


        yield return null;


        valueUnselected = false;
        whichText.text = whichText.text.Substring(0, whichText.text.Length - 1);
        float timer = 0;
        bool underscoreAdded = false;
        while (!valueUnselected)
        {
            timer += Time.deltaTime;

            //underscore logic
            if (timer > 1f && !underscoreAdded)
            {
                //whichText.text = whichText.text.Substring(0,2) + enteredValue + "_";
                //Debug.Log("Adding underscore at time: " + timer);
                underscoreAdded = true;
            }
            if (timer > 2f && underscoreAdded)
            {
                timer -= 2f;
                //whichText.text = whichText.text.Substring(0, whichText.text.Length - 1);
                //Debug.Log("Removing underscore at time: " + timer);
                underscoreAdded = false;
            }

            if (underscoreAdded)
            {
                whichText.text = whichText.text.Substring(0, 2) + " " + enteredValue + "_";
            }
            else
            {
                whichText.text = whichText.text.Substring(0, 2) + " " + enteredValue;
            }



            yield return null;
        }

        //Debug.Log("We've been unselected!");

        /*
        if(timer > 1)
        {
            whichText.text.Remove(whichText.text.Length - 1);
        }
        */

        if (valueEntered)
        {
            whichText.text = enteredValue;
            if (whichText == xText)
            {
                xOffsetCorrect.Invoke(float.Parse(enteredValue) == .1f);
                zeroPosition.x = xRotator.storedRotation - float.Parse(enteredValue);
            }
            else if (whichText == yText)
            {
                yOffsetCorrect.Invoke(float.Parse(enteredValue) == -.1f);
                zeroPosition.y = yRotator.storedRotation - float.Parse(enteredValue);
            }
            else if (whichText == zText)
            {
                //Debug.Log("Zero position.z is: " + zeroPosition.z);
                zeroPosition.z = zRotator.storedRotation + millHeadRotator.storedRotation - float.Parse(enteredValue);
            }
        }
        else
        {
            whichText.text = previousValue;
        }
        //whichText.GetComponent<TextMesh>().fontStyle = FontStyle.Normal;
        if (useExtraScreen)
        {
            otherCoordDisplay.zeroPosition = zeroPosition;
        }
        enterValueRoutineStarted = false;

    }

    IEnumerator EnterSpinSpeedValue()
    {
        yield return null;
        enteredValue = null;
        enterValueRoutineStarted = true;
        valueEntered = false;
        //spinSpeedText.fontStyle = FontStyle.Bold;
        string previousValue = spinSpeedText.text;
        valueUnselected = false;
        //spinSpeedText.text = spinSpeedText.text.Substring(0, spinSpeedText.text.Length - 1);
        float timer = 0;
        bool underscoreAdded = false;
        while (!valueUnselected)
        {
            timer += Time.deltaTime;

            //underscore logic
            if (timer > 1f && !underscoreAdded)
            {
                underscoreAdded = true;
            }
            if (timer > 2f && underscoreAdded)
            {
                timer -= 2f;
                underscoreAdded = false;
            }

            if (underscoreAdded)
            {
                spinSpeedText.text = enteredValue + "_";
            }
            else
            {
                spinSpeedText.text = enteredValue;
            }
            yield return null;
        }

        if (valueEntered)
        {
            spinSpeedText.text = enteredValue;
            Spinning.Instance.SpinSpeed = float.Parse(enteredValue);
            if (Spinning.Instance.SpinSpeed < 500f)
            {
                Spinning.Instance.SpinSpeed = 500f;
                FailureState.Instance.DisplayWarning("Machine on high mode should have a spin speed above 500!");
            }

        }
        else
        {
            spinSpeedText.text = previousValue;
            Spinning.Instance.SpinSpeed = float.Parse(enteredValue);
        }
        //spinSpeedText.fontStyle = FontStyle.Normal;
        enteringSpinSpeed = false;
        enterValueRoutineStarted = false;
        spinSpeedButton.GetComponent<CycleButtonSelected>().CycleSelectionHighlight();

    }

    public void AddDigit(string digit)
    {
        enteredValue += digit;
    }
}
