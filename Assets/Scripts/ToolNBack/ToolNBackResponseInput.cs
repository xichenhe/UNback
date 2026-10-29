using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace ToolNBack
{
#if ENABLE_INPUT_SYSTEM
    /// <summary>
    /// Routes XR controller presses (trigger or primary button) and keyboard presses
    /// (Enter or Space) to <see cref="ToolNBackManagerBehaviour.HandleOnInput"/>, and drives
    /// the effort rating: thumbstick or arrow keys move the selection, digit keys 1–9 enter
    /// a number directly. Uses runtime-created Input System actions, so no .inputactions
    /// asset is required.
    /// </summary>
    public class ToolNBackResponseInput : MonoBehaviour
    {
        /// <summary>Task manager receiving responses. Found via FindFirstObjectByType in Awake when unset.</summary>
        public ToolNBackManagerBehaviour taskManager;

        [Tooltip("Extra Input System binding paths accepted as responses, on top of the built-in trigger/primary/keyboard set — e.g. <XRController>/secondary or <XRController>/gripPressed. Applied when the component enables: toggle it off/on (or restart Play) after editing.")]
        [SerializeField] private string[] _extraBindings = System.Array.Empty<string>();

        [Tooltip("Extra Input System paths for the stick that moves the effort-rating selection, on top of primary2DAxis / thumbstick / joystick — e.g. <XRController>/secondary2DAxis. Find the right path with Log Controller Buttons. Applied when the component enables.")]
        [SerializeField] private string[] _extraNavigationBindings = System.Array.Empty<string>();

        [Tooltip("Log connected XR controllers (with their layout) and every controller button press and stick push with its control path — use this to discover the runtime's real name for a physical button or stick, then add that path to Extra Bindings or Extra Navigation Bindings.")]
        [SerializeField] private bool _logControllerButtons;

        private InputAction _responseAction;
        private InputAction _probeStickAction;
        private InputAction _probeLeftAction;
        private InputAction _probeRightAction;
        private InputAction _probeDigitAction;
        private readonly StickStepper _stickStepper = new StickStepper();
        private readonly HashSet<InputControl> _pushedSticks = new HashSet<InputControl>();
        private int _lastResponseFrame = -1;

        #region Unity methods
        private void Awake()
        {
            if (taskManager == null)
                taskManager = FindFirstObjectByType<ToolNBackManagerBehaviour>();
        }

        private void OnEnable()
        {
            if (_responseAction == null)
            {
                _responseAction = new InputAction(name: "ToolNBackResponse", type: InputActionType.Button);
                _responseAction.AddBinding("<XRController>{RightHand}/triggerPressed");
                _responseAction.AddBinding("<XRController>{LeftHand}/triggerPressed");
                _responseAction.AddBinding("<XRController>/primaryButton");
                // The native Varjo XR plugin names the primary face button control
                // "primary" (no "primaryButton" control or alias on its Index/Vive
                // layouts), so the OpenXR-style binding above never fires on Varjo.
                _responseAction.AddBinding("<XRController>/primary");
                _responseAction.AddBinding("<Keyboard>/enter");
                _responseAction.AddBinding("<Keyboard>/space");

                foreach (string binding in _extraBindings)
                {
                    if (!string.IsNullOrWhiteSpace(binding))
                        _responseAction.AddBinding(binding.Trim());
                }

                _responseAction.performed += HandleActionPerformed;
            }

            if (_probeStickAction == null)
                CreateRatingActions();

            _responseAction.Enable();
            _probeStickAction.Enable();
            _probeLeftAction.Enable();
            _probeRightAction.Enable();
            _probeDigitAction.Enable();

            InputSystem.onDeviceChange += HandleDeviceChange;
            if (_logControllerButtons)
                LogConnectedControllers();
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= HandleDeviceChange;

            if (_responseAction != null)
            {
                _responseAction.Disable();
                _responseAction.performed -= HandleActionPerformed;
                _responseAction.Dispose();
                _responseAction = null;
            }

            if (_probeStickAction != null)
            {
                _probeLeftAction.performed -= HandleLeftPerformed;
                _probeRightAction.performed -= HandleRightPerformed;
                _probeDigitAction.performed -= HandleDigitPerformed;
                foreach (InputAction action in new[] { _probeStickAction, _probeLeftAction, _probeRightAction, _probeDigitAction })
                {
                    action.Disable();
                    action.Dispose();
                }
                _probeStickAction = _probeLeftAction = _probeRightAction = _probeDigitAction = null;
            }
        }

        private void Update()
        {
            if (_probeStickAction != null)
            {
                int step = _stickStepper.Step(_probeStickAction.ReadValue<Vector2>().x);
                if (step != 0)
                    Forward(manager => manager.HandleProbeNavigate(step));
            }

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
                    if (control is ButtonControl button && button.wasPressedThisFrame)
                    {
                        Debug.Log($"[{name}] button pressed: {button.path} " +
                                  $"(layout={device.layout}, isXRController={device is UnityEngine.InputSystem.XR.XRController})");
                    }
                    else if (control is Vector2Control stick && !(control.parent is Vector2Control))
                    {
                        bool pushed = stick.ReadValue().magnitude >= StickStepper.PressThreshold;
                        if (pushed && _pushedSticks.Add(stick))
                            Debug.Log($"[{name}] stick pushed: {stick.path} (layout={device.layout}) — " +
                                      $"add <XRController>/{stick.name} to Extra Navigation Bindings if the rating scale does not move");
                        else if (!pushed)
                            _pushedSticks.Remove(stick);
                    }
                }
            }
        }
        #endregion

        #region Private methods
        private void CreateRatingActions()
        {
            _probeStickAction = new InputAction(name: "EffortProbeStick", type: InputActionType.Value,
                expectedControlType: "Vector2");
            _probeStickAction.AddBinding("<XRController>/primary2DAxis");
            _probeStickAction.AddBinding("<XRController>/thumbstick");
            _probeStickAction.AddBinding("<XRController>/joystick");
            foreach (string binding in _extraNavigationBindings)
            {
                if (!string.IsNullOrWhiteSpace(binding))
                    _probeStickAction.AddBinding(binding.Trim());
            }

            _probeLeftAction = new InputAction(name: "EffortProbeLeft", type: InputActionType.Button);
            _probeLeftAction.AddBinding("<Keyboard>/leftArrow");
            _probeLeftAction.AddBinding("<Keyboard>/a");
            _probeLeftAction.performed += HandleLeftPerformed;

            _probeRightAction = new InputAction(name: "EffortProbeRight", type: InputActionType.Button);
            _probeRightAction.AddBinding("<Keyboard>/rightArrow");
            _probeRightAction.AddBinding("<Keyboard>/d");
            _probeRightAction.performed += HandleRightPerformed;

            // The experimenter types the number the participant says (Index buttons may not
            // arrive in recording mode), on the number row or the numpad.
            _probeDigitAction = new InputAction(name: "EffortProbeDigit", type: InputActionType.Button);
            for (int digit = 1; digit <= 9; digit++)
            {
                _probeDigitAction.AddBinding($"<Keyboard>/{digit}");
                _probeDigitAction.AddBinding($"<Keyboard>/numpad{digit}");
            }
            _probeDigitAction.performed += HandleDigitPerformed;
        }

        /// <summary>Calls the task manager, finding it first when unset (Unity-aware null checks).</summary>
        private void Forward(System.Action<ToolNBackManagerBehaviour> call)
        {
            if (taskManager == null)
                taskManager = FindFirstObjectByType<ToolNBackManagerBehaviour>();

            if (taskManager != null)
                call(taskManager);
        }

        private void HandleActionPerformed(InputAction.CallbackContext context)
        {
            // performed can fire once per matching binding; forward at most one response per frame.
            if (Time.frameCount == _lastResponseFrame)
                return;
            _lastResponseFrame = Time.frameCount;

            Forward(manager => manager.HandleOnInput());
        }

        private void HandleLeftPerformed(InputAction.CallbackContext context) =>
            Forward(manager => manager.HandleProbeNavigate(-1));

        private void HandleRightPerformed(InputAction.CallbackContext context) =>
            Forward(manager => manager.HandleProbeNavigate(1));

        private void HandleDigitPerformed(InputAction.CallbackContext context)
        {
            // Control names are "1".."9" on the number row and "numpad1".."numpad9".
            string key = context.control.name;
            int digit = key[key.Length - 1] - '0';
            if (digit >= 1 && digit <= 9)
                Forward(manager => manager.HandleProbeChoice(digit));
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
    /// Legacy input fallback: Enter or Space responds; for the effort rating, the arrow
    /// keys move the selection and the digit keys 1–9 enter a number. Used when the new
    /// Input System is unavailable.
    /// </summary>
    public class ToolNBackResponseInput : MonoBehaviour
    {
        public ToolNBackManagerBehaviour taskManager;

        private void Awake()
        {
            if (taskManager == null)
                taskManager = FindFirstObjectByType<ToolNBackManagerBehaviour>();
        }

        private void Update()
        {
            if (taskManager == null)
                return;

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                taskManager.HandleOnInput();
            if (Input.GetKeyDown(KeyCode.LeftArrow))
                taskManager.HandleProbeNavigate(-1);
            if (Input.GetKeyDown(KeyCode.RightArrow))
                taskManager.HandleProbeNavigate(1);
            for (int digit = 1; digit <= 9; digit++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + digit) || Input.GetKeyDown(KeyCode.Keypad0 + digit))
                    taskManager.HandleProbeChoice(digit);
            }
        }
    }
#endif
}
