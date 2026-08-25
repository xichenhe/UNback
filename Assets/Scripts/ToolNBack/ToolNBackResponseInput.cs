using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ToolNBack
{
#if ENABLE_INPUT_SYSTEM
    /// <summary>
    /// Routes XR controller presses (trigger or primary button) and keyboard presses
    /// (Enter or Space) to <see cref="ToolNBackManagerBehaviour.HandleOnInput"/>.
    /// Uses a runtime-created Input System action, so no .inputactions asset is required.
    /// </summary>
    public class ToolNBackResponseInput : MonoBehaviour
    {
        /// <summary>Task manager receiving responses. Found via FindFirstObjectByType in Awake when unset.</summary>
        public ToolNBackManagerBehaviour taskManager;

        private InputAction _responseAction;
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
                _responseAction.AddBinding("<Keyboard>/enter");
                _responseAction.AddBinding("<Keyboard>/space");
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
            // performed can fire once per matching binding; forward at most one response per frame.
            if (Time.frameCount == _lastResponseFrame)
                return;
            _lastResponseFrame = Time.frameCount;

            if (taskManager == null)
                taskManager = FindFirstObjectByType<ToolNBackManagerBehaviour>();

            if (taskManager != null)
                taskManager.HandleOnInput();
        }
        #endregion
    }
#else
    /// <summary>
    /// Legacy input fallback: Enter or Space responds. Used when the new Input System
    /// is unavailable.
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
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                if (taskManager != null)
                    taskManager.HandleOnInput();
            }
        }
    }
#endif
}
