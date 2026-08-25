using System;
using System.Collections.Generic;
using UnityEngine;


//TODO: Fix when right about to enter the large box collider the calipers jitter when rotating, but this happens rarely and if user just moves further in collider
//it stops
//TODO: Caliper is supposed to snap to any rotation of the block to be able to drill on perpindicular sides
//currently when in not square to world axes orientation the snapping is erratic and incorrect, but works fine when
//block prefab axes are all aligned with world axes

public class SnapCalipersToStock : MonoBehaviour
{
    public Transform outsideAdjustmentTipTransform;    //GameObject in the calip prefab that is a child of the adjustment jaw that slides relative to the rest of the caliper on the tip of the adjustment jaw
    public Transform outsideFixedTipTransform;     //GameObject that is child of fixed jaw located on fixed jaw tip
    public Transform insideAdjustmentTipTransform;    //GameObject in the calip prefab that is a child of the adjustment jaw that slides relative to the rest of the caliper on the tip of the adjustment jaw
    public Transform insideFixedTipTransform;     //GameObject that is child of fixed jaw located on fixed jaw tip
    public Transform depthRodTipTransform;
    public Transform depthFixedBottomTransform;
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable caliperInteractable;
    public SlideCaliperInputActionToController slideCaliperController;
    public List<Vector3> worldHolePositionsMeters;
    public List<float> worldHoleDiametersMeters;
    public List<bool> isThroughHole;
    public List<float> worldHoleDepths;

    public float thresholdAlignmentAngleFromCalipersToBlockSnapDegrees = 30.0f;

    public float setDistanceAwayFromBlockIfCalipersSnapCentimeters = 1f;
    public float largerBoxColliderOffsetFromBlockCentimeters = 2.0f;
    public float adjustmentTipSnapThresholdDistanceCentimeters = 2f;
    public float unsnapCaliperFromConstrainedDirectionThresholdCentimeters = 5f;

    private Vector3[] blockAxisDirections;

    private BoxCollider largerThanBlockBoxCollider;
    public BoxCollider sameSizeAsBlockBoxCollider; 
    private Bounds localBlockMeshBounds;
    private Bounds worldBlockRendererBounds;

    private int snappedHoleIndex = 0;
    [SerializeField] private bool measuringHolePosition = false;
    [SerializeField] private bool measuringHoleDiameter = false;
    [SerializeField] private bool measuringHoleDepth = false;

    public bool makeTransparentWhenMeasuringDepth = true;
    public bool blockSidesTransparent = false;

    void Start()
    {
        slideCaliperController.adjustmentJawSnappedToMeasurement = false;   //make sure the calipers are not snapped at start

        localBlockMeshBounds = GetComponent<MeshFilter>().mesh.bounds;
        worldBlockRendererBounds = GetComponent<Renderer>().bounds;

        largerThanBlockBoxCollider = gameObject.AddComponent<BoxCollider>();
        largerThanBlockBoxCollider.isTrigger = true;
        largerThanBlockBoxCollider.center = Vector3.zero;

        sameSizeAsBlockBoxCollider = gameObject.AddComponent<BoxCollider>();
        sameSizeAsBlockBoxCollider.isTrigger = true;
        sameSizeAsBlockBoxCollider.center = Vector3.zero;

        sameSizeAsBlockBoxCollider.size = GetComponent<MeshFilter>().mesh.bounds.size; 

        Vector3 boxColliderOffsetSize = Vector3.one;
        boxColliderOffsetSize.x = 1f + 2f * (largerBoxColliderOffsetFromBlockCentimeters / 100f) / worldBlockRendererBounds.size.x;
        boxColliderOffsetSize.y = 1f + 2f * (largerBoxColliderOffsetFromBlockCentimeters / 100f) / worldBlockRendererBounds.size.y;
        boxColliderOffsetSize.z = 1f + 2f * (largerBoxColliderOffsetFromBlockCentimeters / 100f) / worldBlockRendererBounds.size.z;
        
        largerThanBlockBoxCollider.size = boxColliderOffsetSize;

        unsnapCaliperFromConstrainedDirectionThresholdCentimeters = Mathf.Max(largerBoxColliderOffsetFromBlockCentimeters + 1f, unsnapCaliperFromConstrainedDirectionThresholdCentimeters);

        blockAxisDirections = new Vector3[6] {
            transform.right,
            -transform.right,
            transform.up,
            -transform.up,
            transform.forward,
            -transform.forward
        };
    }

