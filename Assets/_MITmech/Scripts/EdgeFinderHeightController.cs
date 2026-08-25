using UnityEngine;
using UnityEngine.Events;

public class EdgeFinderHeightController : MonoBehaviour
{
    public MITmech.ITS.Task xOriginSet;
    public MITmech.ITS.Task yOriginSet;

    public float radius;
    [SerializeField]
    bool correctHeight;
    public bool CorrectHeight
    {
        get { return correctHeight; }
        set
        {
            if (correctHeight != value)
            {
                correctHeight = value;
                EdgeFinderCorrectHeight.Invoke(correctHeight);
            }
        }
    }
    [SerializeField]
    bool xAligned;
    public bool XAligned
    {
        get { return xAligned; }
        set
        {
            if (xAligned != value)
            {
                xAligned = value;
                if (!xOriginSet.completed)
                    EdgeFinderCorrectX.Invoke(xAligned);

            }
        }
    }
    [SerializeField]
    bool yAligned;
    public bool YAligned
    {
        get { return yAligned; }
        set
        {
            if (yAligned != value)
            {
                yAligned = value;
                if (!yOriginSet.completed)
                EdgeFinderCorrectY.Invoke(yAligned);
            }

        }
    }
    public Transform blockLeftTransform;
    public Transform blockBackTransform;
    public Transform blockRightTransform;
    public Transform blockFrontTransform;
    public Transform blockTopTransform;

    public UnityEvent<bool> EdgeFinderCorrectHeight;
    public UnityEvent<bool> EdgeFinderCorrectX;
    public UnityEvent<bool> EdgeFinderCorrectY;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (transform.hasChanged)
        {
            CorrectHeight =
                (transform.position.y + radius >= blockTopTransform.position.y
                && transform.position.y - radius <= blockTopTransform.position.y);

            if (!correctHeight)
                return;





        }
    }

    public void CheckIfXAligned()
    {
        if (transform.position.z <= blockFrontTransform.position.z + radius
    || transform.position.z >= blockBackTransform.position.z - radius)
        {
            Debug.Log($"{transform.position.z} , {blockFrontTransform.position.z} , {blockBackTransform.position.z}");
            return;

        }

            XAligned = (transform.position.x <= blockLeftTransform.position.x + radius
    && transform.position.x >= blockLeftTransform.position.x - radius);
    }

    public void CheckIfYAligned()
    {


        if (transform.position.x <= blockLeftTransform.position.x + radius
    || transform.position.x >= blockRightTransform.position.x - radius)
        {
            Debug.Log($"{transform.position.x} , {blockLeftTransform.position.x} , {blockRightTransform.position.x}");
            return;

        }

        YAligned = (transform.position.z <= blockBackTransform.position.z + radius
    && transform.position.z >= blockBackTransform.position.z - radius);
    }
    public void PoppedOut()
    {
        //EdgeFinderCorrectX.Invoke(XAligned);
        //EdgeFinderCorrectY.Invoke(YAligned);
    }
    public void ResetPop()
    {
        CheckIfXAligned();
        CheckIfYAligned();
        //EdgeFinderCorrectY.Invoke(false);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
