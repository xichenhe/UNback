using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HoleInfo
{
    public int holeNumber;
    public Vector3 holeStartPos;
    public float holeDepth;
    public float holeThickness;
    public bool centerPointDrilled;
    public bool drilledThrough = false;
    public GameObject hole;
    public HoleManager manager;
}

public class Drillable : MonoBehaviour
{
    public Transform referenceZero;

    public Transform rotatingWorldZero;

    public GameObject holeSprite;
    public GameObject BottomholeSprite;

    public List<HoleInfo> drilledHoles;

    public TextMeshProUGUI displayText;

    public int holeCount;

    public float lowestHoleDepth;

    public float currentDrillHeat;
    public float maximumDrillHeatValue;

    public float xDimensions, yDimensions;

    public float minSpinSpeed, maxSpinSpeed;

    public AudioSource drillingSound;
    float startingVolumeDrillingSound;

    public AudioSource rubbingSound;
    float startingVolumeRubbingSound;

    public bool collideWithDrill = false;

    public GameObject steelBlock;
    public float blockDepth;

    public float scaleAdjuster = 1;
    public GameObject interactiveCounterpart;
    public bool copyStatusToCounterBlock;
    public bool VR;

    public float holePrefabfactor = 1.15f;

    

    // Start is called before the first frame update
    void Start()
    {
        drilledHoles = new List<HoleInfo>();
        holeCount = -1;
        drilledHoles.Clear();
        startingVolumeDrillingSound = drillingSound.volume;
        drillingSound.volume = 0;
        startingVolumeRubbingSound = rubbingSound.volume;
        rubbingSound.volume = 0;
        blockDepth = -1.99f;

        xDimensions = xDimensions * scaleAdjuster;
        yDimensions = yDimensions * scaleAdjuster;
        blockDepth = blockDepth * scaleAdjuster;
        lowestHoleDepth = lowestHoleDepth * scaleAdjuster;

    }

    public void TestHole() {
        NewHole(0.5f, new Vector3(0,0.15f,0.085f), true);
    }

