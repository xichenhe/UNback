#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ToolNBack.PlayTests
{
    /// <summary>
    /// Plays the built VR-Tool-NBack scene and checks that the tool based n-back
    /// actually runs: the manager comes up and a tool stimulus appears on the curved
    /// panel. Uses only GameObject queries so this assembly needs no reference to
    /// Assembly-CSharp. Requires the scene built by ToolNBackSceneBuilder.
    /// </summary>
    public class ToolNBackScenePlayTests
    {
        private const string ScenePath = "Assets/Scenes/VR-Tool-NBack.unity";

        [UnityTest]
        public IEnumerator Scene_ShowsAToolStimulusOnTheCurvedPanel()
        {
            var parameters = new LoadSceneParameters(LoadSceneMode.Single);
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(ScenePath, parameters);
            yield return null;

            GameObject root = GameObject.Find("Tool NBack");
            Assert.NotNull(root, "the built scene must contain the Tool NBack object");

            // Start delay (default 5 s) + first stimulus; poll generously.
            float deadline = Time.realtimeSinceStartup + 30f;
            GameObject activeTool = null;
            while (Time.realtimeSinceStartup < deadline && activeTool == null)
            {
                Transform pool = root.transform.Find("Tool Pool");
                if (pool != null)
                {
                    foreach (Transform tool in pool)
                    {
                        if (tool.gameObject.activeInHierarchy)
                        {
                            activeTool = tool.gameObject;
                            break;
                        }
                    }
                }

                yield return null;
            }

            Assert.NotNull(activeTool, "a tool stimulus must appear within 30 s of play");

            Vector3 offset = activeTool.transform.position - root.transform.position;
            float horizontalDistance = Mathf.Sqrt(offset.x * offset.x + offset.z * offset.z);
            Assert.AreEqual(1.2f, horizontalDistance, 1e-2f,
                "the stimulus must sit on the curved panel cylinder (default radius 1.2 m)");
        }
    }
}
#endif
