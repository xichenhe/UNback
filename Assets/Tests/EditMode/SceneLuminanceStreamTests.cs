using CLAd.Recording;
using NUnit.Framework;
using UnityEngine;

namespace ToolNBack.Tests
{
    /// <summary>
    /// The CLAdSceneLuminanceLSL layout is an analysis contract shared by UNback,
    /// CLAdVR and the PhysioLabXR CLAd preset (design 4.2): 13 float channels, gaze
    /// channels set to -1 when there is no gaze point on the view.
    /// </summary>
    public class SceneLuminanceStreamTests
    {
        private static readonly Vector3 Position = new Vector3(1f, 2f, 3f);
        private static readonly Quaternion Rotation = new Quaternion(0.1f, 0.2f, 0.3f, 0.9f);

        [Test]
        public void Contract_MatchesTheDesign()
        {
            Assert.AreEqual("CLAdSceneLuminanceLSL", SceneLuminanceStream.Name);
            Assert.AreEqual("Luminance", SceneLuminanceStream.Type);
            Assert.AreEqual(30.0, SceneLuminanceStream.NominalRateHz);
            Assert.AreEqual(13, SceneLuminanceStream.ChannelCount);
            CollectionAssert.AreEqual(new[]
            {
                "unity_time", "view_luminance", "gaze_luminance", "gaze_valid", "gaze_u", "gaze_v",
                "view_pos_x", "view_pos_y", "view_pos_z", "view_rot_x", "view_rot_y", "view_rot_z", "view_rot_w",
            }, SceneLuminanceStream.ChannelNames);
        }

        [Test]
        public void Fill_WithGazeOnView_WritesEveryChannel()
        {
            var sample = new float[SceneLuminanceStream.ChannelCount];

            SceneLuminanceStream.Fill(sample, 12.5, 0.4f, true, new Vector2(0.25f, 0.75f), 0.6f, Position, Rotation);

            CollectionAssert.AreEqual(
                new[] { 12.5f, 0.4f, 0.6f, 1f, 0.25f, 0.75f, 1f, 2f, 3f, 0.1f, 0.2f, 0.3f, 0.9f }, sample);
        }

        [Test]
        public void Fill_WithInvalidGaze_MarksGazeChannelsMissing()
        {
            var sample = new float[SceneLuminanceStream.ChannelCount];

            SceneLuminanceStream.Fill(sample, 12.5, 0.4f, false, new Vector2(0.25f, 0.75f), 0.6f, Position, Rotation);

            CollectionAssert.AreEqual(
                new[] { 12.5f, 0.4f, -1f, 0f, -1f, -1f, 1f, 2f, 3f, 0.1f, 0.2f, 0.3f, 0.9f }, sample,
                "an invalid gaze sample must not leak a stale patch value or viewport point");
        }

        [Test]
        public void Fill_WithGazeOffView_KeepsValidButMarksPatchAndViewportMissing()
        {
            var sample = new float[SceneLuminanceStream.ChannelCount];

            SceneLuminanceStream.Fill(sample, 12.5, 0.4f, true, null, 0.6f, Position, Rotation);

            CollectionAssert.AreEqual(
                new[] { 12.5f, 0.4f, -1f, 1f, -1f, -1f, 1f, 2f, 3f, 0.1f, 0.2f, 0.3f, 0.9f }, sample,
                "eye tracking worked; the gaze point was just outside the captured view");
        }
    }
}
