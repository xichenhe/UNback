using UnityEngine;

namespace Task
{
    [RequireComponent(typeof(TaskManagerBehaviour))]
    public class NBackBehaviour : TextTaskBehaviour
    {
        #region Parameter
        [Header("Parameter")]
        [SerializeField] private string[] _characterSet = new string[] { "S", "P", "D", "X", "F", "X", "S", "X", "J", "X", "D", "T", "X", "V", "N", "C", "T", "X", "H", "F", "G", "M", "X", "F", "N", "K", "K", "X", "F", "C", "X", "X", "G", "D", "V", "K", "R", "X", "X", "H" };
        [SerializeField] private int _n = 0;
        [SerializeField] private float _stimulusDuration = 0.5f;
        [SerializeField] private float _interStimulusDuration = 2.5f;

        [Header("Sequence Generation (optional)")]
        [SerializeField] private bool _generateSequence = false;
        [SerializeField] private int _sequenceLength = 40;
        [SerializeField, Range(0f, 1f)] private float _targetRate = 0.3f;
        [SerializeField] private int _randomSeed = 0;
        #endregion

        #region Private variables
        // Letter pool for generated sequences; "X" is excluded and reserved for 0-back targets.
        private static readonly string[] _letterPool = new string[]
            { "S", "P", "D", "F", "J", "T", "V", "N", "C", "H", "G", "M", "K", "R" };
        #endregion

        private void Start()
        {
            string[] characterSet = _generateSequence
                ? GenerateSequence(_sequenceLength, _n, _targetRate, _randomSeed)
                : _characterSet;

            Task = new NBackTask(characterSet, _n, _stimulusDuration, _interStimulusDuration);
        }

        #region Private methods
        /// <summary>
        /// Generates a pseudo-random n-back sequence with an exact number of targets,
        /// Mathf.RoundToInt(targetRate * (length - n)), placed uniformly at random among
        /// indices &gt;= n. Non-target positions never form accidental n-back matches
        /// (and never show "X" when n == 0). Deterministic for a fixed non-zero seed;
        /// seed 0 uses a time-based seed.
        /// </summary>
        private static string[] GenerateSequence(int length, int n, float targetRate, int seed)
        {
            length = Mathf.Max(1, length);
            n = Mathf.Max(0, n);

            var random = new System.Random(seed == 0 ? System.Environment.TickCount : seed);

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
            var sequence = new string[length];
            for (int i = 0; i < length; i++)
            {
                if (isTarget[i])
                {
                    // 0-back targets are "X"; n-back targets repeat the letter n positions back.
                    sequence[i] = n == 0 ? "X" : sequence[i - n];
                }
                else
                {
                    // The pool contains no "X", so non-target positions can never become
                    // accidental 0-back targets; re-roll to avoid accidental n-back matches.
                    string letter;
                    do
                    {
                        letter = _letterPool[random.Next(_letterPool.Length)];
                    } while (n > 0 && i >= n && letter.Equals(sequence[i - n]));

                    sequence[i] = letter;
                }
            }

            return sequence;
        }
        #endregion
    }
}
