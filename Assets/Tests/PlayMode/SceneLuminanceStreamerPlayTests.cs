using System;
using System.Collections;
using System.Collections.Generic;
using CLAd.Recording;
using LSL;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace ToolNBack.PlayTests
{
    /// <summary>
    /// End to end in Play mode, with no scene changes: the bootstrap creates the
    /// streamer by itself, which renders the main camera, reads it back without
    /// blocking and publishes CLAdSceneLuminanceLSL. The expected values hold in
    /// Gamma (UNback, Built-in) and Linear (CLAdVR, URP) projects alike, so these tests
    /// run in both. They assume no VarjoEyeTrackingLSL is on the network, so the
    /// bootstrap's streamer reads gaze as missing. Needs a graphics device, so they
    /// are ignored under -nographics.
    /// </summary>
    public class SceneLuminanceStreamerPlayTests
    {
        private const float Tan20 = 0.36397f;

        private readonly List<Camera> _otherMainCameras = new List<Camera>();
        private GameObject _cameraObject;

        [SetUp]
        public void ShowAMidGreyViewOnTheMainCamera()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("no graphics device (-nographics): nothing renders");

            // Camera.main must be this test's camera, not one left by an earlier test's scene.
            foreach (Camera other in Camera.allCameras)
            {
                if (!other.CompareTag("MainCamera"))
                    continue;
                other.enabled = false;
                _otherMainCameras.Add(other);
            }

            _cameraObject = new GameObject("Test Main Camera") { tag = "MainCamera" };
            var camera = _cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            camera.cullingMask = 0; // the view is the background colour only
        }

        [TearDown]
        public void RestoreTheCameras()
        {
            if (_cameraObject != null)
                Object.Destroy(_cameraObject);
            foreach (Camera other in _otherMainCameras)
            {
                if (other != null)
                    other.enabled = true;
            }
            _otherMainCameras.Clear();
        }

        /// <summary>
        /// Reads every CLAdSceneLuminanceLSL outlet until <paramref name="wanted"/> accepted
        /// samples or the time runs out. Every outlet, because a test's own streamer can run
        /// beside the bootstrap's and a destroyed one stays listed for a few seconds.
        /// </summary>
        private static IEnumerator CollectSamples(List<float[]> samples, int wanted, float seconds,
            Func<float[], bool> accept = null, Action eachFrame = null)
        {
            var inlets = new Dictionary<string, StreamInlet>();
            using (var resolver = new ContinuousResolver("name", SceneLuminanceStream.Name))
            {
                try
                {
                    float deadline = Time.realtimeSinceStartup + seconds;
                    while (Time.realtimeSinceStartup < deadline && samples.Count < wanted)
                    {
                        eachFrame?.Invoke();
                        yield return null;

                        foreach (StreamInfo info in resolver.results())
                        {
                            string uid = info.uid();
                            if (inlets.ContainsKey(uid))
                            {
                                info.Dispose();
                                continue;
                            }
                            Assert.AreEqual(SceneLuminanceStream.ChannelCount, info.channel_count());
                            inlets[uid] = new StreamInlet(info);
                        }

                        foreach (StreamInlet inlet in inlets.Values)
                        {
                            var sample = new float[SceneLuminanceStream.ChannelCount];
                            try
                            {
                                while (inlet.pull_sample(sample, 0.0) != 0.0)
                                {
                                    if (accept == null || accept(sample))
                                        samples.Add(sample);
                                    sample = new float[SceneLuminanceStream.ChannelCount];
                                }
                            }
                            catch (LostException)
                            {
                                // a destroyed streamer's outlet; the others still count
                            }
                        }
                    }
                }
                finally
                {
                    foreach (StreamInlet inlet in inlets.Values)
                        inlet.Dispose();
                }
            }
        }

        [UnityTest]
        public IEnumerator Streamer_PublishesTheMainCamerasLuminanceWithoutGaze()
        {
            var samples = new List<float[]>();
            yield return CollectSamples(samples, 10, 10f);

            Assert.GreaterOrEqual(samples.Count, 5, "the streamer published too few samples in 10 s");
            for (int i = 0; i < samples.Count; i++)
            {
                float[] s = samples[i];
                Assert.AreEqual(0.2159f, s[1], 0.02f, $"sample {i}: view luminance of a mid-grey view");
                Assert.AreEqual(-1f, s[2], $"sample {i}: gaze luminance without gaze");
                Assert.AreEqual(0f, s[3], $"sample {i}: gaze valid without a gaze stream");
                Assert.AreEqual(-1f, s[4], $"sample {i}: gaze u without gaze");
                Assert.AreEqual(-1f, s[5], $"sample {i}: gaze v without gaze");
                if (i > 0)
                    Assert.Greater(s[0], samples[i - 1][0], $"sample {i}: Unity time must increase");
            }
        }

        [UnityTest]
        public IEnumerator Streamer_LogsNothingWhileCapturing()
        {
            var streamerLogs = new List<string>();
            void Collect(string message, string stackTrace, LogType type)
            {
                if (type != LogType.Log && stackTrace.Contains("SceneLuminanceStreamer"))
                    streamerLogs.Add($"{type}: {message}");
            }

            var samples = new List<float[]>();
            Application.logMessageReceived += Collect;
            try
            {
                yield return CollectSamples(samples, 20, 10f);
            }
            finally
            {
                Application.logMessageReceived -= Collect;
            }

            Assert.GreaterOrEqual(samples.Count, 20, "the streamer must have been capturing during the check");
            CollectionAssert.IsEmpty(streamerLogs,
                "the capture loop runs 30 times a second, so anything it logs floods the console");
        }

        [UnityTest]
        public IEnumerator Streamer_CapturesTheWholeViewWhateverTheMainCamerasViewport()
        {
            var camera = _cameraObject.GetComponent<Camera>();
            camera.backgroundColor = Color.white;
            camera.rect = new Rect(0f, 0f, 0.5f, 0.5f); // the main camera draws into one screen corner

            var samples = new List<float[]>();
            yield return CollectSamples(samples, 10, 10f);

            Assert.GreaterOrEqual(samples.Count, 10, "the streamer published too few samples in 10 s");
            foreach (float[] s in samples)
                Assert.AreEqual(1f, s[1], 0.02f, "the capture must fill its whole image, not the main camera's corner");
        }

        [UnityTest]
        public IEnumerator Streamer_KeepsPublishingAfterItsCaptureCameraIsDestroyed()
        {
            List<SceneLuminanceStreamer> paused = PauseOtherStreamers();
            var streamerObject = new GameObject("Streamer Under Test");
            var streamer = streamerObject.AddComponent<SceneLuminanceStreamer>();
            try
            {
                yield return null;
                // Something in a scene destroys every camera it finds, the hidden one included.
                Object.Destroy(streamer.GetComponentInChildren<Camera>(true).gameObject);
                yield return null;

                var samples = new List<float[]>();
                yield return CollectSamples(samples, 10, 10f);

                Assert.GreaterOrEqual(samples.Count, 10, "the stream must keep going after its capture camera is destroyed");
                foreach (float[] s in samples)
                    Assert.AreEqual(0.2159f, s[1], 0.02f, "and keep measuring the main camera's view");
            }
            finally
            {
                Object.Destroy(streamerObject);
                foreach (SceneLuminanceStreamer other in paused)
                {
                    if (other != null)
                        other.enabled = true;
                }
            }
        }

        /// <summary>Stops every running streamer, so only the one a test creates publishes.</summary>
        private static List<SceneLuminanceStreamer> PauseOtherStreamers()
        {
            var paused = new List<SceneLuminanceStreamer>();
            foreach (SceneLuminanceStreamer other in Object.FindObjectsByType<SceneLuminanceStreamer>(FindObjectsSortMode.None))
            {
                if (!other.enabled)
                    continue;
                other.enabled = false;
                paused.Add(other);
            }
            return paused;
        }

        /// <summary>
        /// The gaze patch must be read where the eyes look, in both pipelines: a red quad over
        /// the top-right quarter of a black 90° view, and gaze from a synthetic 39-channel stream
        /// that only this test publishes, read by a streamer created for the test. A vertical or
        /// horizontal flip reads black where red is, and a BGRA/RGBA mix-up reads red as 0.0722.
        /// </summary>
        [UnityTest]
        public IEnumerator Streamer_ReadsTheGazePatchWhereTheEyesLook()
        {
            var camera = _cameraObject.GetComponent<Camera>();
            camera.backgroundColor = Color.black;
            camera.cullingMask = ~0;
            camera.fieldOfView = 90f; // the capture's own field of view with or without a headset

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.position = new Vector3(0.55f, 0.55f, 1f);
            quad.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
            string unlit = GraphicsSettings.currentRenderPipeline == null ? "Unlit/Color" : "Universal Render Pipeline/Unlit";
            var red = new Material(Shader.Find(unlit)) { color = Color.red };
            quad.GetComponent<Renderer>().sharedMaterial = red;

            string gazeStreamName = "CLAdRecordingTest-" + Guid.NewGuid().ToString("N");
            var gaze = new StreamOutlet(new StreamInfo(gazeStreamName, "GazeData", 39, 200, channel_format_t.cf_double64, ""));
            var gazeSample = new double[39];
            gazeSample[9] = 1;

            var streamerObject = new GameObject("Streamer Under Test");
            streamerObject.SetActive(false);
            streamerObject.AddComponent<SceneLuminanceStreamer>().gazeStreamName = gazeStreamName;
            streamerObject.SetActive(true);

            try
            {
                var upRight = new List<float[]>();
                yield return LookAlong(new Vector3(Tan20, Tan20, 1f), 0.682f, 0.682f, gaze, gazeSample, upRight);
                var upLeft = new List<float[]>();
                yield return LookAlong(new Vector3(-Tan20, Tan20, 1f), 0.318f, 0.682f, gaze, gazeSample, upLeft);
                var downRight = new List<float[]>();
                yield return LookAlong(new Vector3(Tan20, -Tan20, 1f), 0.682f, 0.318f, gaze, gazeSample, downRight);

                Assert.GreaterOrEqual(upRight.Count, 3, "no valid gaze samples at the upper right of the view");
                Assert.GreaterOrEqual(upLeft.Count, 3, "no valid gaze samples at the upper left of the view");
                Assert.GreaterOrEqual(downRight.Count, 3, "no valid gaze samples at the lower right of the view");
                foreach (float[] s in upRight)
                {
                    Assert.AreEqual(0.2126f, s[2], 0.02f, "gaze on the red quarter must read red (0.2126)");
                    Assert.AreEqual(0.0532f, s[1], 0.005f, "a red quarter of a black view");
                }
                foreach (float[] s in upLeft)
                    Assert.Less(s[2], 0.02f, "gaze on black at the upper left (a horizontal flip would read red)");
                foreach (float[] s in downRight)
                    Assert.Less(s[2], 0.02f, "gaze on black at the lower right (a vertical flip would read red)");
            }
            finally
            {
                Object.Destroy(streamerObject);
                Object.Destroy(quad);
                Object.Destroy(red);
                gaze.Dispose();
            }
        }

        /// <summary>Publishes gaze along <paramref name="forward"/> until the streamer reports it at (u, v).</summary>
        private static IEnumerator LookAlong(Vector3 forward, float u, float v, StreamOutlet gaze, double[] gazeSample,
            List<float[]> samples)
        {
            gazeSample[10] = forward.x;
            gazeSample[11] = forward.y;
            gazeSample[12] = forward.z;
            yield return CollectSamples(samples, 3, 10f,
                accept: s => s[3] == 1f && Mathf.Abs(s[4] - u) < 0.01f && Mathf.Abs(s[5] - v) < 0.01f,
                eachFrame: () => gaze.push_sample(gazeSample, LSL.LSL.local_clock()));
        }
    }
}
