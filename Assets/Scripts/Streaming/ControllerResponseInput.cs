using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace NBackStreaming
{
#if ENABLE_INPUT_SYSTEM
    /// <summary>
    /// Routes XR controller presses (either trigger or the primary button) to
    /// <see cref="Task.TaskManagerBehaviour.HandleOnInput"/> so participants can respond
    /// with the controllers instead of the keyboard. Uses a runtime-created Input System
    /// action, so no .inputactions asset is required.
    /// </summary>
    public class ControllerResponseInput : MonoBehaviour
    {
        /// <summary>Task manager receiving responses. Found via FindFirstObjectByType in Awake when unset.</summary>
        public Task.TaskManagerBehaviour taskManager;

        private InputAction _responseAction;
        private int _lastResponseFrame = -1;

        #region Unity methods
        private void Awake()
        {
            if (taskManager == null)
                taskManager = FindFirstObjectByType<Task.TaskManagerBehaviour>();
        }

        private void OnEnable()
        {
            if (_responseAction == null)
            {
                _responseAction = new InputAction(name: "NBackResponse", type: InputActionType.Button);
                _responseAction.AddBinding("<XRController>{RightHand}/triggerPressed");
                _responseAction.AddBinding("<XRController>{LeftHand}/triggerPressed");
                _responseAction.AddBinding("<XRController>/primaryButton");
                // The native Varjo XR plugin names the primary face button control
                // "primary" (no "primaryButton" control or alias on its Index/Vive
                // layouts), so the OpenXR-style binding above never fires on Varjo.
                _responseAction.AddBinding("<XRController>/primary");
                _responseAction.performed += HandleActionPerformed;
            }

            _responseAction.Enable();
        }

        private void OnDisable()
        {
            if (_responseAction == null)
                return;

            _responseAction.Disable();
            _responseAction.performed -= HandleActionPerformed;
            _responseAction.Dispose();
            _responseAction = null;
        }
        #endregion

        #region Private methods
        private void HandleActionPerformed(InputAction.CallbackContext context)
        {
            // performed can fire once per matching binding (e.g. trigger + primary button
            // pressed together); forward at most one response per frame.
            if (Time.frameCount == _lastResponseFrame)
                return;
            _lastResponseFrame = Time.frameCount;

            if (taskManager == null)
                taskManager = FindFirstObjectByType<Task.TaskManagerBehaviour>();

            if (taskManager != null)
                taskManager.HandleOnInput();
        }
        #endregion
    }
#else
    /// <summary>
    /// Placeholder for when the new Input System is unavailable (activeInputHandler must be
    /// set to "Both"); keeps the class defined so scene references to it survive.
    /// </summary>
    public class ControllerResponseInput : MonoBehaviour
    {
    }
#endif
}
