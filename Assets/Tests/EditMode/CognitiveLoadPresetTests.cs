using NUnit.Framework;
using ToolNBack;

namespace ToolNBack.Tests
{
    public class CognitiveLoadPresetTests
    {
        [Test]
        public void Low_IsZeroBack()
        {
            Assert.AreEqual(0, CognitiveLoadPresets.Get(CognitiveLoadLevel.Low).N);
        }

        [Test]
        public void Optimal_IsTwoBack()
        {
            Assert.AreEqual(2, CognitiveLoadPresets.Get(CognitiveLoadLevel.Optimal).N);
        }

        [Test]
        public void High_IsThreeBack()
        {
            Assert.AreEqual(3, CognitiveLoadPresets.Get(CognitiveLoadLevel.High).N);
        }

        [Test]
        public void N_IncreasesWithLevel()
        {
            int low = CognitiveLoadPresets.Get(CognitiveLoadLevel.Low).N;
            int optimal = CognitiveLoadPresets.Get(CognitiveLoadLevel.Optimal).N;
            int high = CognitiveLoadPresets.Get(CognitiveLoadLevel.High).N;

            Assert.Less(low, optimal);
            Assert.Less(optimal, high);
        }

        [TestCase(CognitiveLoadLevel.Low)]
        [TestCase(CognitiveLoadLevel.Optimal)]
        [TestCase(CognitiveLoadLevel.High)]
        public void AllPresets_ShareIdenticalTimingAndTrialCount(CognitiveLoadLevel level)
        {
            // EEG calibration design: N (working-memory load) must be the only
            // difference between conditions — identical block duration and stimulus
            // rate give equal spectral epochs and remove the rate confound.
            ToolNBackSettings s = CognitiveLoadPresets.Get(level);

            Assert.AreEqual(40, s.SequenceLength, "equal trial count for equal event epochs");
            Assert.AreEqual(1.5f, s.StimulusDuration, 1e-4f);
            Assert.AreEqual(2.0f, s.InterStimulusDuration, 1e-4f);
            Assert.AreEqual(0.3f, s.TargetRate, 1e-4f);
            Assert.AreEqual(8, s.ToolPoolSize);
        }

        [TestCase(CognitiveLoadLevel.Low)]
        [TestCase(CognitiveLoadLevel.Optimal)]
        [TestCase(CognitiveLoadLevel.High)]
        public void AllPresets_AreRunnable(CognitiveLoadLevel level)
        {
            ToolNBackSettings s = CognitiveLoadPresets.Get(level);

            Assert.Greater(s.StimulusDuration, 0f);
            Assert.Greater(s.InterStimulusDuration, 0f);
            Assert.Greater(s.SequenceLength, s.N, "sequence must contain at least one eligible target position");
            Assert.Greater(s.TargetRate, 0f);
            Assert.LessOrEqual(s.TargetRate, 1f);
            Assert.GreaterOrEqual(s.ToolPoolSize, 2, "need at least the designated target tool plus one distractor");
        }
    }
}
