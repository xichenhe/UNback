using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Opens the Tool NBack scenes and reports integration problems a compile pass
/// cannot catch: missing prefab assets, missing MonoBehaviour scripts, and
/// duplicate enabled cameras / audio listeners / XR origins (two rigs fighting).
/// Run via -executeMethod SceneErrorScan.Run; exits non-zero on problems in
/// batch mode.
/// </summary>
public static class SceneErrorScan
{
    private static int _errors;

    [MenuItem("NBack VR/Scan Tool NBack Scenes For Errors")]
    public static void Run()
    {
        _errors = 0;
        Scan("Assets/Scenes/VR-Tool-NBack.unity");
        Scan("Assets/Scenes/VR-Tool-NBack-MultiBlock.unity");

        Debug.Log(_errors == 0
            ? "[SceneErrorScan] PASSED"
            : $"[SceneErrorScan] FAILED with {_errors} problem(s)");
        if (Application.isBatchMode)
            EditorApplication.Exit(_errors == 0 ? 0 : 1);
    }

    private static void Scan(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject[] roots = scene.GetRootGameObjects();
        string label = System.IO.Path.GetFileNameWithoutExtension(scenePath);

        foreach (GameObject root in roots)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (PrefabUtility.GetPrefabInstanceStatus(t.gameObject) == PrefabInstanceStatus.MissingAsset)
                    Problem($"{label}: missing prefab asset on '{PathOf(t)}'");

                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (missing > 0)
                    Problem($"{label}: {missing} missing script(s) on '{PathOf(t)}'");
            }
        }

        Camera[] cameras = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(false))
            .Where(c => c.enabled).ToArray();
        if (cameras.Length > 1)
            Problem($"{label}: {cameras.Length} enabled cameras: " +
                    string.Join(", ", cameras.Select(c => PathOf(c.transform))));

        AudioListener[] listeners = roots.SelectMany(r => r.GetComponentsInChildren<AudioListener>(false))
            .Where(l => l.enabled).ToArray();
        if (listeners.Length > 1)
            Problem($"{label}: {listeners.Length} enabled AudioListeners: " +
                    string.Join(", ", listeners.Select(l => PathOf(l.transform))));

        var origins = roots.SelectMany(r => r.GetComponentsInChildren<Unity.XR.CoreUtils.XROrigin>(false))
            .Where(o => o.enabled).ToArray();
        if (origins.Length > 1)
            Problem($"{label}: {origins.Length} enabled XR Origins (two rigs will fight for the headset): " +
                    string.Join(", ", origins.Select(o => PathOf(o.transform))));

        Debug.Log($"[SceneErrorScan] scanned {label}: {roots.Length} roots, " +
                  $"{cameras.Length} camera(s), {origins.Length} XR origin(s)");
    }

    private static void Problem(string what)
    {
        _errors++;
        Debug.LogError($"[SceneErrorScan] PROBLEM: {what}");
    }

    private static string PathOf(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