    public void NewHole(float sentThickness, Vector3 contactLocation, bool isCenterPointDrill)
    {
        Debug.Log("sent contact location was " + contactLocation);
        Debug.Log("0,0 is at location of: " + rotatingWorldZero.InverseTransformPoint(referenceZero.position));
        Vector3 zeroedReferencePos = contactLocation - rotatingWorldZero.InverseTransformPoint(referenceZero.position);
        zeroedReferencePos = new Vector3(zeroedReferencePos.x, 0, zeroedReferencePos.z);
        Debug.Log("zeroed out contact location is: " + zeroedReferencePos);
        Debug.Log("sent thickness: " + sentThickness);
        Debug.Log("zeroedReferencePos.x: " + zeroedReferencePos.x);
        Debug.Log("zeroedReferencePos.z: " + zeroedReferencePos.z);
        Debug.Log(" xDimensions: " + xDimensions);
        Debug.Log(" yDimensions: " + yDimensions);
        
        bool tooCloseToTheEdge = (zeroedReferencePos.x + sentThickness / 2f) > xDimensions ||
            (zeroedReferencePos.x - sentThickness / 2f) < 0f ||
            (zeroedReferencePos.z + sentThickness / 2f) > yDimensions ||
            (zeroedReferencePos.z - sentThickness / 2f) < 0;
        
        if (tooCloseToTheEdge)
        {
            FailureState.Instance.SystemFailure("You drilled too close to the edge.");
            return;
        }

        //check if we've drilled this position before.
        bool newHole = true;
        foreach (HoleInfo holeInfo in drilledHoles)
        {
            float distanceFromHole = (zeroedReferencePos - holeInfo.holeStartPos).magnitude;
            bool sameHole = distanceFromHole < holeInfo.holeThickness / 5;
            bool tooCloseToOtherHole = distanceFromHole > holeInfo.holeThickness / 5 && distanceFromHole < (holeInfo.holeThickness / 2 + sentThickness / 2);
            
            if (sameHole)
            {
                Debug.Log("Drilling into the same hole!");
                holeCount = holeInfo.holeNumber;
                newHole = false;

                //updating radius of hole
                if (drilledHoles[holeCount].holeThickness < sentThickness)
                {
                    Debug.Log("Updating radius of hole..new sent thickness = " + sentThickness);
                    float thicknessAdjuster = sentThickness / drilledHoles[holeCount].holeThickness;
                    drilledHoles[holeCount].hole.GetComponent<HoleManager>().adjustDimension(thicknessAdjuster);
                    drilledHoles[holeCount].holeThickness = sentThickness;
                }
                
                if (!drilledHoles[holeCount].centerPointDrilled && !isCenterPointDrill)
                {
                    FailureState.Instance.DisplayWarning("The pilot hole wasn't deep enough. You will have an inconsistent hole diameter.");
                }
            }
            else if (tooCloseToOtherHole)
            {
                Debug.Log("You're too close to an existing hole and that's dangerous.");
                FailureState.Instance.SystemFailure("You tried to drill too close to an already existing hole.");
            }
        }

        if (newHole)
        {
            holeCount = drilledHoles.Count;
            Debug.Log("Starting new hole #" + holeCount);
            HoleInfo holeData = new HoleInfo();
            holeData.centerPointDrilled = false;
            holeData.holeThickness = sentThickness;
            
            if (!isCenterPointDrill)
            {
                FailureState.Instance.DisplayWarning("You drilled without a pilot hole, so your hole might not be the correct diameter.");
            }

            holeData.holeStartPos = zeroedReferencePos;
            holeData.holeNumber = holeCount;
            Debug.Log("Hole #" + holeData.holeNumber + " has a desired thickness of " + sentThickness + " and a start location of " + holeData.holeStartPos);

            // instantiating new hole
            GameObject holeSpriteObject = Instantiate(holeSprite);
            holeSpriteObject.transform.position = rotatingWorldZero.TransformPoint(contactLocation);
            holeSpriteObject.transform.parent = steelBlock.transform;
            holeSpriteObject.transform.localScale *= (sentThickness * holePrefabfactor);
            holeData.hole = holeSpriteObject;
            holeData.manager = holeData.hole.GetComponent<HoleManager>();
            holeData.manager.isCounter = copyStatusToCounterBlock;

            if (copyStatusToCounterBlock)
            {
                GameObject holeSpriteObject2 = Instantiate(holeSprite);
                Destroy(holeSpriteObject2.GetComponent<HoleManager>());
                Destroy(holeSpriteObject2.GetComponent<CapsuleCollider>());
                holeSpriteObject2.transform.parent = interactiveCounterpart.transform;
                holeSpriteObject2.transform.position = holeSpriteObject.transform.position;
                holeSpriteObject2.transform.localScale *= (sentThickness * holePrefabfactor);
                holeData.manager.counterTophole = holeSpriteObject2;
            }
            
            drilledHoles.Add(holeData);

            string data =
                $"hole_num_{holeCount}|x_{zeroedReferencePos.x:F3}|z_{zeroedReferencePos.z:F3}|drill_type_{(isCenterPointDrill ? "center" : "standard")}|diameter_{sentThickness:F3}|rpm_{Spinning.Instance.SpinSpeed}|used_center_drill_{isCenterPointDrill}";

            // ============= LOGGING ADDED HERE (AT THE END OF newHole) =============
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 
            // ====================================================================
            // string[] eventMarker = new string[Presets.EventMarkerChannelNum];
            // eventMarker[(int)Presets.EventMarkerChannelInfo.EventType] = "hole_started";
            // eventMarker[(int)Presets.EventMarkerChannelInfo.EventSubType] = data;
            // eventMarker[(int)Presets.EventMarkerChannelInfo.UnityTime] = Time.realtimeSinceStartup.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
            // // TODO: missing step number and step name
            // eventMarker[(int)Presets.EventMarkerChannelInfo.StepNumber] = 
            //
            // EventMarkerLSLOutletController.Instance?.pushEventMarker(eventMarker, LSL.LSL.local_clock());
        }
    }

