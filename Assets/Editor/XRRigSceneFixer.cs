using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Repairs the currently open scene after the CLAdVR player rig experiment:
/// removes any instance of the CLAdVR "XR Player [ Hands ]" prefab (its Meta/OVR
/// components do not exist in this project and its second XR Origin fights the
/// scene rig), strips missing-script components, and creates LeftHand/RightHand
/// Controller objects with ActionBasedController on the scene's own XR Origin,
/// wired to the XRI Default Input Actions from the imported XRI samples.
/// ActionBasedController is deprecated in XRI 3.x but fully functional.
/// </summary>
public static class XRRigSceneFixer
{
    /// <summary>Stable GUID of the XRI Starter Assets "XRI Default Input Actions" asset.</summary>
    private const string InputActionsGuid = "c348712bda248c246b8c49b3db54643f";

    private const string HandsRigPrefabName = "XR Player [ Hands ]";

    [MenuItem("NBack VR/Fix XR Rig In Open Scene")]
    public static void FixOpenScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();
        int changes = 0;

        // 1. Remove CLAdVR player rig instances — Meta SDK scripts are missing here
        //    and their embedded XR Origin duplicates the scene rig.
        foreach (GameObject root in roots)
        {
            string source = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
            if (!string.IsNullOrEmpty(source) && source.EndsWith(HandsRigPrefabName + ".prefab"))
            {
                Debug.Log($"[XRRigSceneFixer] removing broken rig instance '{root.name}' (source: {source})");
                Object.DestroyImmediate(root);
                changes++;
            }
        }

        // 2. Strip missing-script components (OVR leftovers, stale video player, ...).
        roots = scene.GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                if (removed > 0)
                {
                    Debug.Log($"[XRRigSceneFixer] removed {removed} missing script(s) on '{t.name}'");
                    changes += removed;
                }
            }
        }

        // 3. Ensure action-based controllers on the scene's own rig.
        var origin = roots.SelectMany(r => r.GetComponentsInChildren<Unity.XR.CoreUtils.XROrigin>(true))
            .FirstOrDefault();
        if (origin == null)
        {
            Debug.LogWarning("[XRRigSceneFixer] no XR Origin in this scene; controllers not created.");
        }
        else
        {
            Transform offset = origin.CameraFloorOffsetObject != null
                ? origin.CameraFloorOffsetObject.transform
                : origin.transform;
            changes += EnsureController(offset, "LeftHand Controller", "XRI Left");
            changes += EnsureController(offset, "RightHand Controller", "XRI Right");
        }

        if (changes > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        Debug.Log($"[XRRigSceneFixer] done: {changes} change(s) in '{scene.name}'" +
                  (changes > 0 ? " (scene saved)" : " — nothing to fix"));
    }

#pragma warning disable 618 // ActionBasedController: deprecated in XRI 3.x, still functional.
    private static int EnsureController(Transform parent, string name, string actionMap)
    {
        if (parent.Find(name) != null)
        {
            Debug.Log($"[XRRigSceneFixer] '{name}' already exists; leaving it untouched");
            return 0;
        }

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var controller = go.AddComponent<ActionBasedController>();

        controller.positionAction = Reference(actionMap, "Position");
        controller.rotationAction = Reference(actionMap, "Rotation");
        controller.trackingStateAction = Reference(actionMap, "Tracking State");
        controller.selectAction = Reference($"{actionMap} Interaction", "Select");
        controller.selectActionValue = Reference($"{actionMap} Interaction", "Select Value");
        controller.activateAction = Reference($"{actionMap} Interaction", "Activate");
        controller.activateActionValue = Reference($"{actionMap} Interaction", "Activate Value");
        controller.uiPressAction = Reference($"{actionMap} Interaction", "UI Press");
        controller.uiPressActionValue = Reference($"{actionMap} Interaction", "UI Press Value");

        Debug.Log($"[XRRigSceneFixer] created '{name}' under '{parent.name}' bound to '{actionMap}'");
        return 1;
    }
#pragma warning restore 618

    private static InputActionProperty Reference(string map, string action)
    {
        string path = AssetDatabase.GUIDToAssetPath(InputActionsGuid);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("[XRRigSceneFixer] XRI Default Input Actions sample asset not found — " +
                             "import 'Starter Assets' from the XR Interaction Toolkit package samples.");
            return default;
        }

        InputActionReference reference = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<InputActionReference>()
            .FirstOrDefault(r => r.name == $"{map}/{action}");
        if (reference == null)
        {
            Debug.LogWarning($"[XRRigSceneFixer] action '{map}/{action}' not found in the sample asset; left unassigned");
            return default;
        }

        return new InputActionProperty(reference);
    }
}
