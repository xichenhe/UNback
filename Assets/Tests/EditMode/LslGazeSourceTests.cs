using System;
using System.Diagnostics;
using System.Threading;
using CLAd.Recording;
using LSL;
using NUnit.Framework;
using UnityEngine;

namespace ToolNBack.Tests
{
    /// <summary>
    /// The brightness streamer reads gaze from the app's own VarjoEyeTrackingLSL
    /// through an LSL inlet, never through a Varjo API, so it cannot take samples
    /// away from the 200 Hz pupil stream (design 4.2). The app's own stream is always
    /// on this machine; one from another machine would be someone else's eyes. The gaze
    /// outlet has an empty source_id, so when it goes away (UNback leaves Play mode, a
    /// CLAdVR scene rebuilds it) the inlet is lost for good and must be replaced.
    /// </summary>
    public class LslGazeSourceTests
    {
        private const int GazeChannels = 39;

        private static double[] GazeSample(double valid, Vector3 forward)
        {
            var sample = new double[GazeChannels];
            sample[9] = valid;
            sample[10] = forward.x;
            sample[11] = forward.y;
            sample[12] = forward.z;
            return sample;
        }

        // ---- newest-sample validity --------------------------------------------

        [Test]
        public void GazeSampleTracker_WithNoSample_IsInvalid()
        {
            var tracker = new GazeSampleTracker();

            Assert.IsFalse(tracker.TryGetForward(10.0, out _));
        }

        [Test]
        public void GazeSampleTracker_FreshValidSample_GivesItsForward()
        {
            var tracker = new GazeSampleTracker();
            tracker.Offer(GazeSample(1, Vector3.forward), 10.0);

            Assert.IsTrue(tracker.TryGetForward(10.05, out Vector3 forward));
            Assert.AreEqual(Vector3.forward, forward);
        }

        [Test]
        public void GazeSampleTracker_FlagZero_IsInvalid()
        {
            var tracker = new GazeSampleTracker();
            tracker.Offer(GazeSample(0, Vector3.forward), 10.0);

            Assert.IsFalse(tracker.TryGetForward(10.05, out _), "channel 9 = 0 means Varjo had no combined gaze");
        }

        [Test]
        public void GazeSampleTracker_SampleOlderThanMaxAge_IsInvalid()
        {
            var tracker = new GazeSampleTracker(maxAgeSeconds: 0.1);
            tracker.Offer(GazeSample(1, Vector3.forward), 10.0);

            Assert.IsFalse(tracker.TryGetForward(10.2, out _), "a stalled gaze stream must not freeze the gaze point");
        }

        [Test]
        public void GazeSampleTracker_KeepsTheNewestSample()
        {
            var tracker = new GazeSampleTracker();
            tracker.Offer(GazeSample(1, Vector3.forward), 10.0);
            tracker.Offer(GazeSample(0, Vector3.zero), 10.01);

            Assert.IsFalse(tracker.TryGetForward(10.02, out _), "a blink after a valid sample must end the gaze point");
        }

        [Test]
        public void GazeSampleTracker_Clear_ForgetsTheSample()
        {
            var tracker = new GazeSampleTracker();
            tracker.Offer(GazeSample(1, Vector3.forward), 10.0);

            tracker.Clear();

            Assert.IsFalse(tracker.TryGetForward(10.01, out _));
        }

        // ---- real LSL ----------------------------------------------------------

        private static string UniqueStreamName() => "CLAdRecordingTest-" + Guid.NewGuid().ToString("N");

        private static StreamOutlet GazeOutlet(string name, int channels = GazeChannels)
        {
            var info = new StreamInfo(name, "GazeData", channels, 200, channel_format_t.cf_double64, "");
            return new StreamOutlet(info);
        }

        /// <summary>The host name LSL gives streams published from this machine.</summary>
        private static string ThisHost(StreamOutlet outlet)
        {
            using (StreamInfo info = outlet.info())
                return info.hostname();
        }

        private static void Push(StreamOutlet outlet, Vector3 forward, int channels = GazeChannels)
        {
            double[] sample = GazeSample(1, forward);
            Array.Resize(ref sample, channels);
            outlet.push_sample(sample, LSL.LSL.local_clock());
        }

