using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MITmech.ITS
{

public class ITSManager : MonoBehaviour
{
    public static ITSManager Instance;
    public InterfaceModel interfaceModel;
    public ErrorAgent errorAgent;
    private void Awake()
    {
        Instance = this;
    }
}

}
