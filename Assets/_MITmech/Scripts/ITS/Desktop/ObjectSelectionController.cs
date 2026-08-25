using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MITmech.ITS
{

public class ObjectSelectionController : MonoBehaviour
{
    public UnityEvent hoveredEvent;
    public UnityEvent selectedEvent;
    public UnityEvent deselectedEvent;

    public bool Active {  get { return active; }
        private set 
        {
            active = value;
            if (active)
            selectedEvent.Invoke();
            else
                deselectedEvent.Invoke();
        }
    }
    bool active;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnMouseEnter()
    {
        hoveredEvent.Invoke();
    }
    private void OnMouseDown()
    {
        Active = !Active;
    }
}

}
