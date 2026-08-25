using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds the VR-Tool-NBack scene from the existing VR-N-Back scene: keeps the XR rig,
/// lighting and LSL streaming objects, removes the text task canvas and its input, and
/// adds the tool based n-back manager with its curved panel anchor.
/// Run from the menu or via -executeMethod ToolNBackSceneBuilder.Build.
/// </summary>
public static class ToolNBackSceneBuilder
{
    private const string SourceScenePath = "Assets/Scenes/VR-N-Back.unity";
    private const string TargetScenePath = "Assets/Scenes/VR-Tool-NBack.unity";

    /// <summary>
    /// Head-height pivot of the curved tool panel; matches the 1.6 m eye height the
    /// text task canvas uses (see NBackVRSceneBuilder.CanvasPosition).
    /// </summary>
    private static readonly Vector3 PanelAnchorPosition = new Vector3(0f, 1.6f, 0f);

    private const string ToolPrefabFolder = "Assets/_MITmech/Resources/Prefabs/Toolboard/Prefabs/Tools";

    /// <summary>
    /// Stimulus pool wired into the scene, in order: the first entry is the designated
    /// 0-back target and the first ToolPoolSize entries (default 8) form the pool.
    /// Exactly one drill bit is included — the 9 bit variants are visually too similar
    /// to serve as distinct n-back stimuli.
    /// </summary>
    private static readonly string[] CuratedToolPrefabs =
    {
        "Mallet",
        "Chuck",
        "Brush",
        "Edge Finder",
        "Parallels",
        "Chuck Key",
        "Deburring Tool",
        "Drill Bit _ 1in",
        "Center Drill",
        "Block",
    };

    [MenuItem("NBack VR/Build Tool NBack Scene")]
    public static void Build()
    {
        // Rebuilding regenerates the scene from VR-N-Back and discards everything
        // else in it (e.g. hand-placed environment objects) — confirm when a scene
        // already exists and a human is at the keyboard.
        if (!Application.isBatchMode && System.IO.File.Exists(TargetScenePath) &&
            !EditorUtility.DisplayDialog("Rebuild Tool NBack Scene?",
                $"This regenerates {TargetScenePath} from the VR-N-Back scene.\n\n" +
                "Custom objects added to the Tool NBack scene will be LOST.",
                "Rebuild", "Cancel"))
        {
            return;
        }

        var scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);

        // Remove the text based task (canvas hosts TaskManagerBehaviour and the task behaviours).
        var textTask = Object.FindFirstObjectByType<Task.TaskManagerBehaviour>(FindObjectsInactive.Include);
        if (textTask != null)
            Object.DestroyImmediate(textTask.transform.root.gameObject);

        // Replace the text task response input, keeping the LSL streamers on their object.
        var oldInput = Object.FindFirstObjectByType<NBackStreaming.ControllerResponseInput>(FindObjectsInactive.Include);
        GameObject inputHost = null;
        if (oldInput != null)
        {
            inputHost = oldInput.gameObject;
            Object.DestroyImmediate(oldInput);
        }

        var root = new GameObject("Tool NBack");
        root.transform.position = PanelAnchorPosition;
        var manager = root.AddComponent<ToolNBack.ToolNBackManagerBehaviour>();
        AssignCuratedTools(manager);
        SetSerializedFloat(manager, "_panelPivotHeightMeters", PanelAnchorPosition.y);
        (inputHost != null ? inputHost : root).AddComponent<ToolNBack.ToolNBackResponseInput>();
        AddDesktopViewFallback();

        EditorSceneManager.SaveScene(scene, TargetScenePath);
        AddToBuildSettings(TargetScenePath);

