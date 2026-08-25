using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExtraScreen : MonoBehaviour
{
    //public CoordinateDisplay trueDisplay;
    public GameObject trueDisplayCover;
    public Text trueX, trueY, trueZ, trueSpin;
    public Text extraX, extraY, extraZ, extraSpin;

    public CycleButtonSelected trueButton, extraButton;

    public void TurnOnScreen()
    {
        gameObject.SetActive(true);
        StartCoroutine(WaitFrameBeforeTurningOn());
    }

    IEnumerator WaitFrameBeforeTurningOn()
    {
        Debug.Log("Turning on now.");
        yield return null;
        trueDisplayCover.SetActive(true);
        extraX.text = trueX.text;
        extraY.text = trueY.text;
        extraZ.text = trueZ.text;
        extraSpin.text = trueSpin.text;

        if(trueButton.isActivelySelected != extraButton.isActivelySelected)
        {
            extraButton.CycleSelectionHighlight();
        }
    }

    public void TurnOffScreen()
    {
        gameObject.SetActive(false);
        trueDisplayCover.SetActive(false);
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
