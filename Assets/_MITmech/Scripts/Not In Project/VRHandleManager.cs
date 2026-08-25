using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRHandleManager : MonoBehaviour
{

    public Transform target;
    Rigidbody rb;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        rb.MovePosition(target.transform.position);
    }

    public void UpdateGrabbable(GameObject g)
    {
        StartCoroutine(UpdateGrabbableCoroutine(g));
    }

    IEnumerator UpdateGrabbableCoroutine(GameObject g)
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            g.transform.position = transform.position;
            yield break;
        }
    }
}
