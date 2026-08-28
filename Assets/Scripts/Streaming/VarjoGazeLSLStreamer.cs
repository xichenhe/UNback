using System;
using System.Collections.Generic;
using UnityEngine;
using Varjo.XR;
using LSL;

namespace NBackStreaming
{
    /// <summary>
    /// Streams Varjo eye tracking data to an LSL outlet ("VarjoEyeTrackingLSL", 39 channels,
    /// double64, nominal 200 Hz). The channel layout is byte-compatible with the CLAdVR
    /// EyeTrackingVarjo streamer so existing analysis pipelines keep working. Unlike CLAdVR,
    /// <see cref="VarjoEyeTracking.GetGazeList"/> is drained exactly once per Update so no
    /// samples are lost to a double drain.
    ///
    /// Channel layout:
    ///  [0] gaze frame number
    ///  [1] capture time mapped into the LSL clock domain (also used as the sample timestamp)
    ///  [2] log time (milliseconds, DateTime.Now.Ticks / TicksPerMillisecond)
    ///  [3-5] HMD local position xyz
    ///  [6-8] HMD local rotation quaternion x,y,z (w intentionally omitted for CLAdVR
    ///        byte-compatibility; the full world pose lives on NBackHeadPoseLSL)
    ///  [9] combined gaze valid  [10-12] combined forward  [13-15] combined origin
    ///  [16] inter-pupillary distance (mm)
    ///  [17] left valid  [18-20] left forward  [21-23] left origin
    ///  [24] left pupil/iris ratio  [25] left pupil mm  [26] left iris mm
    ///  [27] right valid  [28-30] right forward  [31-33] right origin
    ///  [34] right pupil/iris ratio  [35] right pupil mm  [36] right iris mm
    ///  [37] focus distance  [38] focus stability
    /// Blocks are zeroed when the corresponding status flag reports Invalid.
    /// </summary>
    public class VarjoGazeLSLStreamer : MonoBehaviour
    {
        private const string StreamName = "VarjoEyeTrackingLSL";
        private const string StreamType = "GazeData";
        // Empty like CLAdVR's outlet, so the stream identity is indistinguishable
        // across the UNback -> CLAdVR app switch for any consumer.
        private const string SourceId = "";
        private const int ChannelCount = 39;
        private const double NominalSamplingRate = 200.0;

        /// <summary>XR camera whose local pose fills channels 3-8. Falls back to Camera.main in Start.</summary>
        public Camera xrCamera;

        /// <summary>Master switch for pushing gaze samples to LSL.</summary>
        public bool streamGazeData = true;

        /// <summary>Legacy Input Manager key that requests a gaze calibration (requires activeInputHandler = Both).</summary>
        public KeyCode calibrationKey = KeyCode.F9;

        /// <summary>Calibration mode used when <see cref="calibrationKey"/> is pressed.</summary>
        public VarjoEyeTracking.GazeCalibrationMode calibrationMode = VarjoEyeTracking.GazeCalibrationMode.Fast;

        private StreamOutlet _outlet;
        private readonly double[] _sample = new double[ChannelCount];
        private List<VarjoEyeTracking.GazeData> _gazeList;
        private List<VarjoEyeTracking.EyeMeasurements> _eyeMeasurementsList;

        #region Unity methods
        private void Awake()
        {
            try
            {
                var info = new StreamInfo(StreamName, StreamType, ChannelCount, NominalSamplingRate,
                    channel_format_t.cf_double64, SourceId);
                _outlet = new StreamOutlet(info);
            }
            catch (Exception e)
            {
                _outlet = null;
                Debug.LogError($"VarjoGazeLSLStreamer: failed to create LSL outlet '{StreamName}' — gaze streaming disabled. {e}");
            }
        }

        private void Start()
        {
            if (xrCamera == null)
                xrCamera = Camera.main;

            // Deliberately NO SetGazeOutputFrequency/SetGazeOutputFilterType here:
            // CLAdVR leaves the Varjo Base configuration untouched, and forcing
            // different settings during calibration would change gaze/pupil dynamics
            // at the UNback -> CLAdVR switch (a confound for gaze-derived features).
        }

        private void Update()
        {
            // Request gaze calibration.
            if (Input.GetKeyDown(calibrationKey))
                VarjoEyeTracking.RequestGazeCalibration(calibrationMode);

            if (!streamGazeData || _outlet == null || xrCamera == null)
                return;

            if (!VarjoEyeTracking.IsGazeAllowed() || !VarjoEyeTracking.IsGazeCalibrated())
                return;

            // Drain the gaze queue EXACTLY once per frame; a second GetGazeList call
            // within the same frame would return an empty list and drop samples.
            int dataCount = VarjoEyeTracking.GetGazeList(out _gazeList, out _eyeMeasurementsList);
            for (int i = 0; i < dataCount; i++)
                PushSample(_gazeList[i], _eyeMeasurementsList[i]);
        }

