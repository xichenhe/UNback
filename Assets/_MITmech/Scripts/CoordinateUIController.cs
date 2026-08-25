using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CoordinateUIController : MonoBehaviour
{
    public PositionHolderScriptableObject positionHolder;

    public TMP_Text xText;
    public TMP_Text yText;
    public TMP_Text zText;

    private void Awake()
    {
        if (positionHolder != null)
        {
            positionHolder.xPosition.valueChange.AddListener(SetXText);
            positionHolder.yPosition.valueChange.AddListener(SetYText);
            positionHolder.zPosition.valueChange.AddListener(SetZText);

                }
    }
    void SetXText(float x)
    {
        xText.text = "X : "+x.ToString("n3");
    }
 void SetYText(float y)
    {
        yText.text = "Y : " + y.ToString("n3");

    }
    void SetZText(float z)
    {
        zText.text = "Z : " + z.ToString("n3");

    }
}
