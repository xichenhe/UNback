using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CycleButtonSelected : MonoBehaviour
{
    Image buttonImage;
    public bool isActivelySelected;
    public Color selectedColor;
    public Color defaultColor;

    // Start is called before the first frame update
    void Start()
    {
        buttonImage = GetComponent<Image>();
        //isActivelySelected = false;
    }

    public void CycleSelectionHighlight()
    {
        if(buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
        }
            isActivelySelected = !isActivelySelected;
            if (isActivelySelected)
            {

                buttonImage.color = selectedColor;
            }
            else
            {

                buttonImage.color = defaultColor;
            }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
