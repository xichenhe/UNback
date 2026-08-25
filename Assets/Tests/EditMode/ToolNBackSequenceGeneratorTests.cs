using NUnit.Framework;
using ToolNBack;
using UnityEngine;

namespace ToolNBack.Tests
{
    public class ToolNBackSequenceGeneratorTests
    {
        private const int PoolSize = 8;

        [Test]
        public void Generate_IsDeterministicForSameSeed()
        {
            int[] a = ToolNBackSequenceGenerator.Generate(40, 2, 0.3f, PoolSize, seed: 123);
            int[] b = ToolNBackSequenceGenerator.Generate(40, 2, 0.3f, PoolSize, seed: 123);
            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void Generate_DiffersAcrossSeeds()
        {
            int[] a = ToolNBackSequenceGenerator.Generate(40, 2, 0.3f, PoolSize, seed: 1);
            int[] b = ToolNBackSequenceGenerator.Generate(40, 2, 0.3f, PoolSize, seed: 2);
            CollectionAssert.AreNotEqual(a, b);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Generate_LengthAndRangeAreValid(int n)
        {
            int[] seq = ToolNBackSequenceGenerator.Generate(40, n, 0.3f, PoolSize, seed: 7);

            Assert.AreEqual(40, seq.Length);
            foreach (int tool in seq)
            {
                Assert.GreaterOrEqual(tool, 0);
                Assert.Less(tool, PoolSize);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Generate_HasExactTargetCount(int n)
        {
            const int length = 40;
            const float rate = 0.3f;
            int expected = Mathf.RoundToInt(rate * (length - n));

            int[] seq = ToolNBackSequenceGenerator.Generate(length, n, rate, PoolSize, seed: 42);

            int actual = 0;
            for (int i = 0; i < seq.Length; i++)
            {
                if (ToolNBackSequenceGenerator.IsTargetAt(seq, i, n))
                    actual++;
            }

            Assert.AreEqual(expected, actual,
                "accidental n-back matches or missing targets change the target count");
        }

        [Test]
        public void Generate_ZeroBack_TargetsAreTheDesignatedTool()
        {
            int[] seq = ToolNBackSequenceGenerator.Generate(30, 0, 0.3f, PoolSize, seed: 5);

            for (int i = 0; i < seq.Length; i++)
            {
                bool isTarget = ToolNBackSequenceGenerator.IsTargetAt(seq, i, 0);
                Assert.AreEqual(seq[i] == 0, isTarget,
                    "0-back targets must show tool 0 and non-targets must not");
            }
        }

        [Test]
        public void Generate_TwoBack_TargetsMatchTwoTrialsEarlier()
        {
            int[] seq = ToolNBackSequenceGenerator.Generate(40, 2, 0.3f, PoolSize, seed: 9);

            for (int i = 0; i < seq.Length; i++)
            {
                bool expected = i >= 2 && seq[i] == seq[i - 2];
                Assert.AreEqual(expected, ToolNBackSequenceGenerator.IsTargetAt(seq, i, 2));
            }
        }

        [Test]
        public void IsTargetAt_NoTargetsPossibleBeforeIndexN()
        {
            int[] seq = ToolNBackSequenceGenerator.Generate(40, 3, 0.3f, PoolSize, seed: 11);

            for (int i = 0; i < 3; i++)
                Assert.IsFalse(ToolNBackSequenceGenerator.IsTargetAt(seq, i, 3));
        }

        [Test]
        public void GenerateSlots_AreInUnitRange()
        {
            Vector2[] slots = ToolNBackSequenceGenerator.GenerateSlots(50, 0.25f, seed: 3);

            Assert.AreEqual(50, slots.Length);
            foreach (Vector2 slot in slots)
            {
                Assert.That(slot.x, Is.InRange(0f, 1f));
                Assert.That(slot.y, Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void GenerateSlots_ConsecutiveSlotsAreSeparated()
        {
            const float minSeparation = 0.25f;
            Vector2[] slots = ToolNBackSequenceGenerator.GenerateSlots(50, minSeparation, seed: 3);

            for (int i = 1; i < slots.Length; i++)
            {
                Assert.GreaterOrEqual(Vector2.Distance(slots[i - 1], slots[i]), minSeparation,
                    $"slots {i - 1} and {i} are too close; the stimulus would not visibly move");
            }
        }

        [Test]
        public void GenerateSlots_IsDeterministicForSameSeed()
        {
            Vector2[] a = ToolNBackSequenceGenerator.GenerateSlots(20, 0.25f, seed: 8);
            Vector2[] b = ToolNBackSequenceGenerator.GenerateSlots(20, 0.25f, seed: 8);
            CollectionAssert.AreEqual(a, b);
        }
    }
}
