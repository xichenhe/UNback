using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MITmech.ITS
{

public class ITSHintActivater : MonoBehaviour
{
    public List<HintObject> hintObjects;
    public UnityEvent hintActivatedEvent;
    public UnityEvent hintDeactivatedEvent;

    private void OnEnable()
    {
        
    }
    // Start is called before the first frame update
    void Start()
    {
        foreach (HintObject hintObject in hintObjects)
        {  
            hintObject.DisableHintEvent.AddListener(HintDeactivated);
            hintObject.ActivateHintEvent.AddListener(HintActivated);

        }
    }
    private void OnDestroy()
    {
/*        foreach (HintObject hintObject in hintObjects)
        {
            hintObject.DisableHintEvent.RemoveListener(HintDeactivated);
            hintObject.ActivateHintEvent.RemoveListener(HintActivated);

        }*/
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    void HintActivated(string hintString,HintObject hintObject)
    {
        hintActivatedEvent.Invoke();
        //Debug.Log(hintString + " activated");
    }
    void HintDeactivated()
    {
        hintDeactivatedEvent.Invoke();
        //Debug.Log("deactivated");

    }
}

}
