using UnityEngine;

//replacement for the pneumatic.cs class
public class QuillManualLock : MonoBehaviour
{
    public SnapRestrictions restrictions;
    public GameObject interactableZoneObject;
    public bool startTighit;
    public bool isTight;
    bool areWeSpinning;
    public SnapZone quillSnapZone;

    private void Start()
    {
        //isTight = interactableZoneObject.activeSelf;
        if (isTight)
        {
            Tighten();
        }
        else
        {
            Loosen();
        }
        Spinning.SpinningEvent += StartedSpinning;
    }

    private void OnDestroy()
    {
        Spinning.SpinningEvent -= StartedSpinning;
    }

    void StartedSpinning(bool isSpinning)
    {
/*        areWeSpinning = isSpinning;
        if (isSpinning)
        {
            if (!isTight && quillSnapZone.currentlySnapped == SnapZoneManager.SnapObjectType.Chuck)
            {
                Debug.Log("isTight has a value of: " + isTight);
                FailureState.Instance.SystemFailure("You started spinning before the pneumatics had been used to hold the tool in place.");
            }
        }*/
    }


    //we use these methods attatched to the two buttons, now that there's actually two buttons to press in the model.
    public void Loosen()
    {

        restrictions.removable = true;
        //interactableZoneObject.SetActive(false);
        isTight = false;
        if (!isTight && areWeSpinning)
        {
            FailureState.Instance.SystemFailure("You loosened the tool while the machine was still spinning.");
        }
    }

    public void Tighten()
    {
        restrictions.removable = false;
        //interactableZoneObject.SetActive(true);
        isTight = true;
        if (!restrictions.gameObject.activeSelf)
        {
            FailureState.Instance.SystemFailure("You turned on the pneumatics without any tool to grab.");
        }
    }

}