        private static bool PollUntil(Func<bool> condition, double seconds)
        {
            var clock = Stopwatch.StartNew();
            while (clock.Elapsed.TotalSeconds < seconds)
            {
                if (condition())
                    return true;
                Thread.Sleep(50);
            }
            return false;
        }

        private static bool GazeIs(GazeSampleTracker tracker, Vector3 expected) =>
            tracker.TryGetForward(LSL.LSL.local_clock(), out Vector3 forward)
            && Vector3.Distance(forward, expected) < 1e-4f;

        [Test, Timeout(30000)]
        public void LslGazeSource_ReadsTheNewestSampleOfANamedStream()
        {
            string name = UniqueStreamName();
            var tracker = new GazeSampleTracker();
            using (StreamOutlet outlet = GazeOutlet(name))
            using (var source = new LslGazeSource(name, tracker, ThisHost(outlet), retrySeconds: 0.2))
            {
                bool seen = PollUntil(() =>
                {
                    Push(outlet, Vector3.forward);
                    source.Poll();
                    return GazeIs(tracker, Vector3.forward);
                }, 5.0);

                Assert.IsTrue(seen, "the in-app inlet never delivered the pushed gaze sample");
                Assert.IsTrue(source.IsAttached);
            }
        }

        [Test, Timeout(30000)]
        public void LslGazeSource_IgnoresStreamsFromOtherHosts()
        {
            string name = UniqueStreamName();
            var tracker = new GazeSampleTracker();
            using (StreamOutlet outlet = GazeOutlet(name))
            using (var source = new LslGazeSource(name, tracker, "another-lab-pc", retrySeconds: 0.2))
            {
                PollUntil(() =>
                {
                    Push(outlet, Vector3.forward);
                    source.Poll();
                    return false;
                }, 2.0);

                Assert.IsFalse(source.IsAttached, "a gaze stream from another machine is someone else's eyes");
                Assert.IsFalse(tracker.TryGetForward(LSL.LSL.local_clock(), out _));
            }
        }

        [Test, Timeout(30000)]
        public void LslGazeSource_ReattachesAfterTheStreamIsLost()
        {
            string name = UniqueStreamName();
            var tracker = new GazeSampleTracker();
            StreamOutlet first = GazeOutlet(name);
            using (var source = new LslGazeSource(name, tracker, ThisHost(first), retrySeconds: 0.2))
            {
                Assert.IsTrue(PollUntil(() =>
                {
                    Push(first, Vector3.forward);
                    source.Poll();
                    return GazeIs(tracker, Vector3.forward);
                }, 5.0), "never attached to the first outlet");

                first.Dispose();
                Assert.IsTrue(PollUntil(() =>
                {
                    source.Poll();
                    return !source.IsAttached;
                }, 1.0), "the loss of the gaze stream must be noticed within a second");

                // The resolver keeps listing the lost outlet for a few seconds; it must not be re-opened.
                bool reopenedTheLostStream = PollUntil(() =>
                {
                    source.Poll();
                    return source.IsAttached;
                }, 1.5);
                Assert.IsFalse(reopenedTheLostStream, "the lost outlet was re-opened while the resolver still listed it");
                Assert.IsFalse(tracker.TryGetForward(LSL.LSL.local_clock(), out _),
                    "no gaze point may survive the loss of its stream");

                var expected = new Vector3(0f, 0.6f, 0.8f);
                using (StreamOutlet second = GazeOutlet(name))
                {
                    bool back = PollUntil(() =>
                    {
                        Push(second, expected);
                        source.Poll();
                        return GazeIs(tracker, expected);
                    }, 3.0);

                    Assert.IsTrue(back, "the stream published again under the same name was not read within 3 s");
                }
            }
        }

        [Test, Timeout(30000)]
        public void LslGazeSource_IgnoresStreamsWithTooFewChannels()
        {
            string name = UniqueStreamName();
            var tracker = new GazeSampleTracker();
            using (StreamOutlet outlet = GazeOutlet(name, channels: 5))
            using (var source = new LslGazeSource(name, tracker, ThisHost(outlet), retrySeconds: 0.2))
            {
                PollUntil(() =>
                {
                    source.Poll();
                    return false;
                }, 2.0);

                Assert.IsFalse(source.IsAttached, "a stream without channels 9-12 cannot carry combined gaze");
            }
        }
    }
}
