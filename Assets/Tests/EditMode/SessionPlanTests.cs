using System.Collections.Generic;
using NUnit.Framework;
using ToolNBack;

namespace ToolNBack.Tests
{
    public class SessionPlanTests
    {
        [Test]
        public void CounterbalancedOrders_AreSixDistinctPermutations_EachContainingEveryLevelOnce()
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < 6; i++)
            {
                WMLoadLevel[] order = SessionPlanner.CounterbalancedOrder(i);

                Assert.AreEqual(3, order.Length, $"order {i} must contain all three levels");
                Assert.Contains(WMLoadLevel.Low, order, $"order {i} misses Low");
                Assert.Contains(WMLoadLevel.Optimal, order, $"order {i} misses Optimal");
                Assert.Contains(WMLoadLevel.High, order, $"order {i} misses High");

                seen.Add(string.Join(",", order));
            }

            Assert.AreEqual(6, seen.Count, "the six orders must all differ (full permutation set)");
        }

        [Test]
        public void CounterbalancedOrder_WrapsModuloSix_IncludingNegativeIndices()
        {
            CollectionAssert.AreEqual(
                SessionPlanner.CounterbalancedOrder(0), SessionPlanner.CounterbalancedOrder(6),
                "index wraps at 6 so any participant number is usable");
            CollectionAssert.AreEqual(
                SessionPlanner.CounterbalancedOrder(5), SessionPlanner.CounterbalancedOrder(-1),
                "negative indices must wrap instead of throwing");
        }

        [Test]
        public void DefaultPlan_StartsWithARestBaseline_AndFollowsEveryNonRestBlockWithRest()
        {
            List<SessionBlock> plan = SessionPlanner.DefaultPlan(
                counterbalanceIndex: 0, repetitionsPerLevel: 2, baselineRestDuration: 90f, interBlockRestDuration: 30f, includePractice: true);

            Assert.AreEqual(SessionBlockKind.Rest, plan[0].Kind, "sessions open with a rest baseline");
            for (int i = 0; i < plan.Count; i++)
            {
                if (plan[i].Kind == SessionBlockKind.Rest)
                    continue;
                Assert.Less(i + 1, plan.Count, "the session must not end on a task/practice block");
                Assert.AreEqual(SessionBlockKind.Rest, plan[i + 1].Kind,
                    $"block {i} ({plan[i].Kind}) must be followed by a rest baseline");
            }
        }

        [Test]
        public void DefaultPlan_ContainsEachLevelRepetitionsTimes_AsTaskBlocks()
        {
            List<SessionBlock> plan = SessionPlanner.DefaultPlan(0, repetitionsPerLevel: 2, 90f, 30f, includePractice: false);

            var counts = new Dictionary<WMLoadLevel, int>
            {
                { WMLoadLevel.Low, 0 }, { WMLoadLevel.Optimal, 0 }, { WMLoadLevel.High, 0 }
            };
            foreach (SessionBlock block in plan)
            {
                if (block.Kind == SessionBlockKind.Task)
                    counts[block.Level]++;
            }

            Assert.AreEqual(2, counts[WMLoadLevel.Low]);
            Assert.AreEqual(2, counts[WMLoadLevel.Optimal]);
            Assert.AreEqual(2, counts[WMLoadLevel.High]);
        }

        [Test]
        public void DefaultPlan_PracticeBlock_FollowsOpeningRest_AtOptimalLevel_OnlyWhenEnabled()
        {
            List<SessionBlock> withPractice = SessionPlanner.DefaultPlan(0, 1, 90f, 30f, includePractice: true);
            Assert.AreEqual(SessionBlockKind.Practice, withPractice[1].Kind,
                "practice comes right after the opening rest");
            Assert.AreEqual(WMLoadLevel.Optimal, withPractice[1].Level,
                "practice uses the Optimal (2-back) level participants must master");

            List<SessionBlock> withoutPractice = SessionPlanner.DefaultPlan(0, 1, 90f, 30f, includePractice: false);
            foreach (SessionBlock block in withoutPractice)
                Assert.AreNotEqual(SessionBlockKind.Practice, block.Kind, "practice must be omittable");
        }

        [Test]
        public void DefaultPlan_EachRepetition_RotatesTheOrderByOne()
        {
            // Rotation (A-B-C then B-C-A) keeps average serial positions
            // near-balanced WITHIN the participant without ever running the same
            // level back-to-back — perfect balance (mirroring) provably forces one
            // level onto adjacent slots, which stacks ~5 min of continuous demand.
            WMLoadLevel[] order = SessionPlanner.CounterbalancedOrder(3);
            List<SessionBlock> plan = SessionPlanner.DefaultPlan(3, repetitionsPerLevel: 2, 90f, 30f, includePractice: false);

            var taskLevels = new List<WMLoadLevel>();
            foreach (SessionBlock block in plan)
            {
                if (block.Kind == SessionBlockKind.Task)
                    taskLevels.Add(block.Level);
            }

            CollectionAssert.AreEqual(
                new[] { order[0], order[1], order[2], order[1], order[2], order[0] },
                taskLevels,
                "repetition r runs the counterbalanced order rotated by r");
        }

        [Test]
        public void DefaultPlan_NeverRunsTheSameLevel_InConsecutiveTaskBlocks()
        {
            for (int cb = 0; cb < 6; cb++)
            {
                for (int reps = 1; reps <= 4; reps++)
                {
                    List<SessionBlock> plan = SessionPlanner.DefaultPlan(cb, reps, 90f, 30f, includePractice: false);

                    WMLoadLevel? previous = null;
                    foreach (SessionBlock block in plan)
                    {
                        if (block.Kind != SessionBlockKind.Task)
                            continue;
                        if (previous.HasValue)
                            Assert.AreNotEqual(previous.Value, block.Level,
                                $"cb {cb}, reps {reps}: consecutive task blocks share a level — "
                                + "sustained same-level demand contaminates that level's calibration data");
                        previous = block.Level;
                    }
                }
            }
        }

        [Test]
        public void DefaultPlan_OpeningRestIsBaselineLength_LaterRestsAreShortWashouts()
        {
            // The opening rest feeds the EEG auto-baseline and the frozen artifact-
            // model fit, so it is long; every later rest is only a washout between
            // conditions and stays short.
            List<SessionBlock> plan = SessionPlanner.DefaultPlan(
                0, 2, baselineRestDuration: 90f, interBlockRestDuration: 30f, includePractice: true);

            Assert.AreEqual(SessionBlockKind.Rest, plan[0].Kind);
            Assert.AreEqual(90f, plan[0].RestDuration, 1e-4f, "opening rest = baseline length");

            for (int i = 1; i < plan.Count; i++)
            {
                if (plan[i].Kind == SessionBlockKind.Rest)
                    Assert.AreEqual(30f, plan[i].RestDuration, 1e-4f,
                        $"rest at plan index {i} is a washout and must use the short duration");
            }
        }

        [Test]
        public void BlockSeed_ZeroBaseSeed_StaysZero_SoTimeSeedingStillApplies()
        {
            Assert.AreEqual(0, SessionPlanner.DeriveBlockSeed(0, 0));
            Assert.AreEqual(0, SessionPlanner.DeriveBlockSeed(0, 7));
        }

        [Test]
        public void BlockSeed_IsDeterministic_DistinctPerBlock_AndNeverTheBaseSeed()
        {
            var seen = new HashSet<int>();
            for (int block = 0; block < 20; block++)
            {
                int seed = SessionPlanner.DeriveBlockSeed(123, block);
                Assert.AreEqual(seed, SessionPlanner.DeriveBlockSeed(123, block), "must be deterministic");
                Assert.AreNotEqual(0, seed, "a fixed base seed must never yield the time-based sentinel 0");
                Assert.AreNotEqual(123, seed, "block seeds must differ from the base (sequence) seed");
                seen.Add(seed);
            }

            Assert.AreEqual(20, seen.Count, "every block needs its own seed (distinct sequences/positions)");
        }
    }
}
