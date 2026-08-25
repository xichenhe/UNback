using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MillHeadJogging : MonoBehaviour
{
    MouseRotate millRot;
    Vector3 startPos;
    public float jogMultiplier;

    // Start is called before the first frame update
    void Start()
    {
        millRot = GetComponent<MouseRotate>();
        startPos = transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        transform.localPosition = startPos + Vector3.up * millRot.storedRotation * jogMultiplier;
    }
}
