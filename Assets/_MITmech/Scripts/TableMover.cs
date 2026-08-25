using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TableMover : MonoBehaviour
{
    //public Slider xAxisSlider;
    //public Slider yAxisSlider;

    public MouseRotate xRotator, yRotator,zRotator;

    //public GameObject xAxisHandle;
    //public GameObject yAxisHandle;
    public float handleRotationMultiplier;
    Vector3 startXRot, startYRot;
    Vector3 zeroPosition;

    // Start is called before the first frame update
    void Start()
    {
        zeroPosition = transform.localPosition;
        //startXRot = xAxisHandle.transform.rotation.eulerAngles;
        //startYRot = yAxisHandle.transform.rotation.eulerAngles;
        Debug.Log("Zero position is now set to: " + zeroPosition);
        UpdatePosition();
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePosition();
    }

    public void UpdatePosition()
    {
        Vector3 newPos = new Vector3(xRotator.storedRotation * handleRotationMultiplier, zRotator.storedRotation * handleRotationMultiplier, yRotator.storedRotation * handleRotationMultiplier);
        transform.localPosition = zeroPosition + newPos;

        //Vector3 newXRot = startXRot + new Vector3(1f, 0f, 0f) * handleRotationMultiplier * xAxisSlider.value;
        //xAxisHandle.transform.rotation = Quaternion.Euler(newXRot);
        //Vector3 newYRot = startYRot + new Vector3(1f, 0f, 0f) * handleRotationMultiplier * yAxisSlider.value;
        //yAxisHandle.transform.rotation = Quaternion.Euler(newYRot);

    }
}
