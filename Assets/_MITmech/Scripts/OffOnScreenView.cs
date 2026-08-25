using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OffOnScreenView : MonoBehaviour
{

    public RawImage image;

    // Start is called before the first frame update
    void Start()
    {
        image = GetComponent<RawImage>();
        TurnOnThoseCoords();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TurnOffExtraUICoords()
    {
        image.enabled = false;
    }

    public void TurnOnThoseCoords()
    {
        image.enabled = true;
    }
}
