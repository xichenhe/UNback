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

        [Tooltip("Extra Input System binding paths accepted as responses, on top of the built-in trigger/primary set — e.g. <XRController>/secondary or <XRController>/gripPressed. Applied when the component enables: toggle it off/on (or restart Play) after editing.")]
        [SerializeField] private string[] _extraBindings = System.Array.Empty<string>();

        [Tooltip("Log connected XR controllers (with their layout) and every controller button press with its control path — use this to discover the runtime's real name for a physical button, then add that path to Extra Bindings.")]
        [SerializeField] private bool _logControllerButtons;

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

                foreach (string binding in _extraBindings)
                {
                    if (!string.IsNullOrWhiteSpace(binding))
                        _responseAction.AddBinding(binding.Trim());
                }

                _responseAction.performed += HandleActionPerformed;
            }

            _responseAction.Enable();

            InputSystem.onDeviceChange += HandleDeviceChange;
            if (_logControllerButtons)
                LogConnectedControllers();
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= HandleDeviceChange;

            if (_responseAction == null)
                return;

            _responseAction.Disable();
            _responseAction.performed -= HandleActionPerformed;
            _responseAction.Dispose();
            _responseAction = null;
        }

        private void Update()
        {
            if (!_logControllerButtons)
                return;

            foreach (InputDevice device in InputSystem.devices)
            {
                // Everything except desktop input: Varjo/SteamVR controllers can land
                // on an auto-generated layout that does not derive from XRController.
                if (device is Keyboard || device is Pointer)
                    continue;

                foreach (InputControl control in device.allControls)
                {
                    if (control is UnityEngine.InputSystem.Controls.ButtonControl button &&
                        button.wasPressedThisFrame)
                        Debug.Log($"[{name}] button pressed: {button.path} " +
                                  $"(layout={device.layout}, isXRController={device is UnityEngine.InputSystem.XR.XRController})");
                }
            }
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

        private void HandleDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (!_logControllerButtons || device is Keyboard || device is Pointer)
                return;

            if (change == InputDeviceChange.Added || change == InputDeviceChange.Removed ||
                change == InputDeviceChange.Reconnected || change == InputDeviceChange.Disconnected)
                Debug.Log($"[{name}] device {change}: layout={device.layout} name={device.name} " +
                          $"usages=[{string.Join(", ", device.usages)}] " +
                          $"isXRController={device is UnityEngine.InputSystem.XR.XRController}");
        }

        private void LogConnectedControllers()
        {
            foreach (InputDevice device in InputSystem.devices)
            {
                if (device is Keyboard || device is Pointer)
                    continue;

                Debug.Log($"[{name}] device present: layout={device.layout} name={device.name} " +
                          $"usages=[{string.Join(", ", device.usages)}] " +
                          $"isXRController={device is UnityEngine.InputSystem.XR.XRController}");
            }
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
