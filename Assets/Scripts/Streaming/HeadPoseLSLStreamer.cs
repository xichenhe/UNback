using System;
using UnityEngine;
using LSL;

namespace NBackStreaming
{
    /// <summary>
    /// Streams the XR camera (HMD) world pose to an LSL outlet ("NBackHeadPoseLSL",
    /// 9 channels, double64, irregular rate), one sample per rendered frame.
    ///
    /// Channel layout:
    ///  [0] Unity time (Time.realtimeSinceStartupAsDouble)
    ///  [1-3] world position xyz
    ///  [4-7] world rotation quaternion xyzw
    ///  [8] tracked flag (1 while the XR camera is active and enabled)
    /// Samples are pushed with the LSL local clock as timestamp.
    /// </summary>
    public class HeadPoseLSLStreamer : MonoBehaviour
    {
        private const string StreamName = "NBackHeadPoseLSL";
        private const string StreamType = "HeadPose";
        private const string SourceId = "uNback-headpose";
        private const int ChannelCount = 9;
        private const double IrregularRate = 0.0;

        /// <summary>XR camera whose world pose is streamed. Falls back to Camera.main in Start.</summary>
        public Camera xrCamera;

        private StreamOutlet _outlet;
        private readonly double[] _sample = new double[ChannelCount];

        #region Unity methods
        private void Awake()
        {
            try
            {
                var info = new StreamInfo(StreamName, StreamType, ChannelCount, IrregularRate,
                    channel_format_t.cf_double64, SourceId);
                _outlet = new StreamOutlet(info);
            }
            catch (Exception e)
            {
                _outlet = null;
                Debug.LogError($"HeadPoseLSLStreamer: failed to create LSL outlet '{StreamName}' — head pose streaming disabled. {e}");
            }
        }

        private void Start()
        {
            if (xrCamera == null)
                xrCamera = Camera.main;
        }

        private void Update()
        {
            if (_outlet == null || xrCamera == null)
                return;

            Transform cameraTransform = xrCamera.transform;
            Vector3 position = cameraTransform.position;
            Quaternion rotation = cameraTransform.rotation;

            _sample[0] = Time.realtimeSinceStartupAsDouble;
            _sample[1] = position.x;
            _sample[2] = position.y;
            _sample[3] = position.z;
            _sample[4] = rotation.x;
            _sample[5] = rotation.y;
            _sample[6] = rotation.z;
            _sample[7] = rotation.w;
            _sample[8] = xrCamera.isActiveAndEnabled ? 1.0 : 0.0;

            _outlet.push_sample(_sample, LSL.LSL.local_clock());
        }

        private void OnDestroy() => DisposeOutlet();

        private void OnApplicationQuit() => DisposeOutlet();
        #endregion

        #region Private methods
        private void DisposeOutlet()
        {
            if (_outlet == null)
                return;

            _outlet.Dispose();
            _outlet = null;
        }
        #endregion
    }
}
