using System;

namespace ToolNBack
{
    /// <summary>
    /// One single-item mental-effort rating (the Paas scale) asked right after a block.
    /// Nothing is selected until the participant moves the stick — starting on a number
    /// would anchor the ratings to it — and confirms are ignored for a short lockout after
    /// the scale appears, because the same button answered the n-back a moment earlier.
    /// </summary>
    public sealed class EffortProbe
    {
        /// <summary>Seconds after the scale appears during which a confirm is ignored.</summary>
        public const float LockoutSeconds = 0.5f;

        /// <summary>Planning figure for session-length estimates: a rating takes a few seconds.</summary>
        public const float EstimatedSecondsPerProbe = 6f;

        private readonly float _timeoutSeconds;

        /// <param name="timeoutSeconds">Seconds before an unanswered probe gives up; 0 waits for the answer.</param>
        public EffortProbe(int scaleMin, int scaleMax, float timeoutSeconds)
        {
            if (scaleMax <= scaleMin)
                throw new ArgumentException("The rating scale needs at least two points.", nameof(scaleMax));

            ScaleMin = scaleMin;
            ScaleMax = scaleMax;
            _timeoutSeconds = Math.Max(0f, timeoutSeconds);
        }

        public int ScaleMin { get; }
        public int ScaleMax { get; }

        /// <summary>The highlighted number, or null until the participant first moves.</summary>
        public int? Selection { get; private set; }

        /// <summary>The confirmed answer, or null while unanswered or after a timeout.</summary>
        public int? Rating { get; private set; }

        /// <summary>Seconds since the scale appeared.</summary>
        public float Elapsed { get; private set; }

        /// <summary>Seconds from the scale appearing to the answer; -1 without an answer.</summary>
        public float ResponseTime { get; private set; } = -1f;

        public bool TimedOut { get; private set; }

        public bool IsFinished => Rating.HasValue || TimedOut;

        public void Tick(float deltaTime)
        {
            if (IsFinished)
                return;

            Elapsed += Math.Max(0f, deltaTime);
            if (_timeoutSeconds > 0f && Elapsed >= _timeoutSeconds)
                TimedOut = true;
        }

        /// <summary>One stick push: the first lands on the middle of the scale, later ones step by one.</summary>
        public void Move(int direction)
        {
            if (IsFinished || direction == 0)
                return;

            Selection = Selection.HasValue
                ? Math.Min(ScaleMax, Math.Max(ScaleMin, Selection.Value + Math.Sign(direction)))
                : ScaleMin + (ScaleMax - ScaleMin) / 2;
        }

        /// <summary>Answers with the selected number; false with nothing selected or during the lockout.</summary>
        public bool Confirm()
        {
            if (IsFinished || !Selection.HasValue || Elapsed < LockoutSeconds)
                return false;

            Rating = Selection;
            ResponseTime = Elapsed;
            return true;
        }

        /// <summary>
        /// A number typed on the keyboard: selects it and answers at once, or only selects
        /// it during the lockout. Numbers outside the scale are ignored.
        /// </summary>
        public bool Choose(int value)
        {
            if (IsFinished || value < ScaleMin || value > ScaleMax)
                return false;

            Selection = value;
            return Confirm();
        }
    }

    /// <summary>
    /// Turns a thumbstick's horizontal axis into single steps: one per push past the
    /// press threshold, none while it is held, re-armed once it returns near the centre.
    /// </summary>
    public sealed class StickStepper
    {
        public const float PressThreshold = 0.6f;
        public const float ReleaseThreshold = 0.3f;

        private int _held;

        /// <returns>+1 or -1 on a new push, otherwise 0.</returns>
        public int Step(float x)
        {
            int direction = x >= PressThreshold ? 1 : x <= -PressThreshold ? -1 : 0;
            if (direction != 0 && direction != _held)
            {
                _held = direction; // a fast swing may skip the centre between two frames
                return direction;
            }

            if (Math.Abs(x) <= ReleaseThreshold)
                _held = 0;
            return 0;
        }
    }
}
