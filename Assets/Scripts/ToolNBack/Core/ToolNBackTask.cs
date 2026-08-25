using System;

namespace ToolNBack
{
    /// <summary>
    /// Engine-independent tool n-back state machine. Each trial shows the tool
    /// <see cref="CurrentTool"/> for StimulusDuration seconds, then a blank panel
    /// (CurrentTool == -1) for InterStimulusDuration seconds. Advance it with
    /// <see cref="Run"/> once per frame; register participant responses with
    /// <see cref="RegisterResponse"/>. Mirrors the letter based
    /// Task.NBackTask, but scores every trial (including the last one).
    /// </summary>
    public class ToolNBackTask
    {
        /// <summary>Raised when a tool becomes visible: (trialIndex, toolIndex, isTarget).</summary>
        public event Action<int, int, bool> StimulusOnset;
        /// <summary>Raised when the tool is hidden again: (trialIndex, toolIndex, responded).</summary>
        public event Action<int, int, bool> StimulusOffset;
        /// <summary>Raised when a trial is scored at the end of its ISI: (trialIndex, toolIndex, isTarget, responded).</summary>
        public event Action<int, int, bool, bool> TrialResult;
        /// <summary>Raised once after the last trial is scored.</summary>
        public event Action Finished;

        public int[] Sequence { get; }
        public int N { get; }
        public float StimulusDuration { get; }
        public float InterStimulusDuration { get; }

        /// <summary>Index of the current trial.</summary>
        public int Index { get; private set; }
        /// <summary>Tool index currently visible, or -1 during the inter-stimulus interval.</summary>
        public int CurrentTool { get; private set; }
        /// <summary>True once a response was registered for the current trial.</summary>
        public bool IsInputTriggered { get; private set; }
        /// <summary>Seconds from stimulus onset to the last registered response of the current trial.</summary>
        public float ReactionTime { get; private set; }
        public int NumberOfCorrectAnswers { get; private set; }
        /// <summary>1 - correct/length, valid once <see cref="IsFinished"/>.</summary>
        public float ErrorRate { get; private set; }
        public bool IsFinished { get; private set; }

        private float _phaseRemaining;
        private float _trialElapsed;
        private bool _inStimulus;
        private bool _onsetRaised;

        public ToolNBackTask(int[] sequence, int n, float stimulusDuration, float interStimulusDuration)
        {
            Sequence = sequence;
            N = n;
            StimulusDuration = stimulusDuration;
            InterStimulusDuration = interStimulusDuration;
            CurrentTool = sequence[0];
            _inStimulus = true;
            _phaseRemaining = stimulusDuration;
        }

        /// <summary>
        /// Advances the task by deltaTime seconds and returns <see cref="CurrentTool"/>.
        /// </summary>
        public int Run(float deltaTime)
        {
            if (IsFinished)
                return CurrentTool;

            if (!_onsetRaised)
            {
                _onsetRaised = true;
                StimulusOnset?.Invoke(Index, Sequence[Index], IsTargetAt(Index));
            }

            _trialElapsed += deltaTime;
            _phaseRemaining -= deltaTime;
            if (_phaseRemaining > 0f)
                return CurrentTool;

            if (_inStimulus)
            {
                _inStimulus = false;
                CurrentTool = -1;
                _phaseRemaining = InterStimulusDuration;
                StimulusOffset?.Invoke(Index, Sequence[Index], IsInputTriggered);
            }
            else
            {
                ScoreCurrentTrialAndAdvance();
            }

            return CurrentTool;
        }

        /// <summary>Registers a participant response for the current trial.</summary>
        public void RegisterResponse()
        {
            if (IsFinished)
                return;

            IsInputTriggered = true;
            ReactionTime = _trialElapsed;
        }

        /// <summary>Ground-truth target rule for the trial at <paramref name="index"/>.</summary>
        public bool IsTargetAt(int index)
        {
            return ToolNBackSequenceGenerator.IsTargetAt(Sequence, index, N);
        }

        private void ScoreCurrentTrialAndAdvance()
        {
            bool isTarget = IsTargetAt(Index);
            if (IsInputTriggered == isTarget)
                NumberOfCorrectAnswers++;

            TrialResult?.Invoke(Index, Sequence[Index], isTarget, IsInputTriggered);

            if (Index >= Sequence.Length - 1)
            {
                IsFinished = true;
                CurrentTool = -1;
                ErrorRate = (Sequence.Length - NumberOfCorrectAnswers) / (float)Sequence.Length;
                Finished?.Invoke();
                return;
            }

            Index++;
            IsInputTriggered = false;
            ReactionTime = 0f;
            _trialElapsed = 0f;
            CurrentTool = Sequence[Index];
            _inStimulus = true;
            _phaseRemaining = StimulusDuration;
            _onsetRaised = true;
            StimulusOnset?.Invoke(Index, Sequence[Index], IsTargetAt(Index));
        }
    }
}
