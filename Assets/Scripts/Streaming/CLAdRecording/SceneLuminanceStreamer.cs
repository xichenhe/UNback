using System;
using LSL;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace CLAd.Recording
{
    /// <summary>
    /// Publishes CLAdSceneLuminanceLSL (see SceneLuminanceStream for the layout).
    ///
    /// About 30 times a second a hidden low-resolution copy of the main camera is enabled
    /// for exactly one frame, so it renders in the Built-in pipeline and in URP alike. The
    /// next frame its image is requested with AsyncGPUReadback, and the whole-view and
    /// gaze-patch luminance are pushed when the readback lands, stamped with the LSL time
    /// of the frame that was rendered. No frame ever waits for the GPU, and at most one
    /// readback is in flight: when the GPU is slow, captures are skipped, never queued.
    ///
    /// Gaze comes from the app's own VarjoEyeTrackingLSL through LslGazeSource, never
    /// from a Varjo API. SceneLuminanceBootstrap creates this component; no scene holds it.
    /// </summary>
    public class SceneLuminanceStreamer : MonoBehaviour
    {
        [Tooltip("Width and height of the capture image in pixels (read at start).")]
        public int captureSize = 128;

        [Tooltip("Captures per second.")]
        public float captureRateHz = 30f;

        [Tooltip("Radius of the gaze patch in degrees of visual angle.")]
        public float patchRadiusDegrees = 2f;

        [Tooltip("Vertical field of view of the capture while a headset is active, in degrees. " +
                 "The main camera's own value need not be the headset's under XR.")]
        public float xrVerticalFovDegrees = 90f;

        [Tooltip("Gaze samples older than this many seconds count as invalid (read at start).")]
        public float gazeMaxAgeSeconds = 0.1f;

        [Tooltip("This app's gaze stream, in the VarjoEyeTrackingLSL layout (read at start).")]
        public string gazeStreamName = "VarjoEyeTrackingLSL";

        private const double ReadbackWarningIntervalSeconds = 60.0;

        /// <summary>What was true in the frame a capture was rendered.</summary>
        private struct Capture
        {
            public double UnityTime;
            public double LslTime;
            public Vector3 Position;
            public Quaternion Rotation;
            public bool GazeValid;
            public Vector2? GazeViewport;
            public float VerticalFovDegrees;
        }

        private readonly float[] _sample = new float[SceneLuminanceStream.ChannelCount];
        private StreamOutlet _outlet;
        private RenderTexture _target;
        private PixelLayout _layout;
        private int _size;
        private byte[] _pixels;
        private Camera _captureCamera;
        private GazeSampleTracker _gazeTracker;
        private LslGazeSource _gazeSource;
        private Action<AsyncGPUReadbackRequest> _onReadback;
        private Capture _capture;
        private bool _captureRendering;
        private bool _readbackPending;
        private double _nextCaptureTime;
        private double _nextReadbackWarningTime;
        private string _hostName;
        private bool _waitingForCameraLogged;
        private bool _captureCameraLostLogged;
        private bool _captureErrorLogged;

        private void Awake()
        {
            if (!SystemInfo.supportsAsyncGPUReadback)
            {
                Debug.LogWarning("CLAd scene luminance: this device cannot read the GPU back asynchronously; " +
                                 $"{SceneLuminanceStream.Name} is off.");
                enabled = false;
                return;
            }

            try
            {
                var info = new StreamInfo(SceneLuminanceStream.Name, SceneLuminanceStream.Type,
                    SceneLuminanceStream.ChannelCount, SceneLuminanceStream.NominalRateHz,
                    channel_format_t.cf_float32, "");
                XMLElement channels = info.desc().append_child("channels");
                foreach (string label in SceneLuminanceStream.ChannelNames)
                    channels.append_child("channel").append_child_value("label", label);
                _outlet = new StreamOutlet(info);
                using (StreamInfo own = _outlet.info())
                    _hostName = own.hostname(); // how LSL names this machine, for LslGazeSource
            }
            catch (Exception e)
            {
                Debug.LogError($"CLAd scene luminance: could not create the LSL outlet '{SceneLuminanceStream.Name}'; " +
                               $"brightness is not recorded. {e}");
                enabled = false;
                return;
            }

            _size = Mathf.Max(8, captureSize);
            _target = new RenderTexture(_size, _size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
            {
                name = "CLAd Luminance Capture",
            };
            _target.Create();
            if (!SceneLuminanceMath.TryGetLayout(_target.graphicsFormat, out _layout))
            {
                Debug.LogError($"CLAd scene luminance: unexpected capture format {_target.graphicsFormat}; " +
                               "brightness is not recorded.");
                enabled = false;
                return;
            }
            _pixels = new byte[_size * _size * 4];
            _onReadback = OnReadback;
            CreateCaptureCamera();

            _gazeTracker = new GazeSampleTracker(gazeMaxAgeSeconds);
            _gazeSource = new LslGazeSource(gazeStreamName, _gazeTracker, _hostName);
        }

        private void CreateCaptureCamera()
        {
            var cameraObject = new GameObject("CLAd Luminance Capture Camera");
            cameraObject.transform.SetParent(transform, false);
            _captureCamera = cameraObject.AddComponent<Camera>();
            _captureCamera.enabled = false;
        }

        private void Update()
        {
            try
            {
                _gazeSource.Poll();

                if (!_captureRendering)
                    return;

                // The capture camera rendered at the end of the last frame: stop it and fetch the image.
                _captureRendering = false;
                if (_captureCamera == null)
                    return; // destroyed after it was switched on, so nothing was rendered
                _captureCamera.enabled = false;
                AsyncGPUReadback.Request(_target, 0, _onReadback);
                _readbackPending = true;
            }
            catch (Exception e)
            {
                LogCaptureErrorOnce(e);
            }
        }

        private void LateUpdate()
        {
            try
            {
                CaptureIfDue();
            }
            catch (Exception e)
            {
                LogCaptureErrorOnce(e);
            }
        }

        private void CaptureIfDue()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (_readbackPending || _captureRendering || now < _nextCaptureTime)
                return;

            if (_captureCamera == null)
            {
                // Something destroyed the hidden camera (a scene removing every camera, say).
                if (!_captureCameraLostLogged)
                {
                    _captureCameraLostLogged = true;
                    Debug.LogWarning("CLAd scene luminance: the capture camera was destroyed; making a new one.");
                }
                CreateCaptureCamera();
            }

            Camera main = Camera.main;
            if (main == null || main == _captureCamera)
            {
                if (!_waitingForCameraLogged)
                {
                    _waitingForCameraLogged = true;
                    Debug.Log("CLAd scene luminance: no main camera; waiting for one.");
                }
                return;
            }
            _waitingForCameraLogged = false;

            double period = 1.0 / Mathf.Max(1f, captureRateHz);
            _nextCaptureTime = now - _nextCaptureTime > period ? now + period : _nextCaptureTime + period;

            float verticalFov = XRSettings.isDeviceActive ? xrVerticalFovDegrees : main.fieldOfView;
            _captureCamera.CopyFrom(main);
            _captureCamera.targetTexture = _target;
            _captureCamera.rect = new Rect(0f, 0f, 1f, 1f); // the main camera may draw into part of the screen only
            // Built-in only: URP warns on this setter (every capture), and scriptable pipelines
            // never render a camera with a target texture in XR anyway.
            if (GraphicsSettings.currentRenderPipeline == null)
                _captureCamera.stereoTargetEye = StereoTargetEyeMask.None;
            _captureCamera.ResetProjectionMatrix();
            _captureCamera.ResetWorldToCameraMatrix();
            _captureCamera.ResetAspect();
            _captureCamera.fieldOfView = verticalFov;
            _captureCamera.allowMSAA = false;
            _captureCamera.allowHDR = false;
            _captureCamera.enabled = true;
            _captureRendering = true;

            double lslNow = LSL.LSL.local_clock();
            bool gazeValid = _gazeTracker.TryGetForward(lslNow, out Vector3 forward);
            Vector2? gazeViewport = null;
            if (gazeValid && SceneLuminanceMath.TryGazeToViewport(forward, verticalFov, _captureCamera.aspect,
                    out Vector2 viewport))
                gazeViewport = viewport;

            Transform view = _captureCamera.transform;
            _capture = new Capture
            {
                UnityTime = now,
                LslTime = lslNow,
                Position = view.position,
                Rotation = view.rotation,
                GazeValid = gazeValid,
                GazeViewport = gazeViewport,
                VerticalFovDegrees = verticalFov,
            };
        }

        private void OnReadback(AsyncGPUReadbackRequest request)
        {
            _readbackPending = false;
            if (_outlet == null)
                return; // destroyed while the readback was in flight

            try
            {
                if (request.hasError)
                {
                    double now = Time.realtimeSinceStartupAsDouble;
                    if (now >= _nextReadbackWarningTime)
                    {
                        _nextReadbackWarningTime = now + ReadbackWarningIntervalSeconds;
                        Debug.LogWarning("CLAd scene luminance: a GPU readback failed; that capture is dropped.");
                    }
                    return;
                }

                request.GetData<byte>().CopyTo(_pixels);
                float viewLuminance = SceneLuminanceMath.MeanLuminance(_pixels, _size, _size, _layout);
                float gazeLuminance = -1f;
                if (_capture.GazeViewport.HasValue)
                {
                    float radius = SceneLuminanceMath.PatchRadiusPixels(patchRadiusDegrees,
                        _capture.VerticalFovDegrees, _size);
                    gazeLuminance = SceneLuminanceMath.MeanLuminanceInDisc(_pixels, _size, _size, _layout,
                        _capture.GazeViewport.Value, radius);
                }

                SceneLuminanceStream.Fill(_sample, _capture.UnityTime, viewLuminance, _capture.GazeValid,
                    _capture.GazeViewport, gazeLuminance, _capture.Position, _capture.Rotation);
                _outlet.push_sample(_sample, _capture.LslTime);
            }
            catch (Exception e)
            {
                LogCaptureErrorOnce(e);
            }
        }

        private void LogCaptureErrorOnce(Exception e)
        {
            if (_captureErrorLogged)
                return;
            _captureErrorLogged = true;
            Debug.LogError($"CLAd scene luminance: a capture failed and was dropped; later failures are not logged. {e}");
        }

        private void OnDestroy()
        {
            _gazeSource?.Dispose();
            _gazeSource = null;
            _outlet?.Dispose();
            _outlet = null;

            if (_captureCamera != null)
            {
                _captureCamera.targetTexture = null;
                Destroy(_captureCamera.gameObject);
            }
            if (_target != null)
            {
                _target.Release();
                Destroy(_target);
            }
        }
    }
}
