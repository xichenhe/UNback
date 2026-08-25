using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AssessmentManager : MonoBehaviour
{
    // auto generate a prompt. when in trigger with box, check hole and determine a score. 
    public float dimensionX;
    public float dimensionZ;
    public GameObject CheckingObject;
    public Drillable drillableBlock;
    public TextMeshProUGUI TextDisplay;
    public float AOffset;
    public float BOffset;
    public float COffset;
    public float scaleAdjuster;
    public UnityEngine.UI.Button goBackButton;

    // Start is called before the first frame update
    private void OnEnable()
    {
        TextDisplay.text = "Please drill a hole following the schematic." +
            "After you're done, put it in the gray bin to your left.";
        goBackButton.enabled = false;
    }

    public void DisplayPrompt() {
        TextDisplay.text = "Now, drill a hole on the block that is " + dimensionX + " in. from the left, and " + dimensionZ + " in from the top. " +
            "After you're done, put it in the gray bin to your left.";
        goBackButton.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == CheckingObject)
        {
            TextDisplay.text = AssessBlock();
            goBackButton.enabled = true;
        }
    }

    private string AssessBlock() {
        if (drillableBlock.holeCount == 0) {
            return "No hole drilled on your block.";

        }

        string output = "";
        // check each hole and output the best.
        float xOffset = 6f;
        float zOffset = 4f;
        float totalOffset = 100f;
        int holeNum = 0;

        for (int i = 0; i < drillableBlock.holeCount; i++) {
            float tempX = 0;
            float tempZ = 0;
            tempX = Mathf.Abs(dimensionX - (drillableBlock.drilledHoles[i].holeStartPos.x/scaleAdjuster));
            tempZ = Mathf.Abs(dimensionZ - (drillableBlock.drilledHoles[i].holeStartPos.z/scaleAdjuster));
            if ((tempZ+tempX) <= totalOffset)
            {
                xOffset = tempX;
                zOffset = tempZ;
                totalOffset = tempX + tempZ;
                holeNum = i;
            }
        }

        output += "Your best hole is hole #" + holeNum +" , " + xOffset + " inches off horizontally and " + zOffset + " inches off vertically.";
        if (xOffset + zOffset <= AOffset)
        {
            output += "Great Job!!";
        }
        else if (xOffset + zOffset <= BOffset)
        {
            output += "Good job.";
        }
        else if (xOffset + zOffset <= COffset) {

            output += "Practice makes perfect. You'll do better with more practice!";
        } else {

            output += "You did bad. Try again.";
        }

            return output;
    }
}
