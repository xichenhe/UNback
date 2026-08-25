using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MITmech.ITS
{

public class ITSErrorBroadcaster : MonoBehaviour
{
    public ErrorObject errorObject;

    public void TriggerError()
    { errorObject.TriggerError(); }    
}

}
