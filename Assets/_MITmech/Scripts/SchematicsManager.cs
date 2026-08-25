using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class SchematicsManager : MonoBehaviour
{
    [Header("Current Guide Step")]
    public TutorialStep step;

    [Header("Schematic")]
    public GameObject schematicButton; //This GameObject likely represents a button related to the schematic used to trigger the display or interaction with schematics.
    public GameObject currentSchematic; //Represents the currently displayed schematic
    public GameObject[] schematicOptions; //An array of schematic options that the system can randomly choose from

    [Header("Assesment")]
    public AssessmentManager assessmentManager; //Can this be renamed?
    int round = 1; //Comment Needed : Isa

    [Header("Block Parameters")]
    public Drillable block;
    public float blockDimensionAdjustFactor = 0.0254f; //used for unit conversion or scaling
    public float lineRendererWidth = 0.015f; //Representetive Line Width 
    public float lineRenderScaleAdjustment = 0.183f; //Scale adjust for box based on scale of UI element (needs adjustment) 
    public Material lineRenderMaterial;

    [Header("Hole Parameters")]
    public GameObject currentHolesPanel;
    public GameObject holePrefab; //GenerateCurrentHoles method instantiates the holePrefab. The instantiated hole is made a child of the currentHolesPanel and retains its local position and rotation.
    bool showingCurrentHoles = false;
    public float spawnHoleFactor = 5.3f;
    public float lineRendererLegthHoleLocation = 0.5f; //Representetive Line Length 

    //Comment Needed : Isa
    private Vector2[] holeposesarray = {new Vector2(1,1),
                                        new Vector2(5,3),
                                        new Vector2(4.5f,2.5f),
                                        new Vector2(1.5f,2),
                                        new Vector2(3,1),
                                        new Vector2(1,3),
                                        new Vector2(2.5f,2.5f),
                                        new Vector2(4,2),};

    //Comment Needed : Isa
    private enum ordinateDimensionXDirections {
        positiveX,
        negativeX
    }

    //Comment Needed : Isa
    private enum ordinateDimensionYDirections {
        positveY,
        negativeY
    }

    //Comment Needed : Isa
    void Start()
    {
        currentSchematic.GetComponent<Schematics>().SetDisplayed(true);
        step.promptText = "Set the x and y position according to the schematics, " +
            "at x = " + currentSchematic.GetComponent<Schematics>().xPos + " and y = -" + currentSchematic.GetComponent<Schematics>().yPos
            + ". then slowly move the Z axis quill down to bring the tool into the block. Only drill until you reach the sloped portion of the center drill. ";
        schematicButton.SetActive(false);
        assessmentManager.dimensionX = currentSchematic.GetComponent<Schematics>().xPos;
        assessmentManager.dimensionZ = currentSchematic.GetComponent<Schematics>().yPos;
        showingCurrentHoles = false;

    }

    //Comment Needed : Isa
    private void Update()
    {
        if (!schematicButton.activeSelf) {
            schematicButton.SetActive(true);
        }
    }

    //Comment Needed : Isa
    public void GenerateNewSchematic()
    {
        round++;

        if (round > schematicOptions.Length)
        {
            foreach (Transform child in transform) { child.gameObject.GetComponent<Schematics>().SetDisplayed(false); }
            round = 1;
        }

        int index = UnityEngine.Random.Range(0, schematicOptions.Length);
        Debug.Log("generating new shchematics, index: " + index);

        while (schematicOptions[index].GetComponent<Schematics>().GetDisplayed()) {
            index = UnityEngine.Random.Range(0, schematicOptions.Length);
            Debug.Log("generating new shchematics, index: " + index);
        }

        currentSchematic = schematicOptions[index];
        currentSchematic.GetComponent<Schematics>().SetDisplayed(true);
        step.promptText = "Set the x and y position according to the schematics, " +
            "at x = " + currentSchematic.GetComponent<Schematics>().xPos + " and y = " + currentSchematic.GetComponent<Schematics>().yPos
            +". then slowly move the Z axis quill down to bring the tool into the block.Only drill until you reach the sloped portion of the center drill. ";
        assessmentManager.dimensionX = currentSchematic.GetComponent<Schematics>().xPos;
        assessmentManager.dimensionZ = currentSchematic.GetComponent<Schematics>().yPos;
    }

    //Comment Needed : Isa
    public void DisplaySchematics()
    {
        schematicButton.SetActive(true);
        currentSchematic.SetActive(true);
    }

    //Comment Needed : Isa
    public void DisplayCurrentHoles()
    {
        if (showingCurrentHoles == false)
        {
            schematicButton.SetActive(true);
            currentSchematic.SetActive(true);
            currentHolesPanel.SetActive(true);
            GenerateCurrentHoles();
            showingCurrentHoles = true;
        }
        else {
            currentHolesPanel.SetActive(false);
            showingCurrentHoles = false;
        }
    }

    //Comment Needed : Isa
    public void CloseSchematics()
    {
        schematicButton.SetActive(false);
        showingCurrentHoles = false;
        foreach (Transform child in transform) child.gameObject.SetActive(false);
    }

    //Comment Needed : Isa
    void GenerateCurrentHoles() {
        foreach (Transform child in currentHolesPanel.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        
        //origin
        GenerateSingleOrdinateDimension( 0f, 0f, 0f, ordinateDimensionXDirections.positiveX, ordinateDimensionYDirections.positveY, 0.3f, 0.3f, 0.0f, false);
        //block size Y Length
        GenerateSingleOrdinateDimension(6f, 0f, 0f, ordinateDimensionXDirections.positiveX, ordinateDimensionYDirections.positveY, 0f, 0.3f, 0.0f, false);
        //block size X Length
        GenerateSingleOrdinateDimension(0f, 4f, 0f, ordinateDimensionXDirections.positiveX, ordinateDimensionYDirections.positveY, 0.3f, 0f, 0.0f, false);
        //for(int i =0;i< holeposesarray.Length;i++)    for testing all holes simultaneously
        foreach (HoleInfo holeInfo in block.drilledHoles)
        {
            Debug.Log("generating hole " + holeInfo.holeNumber);
            //Debug.Log("generating hole " + i);
            
            GameObject hole = Instantiate(holePrefab, currentHolesPanel.transform, false);
            

            float x = (holeInfo.holeStartPos.x/blockDimensionAdjustFactor - 3) * spawnHoleFactor;
            float y = (holeInfo.holeStartPos.z/blockDimensionAdjustFactor - 2 ) * spawnHoleFactor;
            //float x = (holeposesarray[i].x - 3) * spawnHoleFactor;
            //float y = -(holeposesarray[i].y - 2 ) * spawnHoleFactor;

            GenerateSingleOrdinateDimension( (x/spawnHoleFactor + 3f), ((-y)/spawnHoleFactor + 2f), holeInfo.holeDepth, ordinateDimensionXDirections.positiveX, ordinateDimensionYDirections.positveY, lineRendererLegthHoleLocation, lineRendererLegthHoleLocation, 0.0f, holeInfo.drilledThrough);
            //GenerateSingleOrdinateDimension( (x/spawnHoleFactor + 3f), ((-y)/spawnHoleFactor + 2f), Convert.ToSingle(i) * 0.2f, ordinateDimensionXDirections.positiveX, ordinateDimensionYDirections.positveY, 0.4f, 0.4f, 0.0f, false);

            Debug.Log("x and y pos are : " + holeInfo.holeStartPos.x + " and " + holeInfo.holeStartPos.z);
            Debug.Log("x and y are : " + x + " and " + y);
            hole.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
        }
    
    
    }

    //Comment Needed : Isa
    void GeneratePositionTMPText(GameObject ordinateDimensionParent, float xCoordinateInches, float yCoordinateInches, float holeDepthInches, float xDirectionFactor, float yDirectionFactor, float xLineLengthInches, float yLineLengthInches, bool drilledThrough){
        GameObject holeTextParent = new GameObject();
        holeTextParent.name = "hole Text " + holeTextParent.transform.GetSiblingIndex().ToString();
        holeTextParent.transform.SetParent(ordinateDimensionParent.transform);
        holeTextParent.transform.rotation = Quaternion.LookRotation(transform.forward, transform.up);
        holeTextParent.layer = holeTextParent.transform.parent.gameObject.layer;

        GameObject holeXPositionText = new GameObject();
        holeXPositionText.name = "holeXPositionText " + holeXPositionText.transform.GetSiblingIndex().ToString();
        holeXPositionText.AddComponent<TextMeshProUGUI>();
        holeXPositionText.transform.SetParent(holeTextParent.transform);
        holeXPositionText.transform.rotation = Quaternion.LookRotation(transform.forward, transform.up);
        holeXPositionText.layer = holeXPositionText.transform.parent.gameObject.layer;

        GameObject holeYPositionText = new GameObject();
        holeYPositionText.name = "holeYPositionText " + holeXPositionText.transform.GetSiblingIndex().ToString();
        holeYPositionText.AddComponent<TextMeshProUGUI>();
        holeYPositionText.transform.SetParent(holeTextParent.transform);
        holeYPositionText.transform.rotation = Quaternion.LookRotation(transform.forward, transform.up);
        holeYPositionText.layer = holeYPositionText.transform.parent.gameObject.layer;
        
        GameObject holeDepthText = new GameObject();
        holeDepthText.AddComponent<TextMeshProUGUI>();
        holeDepthText.name = "holeDepthText " + holeDepthText.transform.GetSiblingIndex().ToString();
        holeDepthText.transform.SetParent(holeTextParent.transform);
        holeDepthText.transform.rotation = Quaternion.LookRotation(transform.forward, transform.up);
        holeDepthText.layer = holeDepthText.transform.parent.gameObject.layer;

        //y
        if(xLineLengthInches > 0f){
            holeYPositionText.GetComponent<TextMeshProUGUI>().text = ((yCoordinateInches)).ToString("f1");
            holeYPositionText.GetComponent<TextMeshProUGUI>().fontSize = 0.03f;
            holeYPositionText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            holeYPositionText.GetComponent<TextMeshProUGUI>().verticalAlignment = VerticalAlignmentOptions.Middle;
            holeYPositionText.GetComponent<TextMeshProUGUI>().horizontalAlignment = HorizontalAlignmentOptions.Center;

            holeYPositionText.GetComponent<TextMeshProUGUI>().GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            holeYPositionText.GetComponent<TextMeshProUGUI>().color = Color.blue;
            holeYPositionText.GetComponent<TextMeshProUGUI>().transform.position = ordinateDimensionParent.GetComponent<LineRenderer>().GetPosition(0) -  0.04f *xDirectionFactor* currentHolesPanel.transform.right.normalized + (ordinateDimensionParent.GetComponent<LineRenderer>().startWidth*0.25f) * currentHolesPanel.transform.up.normalized;
        }
        
        //x
        if(yLineLengthInches > 0f){
            holeXPositionText.GetComponent<TextMeshProUGUI>().text = ((xCoordinateInches)).ToString("f1");
            holeXPositionText.GetComponent<TextMeshProUGUI>().fontSize = 0.03f;
            holeXPositionText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            holeXPositionText.GetComponent<TextMeshProUGUI>().verticalAlignment = VerticalAlignmentOptions.Middle;
            holeXPositionText.GetComponent<TextMeshProUGUI>().horizontalAlignment = HorizontalAlignmentOptions.Center;

            holeXPositionText.GetComponent<TextMeshProUGUI>().GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            holeXPositionText.GetComponent<TextMeshProUGUI>().color = Color.blue;
            holeXPositionText.GetComponent<TextMeshProUGUI>().transform.rotation = Quaternion.LookRotation(currentHolesPanel.transform.forward, -currentHolesPanel.transform.right);
            holeXPositionText.GetComponent<TextMeshProUGUI>().transform.position = ordinateDimensionParent.GetComponent<LineRenderer>().GetPosition(2) +  0.04f *yDirectionFactor* currentHolesPanel.transform.up.normalized - (ordinateDimensionParent.GetComponent<LineRenderer>().startWidth*0.25f) * currentHolesPanel.transform.right.normalized;
        }

        //depth
        if(holeDepthInches > 0f){
            
            if(drilledThrough){
                holeDepthText.GetComponent<TextMeshProUGUI>().text = "Thru";
            }else{
                holeDepthText.GetComponent<TextMeshProUGUI>().text = holeDepthInches.ToString("f2") +  "\n Depth ";
            }
            holeDepthText.GetComponent<TextMeshProUGUI>().fontSize = 0.02f;
            holeDepthText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            holeDepthText.GetComponent<TextMeshProUGUI>().verticalAlignment = VerticalAlignmentOptions.Middle;
            holeDepthText.GetComponent<TextMeshProUGUI>().horizontalAlignment = HorizontalAlignmentOptions.Center;

            holeDepthText.GetComponent<TextMeshProUGUI>().GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            holeDepthText.GetComponent<TextMeshProUGUI>().color = Color.magenta;
            holeDepthText.GetComponent<TextMeshProUGUI>().transform.rotation = Quaternion.LookRotation(currentHolesPanel.transform.forward, currentHolesPanel.transform.up);
            holeDepthText.GetComponent<TextMeshProUGUI>().transform.position = ordinateDimensionParent.GetComponent<LineRenderer>().GetPosition(1) + 0.06f * currentHolesPanel.transform.right.normalized + 0.08f * currentHolesPanel.transform.up.normalized + (ordinateDimensionParent.GetComponent<LineRenderer>().startWidth*0.25f) * currentHolesPanel.transform.up.normalized;
        }

    }

    //Comment Needed : Isa
    void GenerateSingleOrdinateDimension(float xCoordinateInches, float yCoordinatesInches, float holeDepthInches, ordinateDimensionXDirections xDimensionLineDirection, ordinateDimensionYDirections yDimensionLineDirection, float xLineLengthInches, float yLineLengthInches, float lineOutOfPageOffset, bool drilledThrough)
    {
        GameObject newEmptyOrdinateDimension = new GameObject();

        newEmptyOrdinateDimension.AddComponent<LineRenderer>();

        newEmptyOrdinateDimension.transform.SetParent(currentHolesPanel.transform);
        newEmptyOrdinateDimension.transform.position = currentHolesPanel.transform.position;
        newEmptyOrdinateDimension.transform.rotation = Quaternion.LookRotation(transform.forward, transform.up);
        newEmptyOrdinateDimension.name = "ordinate dimensions " + newEmptyOrdinateDimension.transform.GetSiblingIndex().ToString();


        float x = (xCoordinateInches - 3f)*lineRenderScaleAdjustment;
        float y = (-(yCoordinatesInches) + (2f))*lineRenderScaleAdjustment;

        newEmptyOrdinateDimension.GetComponent<LineRenderer>().material = lineRenderMaterial;
        newEmptyOrdinateDimension.GetComponent<LineRenderer>().alignment = LineAlignment.TransformZ;
        newEmptyOrdinateDimension.GetComponent<LineRenderer>().positionCount = 3;
        newEmptyOrdinateDimension.GetComponent<LineRenderer>().startWidth = lineRendererWidth;
        newEmptyOrdinateDimension.GetComponent<LineRenderer>().endWidth = lineRendererWidth;
        newEmptyOrdinateDimension.GetComponent<LineRenderer>().sortingLayerName = SortingLayer.layers[SortingLayer.layers.Length - 1].name;
        newEmptyOrdinateDimension.layer = newEmptyOrdinateDimension.transform.parent.gameObject.layer;


        float xDirectionFactor = 1f;
        float yDirectionFactor = 1f;

        if(xDimensionLineDirection == ordinateDimensionXDirections.negativeX){
            xDirectionFactor = -1f;
        }
        
        if(yDimensionLineDirection == ordinateDimensionYDirections.negativeY){
            yDirectionFactor = -1f;
        }


        //X Line component end point
        newEmptyOrdinateDimension.GetComponent<LineRenderer>().SetPosition(0, currentHolesPanel.transform.position + (x - xDirectionFactor*(xLineLengthInches)*lineRenderScaleAdjustment)*(currentHolesPanel.transform.right.normalized) + y*(currentHolesPanel.transform.up.normalized) - lineOutOfPageOffset*currentHolesPanel.transform.forward);
        
        //hole center (xCoord, yCoord) position
        newEmptyOrdinateDimension.GetComponent<LineRenderer>().SetPosition(1, currentHolesPanel.transform.position + x*(currentHolesPanel.transform.right.normalized) + y*(currentHolesPanel.transform.up.normalized)  - lineOutOfPageOffset*currentHolesPanel.transform.forward);
        
        //Y Line component end point
        newEmptyOrdinateDimension.GetComponent<LineRenderer>().SetPosition(2, currentHolesPanel.transform.position + (x)*(currentHolesPanel.transform.right.normalized) + (y + yDirectionFactor*yLineLengthInches*lineRenderScaleAdjustment)*(currentHolesPanel.transform.up.normalized) - lineOutOfPageOffset*currentHolesPanel.transform.forward);
        
        newEmptyOrdinateDimension.GetComponent<LineRenderer>().startColor = Color.blue;
        newEmptyOrdinateDimension.GetComponent<LineRenderer>().endColor = Color.blue;

        GeneratePositionTMPText(newEmptyOrdinateDimension, xCoordinateInches, yCoordinatesInches, holeDepthInches, xDirectionFactor,  yDirectionFactor, xLineLengthInches, yLineLengthInches, drilledThrough);

    }
}

