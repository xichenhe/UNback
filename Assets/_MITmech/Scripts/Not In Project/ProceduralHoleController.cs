using System;
using System.Collections.Generic;
using UnityEngine;

//TODO: remove cylinder mesh implementation and add Procedural Mesh cylinders so it is easier to make bottom transparent when thru hole

[RequireComponent(typeof(MeshRenderer))]

public class ProceduralHoleController : MonoBehaviour
{
    private MeshRenderer quadSurfaceMeshRenderer;
    private Material quadSurfaceMaterial;
    private Texture2D multipleHoleTexture;

    public bool isSurfaceQuadOnSideOfBlock = false;
    [SerializeField] private List<Vector2> localHolePositions;
    [SerializeField] private List<float> localHoleDepths;
    [SerializeField] private List<float> worldHoleDiameters;
    [SerializeField] private List<bool> holesDrilledThrough = new List<bool>();
    [SerializeField] private bool drillBitCurrentlyCollidingWithThisQuadsBoxCollider = false;
    private List<GameObject> holeMeshes;
    private float currentWorldHoleDiameterInMeters;
    private float thisQuadLargestWorldScaleComponent;

    public Material holeMeshMaterial;
    public SnapCalipersToStock blockStock;
    public float sameHoleDistanceThresholdInMillimeters = 0.5f;
    public static Color opaqueBlockColor = new Color(0.4f, 0.4f, 0.4f, 1.0f); //color of the rest of the block
    public float numSidesOf3DHoleMesh = 20f;
    private float polygonHalfAngle = 180f;
    private Color transparent = new Color(0.0f, 0.0f, 0f, 0f);

    //used in the OnTriggerEvent to reference any drillBit object without haveing to have a public field that gets the current drill bit or an event call
    //Probably not a good idea to have two drill bits in at the same time
    //Drill bit object just needs a capsule collider that has it's lowest point at drill tip
    private GameObject drillBitObject;
    private Vector3 localDrillBitOffsetToDrillTip;

    private string placeHolderString = "(No Hole Mesh)"; //used to differentiate between empty cylinder gameobjects that have no mesh because it was created by another quad face because it's a thru hole

    private List<int> quadHoleIndicesToBlockParentHoleIndices = new List<int>();
    private float drillBitHalfLength = 0f;
    //TODO: find tip of drill by getting drill transform and offset from quad surface only recorded on trigger enter
    //TODO: get render bounds smallest size component to get the scale/diameter of the drill without needing a custom drill object class and don't have to use lossyScale either
    void OnTriggerEnter(Collider collision){
        //Check if colliding with sibling colliders, parent colliders, or child colliders, and if so exit and don't change drill bit variables
        if(collision.gameObject.transform.IsChildOf(transform.parent) || collision.gameObject.transform.IsChildOf(transform) || collision.gameObject.transform.name == transform.parent.name){
            return;
        }

        //localDrillBitOffsetToDrillTip =  Vector3.Project(transform.InverseTransformVector(collision.gameObject.transform.position - transform.position), Vector3.forward); 
        
        if(collision.gameObject.name.ToLower().Contains("drill") && collision.gameObject.name.ToLower().Contains("shader") && Spinning.Instance.isSpinning && ClampMovement.Instance.isClamped)
        {
            drillBitCurrentlyCollidingWithThisQuadsBoxCollider = true;

            drillBitObject = collision.gameObject;

            drillBitHalfLength = Mathf.Max(drillBitObject.transform.lossyScale.x, Mathf.Max(drillBitObject.transform.lossyScale.y, drillBitObject.transform.lossyScale.z));

            currentWorldHoleDiameterInMeters = Mathf.Min(collision.gameObject.transform.lossyScale.x, Mathf.Min(collision.gameObject.transform.lossyScale.y, collision.gameObject.transform.lossyScale.z));
        }
    }

    void OnTriggerExit(Collider collision){
        drillBitCurrentlyCollidingWithThisQuadsBoxCollider = false;
    }

