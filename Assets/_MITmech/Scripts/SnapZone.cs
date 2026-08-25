using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using static SnapZoneManager;

public class SnapZone : MonoBehaviour
{
    public UnityEvent<bool> Enabled;

    public UnityEvent correctToolEnteredZone;
    public UnityEvent correctToolSnapped;
    public SnapObjectType[] snapZoneInventory;
    public GameObject[] inventoryObjectModel;
    public Dictionary<SnapObjectType, GameObject> inventoryObjectModelDictionary = new Dictionary<SnapObjectType, GameObject>();

    public SnapObjectType currentlySnapped;
    [Tooltip("When the chuck is SnappedIn this will be the meters below the locked position to indicate the chuck is not locked yet")]
    public float unlockedChuckOffsetMeters = 0.05f; //public float mouseRotateStoredRotationAtSnapChuck;

    MeshRenderer rend; //Comment Needed : Isa

    // if parallel snap zone, remove block first 

    //Comment Needed : Isa
    void Awake()
    {
        rend = GetComponent<MeshRenderer>();
        rend.enabled = false;
    }

    //Comment Needed : Isa
    private void Start()
    {
        SnapZoneManager.Instance.Hovering += OnHover;
        int i = 0;
        foreach (SnapObjectType snapObject in snapZoneInventory)
        {
            //associating the arrays in the editor. make sure they're referenced in the same order.
            inventoryObjectModelDictionary.Add(snapObject, inventoryObjectModel[i]);
            //Debug.Log(snapObject + " is connected to " + inventoryObjectModelDictionary[snapObject]);

            //and turn them all off at the beginning.
            inventoryObjectModelDictionary[snapObject].SetActive(false);
            //Debug.Log("snapzone: " + name + " , setting inactive: " + inventoryObjectModelDictionary[snapObject].name);
            i++;
        }
        SnapIn(currentlySnapped);

        if(currentlySnapped != SnapObjectType.Empty)
        gameObject.SetActive(false);
    }

   
    //Comment Needed : Isa
    private void OnDestroy()
    {
        SnapZoneManager.Instance.Hovering -= OnHover;
    }

    //Comment Needed : Isa
    public bool CheckIfInInventory(SnapObjectType potentialSnapObject)
    {
        //check if the thing we're trying to snap into the snapzone is a valid thing to snap in.
        bool snapAvailable = false;
        foreach (SnapObjectType objectType in snapZoneInventory)
        {
            if (potentialSnapObject == objectType)
            {
                //we found the snap object type in the inventory of the snap zone, meaning we can now snap that object in.
               
                Debug.Log("That's valid, lets snap!");
                snapAvailable = true;
                SnapIn(potentialSnapObject);
                //SnapZoneManager.Instance.FireSnapReplacement(potentialSnapObject);


            }
        }
        return snapAvailable;
    }

    //Comment Needed : Isa //made public for exception of block and parallels warning
    public void SnapIn(SnapObjectType recievedSnapObject)
    {
        print(gameObject.name + " " + recievedSnapObject.ToString());
        inventoryObjectModelDictionary[currentlySnapped].SetActive(false);
        SnapZoneManager.Instance.FireSnapReplacement(currentlySnapped);
        if(recievedSnapObject == SnapObjectType.Chuck){

            correctToolSnapped.Invoke();
            //Debug.Log("SNAPPING CHUCK");
            //inventoryObjectModelDictionary[recievedSnapObject].transform.Translate(Vector3.forward*unlockedChuckOffsetMeters, Space.Self);
        }
        inventoryObjectModelDictionary[recievedSnapObject].SetActive(true);
        Debug.Log("Removed " + inventoryObjectModelDictionary[currentlySnapped] + 
            " and replaced it with " + inventoryObjectModelDictionary[recievedSnapObject]);

        currentlySnapped = recievedSnapObject;

        correctToolEnteredZone.Invoke();
    }

    //Comment Needed : Isa
    public void OnHover(bool hovering)
    {
        //Debug.Log("On Hover called on " + gameObject.name + " with a value of: " + hovering);
        rend.enabled = hovering;
    }

    private void OnEnable()
    {
        Enabled.Invoke(true);
    }
}
