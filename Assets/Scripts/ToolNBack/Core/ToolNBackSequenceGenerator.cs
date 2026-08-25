using System;
using UnityEngine;

namespace ToolNBack
{
    /// <summary>
    /// Generates pseudo-random tool n-back sequences and matching panel slots.
    /// Tool index 0 is reserved as the designated 0-back target tool (the tool
    /// equivalent of the letter "X" in the text task); random fill only uses
    /// indices 1..poolSize-1 so accidental 0-back targets cannot occur.
    /// </summary>
    public static class ToolNBackSequenceGenerator
    {
        /// <summary>
        /// Generates a sequence of tool indices of the given length with an exact
        /// number of targets, Mathf.RoundToInt(targetRate * (length - n)), placed
        /// uniformly at random among indices &gt;= n. Non-target positions never
        /// form accidental n-back matches. Deterministic for a fixed seed.
        /// </summary>
        public static int[] Generate(int length, int n, float targetRate, int poolSize, int seed)
        {
            length = Mathf.Max(1, length);
            n = Mathf.Max(0, n);
            poolSize = Mathf.Max(2, poolSize);

            var random = new System.Random(seed);

            // Choose the target positions uniformly at random among indices >= n.
            int eligibleCount = Mathf.Max(0, length - n);
            int targetCount = Mathf.Clamp(Mathf.RoundToInt(targetRate * (length - n)), 0, eligibleCount);

            var candidates = new int[eligibleCount];
            for (int i = 0; i < eligibleCount; i++)
                candidates[i] = n + i;

            for (int i = 0; i < targetCount; i++) // partial Fisher-Yates shuffle
            {
                int j = random.Next(i, eligibleCount);
                int swap = candidates[i];
                candidates[i] = candidates[j];
                candidates[j] = swap;
            }

            var isTarget = new bool[length];
            for (int i = 0; i < targetCount; i++)
                isTarget[candidates[i]] = true;

            // Fill the sequence left to right.
            var sequence = new int[length];
            for (int i = 0; i < length; i++)
            {
                if (isTarget[i])
                {
                    // 0-back targets show the designated target tool; n-back targets
                    // repeat the tool n trials back.
                    sequence[i] = n == 0 ? 0 : sequence[i - n];
                }
                else
                {
                    // Random fill never uses tool 0, so non-target positions can never
                    // become accidental 0-back targets; re-roll to avoid accidental
                    // n-back matches.
                    int tool;
                    do
                    {
                        tool = 1 + random.Next(poolSize - 1);
                    } while (n > 0 && i >= n && tool == sequence[i - n]);

                    sequence[i] = tool;
                }
            }

            return sequence;
        }

        /// <summary>
        /// Ground-truth target rule: for n = 0 the designated target tool (index 0)
        /// is a target; for n &gt;= 1 a trial is a target when its tool matches the
        /// tool n trials earlier.
        /// </summary>
        public static bool IsTargetAt(int[] sequence, int index, int n)
        {
            if (n == 0)
                return sequence[index] == 0;

            if (index < n)
                return false;

            return sequence[index] == sequence[index - n];
        }

        /// <summary>
        /// Generates one normalized (u, v) panel slot per trial, each in [0, 1] x [0, 1],
        /// with consecutive slots at least minSeparation apart (Euclidean, in uv space)
        /// so successive stimuli visibly change position. Deterministic for a fixed seed.
        /// </summary>
        public static Vector2[] GenerateSlots(int length, float minSeparation, int seed)
        {
            length = Mathf.Max(1, length);
            // Above ~0.7 rejection sampling in the unit square becomes unreliable.
            minSeparation = Mathf.Clamp(minSeparation, 0f, 0.7f);

            var random = new System.Random(seed);
            var slots = new Vector2[length];

            slots[0] = NextPoint(random);
            for (int i = 1; i < length; i++)
            {
                Vector2 candidate;
                int guard = 0;
                do
                {
                    candidate = NextPoint(random);
                    guard++;
                } while (Vector2.Distance(candidate, slots[i - 1]) < minSeparation && guard < 1000);

                slots[i] = candidate;
            }

            return slots;
        }

        private static Vector2 NextPoint(System.Random random)
        {
            return new Vector2((float)random.NextDouble(), (float)random.NextDouble());
        }
    }
}