    void Start()
    {
        quadSurfaceMeshRenderer = GetComponent<MeshRenderer>();
        quadSurfaceMaterial = quadSurfaceMeshRenderer.material;


        multipleHoleTexture = new Texture2D(2048, 2048, TextureFormat.RGBA32, true, true);

        multipleHoleTexture.name = "multiple_holes_texture_" + transform.GetSiblingIndex();
        multipleHoleTexture.wrapMode = TextureWrapMode.Clamp;
        multipleHoleTexture.filterMode = FilterMode.Trilinear;

        thisQuadLargestWorldScaleComponent = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);

        quadSurfaceMaterial.SetTexture("_BaseMap", multipleHoleTexture);
        quadSurfaceMaterial.SetFloat("_Metallic", 0.95f);
        quadSurfaceMaterial.SetTextureScale("_BaseMap", new Vector2(transform.lossyScale.x/thisQuadLargestWorldScaleComponent, transform.lossyScale.y/thisQuadLargestWorldScaleComponent));

        holeMeshes = new List<GameObject>();

        for(int childIndex = 0;childIndex < transform.parent.childCount;childIndex++){
            if(gameObject.name != transform.parent.GetChild(childIndex).name){
                Physics.IgnoreCollision(gameObject.GetComponent<BoxCollider>(), transform.parent.GetChild(childIndex).GetComponent<Collider>());
            }
        }

        polygonHalfAngle /= numSidesOf3DHoleMesh;


