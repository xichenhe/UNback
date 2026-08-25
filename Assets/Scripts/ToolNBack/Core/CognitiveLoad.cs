using System;

namespace ToolNBack
{
    /// <summary>
    /// Experimenter-facing cognitive load level of the tool n-back calibration task.
    /// </summary>
    public enum CognitiveLoadLevel
    {
        Low = 0,
        Optimal = 1,
        High = 2
    }

    /// <summary>
    /// Concrete parameters of a tool n-back run. Produced from a
    /// <see cref="CognitiveLoadLevel"/> preset or overridden by the experimenter.
    /// </summary>
    [Serializable]
    public struct ToolNBackSettings
    {
        /// <summary>How many trials back a tool must match to be a target (0 = respond to the designated target tool).</summary>
        public int N;
        /// <summary>Seconds the tool stimulus is visible.</summary>
        public float StimulusDuration;
        /// <summary>Seconds of blank panel between stimuli.</summary>
        public float InterStimulusDuration;
        /// <summary>Number of trials in the run.</summary>
        public int SequenceLength;
        /// <summary>Fraction of eligible trials that are targets.</summary>
        public float TargetRate;
        /// <summary>Number of distinct tool prefabs used as stimuli.</summary>
        public int ToolPoolSize;
    }

    /// <summary>
    /// Maps the three experimenter-selectable cognitive load levels to task parameters.
    /// </summary>
    public static class CognitiveLoadPresets
    {
        public static ToolNBackSettings Get(CognitiveLoadLevel level)
        {
            // All levels share identical timing, trial count and pool so that N
            // (working-memory load) is the only difference between conditions:
            // equal block duration (40 x 3.5 s = 140 s) and near-equal event counts
            // per condition for EEG epoching, with no stimulus-rate confound.
            var settings = new ToolNBackSettings
            {
                N = 2, // Optimal: classic 2-back
                StimulusDuration = 1.5f,
                InterStimulusDuration = 2.0f,
                SequenceLength = 40,
                TargetRate = 0.3f,
                ToolPoolSize = 8
            };

            switch (level)
            {
                case CognitiveLoadLevel.Low:
                    settings.N = 0; // respond whenever the designated target tool appears
                    break;
                case CognitiveLoadLevel.High:
                    settings.N = 3;
                    break;
            }

            return settings;
        }
    }
}