    public void UpdateHoleDepth(Vector3 depthLocation, DrillBit bit, bool isCenterPointDrill)
    {
        Debug.Log("reference zero position is: " + rotatingWorldZero.InverseTransformPoint(referenceZero.position));
        Vector3 zeroedDepth = depthLocation - rotatingWorldZero.InverseTransformPoint(referenceZero.position);
        Debug.Log("Zeroed Depth location is " + zeroedDepth.x + ", " + zeroedDepth.y + ", " + zeroedDepth.z);

        Vector3 sentHolePosition = new Vector3(zeroedDepth.x, 0f, zeroedDepth.z);
        Debug.Log("Sent Hole Position zeroed out is " + sentHolePosition + " while hole #" + holeCount + " has starting position of " + drilledHoles[holeCount].holeStartPos);
        bool movedLaterally = (sentHolePosition - drilledHoles[holeCount].holeStartPos).magnitude > drilledHoles[holeCount].holeThickness / 5;
        if (movedLaterally)
        {
            Debug.Log("moved laterally with magnitude of " + (sentHolePosition - drilledHoles[holeCount].holeStartPos).magnitude);
            FailureState.Instance.SystemFailure("You moved the drill laterally while still inside the block and now your drill is broken.");
            drillingSound.volume = 0;
            rubbingSound.volume = 0;
        }
        else if (drilledHoles[holeCount].holeDepth > zeroedDepth.y)
        {
            if (zeroedDepth.y < bit.depthTooDangerous)
            {
                FailureState.Instance.SystemFailure("This bit shouldn't go this deep, you might clog the flutes and fuse the metal.");
                drillingSound.volume = 0;
                rubbingSound.volume = 0;
            }

            //adding the check for the centerpoint drill
            if (!drilledHoles[holeCount].centerPointDrilled && isCenterPointDrill)
            {
                if (zeroedDepth.y < bit.minDepthToCenterPoint)
                {
                    drilledHoles[holeCount].holeThickness = bit.drillThickness;
                    drilledHoles[holeCount].centerPointDrilled = true;
                    Debug.Log("this hole is center point drilled.");
                }
            }

            drilledHoles[holeCount].holeDepth = Mathf.Max(zeroedDepth.y, lowestHoleDepth);
            Debug.Log("Hole #" + holeCount + " depth is now " + drilledHoles[holeCount].holeDepth);
            //turn up the drillingSound
            drillingSound.volume = startingVolumeDrillingSound;
            rubbingSound.volume = 0;
            //This should also add heat to the drill
            if (!isCenterPointDrill) {
                currentDrillHeat += Time.deltaTime * 2;
            }
            
        }
        else if (drilledHoles[holeCount].holeDepth > zeroedDepth.y - .01f)
        {
            //Debug.Log("You're a little too close to rubbing.");
            drillingSound.volume = 0;
            rubbingSound.volume = startingVolumeRubbingSound;
            if (!isCenterPointDrill)
            {
                currentDrillHeat += Time.deltaTime * 0.5f;
            }
        }
        else
        {
            drillingSound.volume = 0;
            rubbingSound.volume = 0;
        }

        //only triggered once - adding bottom hole
        if ((!drilledHoles[holeCount].drilledThrough) && drilledHoles[holeCount].holeDepth <= blockDepth)
        {
            Debug.Log("generated hole on bottom");
            // when hole depth exceeds depth of block, add hole picture on bottom of block
            GameObject holeSpriteObject = Instantiate(BottomholeSprite);
            holeSpriteObject.transform.parent = steelBlock.transform;
            holeSpriteObject.transform.localPosition = new Vector3(drilledHoles[holeCount].hole.transform.localPosition.x, -0.001f, drilledHoles[holeCount].hole.transform.localPosition.z);
            //instead of adding it to the trigger box, add to the steel block
            holeSpriteObject.transform.localScale *= (drilledHoles[holeCount].holeThickness * holePrefabfactor);
            

            if (VR)
            {
                Debug.Log("generated hole on bottom VR");
                Debug.Log("generated hole on bottom before" + holeSpriteObject.transform.eulerAngles.x);
                holeSpriteObject.transform.eulerAngles = new Vector3(
                    holeSpriteObject.transform.eulerAngles.x,
                    holeSpriteObject.transform.eulerAngles.y,
                    holeSpriteObject.transform.eulerAngles.z);
                Debug.Log("generated hole on bottom now" + holeSpriteObject.transform.eulerAngles.x);
            }
            else
            {
                holeSpriteObject.transform.eulerAngles = new Vector3(
                    holeSpriteObject.transform.eulerAngles.x,
                    holeSpriteObject.transform.eulerAngles.y,
                    holeSpriteObject.transform.eulerAngles.z);
            }
            drilledHoles[holeCount].manager.bottomhole = holeSpriteObject;
            

            if (copyStatusToCounterBlock)
            {
                // repeating this again for other block
                GameObject holeSpriteObject2 = Instantiate(BottomholeSprite);
                holeSpriteObject2.transform.parent = interactiveCounterpart.transform;
                holeSpriteObject2.transform.position = holeSpriteObject.transform.position;
                holeSpriteObject2.transform.localScale *= (drilledHoles[holeCount].holeThickness * holePrefabfactor);
                if (VR)
                {
                    holeSpriteObject2.transform.eulerAngles = new Vector3(
                        holeSpriteObject.transform.eulerAngles.x,
                        holeSpriteObject.transform.eulerAngles.y,
                        holeSpriteObject.transform.eulerAngles.z);
                }
                else
                {
                    holeSpriteObject2.transform.eulerAngles = new Vector3(
                        holeSpriteObject.transform.eulerAngles.x,
                        holeSpriteObject.transform.eulerAngles.y,
                        holeSpriteObject.transform.eulerAngles.z);
                }
                drilledHoles[holeCount].manager.counterBottomhole = holeSpriteObject2;
            }

            drilledHoles[holeCount].manager.DrillThrough();
            drilledHoles[holeCount].drilledThrough = true;
            SilenceSound();
        }
    }

