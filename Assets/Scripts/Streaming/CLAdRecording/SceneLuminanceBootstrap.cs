using UnityEngine;
using UnityEngine.Scripting;

// Nothing references this assembly, so tell the managed-code stripper to keep it.
[assembly: AlwaysLinkAssembly]

namespace CLAd.Recording
{
    /// <summary>
    /// Starts CLAdSceneLuminanceLSL in any app that contains this folder, with no scene
    /// changes: after the first scene loads, one "CLAd Scene Luminance" object holding the
    /// streamer is created under DontDestroyOnLoad and lives until the app quits.
    /// </summary>
    public static class SceneLuminanceBootstrap
    {
        public const string ObjectName = "CLAd Scene Luminance";

        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateStreamer()
        {
            if (Object.FindAnyObjectByType<SceneLuminanceStreamer>() != null)
                return;

            var host = new GameObject(ObjectName);
            Object.DontDestroyOnLoad(host);
            host.AddComponent<SceneLuminanceStreamer>();
        }
    }
}