        Debug.Log($"[ToolNBackSceneBuilder] Built {TargetScenePath}");
    }

    /// <summary>
    /// Reopens the built scene and checks its wiring; exits with a non-zero code in
    /// batch mode when something is missing. Run via -executeMethod
    /// ToolNBackSceneBuilder.BuildAndValidate.
    /// </summary>
    public static void BuildAndValidate()
    {
        Build();

        EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);

        int errors = 0;
        void Check(bool ok, string what)
        {
            if (ok)
            {
                Debug.Log($"[ToolNBackSceneBuilder] ok: {what}");
            }
            else
            {
                errors++;
                Debug.LogError($"[ToolNBackSceneBuilder] FAILED: {what}");
            }
        }

        Check(Object.FindFirstObjectByType<ToolNBack.ToolNBackManagerBehaviour>(FindObjectsInactive.Include) != null,
            "scene contains ToolNBackManagerBehaviour");
        Check(Object.FindFirstObjectByType<ToolNBack.ToolNBackResponseInput>(FindObjectsInactive.Include) != null,
            "scene contains ToolNBackResponseInput");
        Check(Object.FindFirstObjectByType<NBackStreaming.NBackMarkerStreamer>(FindObjectsInactive.Include) != null,
            "scene keeps NBackMarkerStreamer (LSL)");
        Check(Object.FindFirstObjectByType<NBackStreaming.HeadPoseLSLStreamer>(FindObjectsInactive.Include) != null,
            "scene keeps HeadPoseLSLStreamer (LSL)");
        Check(Object.FindFirstObjectByType<Task.TaskManagerBehaviour>(FindObjectsInactive.Include) == null,
            "text task removed");
        Check(Object.FindFirstObjectByType<NBackStreaming.ControllerResponseInput>(FindObjectsInactive.Include) == null,
            "text task input removed");
        Check(Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Include) != null,
            "scene keeps the XR camera");
        Check(Resources.LoadAll<GameObject>(ToolNBack.ToolNBackManagerBehaviour.ToolsResourcePath).Length >= 2,
            $"tool prefabs loadable from Resources/{ToolNBack.ToolNBackManagerBehaviour.ToolsResourcePath}");

        var sceneManager = Object.FindFirstObjectByType<ToolNBack.ToolNBackManagerBehaviour>(FindObjectsInactive.Include);
        if (sceneManager != null)
        {
            var serialized = new SerializedObject(sceneManager);
            SerializedProperty tools = serialized.FindProperty("_toolPrefabs");
            int drillBits = 0;
            bool anyNull = false;
            for (int i = 0; i < tools.arraySize; i++)
            {
                Object entry = tools.GetArrayElementAtIndex(i).objectReferenceValue;
                if (entry == null)
                    anyNull = true;
                else if (entry.name.StartsWith("Drill Bit"))
                    drillBits++;
            }

            Check(tools.arraySize >= 8, $"curated tool pool assigned ({tools.arraySize} prefabs, need >= 8)");
            Check(!anyNull, "no missing entries in the curated tool pool");
            Check(drillBits == 1, $"exactly one drill bit in the curated pool (found {drillBits})");

            SerializedProperty pivotHeight = serialized.FindProperty("_panelPivotHeightMeters");
            Check(pivotHeight != null && Mathf.Approximately(pivotHeight.floatValue, PanelAnchorPosition.y),
                $"panel pivot height field set to {PanelAnchorPosition.y} m");
        }

        Check(Object.FindFirstObjectByType<ToolNBack.DesktopViewFallback>(FindObjectsInactive.Include) != null,
            "DesktopViewFallback present on the rig for headset-less desk testing");

        Debug.Log(errors == 0
            ? "[ToolNBackSceneBuilder] Validation PASSED"
            : $"[ToolNBackSceneBuilder] Validation FAILED with {errors} error(s)");

        if (Application.isBatchMode)
            EditorApplication.Exit(errors == 0 ? 0 : 1);
    }

    /// <summary>
    /// Puts the desk-testing camera fallback on the rig's Camera Offset object so
    /// Play without a headset shows the panel instead of the floor.
    /// </summary>
    private static void AddDesktopViewFallback()
    {
        var origin = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsInactive.Include);
        GameObject offset = origin != null && origin.CameraFloorOffsetObject != null
            ? origin.CameraFloorOffsetObject
            : GameObject.Find("Camera Offset");
        if (offset == null)
        {
            Debug.LogWarning("[ToolNBackSceneBuilder] Camera Offset not found; skipping DesktopViewFallback.");
            return;
        }

        var fallback = offset.GetComponent<ToolNBack.DesktopViewFallback>();
        if (fallback == null)
            fallback = offset.AddComponent<ToolNBack.DesktopViewFallback>();
        SetSerializedFloat(fallback, "_desktopEyeHeight", PanelAnchorPosition.y);
    }

    private static void SetSerializedFloat(Object target, string propertyPath, float value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyPath).floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignCuratedTools(ToolNBack.ToolNBackManagerBehaviour manager)
    {
        var loaded = new System.Collections.Generic.List<GameObject>();
        foreach (string name in CuratedToolPrefabs)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ToolPrefabFolder}/{name}.prefab");
            if (prefab == null)
            {
                Debug.LogWarning($"[ToolNBackSceneBuilder] Curated tool prefab not found, skipping: {name}");
                continue;
            }
            loaded.Add(prefab);
        }

        var serialized = new SerializedObject(manager);
        SerializedProperty array = serialized.FindProperty("_toolPrefabs");
        array.arraySize = loaded.Count;
        for (int i = 0; i < loaded.Count; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = loaded[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == scenePath))
            return;

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
