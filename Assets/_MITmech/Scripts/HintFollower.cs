using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HintFollower : MonoBehaviour
{
    public Transform hintTransform;
    // Start is called before the first frame update
    Vector3 offsetValue;
    void Start()
    {
        offsetValue = transform.position - hintTransform.position;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = offsetValue+hintTransform.position;
    }
}
