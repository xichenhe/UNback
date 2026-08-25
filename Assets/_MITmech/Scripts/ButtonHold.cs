using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ButtonHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public UnityEvent heldDown;
    public UnityEvent letGo;
    public bool isHeld;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void OnPointerDown(PointerEventData data)
    {
        isHeld = true;
    }
    public void OnPointerUp(PointerEventData data)
    {
        isHeld = false;
        letGo?.Invoke();
    }

    // Update is called once per frame
    void Update()
    {
        if (isHeld)
        {
            heldDown?.Invoke();
        }

    }
}
