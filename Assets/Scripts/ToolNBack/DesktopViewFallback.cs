using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace ToolNBack
{
    /// <summary>
    /// Desk-testing aid for the XR rig's Camera Offset object: when no XR device is
    /// active (e.g. pressing Play in the editor without the headset), raises the
    /// camera to a standing eye height so the Game view looks at the curved tool
    /// panel instead of sitting on the floor (the rig uses a Floor tracking origin,
    /// so only the headset normally supplies the eye height). Does nothing when a
    /// headset drives the camera.
    /// </summary>
    public class DesktopViewFallback : MonoBehaviour
    {
        [Tooltip("Camera height in meters used when no XR device is connected. Match the panel pivot height.")]
        [SerializeField] private float _desktopEyeHeight = 1.6f;

        private IEnumerator Start()
        {
            // Give XR initialization a frame to settle before deciding.
            yield return null;
            Apply(XRSettings.isDeviceActive);
        }

        /// <summary>Raises the offset when no XR device is active; no-op otherwise.</summary>
        public void Apply(bool xrDeviceActive)
        {
            if (xrDeviceActive)
                return;

            Vector3 position = transform.localPosition;
            transform.localPosition = new Vector3(position.x, _desktopEyeHeight, position.z);
        }
    }
}
