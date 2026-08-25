using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapZoneManager : MonoBehaviour
{

    public enum SnapObjectType { Empty, Drill1_8, Drill1_4, Drill3_8, Drill1_2, Drill5_8, Drill3_4, Drill7_8, Drill1Inch, Chuck, ChuckKey, EdgeFinder, SixByFourSteelBlock, Parallels, Mallet, CenterDrill, Caliper, DeburringTool, Brush}
    public static SnapZoneManager Instance;

    public event System.Action<bool> Hovering;
    public event System.Action<SnapObjectType> SnappedObjectReplaced;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    public void FireSnapReplacement(SnapObjectType replacedObjectType)
    {
        SnappedObjectReplaced?.Invoke(replacedObjectType);
    }

    public void FireHoveringEvent(bool hovering)
    {
        Hovering?.Invoke(hovering);
        //Debug.Log("Where we dropping? Tomato town?");
    }
}
