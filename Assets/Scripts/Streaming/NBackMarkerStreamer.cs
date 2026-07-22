using System;
using LSL;
using UnityEngine;

namespace NBackStreaming
{
    /// <summary>
    /// Streams every <see cref="NBackEvents"/> event to two LSL outlets:
    /// NBackMarkersLSL (1-channel string markers) and NBackEventCodesLSL
    /// (6-channel numeric event codes). Both outlets receive the same
    /// <c>LSL.LSL.local_clock()</c> timestamp per event.
    /// Additionally, a display-truth monitor in <see cref="LateUpdate"/> diffs the
    /// task display text after the frame's task logic has run and raises
    /// display_on / display_off events (mirrored through
    /// <see cref="NBackEvents.RaiseDisplayChanged"/> so the file logger sees them;
    /// the LSL push itself happens once, in the DisplayChanged handler).
    /// If the native liblsl library is unavailable the component stays alive so
    /// events keep flowing to <see cref="NBackEventFileLogger"/>.
    /// </summary>
    public class NBackMarkerStreamer : MonoBehaviour
    {
        /// <summary>
        /// Master switch for pushing LSL samples. Events are still raised and
        /// display truth is still monitored when disabled.
        /// </summary>
        public bool streamMarkers = true;

        /// <summary>
        /// Task manager whose display text is monitored for display-truth markers.
        /// Found automatically in <see cref="Start"/> when left unassigned.
        /// </summary>
        public Task.TaskManagerBehaviour taskManager;

        #region Private variables
        private StreamOutlet _markerOutlet;
        private StreamOutlet _codeOutlet;
        private readonly string[] _markerSample = { string.Empty };
        private string _lastDisplayText = string.Empty;
        #endregion

        #region Unity methods
        private void Awake()
        {
            NBackEvents.SessionStarted += HandleSessionStarted;
            NBackEvents.TaskStarted += HandleTaskStarted;
            NBackEvents.StimulusOnset += HandleStimulusOnset;
            NBackEvents.StimulusOffset += HandleStimulusOffset;
            NBackEvents.ResponseRegistered += HandleResponse;
            NBackEvents.TrialResult += HandleTrialResult;
            NBackEvents.TaskFinished += HandleTaskFinished;
            NBackEvents.SessionEnded += HandleSessionEnded;
            NBackEvents.DisplayChanged += HandleDisplayChanged;

            CreateOutlets();
        }

        private void Start()
        {
            if (taskManager == null)
                taskManager = FindFirstObjectByType<Task.TaskManagerBehaviour>();

            if (taskManager == null)
                Debug.LogWarning("[NBackMarkerStreamer] No TaskManagerBehaviour found; display-truth markers are disabled.");
        }

        private void LateUpdate()
        {
            if (taskManager == null || taskManager.DisplayText == null)
                return;

            string current = taskManager.DisplayText.text ?? string.Empty;
            if (current == _lastDisplayText)
                return;

            string previous = _lastDisplayText;
            _lastDisplayText = current;

            // The LSL push happens exactly once, in HandleDisplayChanged; raising
            // the event here also lets the file logger record display truth.
            NBackEvents.RaiseDisplayChanged(!string.IsNullOrEmpty(current), current, previous);
        }

        private void OnDestroy()
        {
            NBackEvents.SessionStarted -= HandleSessionStarted;
            NBackEvents.TaskStarted -= HandleTaskStarted;
            NBackEvents.StimulusOnset -= HandleStimulusOnset;
            NBackEvents.StimulusOffset -= HandleStimulusOffset;
            NBackEvents.ResponseRegistered -= HandleResponse;
            NBackEvents.TrialResult -= HandleTrialResult;
            NBackEvents.TaskFinished -= HandleTaskFinished;
            NBackEvents.SessionEnded -= HandleSessionEnded;
            NBackEvents.DisplayChanged -= HandleDisplayChanged;

            DisposeOutlets();
        }

        private void OnApplicationQuit() => DisposeOutlets();
        #endregion

