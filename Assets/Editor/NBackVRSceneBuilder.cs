using System;
using System.IO;
using System.Linq;
using System.Text;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Management;

namespace NBackStreaming.EditorTools
{
    /// <summary>
    /// Editor tooling for the VR n-back setup. Builds or repairs the VR-N-Back scene
    /// (XR rig, event system, task canvas, LSL streaming components), enables the Varjo
    /// XR loader and validates the whole configuration. All steps are idempotent, so the
    /// menu items can be re-run safely at any time. <see cref="CI"/> is the batchmode
    /// entry point (-executeMethod NBackStreaming.EditorTools.NBackVRSceneBuilder.CI).
    /// </summary>
    public static class NBackVRSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/VR-N-Back.unity";
        private const string CanvasPrefabPath = "Assets/Prefabs/TextTaskCanvas.prefab";
        private const string VarjoLoaderTypeName = "Varjo.XR.VarjoLoader";
        private const string XRSettingsFolder = "Assets/XR";
        private const string XRSettingsAssetPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
        private const string PositionBindingPath = "<XRHMD>/centerEyePosition";
        private const string RotationBindingPath = "<XRHMD>/centerEyeRotation";
        private const string StreamingObjectName = "LSL Streaming";
        private const float CameraYOffset = 1.1176f;
        private static readonly Vector3 CanvasPosition = new Vector3(0f, 1.6f, 1.5f);

        #region Menu entry points

