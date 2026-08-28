using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ToolNBack;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ToolNBack.Tests
{
    /// <summary>
    /// Drives the real ToolNBackManagerBehaviour (reflection, like the smoke tests)
    /// through multi-block sessions: rest baselines, practice, counterbalanced task
    /// blocks, feedback gating and the final session summary.
    /// </summary>
    public class ToolNBackMultiBlockTests
    {
        private static readonly Type ManagerType =
            Type.GetType("ToolNBack.ToolNBackManagerBehaviour, Assembly-CSharp");
        private static readonly Type FeedbackTintType =
            Type.GetType("ToolNBack.ToolFeedbackTint, Assembly-CSharp");

        private const float Rest = 30f;

        [Test]
        public void MultiBlock_OpensWithARestBaseline_NoTaskYet()
        {
            MonoBehaviour manager = NewMultiBlockManager(cbIndex: 0, reps: 1, includePractice: false);

            Assert.IsNull(GetTask(manager), "no n-back task may run during the opening rest baseline");
            StringAssert.Contains("Rest", ReadInstructionText(manager),
                "the participant must see the rest instruction");
        }

        [Test]
        public void MultiBlock_AfterOpeningRest_FirstTaskBlockUsesTheCounterbalancedLevel()
        {
            MonoBehaviour manager = NewMultiBlockManager(cbIndex: 0, reps: 1, includePractice: false);

            TickRest(manager, Rest + 0.1f);

            ToolNBackTask task = GetTask(manager);
            Assert.NotNull(task, "the first task block must be prepared after the opening rest");
            Assert.AreEqual(0, task.N, "counterbalance order 0 starts with Low (0-back)");
        }

        [Test]
        public void MultiBlock_RunsTaskBlocks_InCounterbalancedOrder_AcrossRests()
        {
            // Order 3 is Optimal, High, Low -> N = 2, 3, 0.
            MonoBehaviour manager = NewMultiBlockManager(cbIndex: 3, reps: 1, includePractice: false);

            var seenNs = new List<int>();
            foreach (int expected in new[] { 2, 3, 0 })
            {
                TickRest(manager, Rest + 0.1f);
                ToolNBackTask task = GetTask(manager);
                Assert.NotNull(task, $"a task block must follow the rest (after N sequence {string.Join(",", seenNs)})");
                seenNs.Add(task.N);

                BeginRunning(manager);
                DriveToFinish(task);
            }

            CollectionAssert.AreEqual(new[] { 2, 3, 0 }, seenNs,
                "task blocks must follow the participant's counterbalanced level order");
        }

        [Test]
        public void MultiBlock_AfterFinalRest_ShowsSessionSummary_WithAggregateSignalDetection()
        {
            MonoBehaviour manager = NewMultiBlockManager(cbIndex: 0, reps: 1, includePractice: false);

            for (int block = 0; block < 3; block++)
            {
                TickRest(manager, Rest + 0.1f);
                BeginRunning(manager);
                DriveToFinish(GetTask(manager));
            }
            TickRest(manager, Rest + 0.1f); // closing baseline

            string summary = ReadInstructionText(manager);
            StringAssert.Contains("Session complete", summary);
            // Never responding: hits 0 of all targets. Preset target counts are exact:
            // 0-back 12, 2-back 11, 3-back 11 -> 34 targets across the three blocks.
            StringAssert.Contains("Hits: 0/34", summary);
        }

        [Test]
        public void MultiBlock_PracticeAllowsFeedback_RecordedTaskBlocksSuppressIt()
        {
            MonoBehaviour manager = NewMultiBlockManager(cbIndex: 0, reps: 1, includePractice: true,
                showFeedback: true);

            // Opening rest -> practice block (feedback must work there).
            TickRest(manager, Rest + 0.1f);
            ToolNBackTask practice = GetTask(manager);
            Assert.AreEqual(2, practice.N, "practice runs at the Optimal (2-back) level");
            BeginRunning(manager);
            practice.Run(0.05f); // first stimulus on the panel
            HandleOnInput(manager);
            Assert.NotNull(ActiveTintColor(manager),
                "practice blocks must show response feedback when enabled");

            DriveToFinish(practice);

            // Practice -> rest -> first recorded task block (feedback must stay dark).
            TickRest(manager, Rest + 0.1f);
            ToolNBackTask recorded = GetTask(manager);
            Assert.NotNull(recorded, "a recorded task block must follow the post-practice rest");
            BeginRunning(manager);
            recorded.Run(0.05f);
            HandleOnInput(manager);
            Assert.IsNull(ActiveTintColor(manager),
                "recorded blocks must never flash feedback — feedback-evoked potentials confound the WM-load contrast");
        }

        [Test]
        public void MultiBlock_UsesDistinctSequences_PerBlockOfTheSameLevel()
        {
            // Two repetitions of each level; the two Low blocks must not repeat the
            // identical stimulus sequence (per-block derived seeds).
            MonoBehaviour manager = NewMultiBlockManager(cbIndex: 0, reps: 2, includePractice: false);

            var lowSequences = new List<string>();
            for (int block = 0; block < 6; block++)
            {
                TickRest(manager, Rest + 0.1f);
                ToolNBackTask task = GetTask(manager);
                if (task.N == 0)
                    lowSequences.Add(string.Join(",", task.Sequence));
                BeginRunning(manager);
                DriveToFinish(task);
            }

            Assert.AreEqual(2, lowSequences.Count, "two repetitions must yield two Low blocks");
            Assert.AreNotEqual(lowSequences[0], lowSequences[1],
                "repeated blocks of one level need distinct sequences (distinct per-block seeds)");
        }

        // --- harness -------------------------------------------------------------

        private static MonoBehaviour NewMultiBlockManager(int cbIndex, int reps, bool includePractice,
            bool showFeedback = false)
        {
            Assert.NotNull(ManagerType, "ToolNBackManagerBehaviour must exist in Assembly-CSharp");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Tool NBack MultiBlock Test");
            var manager = (MonoBehaviour)root.AddComponent(ManagerType);

            var serialized = new SerializedObject(manager);
            serialized.FindProperty("_startDelay").floatValue = 0f;
            serialized.FindProperty("_randomSeed").intValue = 123;
            SerializedProperty multiBlock = serialized.FindProperty("_multiBlockSession");
            Assert.NotNull(multiBlock, "the manager must expose a _multiBlockSession switch");
            multiBlock.boolValue = true;
            serialized.FindProperty("_counterbalanceIndex").intValue = cbIndex;
            serialized.FindProperty("_repetitionsPerLevel").intValue = reps;
            serialized.FindProperty("_baselineRestDuration").floatValue = Rest;
            serialized.FindProperty("_interBlockRestDuration").floatValue = Rest;
            serialized.FindProperty("_includePracticeBlock").boolValue = includePractice;
            serialized.FindProperty("_showResponseFeedback").boolValue = showFeedback;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            ManagerType.GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(manager, null);
            return manager;
        }

        private static void TickRest(MonoBehaviour manager, float seconds)
        {
            ManagerType.GetMethod("TickRest", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(manager, new object[] { seconds });
        }

        private static void BeginRunning(MonoBehaviour manager)
        {
            ManagerType.GetMethod("BeginRunning", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(manager, null);
        }

        private static void HandleOnInput(MonoBehaviour manager)
        {
            ManagerType.GetMethod("HandleOnInput").Invoke(manager, null);
        }

        private static ToolNBackTask GetTask(MonoBehaviour manager)
        {
            return (ToolNBackTask)ManagerType.GetProperty("Task").GetValue(manager);
        }

        private static void DriveToFinish(ToolNBackTask task)
        {
            int guard = 0;
            while (!task.IsFinished && guard++ < 100000)
                task.Run(0.5f);
            Assert.IsTrue(task.IsFinished, "the block must finish");
        }

        /// <summary>The ActiveColor of the tint on the currently visible tool, or null.</summary>
        private static object ActiveTintColor(MonoBehaviour manager)
        {
            Transform pool = manager.transform.Find("Tool Pool");
            Assert.NotNull(pool, "Start must create the tool pool");

            PropertyInfo activeColor = FeedbackTintType.GetProperty("ActiveColor");
            foreach (Component tint in pool.GetComponentsInChildren(FeedbackTintType, true))
            {
                object color = activeColor.GetValue(tint);
                if (color != null)
                    return color;
            }
            return null;
        }

        private static string ReadInstructionText(MonoBehaviour manager)
        {
            Transform text = manager.transform.Find("Instruction Canvas/Instruction Text");
            Assert.NotNull(text, "the manager must create its instruction text");
            return text.GetComponent<UnityEngine.UI.Text>().text;
        }
    }
}