    void StopXRTrackingCalipers()
    {
        caliperInteractable.trackPosition = false;
        caliperInteractable.trackRotation = false;
    }

    void StartXRTrackingCalipers()
    {
        caliperInteractable.trackPosition = true;
        caliperInteractable.trackRotation = true;
    }

    //finds the block direction most aligned with an input direction in world space also under a threshold
    Vector3 GetVectorAlignmentToBlock(Vector3 inputDirection)
    {

        float minimumAlignmentAngleDegrees = 180.0f;

        Vector3 worldCaliperSlideDirectionAlignedWithBlock = Vector3.zero;

        for (int i = 0; i < blockAxisDirections.Length; i++)
        {
            float newMinimumAlignmentAngleDegrees = Vector3.Angle(blockAxisDirections[i], inputDirection);
            if (newMinimumAlignmentAngleDegrees < thresholdAlignmentAngleFromCalipersToBlockSnapDegrees && newMinimumAlignmentAngleDegrees < minimumAlignmentAngleDegrees)
            {
                minimumAlignmentAngleDegrees = newMinimumAlignmentAngleDegrees;
                worldCaliperSlideDirectionAlignedWithBlock = blockAxisDirections[i];
            }
        }

        return worldCaliperSlideDirectionAlignedWithBlock;

    }



    void CheckAndUpdateBlockSideTransparency(){
        
        //If the blockSidesTransparent boolean is true then disable the quads on the side to make the cube attached to this game object visible
        //with its transparent material otherwise do the opposite if the boolean is false
        if(blockSidesTransparent)
        {
            //ProceduralHoleController is attached to the QUAD children of this object and they make the quad textures transparent at hole locations and
            //create procedural mesh cylinders for holes as well
            ProceduralHoleController[] proceduralHoleObjects = transform.GetComponentsInChildren<ProceduralHoleController>();

            if (proceduralHoleObjects.Length > 0)
            {
                foreach (ProceduralHoleController proceduralHole in proceduralHoleObjects)
                {
                    if (proceduralHole.isSurfaceQuadOnSideOfBlock && proceduralHole.gameObject.GetComponent<MeshRenderer>().enabled)
                    {
                        proceduralHole.gameObject.GetComponent<MeshRenderer>().enabled = false;
                    }
                }
            }

        }
        else{
            ProceduralHoleController[] proceduralHoleObjects = transform.GetComponentsInChildren<ProceduralHoleController>();

            if (proceduralHoleObjects.Length > 0)
            {
                foreach (ProceduralHoleController proceduralHole in proceduralHoleObjects)
                {
                    if (proceduralHole.isSurfaceQuadOnSideOfBlock && !proceduralHole.gameObject.GetComponent<MeshRenderer>().enabled)
                    {
                        proceduralHole.gameObject.GetComponent<MeshRenderer>().enabled = true;
                    }
                }
            }
        }

    }




