using System;
using System.Collections.Generic;

namespace ToolNBack
{
    /// <summary>Kind of one block within a multi-block calibration session.</summary>
    public enum SessionBlockKind
    {
        /// <summary>Rest baseline: blank panel, participant relaxes. Bracketed by rest markers.</summary>
        Rest = 0,
        /// <summary>Practice n-back block: response feedback allowed, excluded from analysis.</summary>
        Practice = 1,
        /// <summary>Recorded WM-load n-back block.</summary>
        Task = 2
    }

    /// <summary>One entry of a multi-block session plan.</summary>
    [Serializable]
    public struct SessionBlock
    {
        public SessionBlockKind Kind;
        /// <summary>WM load level of a Practice/Task block (ignored for Rest).</summary>
        public WMLoadLevel Level;
        /// <summary>Rest duration in seconds (Rest blocks only).</summary>
        public float RestDuration;
    }

    /// <summary>
    /// Builds multi-block session plans: rest baselines around counterbalanced,
    /// repeated WM-load task blocks, with stable per-block random seeds.
    /// </summary>
    public static class SessionPlanner
    {
        /// <summary>
        /// One of the six permutations of the three levels, selected modulo 6
        /// (e.g. by participant number) to counterbalance order effects across participants.
        /// </summary>
        public static WMLoadLevel[] CounterbalancedOrder(int index)
        {
            switch (((index % 6) + 6) % 6)
            {
                case 0: return new[] { WMLoadLevel.Low, WMLoadLevel.Optimal, WMLoadLevel.High };
                case 1: return new[] { WMLoadLevel.Low, WMLoadLevel.High, WMLoadLevel.Optimal };
                case 2: return new[] { WMLoadLevel.Optimal, WMLoadLevel.Low, WMLoadLevel.High };
                case 3: return new[] { WMLoadLevel.Optimal, WMLoadLevel.High, WMLoadLevel.Low };
                case 4: return new[] { WMLoadLevel.High, WMLoadLevel.Low, WMLoadLevel.Optimal };
                default: return new[] { WMLoadLevel.High, WMLoadLevel.Optimal, WMLoadLevel.Low };
            }
        }

        /// <summary>
        /// Standard session: a LONG opening rest (it feeds the EEG auto-baseline and
        /// the frozen artifact-model fit), optional practice (Optimal level), then the
        /// levels in the counterbalanced order, repeated. Every non-rest block is
        /// followed by a SHORT washout rest — between conditions the rest only needs
        /// to let pupil and load state settle, and shorter washouts keep the session
        /// length down; the final washout doubles as the closing baseline.
        /// </summary>
        public static List<SessionBlock> DefaultPlan(int counterbalanceIndex, int repetitionsPerLevel,
            float baselineRestDuration, float interBlockRestDuration, bool includePractice)
        {
            repetitionsPerLevel = Math.Max(1, repetitionsPerLevel);
            var plan = new List<SessionBlock> { RestBlock(baselineRestDuration) };

            if (includePractice)
            {
                plan.Add(new SessionBlock { Kind = SessionBlockKind.Practice, Level = WMLoadLevel.Optimal });
                plan.Add(RestBlock(interBlockRestDuration));
            }

            WMLoadLevel[] order = CounterbalancedOrder(counterbalanceIndex);
            for (int repetition = 0; repetition < repetitionsPerLevel; repetition++)
            {
                // Repetition r runs the counterbalanced order rotated by r
                // (A-B-C then B-C-A ...): average serial positions stay
                // near-balanced within the participant (the classifier is
                // per-participant, so drift must not correlate with level in one
                // session), and no level ever runs back-to-back — perfect balance
                // (mirroring) provably forces one level onto adjacent slots,
                // stacking ~5 min of continuous demand. The junction is safe at
                // any rep count: rep r starts with order[r], rep r-1 ends with
                // order[r+1] (mod length), which can never be the same entry.
                for (int i = 0; i < order.Length; i++)
                {
                    WMLoadLevel level = order[(i + repetition) % order.Length];
                    plan.Add(new SessionBlock { Kind = SessionBlockKind.Task, Level = level });
                    plan.Add(RestBlock(interBlockRestDuration));
                }
            }

            return plan;
        }

        /// <summary>
        /// Stable per-block seed so every block has its own reproducible sequence and
        /// stimulus positions. Base seed 0 stays 0 (the time-based-seed sentinel).
        /// </summary>
        public static int DeriveBlockSeed(int baseSeed, int blockIndex)
        {
            if (baseSeed == 0)
                return 0;

            int seed = unchecked(baseSeed + (blockIndex + 1) * 7919);
            return seed == 0 ? 1 : seed; // never collide with the time-based sentinel
        }

        private static SessionBlock RestBlock(float duration) =>
            new SessionBlock { Kind = SessionBlockKind.Rest, RestDuration = duration };
    }
}
