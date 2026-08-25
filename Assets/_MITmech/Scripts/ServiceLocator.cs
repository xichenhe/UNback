using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MITmech.ITS;

public class ServiceLocator : MonoBehaviour
{
    public static ServiceLocator Instance { get; private set; }
    public ITSManager ITSManager;
    private void Awake()
    {
        Instance = this;
    }
}
