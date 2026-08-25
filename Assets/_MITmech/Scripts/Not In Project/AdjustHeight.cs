using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdjustHeight : MonoBehaviour
{
    public float boostBy;

    public void RaiseHead()
    {
        transform.position += Vector3.up * boostBy;
    }

    public void LowerHead()
    {
        transform.position += Vector3.down * boostBy;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
