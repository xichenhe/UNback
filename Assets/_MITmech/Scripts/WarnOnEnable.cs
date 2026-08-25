using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarnOnEnable : MonoBehaviour
{
    public string warningText;

    public bool unSnapFromZone;
    public SnapZone unSnapZone;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnEnable()
    {
        FailureState.Instance.DisplayWarning(warningText);
        if (unSnapFromZone)
        {
            StartCoroutine(WaitFrameBeforeUnsnap());
        }
    }

    IEnumerator WaitFrameBeforeUnsnap()
    {
        yield return null;
        //Debug.Log("Trying to empty snap zone on " + unSnapZone);
        unSnapZone.SnapIn(SnapZoneManager.SnapObjectType.Empty);
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