        private void OnDestroy() => DisposeOutlet();

        private void OnApplicationQuit() => DisposeOutlet();
        #endregion

        #region Private methods
        private void PushSample(VarjoEyeTracking.GazeData gaze, VarjoEyeTracking.EyeMeasurements eyeMeasurements)
        {
            Vector3 hmdPosition = xrCamera.transform.localPosition;
            Quaternion hmdRotation = xrCamera.transform.localRotation;

            // Gaze data frame number.
            _sample[0] = gaze.frameNumber;

            // Channel 1 is assigned below: capture time mapped into the LSL clock domain.

            // Log time (milliseconds).
            _sample[2] = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            // HMD local pose (quaternion w intentionally omitted, see class docs).
            _sample[3] = hmdPosition.x;
            _sample[4] = hmdPosition.y;
            _sample[5] = hmdPosition.z;
            _sample[6] = hmdRotation.x;
            _sample[7] = hmdRotation.y;
            _sample[8] = hmdRotation.z;

            // Combined gaze.
            bool invalid = gaze.status == VarjoEyeTracking.GazeStatus.Invalid;
            _sample[9] = invalid ? 0 : 1;
            _sample[10] = invalid ? 0 : gaze.gaze.forward.x;
            _sample[11] = invalid ? 0 : gaze.gaze.forward.y;
            _sample[12] = invalid ? 0 : gaze.gaze.forward.z;
            _sample[13] = invalid ? 0 : gaze.gaze.origin.x;
            _sample[14] = invalid ? 0 : gaze.gaze.origin.y;
            _sample[15] = invalid ? 0 : gaze.gaze.origin.z;

            // IPD.
            _sample[16] = invalid ? 0 : eyeMeasurements.interPupillaryDistanceInMM;

            // Left eye.
            bool leftInvalid = gaze.leftStatus == VarjoEyeTracking.GazeEyeStatus.Invalid;
            _sample[17] = leftInvalid ? 0 : 1;
            _sample[18] = leftInvalid ? 0 : gaze.left.forward.x;
            _sample[19] = leftInvalid ? 0 : gaze.left.forward.y;
            _sample[20] = leftInvalid ? 0 : gaze.left.forward.z;
            _sample[21] = leftInvalid ? 0 : gaze.left.origin.x;
            _sample[22] = leftInvalid ? 0 : gaze.left.origin.y;
            _sample[23] = leftInvalid ? 0 : gaze.left.origin.z;
            _sample[24] = leftInvalid ? 0 : eyeMeasurements.leftPupilIrisDiameterRatio;
            _sample[25] = leftInvalid ? 0 : eyeMeasurements.leftPupilDiameterInMM;
            _sample[26] = leftInvalid ? 0 : eyeMeasurements.leftIrisDiameterInMM;

            // Right eye.
            bool rightInvalid = gaze.rightStatus == VarjoEyeTracking.GazeEyeStatus.Invalid;
            _sample[27] = rightInvalid ? 0 : 1;
            _sample[28] = rightInvalid ? 0 : gaze.right.forward.x;
            _sample[29] = rightInvalid ? 0 : gaze.right.forward.y;
            _sample[30] = rightInvalid ? 0 : gaze.right.forward.z;
            _sample[31] = rightInvalid ? 0 : gaze.right.origin.x;
            _sample[32] = rightInvalid ? 0 : gaze.right.origin.y;
            _sample[33] = rightInvalid ? 0 : gaze.right.origin.z;
            _sample[34] = rightInvalid ? 0 : eyeMeasurements.rightPupilIrisDiameterRatio;
            _sample[35] = rightInvalid ? 0 : eyeMeasurements.rightPupilDiameterInMM;
            _sample[36] = rightInvalid ? 0 : eyeMeasurements.rightIrisDiameterInMM;

            // Focus.
            _sample[37] = invalid ? 0 : gaze.focusDistance;
            _sample[38] = invalid ? 0 : gaze.focusStability;

            // Back-date the LSL timestamp by the age of the sample: both GetVarjoTimestamp
            // and captureTime are Varjo-clock nanoseconds, so their difference maps the
            // capture instant into the LSL local clock domain.
            long timeDifference = VarjoTime.GetVarjoTimestamp() - gaze.captureTime;
            double timestamp = LSL.LSL.local_clock() - (double)timeDifference / 1000000000.0;
            _sample[1] = timestamp;

            _outlet.push_sample(_sample, timestamp);
        }

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
