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
            // Never responding: hits 0 of all targets. Preset target counts are exact,
            // round(0.3 x (13 - N)): 0-back 4, 2-back 3, 3-back 3 -> 10 across the three blocks.
            StringAssert.Contains("Hits: 0/10", summary);
        }

        [Test]
        public void RecordedBlock_IsFollowedByTheEffortRating_BeforeItsWashoutRest()
        {
            MonoBehaviour manager = NewMultiBlockManager(cbIndex: 0, reps: 1, includePractice: false,
                effortProbe: true);
            TickRest(manager, Rest + 0.1f);
            BeginRunning(manager);

            DriveToFinish(GetTask(manager));

            Assert.IsTrue(IsAskingEffort(manager), "the rating comes right after the block, while it is fresh");
            Assert.IsTrue(ProbePanelVisible(manager), "the rating scale must be on screen");
            StringAssert.DoesNotContain("Rest", ReadInstructionText(manager),
                "the washout rest starts only after the rating");
        }

        [Test]
        public void ConfirmedRating_IsRecorded_AndTheWashoutRestFollows()
        {
            MonoBehaviour manager = NewMultiBlockManager(cbIndex: 0, reps: 1, includePractice: false,
                effortProbe: true);
            TickRest(manager, Rest + 0.1f);
            BeginRunning(manager);
            DriveToFinish(GetTask(manager));

            Navigate(manager, 1); // first push selects the middle, 5
            Navigate(manager, 1); // 6
            TickProbe(manager, 1f); // past the lockout
            HandleOnInput(manager); // the response button confirms

            Assert.AreEqual(6, LastEffortRating(manager));
            Assert.IsFalse(IsAskingEffort(manager));
            Assert.IsFalse(ProbePanelVisible(manager), "the scale must leave the view for the rest");
            StringAssert.Contains("Rest", ReadInstructionText(manager));
        }

        [Test]
        public void UnansweredRating_TimesOut_WithoutARating_AndTheSessionGoesOn()
        {
            MonoBehaviour manager = NewMultiBlockManager(cbIndex: 0, reps: 1, includePractice: false,
                effortProbe: true, probeTimeout: 5f);
            TickRest(manager, Rest + 0.1f);
            BeginRunning(manager);
            DriveToFinish(GetTask(manager));

            TickProbe(manager, 5.1f);

            Assert.IsNull(LastEffortRating(manager), "a skipped rating must be logged as missing, not as a number");
            Assert.IsFalse(IsAskingEffort(manager));
            StringAssert.Contains("Rest", ReadInstructionText(manager));
        }

        [Test]
        public void PressWithNothingSelected_DoesNotAnswerTheRating()
        {
            MonoBehaviour manager = NewMultiBlockManager(cbIndex: 0, reps: 1, includePractice: false,
                effortProbe: true);
            TickRest(manager, Rest + 0.1f);
            BeginRunning(manager);
            DriveToFinish(GetTask(manager));

            TickProbe(manager, 1f);
            HandleOnInput(manager);

            Assert.IsTrue(IsAskingEffort(manager), "the participant must pick a number before confirming");
        }

        [Test]
        public void TypedDigit_AnswersTheRating()
        {
            MonoBehaviour manager = NewMultiBlockManager(cbIndex: 0, reps: 1, includePractice: false,
                effortProbe: true);
            TickRest(manager, Rest + 0.1f);
            BeginRunning(manager);
            DriveToFinish(GetTask(manager));
            TickProbe(manager, 1f);

            MethodInfo choose = ManagerType.GetMethod("HandleProbeChoice");
            Assert.NotNull(choose, "the manager must expose HandleProbeChoice(int) for the digit keys");
            choose.Invoke(manager, new object[] { 8 });

            Assert.AreEqual(8, LastEffortRating(manager));
            StringAssert.Contains("Rest", ReadInstructionText(manager));
        }

        [Test]
        public void EndingPractice_AsksTheRating_WithTheButtonDark()
        {
            MonoBehaviour manager = NewMultiBlockManager(cbIndex: 0, reps: 1, includePractice: true,
                effortProbe: true);
            TickRest(manager, Rest + 0.1f);
            BeginRunning(manager);
            GetTask(manager).Run(0.05f);

            EndPractice(manager);

            Assert.IsTrue(IsAskingEffort(manager), "practice also teaches the rating scale");
            Assert.IsFalse(IsPracticing(manager), "End Practice must go dark while the rating is up");
            Assert.IsFalse(AnyToolVisible(manager), "the practice tool must disappear");
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

            EndPractice(manager);

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
        public void Practice_StartsAFreshRound_InsteadOfEndingOnItsOwn()
        {
            MonoBehaviour manager = NewMultiBlockManager(cbIndex: 0, reps: 1, includePractice: true);
            TickRest(manager, Rest + 0.1f);
            ToolNBackTask firstRound = GetTask(manager);
            BeginRunning(manager);

            DriveToFinish(firstRound);

            ToolNBackTask nextRound = GetTask(manager);
            Assert.AreNotSame(firstRound, nextRound, "a finished practice round must be followed by another round");
            Assert.AreEqual(2, nextRound.N, "the new round is practice again (Optimal, 2-back)");
            Assert.AreNotEqual(string.Join(",", firstRound.Sequence), string.Join(",", nextRound.Sequence),
                "each round needs a fresh sequence so it cannot be memorized");
            Assert.IsTrue(IsPracticing(manager), "practice only ends when the experimenter ends it");
            StringAssert.DoesNotContain("Rest", ReadInstructionText(manager));
        }

        [Test]
        public void EndPractice_HidesTheTool_AndTheSessionMovesOnToTheRecordedBlocks()
        {
            MonoBehaviour manager = NewMultiBlockManager(cbIndex: 0, reps: 1, includePractice: true);
            TickRest(manager, Rest + 0.1f);
            ToolNBackTask practice = GetTask(manager);
            BeginRunning(manager);
            practice.Run(0.05f); // a practice tool is on the panel
            Assert.IsTrue(IsPracticing(manager));

            EndPractice(manager);

            Assert.IsFalse(IsPracticing(manager));
            Assert.IsFalse(AnyToolVisible(manager), "the practice tool must disappear");
            StringAssert.Contains("Rest", ReadInstructionText(manager), "practice is followed by its washout rest");

            TickRest(manager, Rest + 0.1f);
            ToolNBackTask recorded = GetTask(manager);
            Assert.AreNotSame(practice, recorded, "a recorded block must follow the post-practice rest");
            Assert.AreEqual(0, recorded.N, "counterbalance order 0 starts the recorded blocks with Low (0-back)");
        }

        [Test]
        public void EndPractice_IsIgnoredDuringARecordedBlock()
        {
            MonoBehaviour manager = NewMultiBlockManager(cbIndex: 0, reps: 1, includePractice: false);
            TickRest(manager, Rest + 0.1f);
            ToolNBackTask recorded = GetTask(manager);
            BeginRunning(manager);
            recorded.Run(0.05f);

            EndPractice(manager);

            Assert.AreSame(recorded, GetTask(manager), "the button must never skip a recorded block");
            Assert.IsFalse(recorded.IsFinished);
            Assert.IsTrue(AnyToolVisible(manager), "the recorded block keeps running");
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
            bool showFeedback = false, bool effortProbe = false, float probeTimeout = 20f)
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
            // The block-flow tests above run without the rating; the rating tests opt in.
            SerializedProperty probe = serialized.FindProperty("_effortProbeAfterBlocks");
            if (probe != null)
            {
                probe.boolValue = effortProbe;
                serialized.FindProperty("_effortProbeTimeout").floatValue = probeTimeout;
            }
            else if (effortProbe)
            {
                Assert.Fail("the manager must expose an _effortProbeAfterBlocks switch");
            }
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

        private static void EndPractice(MonoBehaviour manager)
        {
            MethodInfo endPractice = ManagerType.GetMethod("EndPractice");
            Assert.NotNull(endPractice, "the manager must expose EndPractice() for the experimenter's inspector button");
            endPractice.Invoke(manager, null);
        }

        private static bool IsPracticing(MonoBehaviour manager)
        {
            PropertyInfo isPracticing = ManagerType.GetProperty("IsPracticing");
            Assert.NotNull(isPracticing, "the manager must expose IsPracticing for the inspector button");
            return (bool)isPracticing.GetValue(manager);
        }

        private static bool IsAskingEffort(MonoBehaviour manager)
        {
            PropertyInfo asking = ManagerType.GetProperty("IsAskingEffort");
            Assert.NotNull(asking, "the manager must expose IsAskingEffort");
            return (bool)asking.GetValue(manager);
        }

        private static int? LastEffortRating(MonoBehaviour manager)
        {
            PropertyInfo rating = ManagerType.GetProperty("LastEffortRating");
            Assert.NotNull(rating, "the manager must expose LastEffortRating");
            return (int?)rating.GetValue(manager);
        }

        private static void Navigate(MonoBehaviour manager, int direction)
        {
            MethodInfo navigate = ManagerType.GetMethod("HandleProbeNavigate");
            Assert.NotNull(navigate, "the manager must expose HandleProbeNavigate(int) for the thumbstick");
            navigate.Invoke(manager, new object[] { direction });
        }

        private static void TickProbe(MonoBehaviour manager, float seconds)
        {
            MethodInfo tick = ManagerType.GetMethod("TickProbe", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(tick, "the manager must advance the rating in TickProbe(float)");
            tick.Invoke(manager, new object[] { seconds });
        }

        private static bool ProbePanelVisible(MonoBehaviour manager)
        {
            Transform panel = manager.transform.Find("Effort Probe Canvas");
            Assert.NotNull(panel, "the manager must create the effort rating canvas");
            return panel.gameObject.activeSelf;
        }

        private static bool AnyToolVisible(MonoBehaviour manager)
        {
            Transform pool = manager.transform.Find("Tool Pool");
            Assert.NotNull(pool, "Start must create the tool pool");
            foreach (Transform tool in pool)
            {
                if (tool.gameObject.activeSelf)
                    return true;
            }
            return false;
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
