using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Draggable2D : MonoBehaviour
{
    private Vector3 screenPoint;
    private Vector3 offset;

    public bool shouldSnap;
    private Vector3 originalPos;
    public Vector3 snapAnchor;
    public float snapOffset;

    public bool adjustCollider;
    public Vector2 originalSize;
    public Vector2 snappedSize;

    public bool snapped = false;

    public UnityEngine.UI.Image mr;

    private void Start()
    {
        originalPos = gameObject.transform.position;

        //mr = GetComponent<SpriteRenderer>();
    }

    public void Toggle() {
        snapped = !snapped;
        mr.enabled = snapped;
        Debug.Log("toggling.. snapped is " + snapped + " enabled: " + mr.enabled);
    }
    /*
    void OnMouseDown()
    {
        offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
    }

    void OnMouseDrag()
    {
        Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
        Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;
        transform.position = curPosition;
        Debug.Log("current pos: " + curPosition);

    }

    void OnMouseUp()
    {
        if (shouldSnap)
        {
            // if within offset distance, snap
            Debug.Log(Mathf.Sqrt(Mathf.Pow(transform.position.x - snapAnchor.x, 2) + Mathf.Pow(transform.position.y - snapAnchor.y, 2)));
            if (Mathf.Sqrt(Mathf.Pow(transform.position.x - snapAnchor.x, 2) + Mathf.Pow(transform.position.y - snapAnchor.y, 2)) <= snapOffset)
            {
                snapped = true;
                transform.position = snapAnchor;
                if (adjustCollider)
                {
                    GetComponent<BoxCollider2D>().size = snappedSize;
                }

            }
            else // else go back to orignal position
            {
                transform.position = originalPos;
                snapped = false;
                if (adjustCollider)
                {
                    GetComponent<BoxCollider2D>().size = originalSize;
                }
            }
            Debug.Log("snapped: " + snapped);
        }
    }*/
}
