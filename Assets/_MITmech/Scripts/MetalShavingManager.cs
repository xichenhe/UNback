using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MetalShavingManager : MonoBehaviour
{
    public bool hasShaving = true;
    public GameObject brush;
    public TutorialStepButtonPressed step;
    public GameObject shaving1;
    public GameObject shaving2;

    public UnityEvent zoneClearedEvent;

    private void OnTriggerEnter(Collider other)
    {

        if (other.gameObject == brush)
        {
            shaving1.GetComponent<Renderer>().material.color = Color.red;
            shaving2.GetComponent<Renderer>().material.color = Color.red;
            Debug.Log("Brushing");
            if (hasShaving) {
                hasShaving = false;
                StartCoroutine(SwipeOff());
            }
            
        }
    }

    IEnumerator SwipeOff()
    {
        yield return new WaitForSeconds(1.5f);
        hasShaving = false;
        step.Press();
        foreach (Collider c in GetComponents<Collider>())
        {
            Destroy(c);
        }
        Destroy(shaving1);
        Destroy(shaving2);

        zoneClearedEvent.Invoke();

        yield break;
    }
}
