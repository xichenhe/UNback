using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FailureState;

public class FailOnCollision : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collided with something.");


    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Drillable"))
        {
            FailureState.Instance.SystemFailure("Your chuck should not be hitting the drillable piece.");
        }
        else if (other.gameObject.CompareTag("Untagged"))
        {
            //do we really care about when the chuck hits stuff by accident? This triggers when removing the chuck, which is annoying.
            //FailureState.Instance.DisplayWarning("Careful where you're swinging that chuck!");
        }
        else if (other.gameObject.CompareTag("Controller"))
        {
            if (Spinning.Instance.isSpinning)
            {
                FailureState.Instance.SystemFailure("You touched the spinning chuck with your hands.");
            }
        }
    }
}