        /// <summary>
        /// Opens Assets/Scenes/VR-N-Back.unity and (re)creates the XR rig, event system,
        /// task canvas placement and LSL streaming object, then saves the scene and adds
        /// it to the build settings. Safe to run repeatedly.
        /// </summary>
        [MenuItem("NBack VR/Build or Repair VR-N-Back Scene")]
        public static void BuildOrRepairScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("NBack VR: build cancelled by user.");
                return;
            }

            BuildOrRepair();
        }

        /// <summary>
        /// Logs a readable PASS/FAIL report covering packages, types, Varjo loader
        /// assignment, project settings and the wiring of the VR-N-Back scene.
        /// </summary>
        [MenuItem("NBack VR/Validate Setup")]
        public static void ValidateSetup()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log("NBack VR: validation cancelled by user.");
                return;
            }

            Validate();
        }

        /// <summary>
        /// Assigns the Varjo XR loader for the Standalone build target and enables
        /// "Initialize XR on Startup", creating the XR management settings assets under
        /// Assets/XR when they do not exist yet.
        /// </summary>
        [MenuItem("NBack VR/Enable Varjo XR Loader")]
        public static void EnableVarjoLoader()
        {
            if (EnableVarjoLoaderInternal())
                Debug.Log("NBack VR: Varjo XR loader is enabled for Standalone (init on startup: ON).");
        }

        /// <summary>
        /// Batchmode entry point: enables the Varjo loader, builds/repairs the scene and
        /// validates the setup. Logs NBACKVR_CI_SUCCESS and exits 0 on success; exits 1
        /// on any hard failure. Safe with -batchmode -nographics.
        /// </summary>
        public static void CI()
        {
            try
            {
                Debug.Log("NBack VR CI: enabling Varjo XR loader...");
                if (!EnableVarjoLoaderInternal())
                {
                    Debug.LogError("NBack VR CI: Varjo XR loader assignment failed.");
                    EditorApplication.Exit(1);
                    return;
                }

                Debug.Log("NBack VR CI: building/repairing scene...");
                BuildOrRepair();

                Debug.Log("NBack VR CI: validating setup...");
                if (!Validate())
                {
                    Debug.LogError("NBack VR CI: validation failed (see report above).");
                    EditorApplication.Exit(1);
                    return;
                }

                Debug.Log("NBACKVR_CI_SUCCESS");
                // Intentionally no EditorApplication.Exit(0) here: run CI with -quit so the
                // editor shuts down gracefully and flushes EditorBuildSettings.asset
                // (scene list + XR loader config object) to disk. A hard Exit skips that save.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        #endregion

        #region Build steps

        private static void BuildOrRepair()
        {
            Scene scene = OpenTargetScene();

            RemoveStrayDesktopCamera(scene);
            Camera rigCamera = EnsureXRRig();
            EnsureEventSystem();
            global::Task.TaskManagerBehaviour taskManager = EnsureTaskCanvas(scene, rigCamera);
            EnsureStreamingObject(scene, rigCamera, taskManager);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"NBack VR: failed to save scene '{ScenePath}'.");

            EnsureSceneInBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log($"NBack VR: build/repair of '{ScenePath}' finished.");
        }

        private static Scene OpenTargetScene()
        {
            Scene active = SceneManager.GetActiveScene();
            if (string.Equals(active.path, ScenePath, StringComparison.OrdinalIgnoreCase) && active.isLoaded)
                return active;

            if (!File.Exists(ScenePath))
                throw new FileNotFoundException($"NBack VR: scene not found at '{ScenePath}'.");

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        /// <summary>Removes the leftover desktop camera (a root object named "Main Camera" that is not part of an XR rig).</summary>
        private static void RemoveStrayDesktopCamera(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == null || root.name != "Main Camera")
                    continue;
                if (root.GetComponentInParent<XROrigin>() != null || root.GetComponentInChildren<XROrigin>(true) != null)
                    continue;

                Debug.Log("NBack VR: removing stray desktop 'Main Camera'.");
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        /// <summary>Creates or repairs "XR Origin (VR)" &gt; "Camera Offset" &gt; "Main Camera" and returns the rig camera.</summary>
        private static Camera EnsureXRRig()
        {
            XROrigin origin = UnityEngine.Object.FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
            if (origin == null)
                origin = new GameObject("XR Origin (VR)").AddComponent<XROrigin>();
            origin.gameObject.name = "XR Origin (VR)";

            GameObject offsetGo = origin.CameraFloorOffsetObject;
            if (offsetGo == null)
            {
                Transform existingOffset = origin.transform.Find("Camera Offset");
                offsetGo = existingOffset != null ? existingOffset.gameObject : new GameObject("Camera Offset");
            }
            if (offsetGo.transform.parent != origin.transform)
                offsetGo.transform.SetParent(origin.transform, false);

            Camera rigCamera = origin.Camera;
            if (rigCamera == null)
                rigCamera = offsetGo.GetComponentInChildren<Camera>(true);
            if (rigCamera == null)
            {
                var cameraGo = new GameObject("Main Camera");
                cameraGo.transform.SetParent(offsetGo.transform, false);
                rigCamera = cameraGo.AddComponent<Camera>();
            }
            if (rigCamera.transform.parent != offsetGo.transform)
                rigCamera.transform.SetParent(offsetGo.transform, false);

            rigCamera.gameObject.tag = "MainCamera";
            rigCamera.clearFlags = CameraClearFlags.Skybox;
            rigCamera.nearClipPlane = 0.01f;
            rigCamera.farClipPlane = 1000f;
            EnsureComponent<AudioListener>(rigCamera.gameObject);
            ConfigureTrackedPoseDriver(EnsureComponent<TrackedPoseDriver>(rigCamera.gameObject));

            origin.Camera = rigCamera;
            origin.CameraFloorOffsetObject = offsetGo;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            origin.CameraYOffset = CameraYOffset;
            return rigCamera;
        }

        /// <summary>
        /// Binds inline HMD center-eye actions on the Input System TrackedPoseDriver.
        /// Inline (non-reference) actions assigned through positionInput/rotationInput are
        /// enabled by the TrackedPoseDriver itself in OnEnable, so no extra runtime
        /// enabling step is required.
        /// </summary>
        private static void ConfigureTrackedPoseDriver(TrackedPoseDriver driver)
        {
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

            if (!HasBinding(driver.positionInput, PositionBindingPath))
            {
                driver.positionInput = new InputActionProperty(new InputAction(
                    name: "Center Eye Position",
                    type: InputActionType.Value,
                    binding: PositionBindingPath,
                    expectedControlType: "Vector3"));
            }

            if (!HasBinding(driver.rotationInput, RotationBindingPath))
            {
                driver.rotationInput = new InputActionProperty(new InputAction(
                    name: "Center Eye Rotation",
                    type: InputActionType.Value,
                    binding: RotationBindingPath,
                    expectedControlType: "Quaternion"));
            }
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (eventSystem == null)
                eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();

            StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                Debug.Log("NBack VR: replacing StandaloneInputModule with XRUIInputModule.");
                UnityEngine.Object.DestroyImmediate(legacyModule);
            }

            EnsureComponent<XRUIInputModule>(eventSystem.gameObject);
        }

        /// <summary>Finds (or instantiates) the TextTaskCanvas, places it in front of the rig and wires it for XR UI raycasts.</summary>
        private static global::Task.TaskManagerBehaviour EnsureTaskCanvas(Scene scene, Camera rigCamera)
        {
            var taskManager = UnityEngine.Object.FindFirstObjectByType<global::Task.TaskManagerBehaviour>(FindObjectsInactive.Include);
            if (taskManager == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPrefabPath);
                if (prefab == null)
                    throw new InvalidOperationException($"NBack VR: TextTaskCanvas prefab not found at '{CanvasPrefabPath}'.");

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                taskManager = instance.GetComponentInChildren<global::Task.TaskManagerBehaviour>(true);
                if (taskManager == null)
                    throw new InvalidOperationException("NBack VR: instantiated TextTaskCanvas prefab has no TaskManagerBehaviour.");
            }

            Transform canvasRoot = taskManager.transform;
            canvasRoot.position = CanvasPosition;
            canvasRoot.rotation = Quaternion.identity; // scale intentionally untouched (prefab is 0.002 => 1 m panel)

            EnsureComponent<TrackedDeviceGraphicRaycaster>(taskManager.gameObject);
            Canvas canvas = taskManager.GetComponent<Canvas>();
            if (canvas != null)
                canvas.worldCamera = rigCamera;

            return taskManager;
        }

        /// <summary>Creates or repairs the "LSL Streaming" object carrying all streaming components.</summary>
        private static void EnsureStreamingObject(Scene scene, Camera rigCamera, global::Task.TaskManagerBehaviour taskManager)
        {
            NBackMarkerStreamer existing = UnityEngine.Object.FindFirstObjectByType<NBackMarkerStreamer>(FindObjectsInactive.Include);
            GameObject streamingGo = existing != null
                ? existing.gameObject
                : scene.GetRootGameObjects().FirstOrDefault(go => go.name == StreamingObjectName);
            if (streamingGo == null)
                streamingGo = new GameObject(StreamingObjectName);

            NBackMarkerStreamer markerStreamer = EnsureComponent<NBackMarkerStreamer>(streamingGo);
            EnsureComponent<NBackEventFileLogger>(streamingGo);
            VarjoGazeLSLStreamer gazeStreamer = EnsureComponent<VarjoGazeLSLStreamer>(streamingGo);
            HeadPoseLSLStreamer headPoseStreamer = EnsureComponent<HeadPoseLSLStreamer>(streamingGo);
            ControllerResponseInput responseInput = EnsureComponent<ControllerResponseInput>(streamingGo);

            SetSerializedReference(markerStreamer, taskManager, "taskManager", "_taskManager", "m_TaskManager");
            SetSerializedReference(gazeStreamer, rigCamera, "xrCamera", "_xrCamera", "m_XrCamera");
            SetSerializedReference(headPoseStreamer, rigCamera, "xrCamera", "_xrCamera", "m_XrCamera");
            SetSerializedReference(responseInput, taskManager, "taskManager", "_taskManager", "m_TaskManager");
        }

        private static void EnsureSceneInBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (!string.Equals(scenes[i].path, ScenePath, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!scenes[i].enabled)
                {
                    scenes[i].enabled = true;
                    EditorBuildSettings.scenes = scenes;
                }
                return;
            }

            EditorBuildSettings.scenes = scenes.Append(new EditorBuildSettingsScene(ScenePath, true)).ToArray();
        }

        #endregion

        #region Varjo loader

        private static bool EnableVarjoLoaderInternal()
        {
            const string manualHint = "Enable it manually instead: Edit > Project Settings > XR Plug-in Management "
                                      + "> Windows tab > check 'Varjo' and enable 'Initialize XR on Startup'.";
            try
            {
                XRGeneralSettingsPerBuildTarget buildTargetSettings;
                if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out buildTargetSettings)
                    || buildTargetSettings == null)
                {
                    if (!AssetDatabase.IsValidFolder(XRSettingsFolder))
                        AssetDatabase.CreateFolder("Assets", "XR");
                    buildTargetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                    AssetDatabase.CreateAsset(buildTargetSettings, XRSettingsAssetPath);
                    EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, buildTargetSettings, true);
                }

                XRGeneralSettings standaloneSettings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Standalone);
                if (standaloneSettings == null)
                {
                    standaloneSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
                    standaloneSettings.name = "Standalone Settings";
                    AssetDatabase.AddObjectToAsset(standaloneSettings, buildTargetSettings);
                    buildTargetSettings.SetSettingsForBuildTarget(BuildTargetGroup.Standalone, standaloneSettings);
                }

                XRManagerSettings manager = standaloneSettings.Manager;
                if (manager == null)
                {
                    manager = ScriptableObject.CreateInstance<XRManagerSettings>();
                    manager.name = "Standalone Providers";
                    AssetDatabase.AddObjectToAsset(manager, buildTargetSettings);
                    standaloneSettings.Manager = manager;
                }

                if (!IsVarjoLoaderAssigned(manager)
                    && !XRPackageMetadataStore.AssignLoader(manager, VarjoLoaderTypeName, BuildTargetGroup.Standalone))
                {
                    Debug.LogError("NBack VR: XRPackageMetadataStore.AssignLoader could not assign "
                                   + $"'{VarjoLoaderTypeName}' for Standalone. Is the com.varjo.xr package installed? {manualHint}");
                    return false;
                }

                standaloneSettings.InitManagerOnStart = true;

                EditorUtility.SetDirty(buildTargetSettings);
                EditorUtility.SetDirty(standaloneSettings);
                EditorUtility.SetDirty(manager);
                AssetDatabase.SaveAssets();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"NBack VR: enabling the Varjo XR loader failed: {exception.Message}\n{manualHint}");
                return false;
            }
        }

        private static bool IsVarjoLoaderAssigned(XRManagerSettings manager)
        {
            return manager != null
                   && manager.activeLoaders != null
                   && manager.activeLoaders.Any(l => l != null && l.GetType().FullName == VarjoLoaderTypeName);
        }

        #endregion

        #region Validation

        private static bool Validate()
        {
            var report = new StringBuilder();
            bool allPassed = true;
            report.AppendLine("=== NBack VR Setup Validation ===");

            void Check(string label, bool passed, string detail = null)
            {
                allPassed &= passed;
                report.Append(passed ? "[PASS] " : "[FAIL] ").Append(label);
                if (!string.IsNullOrEmpty(detail))
                    report.Append(" -- ").Append(detail);
                report.AppendLine();
            }

            // --- Packages ---
            UnityEditor.PackageManager.PackageInfo[] packages;
            try
            {
                packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            }
            catch (Exception exception)
            {
                packages = Array.Empty<UnityEditor.PackageManager.PackageInfo>();
                Check("Package registry readable", false, exception.Message);
            }

            bool HasPackage(string name) => packages.Any(p => p.name == name);
            Check("Package com.unity.inputsystem", HasPackage("com.unity.inputsystem"));
            Check("Package com.unity.xr.management", HasPackage("com.unity.xr.management"));
            Check("Package com.unity.xr.interaction.toolkit", HasPackage("com.unity.xr.interaction.toolkit"));
            Check("Package com.varjo.xr", HasPackage("com.varjo.xr"));
            Check("Package com.physiolabxr.unityplugin (LSL)", HasPackage("com.physiolabxr.unityplugin"));

            // --- liblsl native library probe ---
            UnityEditor.PackageManager.PackageInfo lslPackage = packages.FirstOrDefault(p => p.name == "com.physiolabxr.unityplugin");
            bool lslDllFound = false;
            string lslDetail = "package not installed";
            if (lslPackage != null)
            {
                lslDetail = lslPackage.resolvedPath;
                try
                {
                    lslDllFound = Directory.Exists(lslPackage.resolvedPath)
                                  && Directory.GetFiles(lslPackage.resolvedPath, "*.dll", SearchOption.AllDirectories)
                                      .Any(f => Path.GetFileName(f).ToLowerInvariant().Contains("lsl"));
                }
                catch (Exception exception)
                {
                    lslDetail = exception.Message;
                }
            }
            Check("liblsl native library present in LSL package", lslDllFound, lslDetail);

            // --- Types (reflection probes, robust against missing packages) ---
            Check("Type Unity.XR.CoreUtils.XROrigin", FindType("Unity.XR.CoreUtils.XROrigin") != null);
            Check("Type UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule",
                FindType("UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule") != null);
            Check("Type UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster",
                FindType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster") != null);
            Check("Type UnityEngine.InputSystem.XR.TrackedPoseDriver",
                FindType("UnityEngine.InputSystem.XR.TrackedPoseDriver") != null);
            Check("Type Varjo.XR.VarjoEyeTracking", FindType("Varjo.XR.VarjoEyeTracking") != null);
            Check("Type LSL.StreamOutlet", FindType("LSL.StreamOutlet") != null);

            // --- Varjo loader assignment (Standalone) ---
            bool loaderAssigned = false;
            string loaderDetail = "XR general settings not found -- run 'NBack VR/Enable Varjo XR Loader'";
            if (EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,
                    out XRGeneralSettingsPerBuildTarget buildTargetSettings) && buildTargetSettings != null)
            {
                XRGeneralSettings standaloneSettings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Standalone);
                if (standaloneSettings == null)
                {
                    loaderDetail = "no Standalone XR settings instance";
                }
                else if (standaloneSettings.Manager == null)
                {
                    loaderDetail = "no Standalone XR manager instance";
                }
                else
                {
                    bool inList = IsVarjoLoaderAssigned(standaloneSettings.Manager);
                    loaderAssigned = inList && standaloneSettings.InitManagerOnStart;
                    loaderDetail = !inList
                        ? "Varjo loader not in the Standalone loader list"
                        : standaloneSettings.InitManagerOnStart
                            ? "assigned, init-on-startup ON"
                            : "assigned but 'Initialize XR on Startup' is OFF";
                }
            }
            Check("Varjo XR loader assigned for Standalone", loaderAssigned, loaderDetail);

            // --- Project settings: active input handler must be 'Both' (2) ---
            bool inputHandlerBoth = false;
            string inputHandlerDetail = "Project Settings > Player > Active Input Handling must be 'Both'";
            try
            {
                inputHandlerBoth = File.ReadAllText("ProjectSettings/ProjectSettings.asset")
                    .Contains("activeInputHandler: 2");
            }
            catch (Exception exception)
            {
                inputHandlerDetail = exception.Message;
            }
            Check("activeInputHandler: 2 (Both)", inputHandlerBoth, inputHandlerBoth ? null : inputHandlerDetail);

            // --- Scene wiring ---
            bool sceneOpened = false;
            try
            {
                Scene scene = OpenTargetScene();
                sceneOpened = scene.IsValid() && scene.isLoaded;
            }
            catch (Exception exception)
            {
                Check($"Scene openable: {ScenePath}", false, exception.Message);
            }

            if (sceneOpened)
            {
                Check($"Scene openable: {ScenePath}", true);

                XROrigin origin = UnityEngine.Object.FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
                Camera rigCamera = origin != null ? origin.Camera : null;
                Check("XROrigin present", origin != null);
                Check("XROrigin.Camera assigned", rigCamera != null);
                Check("XROrigin.CameraFloorOffsetObject assigned", origin != null && origin.CameraFloorOffsetObject != null);
                Check("Rig camera tagged MainCamera", rigCamera != null && rigCamera.CompareTag("MainCamera"));

                TrackedPoseDriver poseDriver = rigCamera != null ? rigCamera.GetComponent<TrackedPoseDriver>() : null;
                Check("TrackedPoseDriver on rig camera", poseDriver != null);
                Check("TrackedPoseDriver bound to <XRHMD> center eye",
                    poseDriver != null
                    && HasBinding(poseDriver.positionInput, PositionBindingPath)
                    && HasBinding(poseDriver.rotationInput, RotationBindingPath));

                EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
                Check("EventSystem present", eventSystem != null);
                Check("XRUIInputModule on EventSystem",
                    eventSystem != null && eventSystem.GetComponent<XRUIInputModule>() != null);
                Check("No StandaloneInputModule on EventSystem",
                    eventSystem == null || eventSystem.GetComponent<StandaloneInputModule>() == null);

                var taskManager = UnityEngine.Object.FindFirstObjectByType<global::Task.TaskManagerBehaviour>(FindObjectsInactive.Include);
                Canvas canvas = taskManager != null ? taskManager.GetComponent<Canvas>() : null;
                Check("TextTaskCanvas (TaskManagerBehaviour) present", taskManager != null);
                Check("TrackedDeviceGraphicRaycaster on canvas",
                    taskManager != null && taskManager.GetComponent<TrackedDeviceGraphicRaycaster>() != null);
                Check("Canvas.worldCamera set to rig camera",
                    canvas != null && rigCamera != null && canvas.worldCamera == rigCamera);
                Check("Canvas placed at (0, 1.6, 1.5)",
                    taskManager != null && (taskManager.transform.position - CanvasPosition).sqrMagnitude < 1e-6f);

                Check("NBackMarkerStreamer in scene",
                    UnityEngine.Object.FindFirstObjectByType<NBackMarkerStreamer>(FindObjectsInactive.Include) != null);
                Check("NBackEventFileLogger in scene",
                    UnityEngine.Object.FindFirstObjectByType<NBackEventFileLogger>(FindObjectsInactive.Include) != null);
                Check("VarjoGazeLSLStreamer in scene",
                    UnityEngine.Object.FindFirstObjectByType<VarjoGazeLSLStreamer>(FindObjectsInactive.Include) != null);
                Check("HeadPoseLSLStreamer in scene",
                    UnityEngine.Object.FindFirstObjectByType<HeadPoseLSLStreamer>(FindObjectsInactive.Include) != null);
                Check("ControllerResponseInput in scene",
                    UnityEngine.Object.FindFirstObjectByType<ControllerResponseInput>(FindObjectsInactive.Include) != null);
            }

            Check("Scene enabled in Build Settings",
                EditorBuildSettings.scenes.Any(s =>
                    string.Equals(s.path, ScenePath, StringComparison.OrdinalIgnoreCase) && s.enabled));

            report.AppendLine(allPassed
                ? "All checks passed."
                : "One or more checks FAILED. Run 'NBack VR/Enable Varjo XR Loader' and "
                  + "'NBack VR/Build or Repair VR-N-Back Scene', then validate again.");
            Debug.Log(report.ToString());
            return allPassed;
        }

        #endregion

        #region Helpers

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component == null)
                component = go.AddComponent<T>();
            return component;
        }

        private static bool HasBinding(InputActionProperty property, string path)
        {
            InputAction action = property.action;
            if (action == null)
                return false;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].path == path)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Sets a serialized object-reference field on a component, trying several
        /// candidate field names (works for both public and [SerializeField] private
        /// fields). Logs a warning when none match; all streaming components also have a
        /// runtime auto-find fallback, so this is not fatal.
        /// </summary>
        private static void SetSerializedReference(Component target, UnityEngine.Object value, params string[] candidateNames)
        {
            if (target == null)
                return;

            var serializedObject = new SerializedObject(target);
            foreach (string candidate in candidateNames)
            {
                SerializedProperty property = serializedObject.FindProperty(candidate);
                if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                    continue;
                property.objectReferenceValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                return;
            }

            Debug.LogWarning($"NBack VR: no serialized field ({string.Join(", ", candidateNames)}) found on "
                             + $"{target.GetType().Name}; relying on its runtime auto-find fallback.");
        }

        private static Type FindType(string fullName)
        {
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type;
                try
                {
                    type = assembly.GetType(fullName, false);
                }
                catch
                {
                    type = null;
                }
                if (type != null)
                    return type;
            }
            return null;
        }

        #endregion
    }
}