    public void DisplayHoleData()
    {
        if (drilledHoles.Count > 0)
        {
            displayText.text = "";
            foreach (HoleInfo holeInfo in drilledHoles)
            {
                string displayXPos = (holeInfo.holeStartPos.x/ scaleAdjuster).ToString();
                displayXPos = displayXPos.Substring(0, Mathf.Min(displayXPos.Length, 5));
                string displayZPos = (4 - (holeInfo.holeStartPos.z / scaleAdjuster)).ToString();
                displayZPos = displayZPos.Substring(0, Mathf.Min(displayZPos.Length, 5));
                string displayDepth = (-holeInfo.holeDepth / scaleAdjuster).ToString();
                displayDepth = displayDepth.Substring(0, Mathf.Min(displayDepth.Length, 5));

                displayText.text += "Hole #" + holeInfo.holeNumber
                    + " at coordinates (" + displayXPos + ", "
                    + displayZPos
                    + ") with a diameter of " + (holeInfo.holeThickness / scaleAdjuster)
                    + " with a depth of " + displayDepth + ". Deburred: " + holeInfo.manager.deburred + "\n";

                Debug.Log("Hole #" + holeInfo.holeNumber + " at coordinates (" +
                    holeInfo.holeStartPos.x + ", " + holeInfo.holeStartPos.z + ") with a diameter of " +
                    holeInfo.holeThickness + " with a depth of " + holeInfo.holeDepth);
            }
        }
        else
        {
            displayText.text = "No holes drilled yet.";
        }

    }



    // Update is called once per frame
    void Update()
    {
        if (currentDrillHeat > maximumDrillHeatValue)
        {
            FailureState.Instance.SystemFailure("Your drill overheated. Try pecking next time.");
        }
        currentDrillHeat = Mathf.Max(currentDrillHeat - Time.deltaTime * 0.7f, 0);

        // for debugging
        // drill heat panel
        //GameObject.Find("Drill Heat [ Text ]").GetComponent<TextMeshProUGUI>().text = currentDrillHeat.ToString("#.##");
        // hole depth
        DisplayHoleData();

        if (!Spinning.Instance.isSpinning)
        {
            SilenceSound();
        }
    }

    public void SilenceSound()
    {
        drillingSound.volume = 0f;
        rubbingSound.volume = 0f;
    }

    /* tutorial */
    // determine if something is touching the block
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("OKToTouch"))
        {
            collideWithDrill = true;
        }
    }

    // determine if something is touching the block
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("OKToTouch"))
        {
            collideWithDrill = false;
        }
    }
}
