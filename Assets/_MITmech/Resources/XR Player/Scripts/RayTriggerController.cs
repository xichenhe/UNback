using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class RayTriggerController : MonoBehaviour
{

    public XRController leftTeleporter, rightTeleporter;
    public InputHelpers.Button teleportAtivation;
    public float activationThreshold;

    public bool enableLeftTeleport { get; set; } = true;
    public bool enableRightTeleport { get; set; } = true;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (leftTeleporter)
        {
            leftTeleporter.gameObject.SetActive(enableLeftTeleport && CheckIfRayActiveated(leftTeleporter));
        }

        if (rightTeleporter)
        {
            rightTeleporter.gameObject.SetActive(enableRightTeleport && CheckIfRayActiveated(rightTeleporter));
        }
    }

    public bool CheckIfRayActiveated(XRController controller)
    {
        InputHelpers.IsPressed(controller.inputDevice, teleportAtivation, out bool isActivated, activationThreshold);
        return isActivated;
    }
}
