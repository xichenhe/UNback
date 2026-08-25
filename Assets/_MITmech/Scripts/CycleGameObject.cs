using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CycleGameObject : MonoBehaviour
{
public void CycleGameObjectStatus()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}
