using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoverTextEnable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject hoverGraphic;


    // Start is called before the first frame update
    void Start()
    {
        hoverGraphic.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {      
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hoverGraphic.SetActive(true);

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hoverGraphic.SetActive(false);
    }

}
