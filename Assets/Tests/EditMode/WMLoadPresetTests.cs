using NUnit.Framework;
using ToolNBack;

namespace ToolNBack.Tests
{
    public class WMLoadPresetTests
    {
        [Test]
        public void Low_IsZeroBack()
        {
            Assert.AreEqual(0, WMLoadPresets.Get(WMLoadLevel.Low).N);
        }

        [Test]
        public void Optimal_IsTwoBack()
        {
            Assert.AreEqual(2, WMLoadPresets.Get(WMLoadLevel.Optimal).N);
        }

        [Test]
        public void High_IsThreeBack()
        {
            Assert.AreEqual(3, WMLoadPresets.Get(WMLoadLevel.High).N);
        }

        [Test]
        public void N_IncreasesWithLevel()
        {
            int low = WMLoadPresets.Get(WMLoadLevel.Low).N;
            int optimal = WMLoadPresets.Get(WMLoadLevel.Optimal).N;
            int high = WMLoadPresets.Get(WMLoadLevel.High).N;

            Assert.Less(low, optimal);
            Assert.Less(optimal, high);
        }

        [TestCase(WMLoadLevel.Low)]
        [TestCase(WMLoadLevel.Optimal)]
        [TestCase(WMLoadLevel.High)]
        public void AllPresets_ShareIdenticalTimingAndTrialCount(WMLoadLevel level)
        {
            // EEG calibration design: N (working-memory load) must be the only
            // difference between conditions — identical block duration and stimulus
            // rate give equal spectral epochs and remove the rate confound.
            ToolNBackSettings s = WMLoadPresets.Get(level);

            Assert.AreEqual(WMLoadPresets.Get(WMLoadLevel.Optimal).SequenceLength, s.SequenceLength,
                "equal trial count for equal event epochs");
            Assert.AreEqual(1.5f, s.StimulusDuration, 1e-4f);
            Assert.AreEqual(2.0f, s.InterStimulusDuration, 1e-4f);
            Assert.AreEqual(0.3f, s.TargetRate, 1e-4f);
            Assert.AreEqual(8, s.ToolPoolSize);
        }

        [TestCase(WMLoadLevel.Low)]
        [TestCase(WMLoadLevel.Optimal)]
        [TestCase(WMLoadLevel.High)]
        public void Blocks_LastAboutFortyFiveSeconds(WMLoadLevel level)
        {
            // Four short blocks per level instead of two long ones: the same task time
            // per level, but enough blocks to validate a classifier by holding whole
            // blocks out, so slow drift between blocks cannot pass for load.
            ToolNBackSettings s = WMLoadPresets.Get(level);

            float seconds = s.SequenceLength * (s.StimulusDuration + s.InterStimulusDuration);

            Assert.That(seconds, Is.InRange(42f, 48f), $"a {level} block lasts {seconds:F1} s");
        }

        [TestCase(WMLoadLevel.Low)]
        [TestCase(WMLoadLevel.Optimal)]
        [TestCase(WMLoadLevel.High)]
        public void AllPresets_AreRunnable(WMLoadLevel level)
        {
            ToolNBackSettings s = WMLoadPresets.Get(level);

            Assert.Greater(s.StimulusDuration, 0f);
            Assert.Greater(s.InterStimulusDuration, 0f);
            Assert.Greater(s.SequenceLength, s.N, "sequence must contain at least one eligible target position");
            Assert.Greater(s.TargetRate, 0f);
            Assert.LessOrEqual(s.TargetRate, 1f);
            Assert.GreaterOrEqual(s.ToolPoolSize, 2, "need at least the designated target tool plus one distractor");
        }
    }
}