        #region Handler
        private void HandleSessionStarted(SessionStartedData d) =>
            Push(NBackMarkerFormat.Marker(d), NBackMarkerFormat.Code(d));

        private void HandleTaskStarted(TaskStartedData d) =>
            Push(NBackMarkerFormat.Marker(d), NBackMarkerFormat.Code(d));

        private void HandleStimulusOnset(StimulusOnsetData d) =>
            Push(NBackMarkerFormat.Marker(d), NBackMarkerFormat.Code(d));

        private void HandleStimulusOffset(StimulusOffsetData d) =>
            Push(NBackMarkerFormat.Marker(d), NBackMarkerFormat.Code(d));

        private void HandleResponse(ResponseData d) =>
            Push(NBackMarkerFormat.Marker(d), NBackMarkerFormat.Code(d));

        private void HandleTrialResult(TrialResultData d) =>
            Push(NBackMarkerFormat.Marker(d), NBackMarkerFormat.Code(d));

        private void HandleTaskFinished(TaskFinishedData d) =>
            Push(NBackMarkerFormat.Marker(d), NBackMarkerFormat.Code(d));

        private void HandleSessionEnded() =>
            Push(NBackMarkerFormat.SessionEndMarker(), NBackMarkerFormat.SessionEndCode());

        private void HandleDisplayChanged(DisplayChangedData d) =>
            Push(NBackMarkerFormat.Marker(d), NBackMarkerFormat.Code(d));
        #endregion

        #region Private methods
        private void CreateOutlets()
        {
            try
            {
                var markerInfo = new StreamInfo(
                    "NBackMarkersLSL",
                    "Markers",
                    1,
                    0,
                    channel_format_t.cf_string,
                    "uNback-markers");
                _markerOutlet = new StreamOutlet(markerInfo);

                var codeInfo = new StreamInfo(
                    "NBackEventCodesLSL",
                    "Markers",
                    NBackMarkerFormat.CodeChannels,
                    0,
                    channel_format_t.cf_float32,
                    "uNback-eventcodes");
                _codeOutlet = new StreamOutlet(codeInfo);
            }
            catch (DllNotFoundException e)
            {
                DisposeOutlets();
                Debug.LogError($"[NBackMarkerStreamer] liblsl native library not found; LSL marker streaming disabled (events are still file-logged): {e.Message}");
            }
            catch (Exception e) when (e.InnerException is DllNotFoundException)
            {
                DisposeOutlets();
                Debug.LogError($"[NBackMarkerStreamer] liblsl native library not found; LSL marker streaming disabled (events are still file-logged): {e.InnerException.Message}");
            }
            catch (Exception e)
            {
                DisposeOutlets();
                Debug.LogError($"[NBackMarkerStreamer] Failed to create LSL outlets; LSL marker streaming disabled (events are still file-logged): {e}");
            }
        }

        private void Push(string marker, float[] code)
        {
            if (!streamMarkers)
                return;

            if (_markerOutlet == null && _codeOutlet == null)
                return;

            double timestamp = LSL.LSL.local_clock();

            if (_markerOutlet != null)
            {
                // The native LSL string push crashes on null entries.
                _markerSample[0] = marker ?? string.Empty;
                _markerOutlet.push_sample(_markerSample, timestamp);
            }

            if (_codeOutlet != null && code != null)
                _codeOutlet.push_sample(code, timestamp);
        }

        private void DisposeOutlets()
        {
            if (_markerOutlet != null)
            {
                try
                {
                    _markerOutlet.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[NBackMarkerStreamer] Error disposing marker outlet: {e.Message}");
                }
                finally
                {
                    _markerOutlet = null;
                }
            }

            if (_codeOutlet != null)
            {
                try
                {
                    _codeOutlet.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[NBackMarkerStreamer] Error disposing code outlet: {e.Message}");
                }
                finally
                {
                    _codeOutlet = null;
                }
            }
        }
        #endregion
    }
}