        //sets the initial block quad surface color, and is the only time that all pixels in the texture are iterated over
        InitializeNoHoleTexture(opaqueBlockColor);
    }


    //Removes all cylinder meshes from list, clears lists, and resets transparent holes to opaque this is not well tested might have to be fixed 
    //if it needs to be used, but the parent block object should go through it's children with Procedural Hole component and reset block for them until all 
    //Reset blocks return true, could also do this from here by getting all siblings and reseting them
    public bool ResetThisQuadBlockSurface(){  
        if(localHolePositions.Count == 0){
            return true;
        }

        if(drillBitCurrentlyCollidingWithThisQuadsBoxCollider){
            return false;
        }
        
        InitializeNoHoleTexture(opaqueBlockColor);

        for(int i = 0;i < holeMeshes.Count;i++){
            Destroy(holeMeshes[i]);
        }

        holeMeshes.RemoveAll(s => s==null);
        holeMeshes.Clear();

        localHolePositions.Clear();
        localHoleDepths.Clear();
        worldHoleDiameters.Clear();
        holesDrilledThrough.Clear();

        return true;
    }

    private void InitializeNoHoleTexture(Color surfaceColor){
        for(int y = 0;y < multipleHoleTexture.height; y++){
            for(int x = 0;x < multipleHoleTexture.width; x++){
                multipleHoleTexture.SetPixel(x, y, surfaceColor);
            }   
        }

        multipleHoleTexture.Apply();
    }

    void CreateAndUpdateHoles(){
        bool newHole = false;
        bool sameHole = false;
        bool drilledThroughFromInside = false;
        int holeIndex = 0;

        if(drillBitObject == null){
            return;
        }

        Vector3 localDrillBitTipPosition = transform.InverseTransformVector((drillBitObject.transform.position -(drillBitHalfLength)*drillBitObject.transform.up) - transform.position);
        
        //Check if the drill bit is too high, and if so exit the method
        if(!drillBitCurrentlyCollidingWithThisQuadsBoxCollider){
            return;
        }
        //if drill bit tip is inside block and it's world up direction is aligned with the world forward direction of this quad 
        //which point's into the block then the bit is going to drill through to the other side of this quad if the drill tip local.z is less than 0 so to make sure a
        //cylinder mesh isn't created it returns
        if(Vector3.Angle(drillBitObject.transform.up, transform.forward) > 1f && Vector3.Angle(drillBitObject.transform.up, transform.forward) < 179f){
            //maybe a failure state that drill bit axis is not perpindicular to block surface?
            return;
        }else{
            //drill bit is lined up but if it is still inside block i.e locldrillBitPosition.z is in block in this quads reference frame
            if(Vector3.Angle(drillBitObject.transform.up, transform.forward) < 1f){
                //drill bit is approaching the bottom of stock in towards quad surface
                if(localDrillBitTipPosition.z < 0f){ //went through quad above it and then below this quad
                    drilledThroughFromInside=true;
                }else{
                    return;
                }

            }else if(Vector3.Angle(drillBitObject.transform.up, transform.forward) > 179f){
                if(localDrillBitTipPosition.z > 1f){ //went through the quad below this quad
                    drilledThroughFromInside=true;
                }
            }
        }
        
        /* if(localDrillBitTipPosition.z < 0f){ //went through quad above it and then below this quad
            drilledThroughFromInside=true;
        }else if(localDrillBitTipPosition.z > 1f){ 
            drilledThroughFromInside=true;
        } */


        if(localHolePositions.Count > 0){
            for(int i = 0;i < localHolePositions.Count;i++){

                float worldDistanceFromTipToHolePositionNoDepth = Vector3.Distance(transform.TransformPoint(new Vector3(localDrillBitTipPosition.x, localDrillBitTipPosition.y, 0f)) , transform.TransformPoint(new Vector3(localHolePositions[i].x, localHolePositions[i].y, 0f )));
                
                if(worldDistanceFromTipToHolePositionNoDepth < (sameHoleDistanceThresholdInMillimeters/1000f) && localDrillBitTipPosition.z > localHoleDepths[i] && !holesDrilledThrough[i]){
                    localHoleDepths[i] = Mathf.Clamp(localDrillBitTipPosition.z, 0f, 1f);
                    sameHole = true;
                    newHole = false;
                    holeIndex = i;
                    break;
                }else if(worldDistanceFromTipToHolePositionNoDepth > ((currentWorldHoleDiameterInMeters/2f) + worldHoleDiameters[i]/2f)){
                    newHole = true;
                    sameHole = false;
                }else{
                    newHole = false;
                    sameHole = false;
                }

            }
        }else{
            newHole = true;
        }

        if(!newHole && !sameHole){
            //can add ERROR messages here if needed to say that holes are too close together, too close to the edge, misaligned with the block etc.
            return;
        }
        
        if(newHole){
            holeIndex = localHolePositions.Count;
            holesDrilledThrough.Add(drilledThroughFromInside);
            localHolePositions.Add(new Vector2(localDrillBitTipPosition.x, localDrillBitTipPosition.y));
            worldHoleDiameters.Add(currentWorldHoleDiameterInMeters);
            blockStock.worldHolePositionsMeters.Add(transform.TransformPoint(new Vector3(localHolePositions[holeIndex].x, localHolePositions[holeIndex].y, 0f)));
            blockStock.worldHoleDiametersMeters.Add(currentWorldHoleDiameterInMeters);
            blockStock.isThroughHole.Add(drilledThroughFromInside);
            quadHoleIndicesToBlockParentHoleIndices.Add(blockStock.worldHolePositionsMeters.Count - 1);
            
            if(!drilledThroughFromInside){
                localHoleDepths.Add(localDrillBitTipPosition.z);
                blockStock.worldHoleDepths.Add(transform.TransformVector(new Vector3(0f, 0f, localDrillBitTipPosition.z)).magnitude);
            }else{
                localHoleDepths.Add(1f);
                blockStock.worldHoleDepths.Add(transform.TransformVector(new Vector3(0f, 0f, 1f)).magnitude);
            }
            
            //set the texture pixels to transparent that are within the min and max of the hole position on the surface based on the diameter
            int textureHoleRadiusPixels = Convert.ToInt32(multipleHoleTexture.width*((currentWorldHoleDiameterInMeters/2.0f))/thisQuadLargestWorldScaleComponent);
            
            //TODO: Check to see if this still works when texture width and height are not equal might need to be 
            //flipped(lossyScale x -> y) but works as is and a material scaled square texture is probably better 
            if(thisQuadLargestWorldScaleComponent == transform.lossyScale.y){
                textureHoleRadiusPixels = Convert.ToInt32(multipleHoleTexture.height*((currentWorldHoleDiameterInMeters/2.0f))/thisQuadLargestWorldScaleComponent);
            }

            Vector2 textureHolePosition = new Vector2(multipleHoleTexture.width*localHolePositions[holeIndex].x + multipleHoleTexture.width/2f, multipleHoleTexture.height*localHolePositions[holeIndex].y + multipleHoleTexture.height/2f);
            textureHolePosition = Vector2.Scale(textureHolePosition, quadSurfaceMaterial.GetTextureScale("_BaseMap"));
            
            //Essentially 2D bounds of the circle so not iterating over entire texture
            int textureXmax = Mathf.Clamp(Convert.ToInt32(textureHolePosition.x + (textureHoleRadiusPixels)), 0, multipleHoleTexture.width);
            int textureXmin = Mathf.Clamp(Convert.ToInt32(textureHolePosition.x - (textureHoleRadiusPixels)), 0, multipleHoleTexture.width);
            int textureYmax = Mathf.Clamp(Convert.ToInt32(textureHolePosition.y + (textureHoleRadiusPixels)), 0, multipleHoleTexture.height);
            int textureYmin = Mathf.Clamp(Convert.ToInt32(textureHolePosition.y - (textureHoleRadiusPixels)), 0, multipleHoleTexture.height);

            for(int textureY = textureYmin;textureY < textureYmax; textureY++){
                for(int textureX = textureXmin;textureX < textureXmax; textureX++){
                    Vector2 texturePosition = new Vector2((textureX), (textureY));

                    if(Vector2.Distance(textureHolePosition, texturePosition) < textureHoleRadiusPixels){
                        multipleHoleTexture.SetPixel(textureX, textureY, transparent);
                    }
                }   
            }

            multipleHoleTexture.Apply();

            //create a new cylinder mesh only if not drilled through, otherwise create and empty game object designated by the placeHolder string, 
            //so that all lists are the same length
            if(!drilledThroughFromInside){
                UpdateProceduralHoleMeshObject(holeIndex);
            }else{
                GameObject noMeshHole = new GameObject();
                noMeshHole.transform.SetParent(transform);
                noMeshHole.name = ((currentWorldHoleDiameterInMeters*1000f)/25.4f).ToString("f3") + " inch Diameter Hole "  + noMeshHole.transform.GetSiblingIndex().ToString() + " (Drilled Through Bottom) " + placeHolderString;
                noMeshHole.transform.localPosition = Vector3.zero;

                holeMeshes.Add(noMeshHole);
            }
            

        }else if(sameHole){
            //same hole
            holesDrilledThrough[holeIndex] = drilledThroughFromInside;
            if (worldHoleDiameters[holeIndex] < currentWorldHoleDiameterInMeters)
            {
                worldHoleDiameters[holeIndex] = currentWorldHoleDiameterInMeters;

                ///////////////////////
                // TODO: Only change if hole diameter increases

                /////set the texture pixels to transparent that are within the min and max of the hole position on the surface based on the diameter
                int textureHoleRadiusPixels = Convert.ToInt32(multipleHoleTexture.width * ((currentWorldHoleDiameterInMeters / 2.0f)) / thisQuadLargestWorldScaleComponent);

                //TODO: Check to see if this still works when texture width and height are not equal might need to be 
                //flipped(lossyScale x -> y) but works as is and a material scaled square texture is probably better 
                if (thisQuadLargestWorldScaleComponent == transform.lossyScale.y)
                {
                    textureHoleRadiusPixels = Convert.ToInt32(multipleHoleTexture.height * ((currentWorldHoleDiameterInMeters / 2.0f)) / thisQuadLargestWorldScaleComponent);
                }

                Vector2 textureHolePosition = new Vector2(multipleHoleTexture.width * localHolePositions[holeIndex].x + multipleHoleTexture.width / 2f, multipleHoleTexture.height * localHolePositions[holeIndex].y + multipleHoleTexture.height / 2f);
                textureHolePosition = Vector2.Scale(textureHolePosition, quadSurfaceMaterial.GetTextureScale("_BaseMap"));

                //Essentially 2D bounds of the circle so not iterating over entire texture
                int textureXmax = Mathf.Clamp(Convert.ToInt32(textureHolePosition.x + (textureHoleRadiusPixels)), 0, multipleHoleTexture.width);
                int textureXmin = Mathf.Clamp(Convert.ToInt32(textureHolePosition.x - (textureHoleRadiusPixels)), 0, multipleHoleTexture.width);
                int textureYmax = Mathf.Clamp(Convert.ToInt32(textureHolePosition.y + (textureHoleRadiusPixels)), 0, multipleHoleTexture.height);
                int textureYmin = Mathf.Clamp(Convert.ToInt32(textureHolePosition.y - (textureHoleRadiusPixels)), 0, multipleHoleTexture.height);

                for (int textureY = textureYmin; textureY < textureYmax; textureY++)
                {
                    for (int textureX = textureXmin; textureX < textureXmax; textureX++)
                    {
                        Vector2 texturePosition = new Vector2((textureX), (textureY));

                        if (Vector2.Distance(textureHolePosition, texturePosition) < textureHoleRadiusPixels)
                        {
                            multipleHoleTexture.SetPixel(textureX, textureY, transparent);
                        }
                    }
                }

                multipleHoleTexture.Apply();
            }
            ///////////////////////
            if (!holesDrilledThrough[holeIndex] || holeMeshes[holeIndex].transform.localScale.y < 1f){
                UpdateProceduralHoleMeshObject(holeIndex);
                blockStock.worldHoleDepths[quadHoleIndicesToBlockParentHoleIndices[holeIndex]] = transform.TransformVector(new Vector3(0f, 0f, localHoleDepths[holeIndex])).magnitude;
            }else{
                if(!holeMeshes[holeIndex].name.Contains(placeHolderString)){
                    UpdateProceduralHoleMeshObject(holeIndex);
                    holeMeshes[holeIndex].name = ((currentWorldHoleDiameterInMeters*1000f)/25.4f).ToString("f3") + " inch Diameter Hole " + holeMeshes[holeIndex].transform.GetSiblingIndex().ToString() + " (Drilled Through)";
                }
            }

            blockStock.isThroughHole[quadHoleIndicesToBlockParentHoleIndices[holeIndex]] = holesDrilledThrough[holeIndex];
        }



    }

    void UpdateProceduralHoleMeshObject(int currentHoleIndex){
        //create new hole object if needed
        GameObject proceduralMeshHole;
        if(currentHoleIndex < holeMeshes.Count){
            proceduralMeshHole = holeMeshes[currentHoleIndex];
        }else{
            proceduralMeshHole = new GameObject();
            proceduralMeshHole.AddComponent<MeshRenderer>();
            proceduralMeshHole.AddComponent<MeshFilter>();
            proceduralMeshHole.transform.SetParent(transform);
            proceduralMeshHole.transform.localScale = Vector3.one;
            proceduralMeshHole.name = ((currentWorldHoleDiameterInMeters*1000f)/25.4f).ToString("f3") + " inch Diameter Hole " + proceduralMeshHole.transform.GetSiblingIndex().ToString();
            proceduralMeshHole.transform.rotation = Quaternion.LookRotation(transform.forward, transform.up);
            proceduralMeshHole.transform.localPosition = new Vector3(localHolePositions[currentHoleIndex].x, localHolePositions[currentHoleIndex].y, 0f);
            proceduralMeshHole.GetComponent<Renderer>().material = holeMeshMaterial;
            holeMeshes.Add(proceduralMeshHole);
        }

        Mesh holeMesh = proceduralMeshHole.GetComponent<MeshFilter>().mesh;

        int numVertices = 5*Convert.ToInt32(numSidesOf3DHoleMesh) + 1;
        int lengthOfTriangleArray = 3*3*Convert.ToInt32(numSidesOf3DHoleMesh);

        if(holesDrilledThrough[currentHoleIndex]){
            numVertices = 4*Convert.ToInt32(numSidesOf3DHoleMesh);
            lengthOfTriangleArray = 3*2*Convert.ToInt32(numSidesOf3DHoleMesh);
        }

        Vector3[] holeMeshVertices = new Vector3[numVertices];
        int[] holeMeshTriangles = new int[lengthOfTriangleArray];
        /*if (worldHoleDiameters[currentHoleIndex] < currentWorldHoleDiameterInMeters) {
            worldHoleDiameters[currentHoleIndex] = currentWorldHoleDiameterInMeters;
        }*/
        float holeRadius = worldHoleDiameters[currentHoleIndex]/2f;

        //TODO: Maybe use current code from drilled through else if section below for every time, and if it hasn't drilled through add the
        //bottom of the hole, if it has been drilledthrough then don't create bottom of hole mesh vertices and triangles

        if(!holesDrilledThrough[currentHoleIndex]){
            //center of hole at hole depth for bottom of hole triangles when not drilledThrough
            holeMeshVertices[numVertices - 1] = new Vector3(0f, 0f, localHoleDepths[currentHoleIndex]);
            for(int i = 0;i < numSidesOf3DHoleMesh + 1;i++){
                if(i < numSidesOf3DHoleMesh){
                    //top of hole
                    holeMeshVertices[5*i] = (((holeRadius * Mathf.Cos((Convert.ToSingle(i) * polygonHalfAngle * 2f * Mathf.Deg2Rad))/Mathf.Cos(polygonHalfAngle*Mathf.Deg2Rad)/transform.lossyScale.x)*Vector3.right + (holeRadius * Mathf.Sin((Convert.ToSingle(i) * polygonHalfAngle * 2f * Mathf.Deg2Rad))/Mathf.Cos(polygonHalfAngle*Mathf.Deg2Rad)/transform.lossyScale.y)*Vector3.up));
                    //bottom of hole
                    holeMeshVertices[5*i + 1] = (((holeRadius * Mathf.Cos((Convert.ToSingle(i) * polygonHalfAngle * 2f * Mathf.Deg2Rad))/Mathf.Cos(polygonHalfAngle*Mathf.Deg2Rad)/transform.lossyScale.x)*Vector3.right + localHoleDepths[currentHoleIndex]*Vector3.forward + (holeRadius * Mathf.Sin((Convert.ToSingle(i) * polygonHalfAngle * 2f * Mathf.Deg2Rad))/Mathf.Cos(polygonHalfAngle*Mathf.Deg2Rad)/transform.lossyScale.y)*Vector3.up));
                    holeMeshVertices[5*i + 2] = holeMeshVertices[5*i];
                    holeMeshVertices[5*i + 3] = holeMeshVertices[5*i + 1];
                    holeMeshVertices[5*i + 4] = holeMeshVertices[5*i + 1];
                }

                if(i == 1){
                    holeMeshTriangles[0] = 0; 
                    holeMeshTriangles[1] = 5; 
                    holeMeshTriangles[2] = 6; 

                    holeMeshTriangles[3] = 1; 
                    holeMeshTriangles[4] = 0; 
                    holeMeshTriangles[5] = 6; 

                    holeMeshTriangles[6] = 4;
                    holeMeshTriangles[7] = 9;
                    holeMeshTriangles[8] = numVertices - 1;
                }else if(i > 1 && i < numSidesOf3DHoleMesh){
                    holeMeshTriangles[9*(i - 1)] = 5*(i - 1) + 2; 
                    holeMeshTriangles[9*(i - 1) + 1] = 5*(i - 1) + 5; 
                    holeMeshTriangles[9*(i - 1) + 2] = 5*(i - 1) + 6; 

                    holeMeshTriangles[9*(i - 1) + 3] = 5*(i - 1) + 3; 
                    holeMeshTriangles[9*(i - 1) + 4] = 5*(i - 1) + 2; 
                    holeMeshTriangles[9*(i - 1) + 5] = 5*(i - 1) + 6; 

                    holeMeshTriangles[9*(i - 1) + 6] = 5*(i - 1) + 4;
                    holeMeshTriangles[9*(i - 1) + 7] = 5*(i - 1) + 9;
                    holeMeshTriangles[9*(i - 1) + 8] = numVertices - 1;
                }else if(i == numSidesOf3DHoleMesh){
                    holeMeshTriangles[lengthOfTriangleArray - 9] = (numVertices - 2) - 2;   //top surface of final edge set
                    holeMeshTriangles[lengthOfTriangleArray - 8] = 2;   //top surface of first edge set
                    holeMeshTriangles[lengthOfTriangleArray - 7] = 3;   //at depth first edge set

                    holeMeshTriangles[lengthOfTriangleArray - 6] = (numVertices - 2) - 1; 
                    holeMeshTriangles[lengthOfTriangleArray - 5] = (numVertices - 2) - 2; 
                    holeMeshTriangles[lengthOfTriangleArray - 4] = 3; 

                    holeMeshTriangles[lengthOfTriangleArray - 3] = (numVertices - 2) ;
                    holeMeshTriangles[lengthOfTriangleArray - 2] = 4;
                    holeMeshTriangles[lengthOfTriangleArray - 1] = numVertices - 1;
                }
            }
        }else{
            for(int i = 0;i < numSidesOf3DHoleMesh + 1;i++){
                if(i < numSidesOf3DHoleMesh){
                    //top of hole
                    holeMeshVertices[4*i] = (((holeRadius * Mathf.Cos((Convert.ToSingle(i) * polygonHalfAngle * 2f * Mathf.Deg2Rad))/Mathf.Cos(polygonHalfAngle*Mathf.Deg2Rad)/transform.lossyScale.x)*Vector3.right + (holeRadius * Mathf.Sin((Convert.ToSingle(i) * polygonHalfAngle * 2f * Mathf.Deg2Rad))/Mathf.Cos(polygonHalfAngle*Mathf.Deg2Rad)/transform.lossyScale.y)*Vector3.up));
                    //bottom of hole
                    holeMeshVertices[4*i + 1] = (((holeRadius * Mathf.Cos((Convert.ToSingle(i) * polygonHalfAngle * 2f * Mathf.Deg2Rad))/Mathf.Cos(polygonHalfAngle*Mathf.Deg2Rad)/transform.lossyScale.x)*Vector3.right + localHoleDepths[currentHoleIndex]*Vector3.forward + (holeRadius * Mathf.Sin((Convert.ToSingle(i) * polygonHalfAngle * 2f * Mathf.Deg2Rad))/Mathf.Cos(polygonHalfAngle*Mathf.Deg2Rad)/transform.lossyScale.y)*Vector3.up));
                    holeMeshVertices[4*i + 2] = holeMeshVertices[4*i];
                    holeMeshVertices[4*i + 3] = holeMeshVertices[4*i + 1];
                }

                if(i == 1){
                    holeMeshTriangles[0] = 0; 
                    holeMeshTriangles[1] = 4; 
                    holeMeshTriangles[2] = 5; 

                    holeMeshTriangles[3] = 1; 
                    holeMeshTriangles[4] = 0; 
                    holeMeshTriangles[5] = 5; 
                }else if(i > 1 && i < numSidesOf3DHoleMesh){
                    holeMeshTriangles[6*(i - 1)] = 4*(i - 1) + 2; //top hole prev
                    holeMeshTriangles[6*(i - 1) + 1] = 4*i; //top of hole current
                    holeMeshTriangles[6*(i - 1) + 2] = 4*i + 1; //bottom hole current

                    holeMeshTriangles[6*(i - 1) + 3] = 4*(i - 1) + 3; //bottom hole prev
                    holeMeshTriangles[6*(i - 1) + 4] = 4*(i - 1) + 2; //top hole prev 
                    holeMeshTriangles[6*(i - 1) + 5] = 4*i + 1; //bottom of hole current
                }else if(i == numSidesOf3DHoleMesh){
                    holeMeshTriangles[lengthOfTriangleArray - 6] = (numVertices - 4);   //top surface of final edge set
                    holeMeshTriangles[lengthOfTriangleArray - 5] = 2;   //top surface of first edge set
                    holeMeshTriangles[lengthOfTriangleArray - 4] = 3;   //at depth first edge set

                    holeMeshTriangles[lengthOfTriangleArray - 3] = (numVertices - 1); // last at depth
                    holeMeshTriangles[lengthOfTriangleArray - 2] = (numVertices - 2);   //last at top of hole 
                    holeMeshTriangles[lengthOfTriangleArray - 1] = 3; //at depth first edge set
                }
            }
        }
        holeMesh.Clear();
        holeMesh.vertices = holeMeshVertices;
        holeMesh.triangles = holeMeshTriangles;
        holeMesh.RecalculateNormals();

        //add gameobject to hole list
    }

    void Update(){
        if(!isSurfaceQuadOnSideOfBlock){
            CreateAndUpdateHoles();
        }
    }
}
