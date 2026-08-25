using System;
using System.Reflection;
using NUnit.Framework;
using ToolNBack;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ToolNBack.Tests
{
    /// <summary>
    /// Functional smoke test that drives the real ToolNBackManagerBehaviour (which lives
    /// in Assembly-CSharp, so it is reached via reflection) in a temporary edit-mode
    /// scene: real _MITmech tool prefabs are instantiated, shown on the curved panel and
    /// hidden again as the core task advances.
    /// </summary>
    public class ToolNBackManagerSmokeTests
    {
        private static readonly Type ManagerType =
            Type.GetType("ToolNBack.ToolNBackManagerBehaviour, Assembly-CSharp");

        private MonoBehaviour _manager;
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            Assert.NotNull(ManagerType, "ToolNBackManagerBehaviour must exist in Assembly-CSharp");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _root = new GameObject("Tool NBack");
            _root.transform.position = new Vector3(0f, 1.3f, 0f);
            _manager = (MonoBehaviour)_root.AddComponent(ManagerType);

            var serialized = new SerializedObject(_manager);
            serialized.FindProperty("_startDelay").floatValue = 0f;
            serialized.FindProperty("_randomSeed").intValue = 123;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Unity only calls Start in play mode; invoke it directly to drive the
            // real initialization path in this edit-mode test.
            ManagerType.GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_manager, null);
        }

        [Test]
        public void ZeroBack_TargetTool_IsSeededRandom()
        {
            var names = new System.Collections.Generic.HashSet<string>();
            foreach (int seed in new[] { 1, 2, 3, 4, 5, 6, 7, 8 })
                names.Add(ZeroBackTargetName(seed, randomizeTarget: true));

            Assert.Greater(names.Count, 1,
                "different seeds must be able to pick different 0-back target tools");
            Assert.AreEqual(
                ZeroBackTargetName(3, randomizeTarget: true),
                ZeroBackTargetName(3, randomizeTarget: true),
                "the same seed must reproduce the same target tool");
        }

        [Test]
        public void ZeroBack_TargetRandomization_CanBeDisabled()
        {
            Assert.AreEqual(
                ZeroBackTargetName(1, randomizeTarget: false),
                ZeroBackTargetName(9, randomizeTarget: false),
                "with randomization off the target must be seed-independent (first pool entry)");
        }

        [Test]
        public void ZeroBack_ShowsTargetPreviewDuringCountdown_AndHidesOnRunStart()
        {
            MonoBehaviour manager = NewManager(seed: 5, level: 0, startDelay: 5f, randomizeTarget: true);
            var toolNames = (string[])ManagerType.GetProperty("ToolNames").GetValue(manager);

            GameObject active = SingleActivePoolChild(manager.transform);
            Assert.NotNull(active, "the 0-back target must be visible during the countdown");
            Assert.AreEqual(toolNames[0], active.name, "the preview must show the designated target tool");

            ManagerType.GetMethod("BeginRunning", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(manager, null);
            Assert.IsNull(SingleActivePoolChild(manager.transform),
                "the preview must be hidden when the trials start");
        }

        [Test]
        public void NonZeroBack_HasNoTargetPreview()
        {
            MonoBehaviour manager = NewManager(seed: 5, level: 1, startDelay: 5f, randomizeTarget: true);
            Assert.IsNull(SingleActivePoolChild(manager.transform),
                "n >= 1 has no fixed target object, so nothing may be previewed");
        }

        private string ZeroBackTargetName(int seed, bool randomizeTarget)
        {
            MonoBehaviour manager = NewManager(seed, level: 0, startDelay: 0f, randomizeTarget: randomizeTarget);
            return ((string[])ManagerType.GetProperty("ToolNames").GetValue(manager))[0];
        }

        private MonoBehaviour NewManager(int seed, int level, float startDelay, bool randomizeTarget)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Tool NBack Target Test");
            var manager = (MonoBehaviour)root.AddComponent(ManagerType);

            var serialized = new SerializedObject(manager);
            serialized.FindProperty("_level").enumValueIndex = level;
            serialized.FindProperty("_startDelay").floatValue = startDelay;
            serialized.FindProperty("_randomSeed").intValue = seed;
            serialized.FindProperty("_randomizeTargetTool").boolValue = randomizeTarget;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            ManagerType.GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(manager, null);
            return manager;
        }

        private static GameObject SingleActivePoolChild(Transform root)
        {
            GameObject active = null;
            foreach (Transform tool in root.Find("Tool Pool"))
            {
                if (tool.gameObject.activeSelf)
                    active = tool.gameObject;
            }
            return active;
        }

        [Test]
        public void PanelPivotHeight_MovesAnchorAndStimuliToConfiguredHeight()
        {
            // The experimenter sets the panel height in meters in the inspector; the
            // manager must place its anchor (and therefore every stimulus) at it.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Tool NBack Height Test");
            var manager = (MonoBehaviour)root.AddComponent(ManagerType);

            var serialized = new SerializedObject(manager);
            serialized.FindProperty("_startDelay").floatValue = 0f;
            serialized.FindProperty("_randomSeed").intValue = 7;
            serialized.FindProperty("_panelPivotHeightMeters").floatValue = 1.23f;
            float heightMin = serialized.FindProperty("_panelHeightMin").floatValue;
            float heightMax = serialized.FindProperty("_panelHeightMax").floatValue;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            ManagerType.GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(manager, null);

            Assert.AreEqual(1.23f, root.transform.position.y, 1e-4f,
                "the anchor must move to the configured pivot height");

            var task = (ToolNBackTask)ManagerType.GetProperty("Task").GetValue(manager);
            task.Run(0.05f);

            GameObject active = null;
            foreach (Transform tool in root.transform.Find("Tool Pool"))
            {
                if (tool.gameObject.activeSelf)
                    active = tool.gameObject;
            }

            Assert.NotNull(active);
            Assert.That(active.transform.position.y,
                Is.InRange(1.23f + heightMin - 1e-3f, 1.23f + heightMax + 1e-3f),
                "stimuli must appear within the panel height band around the configured pivot");
        }

        [Test]
        public void DesktopViewFallback_RaisesCameraOnlyWithoutXRDevice()
        {
            var fallbackType = Type.GetType("ToolNBack.DesktopViewFallback, Assembly-CSharp");
            Assert.NotNull(fallbackType, "DesktopViewFallback must exist in Assembly-CSharp");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var offset = new GameObject("Camera Offset");
            var fallback = (MonoBehaviour)offset.AddComponent(fallbackType);

            var serialized = new SerializedObject(fallback);
            serialized.FindProperty("_desktopEyeHeight").floatValue = 1.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            MethodInfo apply = fallbackType.GetMethod("Apply");

            apply.Invoke(fallback, new object[] { true }); // headset connected: do nothing
            Assert.AreEqual(0f, offset.transform.localPosition.y, 1e-4f,
                "with an active XR device the camera offset must stay untouched");

            apply.Invoke(fallback, new object[] { false }); // desk test: raise to eye height
            Assert.AreEqual(1.5f, offset.transform.localPosition.y, 1e-4f,
                "without an XR device the camera offset must rise to the desktop eye height");
        }

        [Test]
        public void ToolPool_ContainsAtMostOneDrillBit()
        {
            // The tool board ships 9 nearly identical drill bits; using more than one as
            // n-back stimuli makes trials indistinguishable, so the pool must collapse
            // the "Drill Bit ..." family to a single representative.
            Transform pool = _root.transform.Find("Tool Pool");
            Assert.NotNull(pool, "Start must create the tool pool");

            int drillBits = 0;
            foreach (Transform tool in pool)
            {
                if (tool.name.StartsWith("Drill Bit"))
                    drillBits++;
            }

            Assert.LessOrEqual(drillBits, 1,
                "at most one drill bit may be in the stimulus pool (they are too similar to tell apart)");
        }

        [Test]
        public void Start_CreatesTaskAndToolPool()
        {
            var task = GetTask();

            Assert.NotNull(task, "Start must create the core task");
            Assert.AreEqual(2, task.N, "default level Optimal must be a 2-back");

            Transform pool = _root.transform.Find("Tool Pool");
            Assert.NotNull(pool, "Start must create the tool pool");
            Assert.GreaterOrEqual(pool.childCount, 2, "the pool must contain the tool instances");

            foreach (Transform tool in pool)
                Assert.IsFalse(tool.gameObject.activeSelf, "all tools start hidden");
        }

        [Test]
        public void StimulusOnset_ShowsExactlyOneToolOnTheCurvedPanel()
        {
            var task = GetTask();
            task.Run(0.05f); // raises the first onset -> manager shows the tool

            Transform pool = _root.transform.Find("Tool Pool");
            GameObject active = null;
            int activeCount = 0;
            foreach (Transform tool in pool)
            {
                if (tool.gameObject.activeSelf)
                {
                    active = tool.gameObject;
                    activeCount++;
                }
            }

            Assert.AreEqual(1, activeCount, "exactly one tool is visible during a stimulus");

            float radius = new SerializedObject(_manager).FindProperty("_panelRadius").floatValue;
            Vector3 offset = active.transform.position - _root.transform.position;
            float horizontalDistance = Mathf.Sqrt(offset.x * offset.x + offset.z * offset.z);
            Assert.AreEqual(radius, horizontalDistance, 1e-3f,
                "the tool must sit on the curved panel cylinder around the anchor");
        }

        [Test]
        public void StimulusOffset_HidesTheTool()
        {
            var task = GetTask();
            task.Run(0.05f);            // onset
            task.Run(task.StimulusDuration + 1f); // past the stimulus window -> offset

            Transform pool = _root.transform.Find("Tool Pool");
            foreach (Transform tool in pool)
                Assert.IsFalse(tool.gameObject.activeSelf, "no tool is visible during the ISI");
        }

        // Renderer-level glow tint. Screen-space outline assets (QuickOutline, EPO)
        // both failed here — mesh Read/Write requirements and Varjo multi-view
        // buffer mismatches respectively — so feedback tints the tool's own materials.
        private static readonly Type FeedbackTintType =
            Type.GetType("ToolNBack.ToolFeedbackTint, Assembly-CSharp");

        [Test]
        public void Feedback_OffByDefault_AddsNoTintComponents()
        {
            var task = GetTask();
            task.Run(0.05f); // first stimulus shown

            Transform pool = _root.transform.Find("Tool Pool");
            Assert.AreEqual(0, pool.GetComponentsInChildren(FeedbackTintType, true).Length,
                "recording blocks must run without feedback components by default");
        }

        [Test]
        public void CorrectPress_ShowsOutlineInCorrectColor()
        {
            (MonoBehaviour manager, ToolNBackTask task) = NewFeedbackManager();
            DriveToStimulus(task, wantTarget: true);

            ManagerType.GetMethod("HandleOnInput").Invoke(manager, null);

            Color expected = new SerializedObject(manager)
                .FindProperty("_correctFeedbackColor").colorValue;
            AssertActiveOutlines(manager, task, expected);
        }

        [Test]
        public void IncorrectPress_ShowsOutlineInIncorrectColor()
        {
            (MonoBehaviour manager, ToolNBackTask task) = NewFeedbackManager();
            DriveToStimulus(task, wantTarget: false);

            ManagerType.GetMethod("HandleOnInput").Invoke(manager, null);

            Color expected = new SerializedObject(manager)
                .FindProperty("_incorrectFeedbackColor").colorValue;
            AssertActiveOutlines(manager, task, expected);
        }

        [Test]
        public void Feedback_ClearsAfterConfiguredDuration()
        {
            (MonoBehaviour manager, ToolNBackTask task) = NewFeedbackManager();
            DriveToStimulus(task, wantTarget: false);
            ManagerType.GetMethod("HandleOnInput").Invoke(manager, null);

            ManagerType.GetMethod("TickFeedback", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(manager, new object[] { 10f });

            var activeColorProperty = FeedbackTintType.GetProperty("ActiveColor");
            foreach (Component tint in manager.transform.Find("Tool Pool")
                         .GetComponentsInChildren(FeedbackTintType, true))
                Assert.IsNull(activeColorProperty.GetValue(tint),
                    "feedback tints must clear after the duration");
        }

        private (MonoBehaviour manager, ToolNBackTask task) NewFeedbackManager()
        {
            Assert.NotNull(FeedbackTintType, "ToolFeedbackTint must exist in Assembly-CSharp");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Test Camera").AddComponent<Camera>(); // Outliner host, like the XR rig camera
            var root = new GameObject("Tool NBack Feedback Test");
            var manager = (MonoBehaviour)root.AddComponent(ManagerType);

            var serialized = new SerializedObject(manager);
            serialized.FindProperty("_startDelay").floatValue = 0f;
            serialized.FindProperty("_randomSeed").intValue = 123;
            serialized.FindProperty("_showResponseFeedback").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            ManagerType.GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(manager, null);
            ManagerType.GetMethod("BeginRunning", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(manager, null);

            var task = (ToolNBackTask)ManagerType.GetProperty("Task").GetValue(manager);
            return (manager, task);
        }

        /// <summary>Advances until a stimulus of the wanted target status is on the panel.</summary>
        private static void DriveToStimulus(ToolNBackTask task, bool wantTarget)
        {
            task.Run(0.05f); // make sure the current stimulus is actually displayed

            int guard = 0;
            while (guard++ < 100000 && !task.IsFinished)
            {
                if (task.CurrentTool >= 0 && task.IsTargetAt(task.Index) == wantTarget)
                    return;
                task.Run(0.1f);
            }
            Assert.Fail($"no {(wantTarget ? "target" : "non-target")} stimulus reached");
        }

        private static void AssertActiveOutlines(MonoBehaviour manager, ToolNBackTask task, Color expected)
        {
            GameObject visible = SingleActivePoolChild(manager.gameObject);
            Assert.NotNull(visible, "a tool must be visible when feedback is shown");

            Component[] tints = visible.GetComponentsInChildren(FeedbackTintType, true);
            Assert.AreEqual(1, tints.Length, "the visible tool must carry exactly one feedback tint");

            object activeColor = FeedbackTintType.GetProperty("ActiveColor").GetValue(tints[0]);
            Assert.NotNull(activeColor, "the tint must be active after a press");
            Assert.AreEqual(expected, (Color)activeColor, "tint color must reflect response correctness");

            // The tint must actually reach the renderers via material property blocks.
            var block = new MaterialPropertyBlock();
            Renderer tintedRenderer = visible.GetComponentInChildren<Renderer>(true);
            tintedRenderer.GetPropertyBlock(block);
            Assert.IsFalse(block.isEmpty, "the renderer must carry the feedback property block");
        }

        private static GameObject SingleActivePoolChild(GameObject root)
        {
            return SingleActivePoolChild(root.transform);
        }

        [Test]
        public void Summary_ShowsSignalDetectionBreakdown_ForPassiveRun()
        {
            // A participant who never responds must be flagged by the summary:
            // all targets become misses, all non-targets correct rejections.
            var task = GetTask();
            int guard = 0;
            while (!task.IsFinished && guard++ < 100000)
                task.Run(0.5f);

            int targets = CountTargets(task);
            string summary = ReadInstructionText(_root);

            StringAssert.Contains($"Hits: 0/{targets}", summary);
            StringAssert.Contains("False alarms: 0", summary);
            StringAssert.Contains($"Misses: {targets}", summary);
            StringAssert.Contains($"Correct rejections: {task.Sequence.Length - targets}", summary);
        }

        [Test]
        public void Summary_ShowsSignalDetectionBreakdown_ForAlwaysRespondingRun()
        {
            // Responding on every trial: all targets are hits, everything else
            // a false alarm — catches swapped counters in the summary.
            var task = GetTask();
            int guard = 0;
            while (!task.IsFinished && guard++ < 100000)
            {
                if (!task.IsInputTriggered)
                    task.RegisterResponse();
                task.Run(0.5f);
            }

            int targets = CountTargets(task);
            string summary = ReadInstructionText(_root);

            StringAssert.Contains($"Hits: {targets}/{targets}", summary);
            StringAssert.Contains($"False alarms: {task.Sequence.Length - targets}", summary);
            StringAssert.Contains("Misses: 0", summary);
            StringAssert.Contains("Correct rejections: 0", summary);
        }

        private static int CountTargets(ToolNBackTask task)
        {
            int targets = 0;
            for (int i = 0; i < task.Sequence.Length; i++)
            {
                if (task.IsTargetAt(i))
                    targets++;
            }
            return targets;
        }

        private static string ReadInstructionText(GameObject root)
        {
            Transform textTransform = root.transform.Find("Instruction Canvas/Instruction Text");
            Assert.NotNull(textTransform, "the manager must create its instruction text");
            return textTransform.GetComponent<UnityEngine.UI.Text>().text;
        }

        [Test]
        public void FullRun_FinishesAndScoresEveryTrial()
        {
            var task = GetTask();

            int guard = 0;
            while (!task.IsFinished && guard++ < 100000)
                task.Run(0.5f);

            Assert.IsTrue(task.IsFinished, "the task must finish");
            Assert.AreEqual(task.Sequence.Length,
                task.NumberOfCorrectAnswers + Mathf.RoundToInt(task.ErrorRate * task.Sequence.Length),
                "correct + incorrect must cover every trial");
        }

        private ToolNBackTask GetTask()
        {
            return (ToolNBackTask)ManagerType.GetProperty("Task").GetValue(_manager);
        }
    }
}