    void UpdateCaliperSnapping()
    {
        if(caliperInteractable.interactorsSelecting.Count == 0) return;

        //              MAKE TRANSPARENT OR CHECK IF TRANSPARENT 
        if(makeTransparentWhenMeasuringDepth) CheckAndUpdateBlockSideTransparency();
        //              FIGURE OUT IF MEASURING HOLE DEPTH OR DIAMETER OR POSITION AND IF CLOSE ENOUGH TO START MEASURING
        bool outsideJawsInsideLargeCollider = (largerThanBlockBoxCollider.bounds.Contains(outsideAdjustmentTipTransform.position) || largerThanBlockBoxCollider.bounds.Contains(outsideFixedTipTransform.position));
        bool insideJawsInsideLargeCollider = ((largerThanBlockBoxCollider.bounds.Contains(insideAdjustmentTipTransform.position) || largerThanBlockBoxCollider.bounds.Contains(insideFixedTipTransform.position)));
        bool depthRodInsideLargeCollider = (largerThanBlockBoxCollider.bounds.Contains(depthFixedBottomTransform.position));

        //Checks if potentially measuring hole positions. Position and Diameter measurements exclude the depth fixed bottom bool because with a large
        //enough block the depth rod could be in the collider simultaneously
        if (outsideJawsInsideLargeCollider && !insideJawsInsideLargeCollider)
        {
            StopXRTrackingCalipers();
            measuringHolePosition = true;
            measuringHoleDiameter = false;
            measuringHoleDepth = false;
            blockSidesTransparent = false;
        }//Checks if potentially measuring Hole Diameter
        else if (!outsideJawsInsideLargeCollider && insideJawsInsideLargeCollider)
        {
            StopXRTrackingCalipers();
            measuringHolePosition = false;
            measuringHoleDiameter = true;
            measuringHoleDepth = false;
            blockSidesTransparent = false;
        }//Check if potentially measuring Hole Depth
        else if (!outsideJawsInsideLargeCollider && !insideJawsInsideLargeCollider && depthRodInsideLargeCollider)
        {
            StopXRTrackingCalipers();
            measuringHolePosition = false;
            measuringHoleDepth = true;
            measuringHoleDiameter = false;
        }
        else
        {
            //If the caliper jaw tips are no longer inside of the collider resume xr grab interactable tracking and exit the method 
            StartXRTrackingCalipers();
            measuringHolePosition = false;
            measuringHoleDiameter = false;
            measuringHoleDepth = false;
            blockSidesTransparent = false;
            return;
        }

        //              CREATE DIRECTION VECTOR VARIABLES

        //see which axis that calipers are lined up with on the block
        Vector3 worldCaliperSlideDirectionAlignedWithBlock = GetVectorAlignmentToBlock(caliperInteractable.transform.forward);
        Vector3 worldCaliperConstrainedDirection = Vector3.zero;
        Vector3[] worldCaliperUnconstrainedDirections = new Vector3[2] {Vector3.zero, Vector3.zero};
        

        if(measuringHolePosition){
            worldCaliperConstrainedDirection = GetVectorAlignmentToBlock(caliperInteractable.transform.right);
            worldCaliperUnconstrainedDirections[0] = GetVectorAlignmentToBlock(caliperInteractable.transform.up);
        }else if(measuringHoleDiameter){
            worldCaliperConstrainedDirection = GetVectorAlignmentToBlock(-caliperInteractable.transform.right);
            worldCaliperUnconstrainedDirections[0] = GetVectorAlignmentToBlock(caliperInteractable.transform.up);
            worldCaliperUnconstrainedDirections[1] = worldCaliperSlideDirectionAlignedWithBlock;
        }else if(measuringHoleDepth){
            worldCaliperConstrainedDirection = worldCaliperSlideDirectionAlignedWithBlock;
            worldCaliperUnconstrainedDirections[0] = GetVectorAlignmentToBlock(caliperInteractable.transform.up);
            worldCaliperUnconstrainedDirections[1] = GetVectorAlignmentToBlock(caliperInteractable.transform.right);
        }

        if(worldCaliperSlideDirectionAlignedWithBlock.magnitude < 0.1f 
        || worldCaliperConstrainedDirection.magnitude < 0.1f 
        || worldCaliperUnconstrainedDirections[0].magnitude < 0.1f){
            StartXRTrackingCalipers();
            return;
        }

        Vector3 worldControllerPositionProjectedOnSlideDirectionComponent = Vector3.zero;
        Vector3[] worldControllerPositionProjectedOnUnconstrainedDirectionsComponents = new Vector3[2] {Vector3.zero, Vector3.zero};
        
        
        if (caliperInteractable.interactorsSelecting.Count > 0){
            Vector3 worldCaliperInteractableToInsideFixedJawProjectedOnUnconstrained1 =Vector3.Project((insideFixedTipTransform.position - caliperInteractable.transform.position), worldCaliperUnconstrainedDirections[1]);
            worldControllerPositionProjectedOnUnconstrainedDirectionsComponents[0] = Vector3.Project(caliperInteractable.interactorsSelecting[0].transform.position - transform.position, worldCaliperUnconstrainedDirections[0]);

            if(worldCaliperUnconstrainedDirections[1] != Vector3.zero){
                worldControllerPositionProjectedOnUnconstrainedDirectionsComponents[1] = Vector3.Project(caliperInteractable.interactorsSelecting[0].transform.position - transform.position, worldCaliperUnconstrainedDirections[1]);
            }

            if(measuringHoleDiameter){
                worldControllerPositionProjectedOnUnconstrainedDirectionsComponents[1] = worldControllerPositionProjectedOnUnconstrainedDirectionsComponents[1] + worldCaliperInteractableToInsideFixedJawProjectedOnUnconstrained1;
                if((worldControllerPositionProjectedOnUnconstrainedDirectionsComponents[0].magnitude > (sameSizeAsBlockBoxCollider.ClosestPoint(worldCaliperUnconstrainedDirections[0] + transform.position) - transform.position).magnitude 
                || Vector3.Project((insideFixedTipTransform.position - transform.position), worldCaliperUnconstrainedDirections[1]).magnitude > (sameSizeAsBlockBoxCollider.ClosestPoint(worldCaliperUnconstrainedDirections[1] + transform.position) - transform.position).magnitude) 
                && !slideCaliperController.adjustmentJawSnappedToMeasurement){
                    StartXRTrackingCalipers();
                    return;
                }
            }else{
                if((worldControllerPositionProjectedOnUnconstrainedDirectionsComponents[0].magnitude > (sameSizeAsBlockBoxCollider.ClosestPoint(worldCaliperUnconstrainedDirections[0] + transform.position) - transform.position).magnitude 
                || worldControllerPositionProjectedOnUnconstrainedDirectionsComponents[1].magnitude > (sameSizeAsBlockBoxCollider.ClosestPoint(worldCaliperUnconstrainedDirections[1] + transform.position) - transform.position).magnitude)
                && !slideCaliperController.adjustmentJawSnappedToMeasurement){
                    StartXRTrackingCalipers();
                    return;
                }
            }
        }

        float doubleCountSlideAlignmentConstraintFactor = Convert.ToSingle(!measuringHoleDepth);
        float measuringHoleDiameterFactor = Convert.ToSingle(!measuringHoleDiameter);
        Vector3 worldCaliperFixedTipMeasurementPosition = transform.position + worldControllerPositionProjectedOnUnconstrainedDirectionsComponents[0] + worldControllerPositionProjectedOnUnconstrainedDirectionsComponents[1] + measuringHoleDiameterFactor*doubleCountSlideAlignmentConstraintFactor*(sameSizeAsBlockBoxCollider.ClosestPoint(worldCaliperSlideDirectionAlignedWithBlock + transform.position) - transform.position) + (sameSizeAsBlockBoxCollider.ClosestPoint(worldCaliperConstrainedDirection + transform.position) - transform.position);

        Quaternion snappedRotation = Quaternion.LookRotation(worldCaliperSlideDirectionAlignedWithBlock, worldCaliperUnconstrainedDirections[0]);

        Vector3 testPositionOfOutsideFixedTipIfLookRotated = snappedRotation * (outsideFixedTipTransform.position);
        Vector3 testPositionOfInsideFixedTipIfLookRotated = snappedRotation * (insideFixedTipTransform.position);

        caliperInteractable.transform.rotation = snappedRotation;

        //              FIND OFFSETS TO FIXED CALIPERS

        //Find offsets to fixed calipers so that when we find where fixed jaw tip should snap to just combine desired/snapped fix tip position with outside/inside offset to effectively set fixed tip position
        Vector3 worldCaliperInteractableToOutsideFixedCaliperJawVectorOffset = (outsideFixedTipTransform.position - caliperInteractable.transform.position);
        Vector3 worldCaliperInteractableToInsideFixedCaliperJawVectorOffset = (insideFixedTipTransform.position - caliperInteractable.transform.position);
        Vector3 worldCaliperInteractableToDepthFixedBottomCaliperJawVectorOffset = (depthFixedBottomTransform.position - caliperInteractable.transform.position);

        //Adjustment Jaw Snapping to hole position
        Vector3 worldSnappedHoleUnconstrainedProjection = Vector3.zero;

        float minimumCaliperToHoleDistanceMeters = SlideCaliperInputActionToController.maxJawSlidePosition;

        //              SNAPPING ADJUSTMENT CALIPERS TO THE MEASUREMENT
        Vector3 positionMeasurementCaliperHandleToControllerConstrainedProjection = Vector3.zero;
        Vector3 controllerToBlockSurfaceConstrainedDirectionProjection = Vector3.zero;

        if(caliperInteractable.interactorsSelecting.Count > 0){
            positionMeasurementCaliperHandleToControllerConstrainedProjection = (Vector3.Project(caliperInteractable.interactorsSelecting[0].transform.position - caliperInteractable.transform.position, worldCaliperConstrainedDirection));
            controllerToBlockSurfaceConstrainedDirectionProjection = (Vector3.Project(caliperInteractable.interactorsSelecting[0].transform.position - transform.position, worldCaliperConstrainedDirection)) - (sameSizeAsBlockBoxCollider.ClosestPoint(worldCaliperConstrainedDirection + transform.position) - transform.position);
        }else{
            return;
        }

        if (worldHolePositionsMeters.Count > snappedHoleIndex)
        {
            if (!slideCaliperController.adjustmentJawSnappedToMeasurement)
            {
                for (int i = 0; i < worldHolePositionsMeters.Count; i++)
                {
                    //for hole position measurement Essentially the distance from non selecting controller sliding adjustment jaw value when grip/primary button pressed to hole on hole surface
                    float caliperToHoleDistanceMeters = Vector3.Distance(outsideAdjustmentTipTransform.position, worldHolePositionsMeters[i]);

                    //change the distance calculation if measuring a hole diameter
                    if (measuringHoleDiameter)
                    {
                        caliperToHoleDistanceMeters = Vector3.Distance(insideFixedTipTransform.position, worldHolePositionsMeters[i]);
                    }
                    else if (measuringHoleDepth)
                    {
                        caliperToHoleDistanceMeters = Vector3.Distance(depthRodTipTransform.position, worldHolePositionsMeters[i] - worldHoleDepths[snappedHoleIndex] * worldCaliperConstrainedDirection);
                    }

                    //helps find the hole that's closest to the relevant caliper jaw tips
                    if (caliperToHoleDistanceMeters < minimumCaliperToHoleDistanceMeters)
                    {
                        minimumCaliperToHoleDistanceMeters = caliperToHoleDistanceMeters;
                        snappedHoleIndex = i;
                    }
                }

                //If the caliper to hole distance is less than hole radius and offset (clamped for smaller and larger holes) then set the slider measurement and this set function also sets adjustmentJawSnappedToMeasurement field to true
                if (minimumCaliperToHoleDistanceMeters < (adjustmentTipSnapThresholdDistanceCentimeters/100f))
                {
                    //Snaps to hole according to measurement type
                    if (measuringHolePosition){
                        if(positionMeasurementCaliperHandleToControllerConstrainedProjection.magnitude > (adjustmentTipSnapThresholdDistanceCentimeters/100f) && Vector3.Angle(positionMeasurementCaliperHandleToControllerConstrainedProjection, worldCaliperConstrainedDirection) > 178f){
                            slideCaliperController.SetCaliperSnapMeasurementMeters(Vector3.Project(outsideFixedTipTransform.position - worldHolePositionsMeters[snappedHoleIndex], worldCaliperSlideDirectionAlignedWithBlock).magnitude - worldHoleDiametersMeters[snappedHoleIndex] / 2f);
                        }
                    }
                    else if (measuringHoleDepth){
                        if (!isThroughHole[snappedHoleIndex])
                        {
                            slideCaliperController.SetCaliperSnapMeasurementMeters(worldHoleDepths[snappedHoleIndex]);
                        }else{
                            slideCaliperController.adjustmentJawSnappedToMeasurement = false;
                        }
                    }
                    else if(measuringHoleDiameter){
                        if(positionMeasurementCaliperHandleToControllerConstrainedProjection.magnitude > (adjustmentTipSnapThresholdDistanceCentimeters/100f) && Vector3.Angle(positionMeasurementCaliperHandleToControllerConstrainedProjection, worldCaliperConstrainedDirection) > 178f){
                            slideCaliperController.SetCaliperSnapMeasurementMeters(worldHoleDiametersMeters[snappedHoleIndex]);
                        }
                    }
                }
            }

            //              PUT CALIPERS INTO CONSTRAINED AND SNAPPED POSITIONS AND STOP SNAPPING IF NEEDED
            //GetCaliperMeasurementMeters gets the theoretical slide value from the non selecting controller even if the adjustment jaw is snapped
            if(slideCaliperController.adjustmentJawSnappedToMeasurement){
                //Snapping to hole positions
                if (measuringHolePosition){
                    blockSidesTransparent = false;
                    if (caliperInteractable.interactorsSelecting.Count > 0){
                        
                        //If the non-selecting controller is out of snap zone stop snapping
                        if (positionMeasurementCaliperHandleToControllerConstrainedProjection.magnitude > 0f && Vector3.Angle(positionMeasurementCaliperHandleToControllerConstrainedProjection, worldCaliperConstrainedDirection) < 2f){
                            slideCaliperController.SetCaliperSnapMeasurementMeters(Vector3.Project(outsideFixedTipTransform.position - worldHolePositionsMeters[snappedHoleIndex], worldCaliperSlideDirectionAlignedWithBlock).magnitude - worldHoleDiametersMeters[snappedHoleIndex] / 2f - 1.5f*(adjustmentTipSnapThresholdDistanceCentimeters/100f));
                            slideCaliperController.adjustmentJawSnappedToMeasurement = false;
                        }
                        else{
                            caliperInteractable.transform.position = transform.position - worldCaliperInteractableToOutsideFixedCaliperJawVectorOffset + (sameSizeAsBlockBoxCollider.ClosestPoint(worldCaliperSlideDirectionAlignedWithBlock + transform.position) - transform.position)  + (sameSizeAsBlockBoxCollider.ClosestPoint(worldCaliperConstrainedDirection + transform.position) - transform.position) - (setDistanceAwayFromBlockIfCalipersSnapCentimeters/ 100f)*worldCaliperConstrainedDirection.normalized + Vector3.Project(worldHolePositionsMeters[snappedHoleIndex] - transform.position, worldCaliperUnconstrainedDirections[0]);
                        }
                    }

                }
                //snapping to hole depth
                else if(measuringHoleDepth){
                    if (caliperInteractable.interactorsSelecting.Count > 0){
                        if (worldHoleDepths[snappedHoleIndex] - (slideCaliperController.GetCaliperMeasurementMeters()) > Mathf.Min(1.5f*(adjustmentTipSnapThresholdDistanceCentimeters/100f), 0.5f*worldHoleDepths[snappedHoleIndex])){
                            slideCaliperController.SetCaliperSnapMeasurementMeters(0f);
                            slideCaliperController.adjustmentJawSnappedToMeasurement = false;
                            caliperInteractable.transform.position = transform.position - worldCaliperInteractableToDepthFixedBottomCaliperJawVectorOffset   + (sameSizeAsBlockBoxCollider.ClosestPoint(worldCaliperConstrainedDirection + transform.position) - transform.position) + Vector3.Project(worldHolePositionsMeters[snappedHoleIndex] - transform.position, worldCaliperUnconstrainedDirections[0]) + Vector3.Project(worldHolePositionsMeters[snappedHoleIndex] - transform.position, worldCaliperUnconstrainedDirections[1]);
                        }
                        else{
                            caliperInteractable.transform.position = transform.position - worldCaliperInteractableToDepthFixedBottomCaliperJawVectorOffset   + (sameSizeAsBlockBoxCollider.ClosestPoint(worldCaliperConstrainedDirection + transform.position) - transform.position) + Vector3.Project(worldHolePositionsMeters[snappedHoleIndex] - transform.position, worldCaliperUnconstrainedDirections[0]) + Vector3.Project(worldHolePositionsMeters[snappedHoleIndex] - transform.position, worldCaliperUnconstrainedDirections[1]);
                        }

                        if(controllerToBlockSurfaceConstrainedDirectionProjection.magnitude > (unsnapCaliperFromConstrainedDirectionThresholdCentimeters/100f) && Vector3.Angle(controllerToBlockSurfaceConstrainedDirectionProjection, worldCaliperConstrainedDirection) < 2f){
                            slideCaliperController.adjustmentJawSnappedToMeasurement = false;
                            measuringHoleDepth = false;
                            StartXRTrackingCalipers();
                            return;
                        }
                    }

                }
                //snapping to hole diameter
                else if (measuringHoleDiameter)
                {
                    blockSidesTransparent = false;
                    if (caliperInteractable.interactorsSelecting.Count > 0)
                    {
                        if(positionMeasurementCaliperHandleToControllerConstrainedProjection.magnitude > 0f && Vector3.Angle(positionMeasurementCaliperHandleToControllerConstrainedProjection, worldCaliperConstrainedDirection) < 2f) 
                        {
                            slideCaliperController.SetCaliperSnapMeasurementMeters(0f);
                            slideCaliperController.adjustmentJawSnappedToMeasurement = false;
                        }
                        else
                        {
                            caliperInteractable.transform.position = worldHolePositionsMeters[snappedHoleIndex] + (worldHoleDiametersMeters[snappedHoleIndex] / 2f) * worldCaliperSlideDirectionAlignedWithBlock.normalized - worldCaliperInteractableToInsideFixedCaliperJawVectorOffset - (setDistanceAwayFromBlockIfCalipersSnapCentimeters / 100f) * (worldCaliperConstrainedDirection.normalized);
                        }
                    }

                }
            }
        }

        //              WHEN SNAPPING ADJUSTMENT TO MEASUREMENT IS NOT ACTIVE

        //Don't add to above else if
        if (!slideCaliperController.adjustmentJawSnappedToMeasurement)
        {
            if (measuringHolePosition)
            {
                if(controllerToBlockSurfaceConstrainedDirectionProjection.magnitude > (unsnapCaliperFromConstrainedDirectionThresholdCentimeters/100f) && Vector3.Angle(controllerToBlockSurfaceConstrainedDirectionProjection, worldCaliperConstrainedDirection) < 2f){
                    measuringHolePosition = false;
                    StartXRTrackingCalipers();
                    return;
                }
                caliperInteractable.transform.position = worldCaliperFixedTipMeasurementPosition - worldCaliperInteractableToOutsideFixedCaliperJawVectorOffset;
                blockSidesTransparent = false;
            }
            else if (measuringHoleDiameter)
            {
                if(controllerToBlockSurfaceConstrainedDirectionProjection.magnitude > (unsnapCaliperFromConstrainedDirectionThresholdCentimeters/100f) && Vector3.Angle(controllerToBlockSurfaceConstrainedDirectionProjection, worldCaliperConstrainedDirection) < 2f){
                    measuringHoleDiameter = false;
                    StartXRTrackingCalipers();
                    return;
                }
                caliperInteractable.transform.position = worldCaliperFixedTipMeasurementPosition - (worldCaliperInteractableToInsideFixedCaliperJawVectorOffset);
                blockSidesTransparent = false;
            }
            else if (measuringHoleDepth)
            {
                if(controllerToBlockSurfaceConstrainedDirectionProjection.magnitude > (unsnapCaliperFromConstrainedDirectionThresholdCentimeters/100f) && Vector3.Angle(controllerToBlockSurfaceConstrainedDirectionProjection, worldCaliperConstrainedDirection) < 2f){
                    measuringHoleDepth = false;
                    StartXRTrackingCalipers();
                    return;
                }

                caliperInteractable.transform.position = worldCaliperFixedTipMeasurementPosition - worldCaliperInteractableToDepthFixedBottomCaliperJawVectorOffset;
                blockSidesTransparent = true;
            }
        }

    }

    void Update()
    {
        UpdateCaliperSnapping();
    }

}