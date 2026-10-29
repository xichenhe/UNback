using System;
using System.Reflection;
using NUnit.Framework;

namespace ToolNBack.Tests
{
    /// <summary>
    /// The effort rating joins the analysis contract (EXPERIMENTER_GUIDE.md 5.2/5.3):
    /// effort_probe (code 11) when the scale appears, effort_rating (code 12) when it
    /// closes. The numeric sample keeps the 9-channel layout: the rating rides on the
    /// outcome channel (5, 0 = none) and its response time on the RT channel (6, ms).
    /// </summary>
    public class EffortMarkerFormatTests
    {
        private static readonly Type FormatType =
            Type.GetType("NBackStreaming.NBackMarkerFormat, Assembly-CSharp");
        private static readonly Type ProbeStartedType =
            Type.GetType("NBackStreaming.EffortProbeStartedData, Assembly-CSharp");
        private static readonly Type RatedType =
            Type.GetType("NBackStreaming.EffortRatedData, Assembly-CSharp");

        [Test]
        public void ProbeMarker_NamesTheRatedBlockAndScale()
        {
            object probe = ProbeStarted(taskIndex: 4, n: 3, practice: false);

            Assert.AreEqual("effort_probe|index=4|n=3|scale=1-9", Marker(probe));
        }

        [Test]
        public void ProbeEvent_IsCode11_StampedWithTheRatedBlock()
        {
            object probe = ProbeStarted(taskIndex: 4, n: 3, practice: false);

            CollectionAssert.AreEqual(new[] { 40f, 11f, 4f, -1f, -1f, 0f, -1f, 3f, 2f }, Event(probe, 40f));
        }

        [Test]
        public void RatingMarker_CarriesTheRating_ResponseTime_AndScale()
        {
            object rated = Rated(taskIndex: 4, n: 3, rating: 7, responseTime: 2.35f, timedOut: false, practice: false);

            Assert.AreEqual("effort_rating|index=4|n=3|rating=7|rt=2.350|scale=1-9|timeout=0", Marker(rated));
        }

        [Test]
        public void RatingEvent_IsCode12_RatingOnChannel5_RtInMsOnChannel6()
        {
            object rated = Rated(taskIndex: 4, n: 3, rating: 7, responseTime: 2.35f, timedOut: false, practice: false);

            float[] sample = Event(rated, 40f);

            float[] expected = { 40f, 12f, 4f, -1f, -1f, 7f, 2350f, 3f, 2f };
            Assert.AreEqual(expected.Length, sample.Length, "the 9-channel layout must not change");
            for (int ch = 0; ch < expected.Length; ch++)
                Assert.AreEqual(expected[ch], sample[ch], 0.01f, $"channel {ch}");
        }

        [Test]
        public void TimedOutPracticeRating_HasNoRatingOrRt_AndIsMarkedPractice()
        {
            object rated = Rated(taskIndex: 1, n: 2, rating: 0, responseTime: -1f, timedOut: true, practice: true);

            Assert.AreEqual("effort_rating|index=1|n=2|rating=na|rt=na|scale=1-9|timeout=1|practice=1", Marker(rated));
            CollectionAssert.AreEqual(new[] { 40f, 12f, 1f, -1f, -1f, 0f, -1f, 2f, 1f }, Event(rated, 40f));
        }

        // --- harness -------------------------------------------------------------

        private static object ProbeStarted(int taskIndex, int n, bool practice)
        {
            Assert.NotNull(ProbeStartedType, "EffortProbeStartedData must exist in Assembly-CSharp");
            object data = Activator.CreateInstance(ProbeStartedType);
            Set(data, "TaskIndex", taskIndex);
            Set(data, "N", n);
            Set(data, "ScaleMin", 1);
            Set(data, "ScaleMax", 9);
            Set(data, "Practice", practice);
            return data;
        }

        private static object Rated(int taskIndex, int n, int rating, float responseTime, bool timedOut, bool practice)
        {
            Assert.NotNull(RatedType, "EffortRatedData must exist in Assembly-CSharp");
            object data = Activator.CreateInstance(RatedType);
            Set(data, "TaskIndex", taskIndex);
            Set(data, "N", n);
            Set(data, "ScaleMin", 1);
            Set(data, "ScaleMax", 9);
            Set(data, "Rating", rating);
            Set(data, "ResponseTime", responseTime);
            Set(data, "TimedOut", timedOut);
            Set(data, "Practice", practice);
            return data;
        }

        private static void Set(object boxedStruct, string field, object value)
        {
            FieldInfo info = boxedStruct.GetType().GetField(field);
            Assert.NotNull(info, $"{boxedStruct.GetType().Name} must have a {field} field");
            info.SetValue(boxedStruct, value);
        }

        private static string Marker(object data)
        {
            MethodInfo marker = FormatType.GetMethod("Marker", new[] { data.GetType() });
            Assert.NotNull(marker, $"NBackMarkerFormat must format {data.GetType().Name} as a marker string");
            return (string)marker.Invoke(null, new[] { data });
        }

        private static float[] Event(object data, float unityTime)
        {
            MethodInfo evt = FormatType.GetMethod("Event", new[] { data.GetType(), typeof(float) });
            Assert.NotNull(evt, $"NBackMarkerFormat must encode {data.GetType().Name} as an event sample");
            return (float[])evt.Invoke(null, new[] { data, unityTime });
        }
    }
}
