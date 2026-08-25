using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Example_SwitchInteraction : MonoBehaviour
{
    public GameObject switchObject;
    public float switchRotation;
    public Material switchMaterialChange;
    Quaternion switchObjectCurrentRotation;


    // Start is called before the first frame update
    void Start()
    {
       switchObjectCurrentRotation = switchObject.transform.rotation;

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SwitchObjectChange()
    {
        //switchObjectCurrentRotation == switchObjectCurrentRotation;

    }
}
