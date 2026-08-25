using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeactivateList : MonoBehaviour
{
    private bool deactivateObjectList = false;
    public GameObject[] objectList;

    void Start()
    {
    }

    public void ObjectDeactivateList()
    {
        foreach (GameObject o in objectList)
        {
            o.SetActive(deactivateObjectList);
        }
    }

}
