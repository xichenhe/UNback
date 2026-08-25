using System.Collections.Generic;
using NUnit.Framework;
using ToolNBack;

namespace ToolNBack.Tests
{
    public class ToolNBackTaskTests
    {
        // n = 2 targets: index 2 (1 == seq[0]) and index 3 (2 == seq[1]).
        private static readonly int[] Sequence = { 1, 2, 1, 2, 3 };
        private const int N = 2;
        private const float Stim = 1f;
        private const float Isi = 1f;

        private static ToolNBackTask NewTask() => new ToolNBackTask(Sequence, N, Stim, Isi);

        [Test]
        public void InitialState_ShowsFirstTool()
        {
            var task = NewTask();

            Assert.AreEqual(0, task.Index);
            Assert.AreEqual(Sequence[0], task.CurrentTool);
            Assert.IsFalse(task.IsFinished);
        }

        [Test]
        public void FirstRun_RaisesOnsetForTrialZeroOnce()
        {
            var task = NewTask();
            var onsets = new List<(int trial, int tool, bool isTarget)>();
            task.StimulusOnset += (trial, tool, isTarget) => onsets.Add((trial, tool, isTarget));

            task.Run(0.1f);
            task.Run(0.1f);

            Assert.AreEqual(1, onsets.Count);
            Assert.AreEqual((0, Sequence[0], false), onsets[0]);
        }

        [Test]
        public void StimulusHides_AfterStimulusDuration()
        {
            var task = NewTask();
            bool offsetRaised = false;
            task.StimulusOffset += (trial, tool, responded) => offsetRaised = true;

            Assert.AreEqual(Sequence[0], task.Run(0.6f), "still inside the stimulus window");
            Assert.AreEqual(-1, task.Run(0.6f), "stimulus window is over, panel must be blank");
            Assert.IsTrue(offsetRaised);
        }

        [Test]
        public void NextTrialStarts_AfterInterStimulusInterval()
        {
            var task = NewTask();
            var onsets = new List<int>();
            task.StimulusOnset += (trial, tool, isTarget) => onsets.Add(trial);

            task.Run(0.6f);  // trial 0 stimulus
            task.Run(0.6f);  // -> ISI
            task.Run(1.1f);  // -> trial 1 stimulus

            Assert.AreEqual(1, task.Index);
            Assert.AreEqual(Sequence[1], task.CurrentTool);
            CollectionAssert.AreEqual(new[] { 0, 1 }, onsets);
        }

        [Test]
        public void TrialResult_IsRaisedWhenTrialEnds()
        {
            var task = NewTask();
            var results = new List<(int trial, int tool, bool isTarget, bool responded)>();
            task.TrialResult += (trial, tool, isTarget, responded) => results.Add((trial, tool, isTarget, responded));

            task.Run(0.6f);
            task.Run(0.6f);
            task.Run(1.1f);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual((0, Sequence[0], false, false), results[0]);
        }

        [Test]
        public void RegisterResponse_SetsReactionTimeFromOnset()
        {
            var task = NewTask();

            task.Run(0.25f);
            task.Run(0.25f);
            task.RegisterResponse();

            Assert.IsTrue(task.IsInputTriggered);
            Assert.AreEqual(0.5f, task.ReactionTime, 1e-4f);
        }

        [Test]
        public void ResponseDuringIsi_StillCountsForCurrentTrial()
        {
            var task = NewTask();
            var results = new List<(int trial, int tool, bool isTarget, bool responded)>();
            task.TrialResult += (trial, tool, isTarget, responded) => results.Add((trial, tool, isTarget, responded));

            task.Run(0.6f);
            task.Run(0.6f);           // ISI of trial 0
            task.RegisterResponse();  // late press
            task.Run(1.1f);           // trial 0 scored

            Assert.IsTrue(results[0].responded);
        }

        [Test]
        public void InputFlag_ResetsOnNewTrial()
        {
            var task = NewTask();

            task.Run(0.5f);
            task.RegisterResponse();
            task.Run(0.6f);   // -> ISI
            task.Run(1.1f);   // -> trial 1

            Assert.IsFalse(task.IsInputTriggered);
        }

        [Test]
        public void FullRun_ScoresEveryTrial()
        {
            var task = NewTask();
            var results = new List<(int trial, int tool, bool isTarget, bool responded)>();
            task.TrialResult += (trial, tool, isTarget, responded) => results.Add((trial, tool, isTarget, responded));
            bool finished = false;
            task.Finished += () => finished = true;

            // Respond only on the two targets (trials 2 and 3) => everything correct.
            RunTrial(task, respond: false);
            RunTrial(task, respond: false);
            RunTrial(task, respond: true);
            RunTrial(task, respond: true);
            RunTrial(task, respond: false);

            Assert.IsTrue(finished);
            Assert.IsTrue(task.IsFinished);
            Assert.AreEqual(Sequence.Length, results.Count, "every trial must produce a result");
            Assert.AreEqual(Sequence.Length, task.NumberOfCorrectAnswers);
            Assert.AreEqual(0f, task.ErrorRate, 1e-4f);
        }

        [Test]
        public void FalseAlarmAndMiss_LowerTheScore()
        {
            var task = NewTask();

            RunTrial(task, respond: true);   // false alarm
            RunTrial(task, respond: false);  // correct rejection
            RunTrial(task, respond: false);  // miss (target)
            RunTrial(task, respond: true);   // hit (target)
            RunTrial(task, respond: false);  // correct rejection

            Assert.AreEqual(3, task.NumberOfCorrectAnswers);
            Assert.AreEqual(2f / 5f, task.ErrorRate, 1e-4f);
        }

        [Test]
        public void Finished_IsRaisedExactlyOnce()
        {
            var task = NewTask();
            int finishedCount = 0;
            task.Finished += () => finishedCount++;

            for (int i = 0; i < Sequence.Length; i++)
                RunTrial(task, respond: false);
            task.Run(1f);
            task.Run(1f);

            Assert.AreEqual(1, finishedCount);
        }

        [Test]
        public void IsTargetAt_MatchesGroundTruth()
        {
            var task = NewTask();

            Assert.IsFalse(task.IsTargetAt(0));
            Assert.IsFalse(task.IsTargetAt(1));
            Assert.IsTrue(task.IsTargetAt(2));
            Assert.IsTrue(task.IsTargetAt(3));
            Assert.IsFalse(task.IsTargetAt(4));
        }

        /// <summary>Steps the task through one full trial (stimulus + ISI) in small ticks.</summary>
        private static void RunTrial(ToolNBackTask task, bool respond)
        {
            int trial = task.Index;

            // Stimulus phase in ticks that never cross both boundaries at once.
            for (int i = 0; i < 4 && task.CurrentTool >= 0 && !task.IsFinished; i++)
                task.Run(0.3f);

            if (respond)
                task.RegisterResponse();

            // ISI phase until the trial advances (or the task finishes).
            for (int i = 0; i < 5 && task.Index == trial && !task.IsFinished; i++)
                task.Run(0.3f);
        }
    }
}
