using System;
using LSL;
using UnityEngine;

namespace NBackStreaming
{
    /// <summary>
    /// Streams every <see cref="NBackEvents"/> event to two LSL outlets:
    /// NBackMarkersLSL (1-channel string markers, for LabRecorder/debugging) and
    /// UNbackEventMarkerLSL (9-channel numeric events following the CLAd
    /// conventions: channel 0 carries Unity time for LSL&lt;-&gt;Unity clock-offset
    /// recovery, float32, nominal 75 Hz, empty source_id — PhysioLabXR cannot
    /// record string streams, so this is the stream its CLAd preset consumes).
    /// Both outlets receive the same <c>LSL.LSL.local_clock()</c> timestamp per event.
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
        private StreamOutlet _eventOutlet;
        private readonly string[] _markerSample = { string.Empty };
        private string _lastDisplayText = string.Empty;
        private MarkerBlockContext _blockContext = MarkerBlockContext.None;
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
            NBackEvents.EffortProbeStarted += HandleEffortProbeStarted;
            NBackEvents.EffortRated += HandleEffortRated;

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
            NBackEvents.EffortProbeStarted -= HandleEffortProbeStarted;
            NBackEvents.EffortRated -= HandleEffortRated;

            DisposeOutlets();
        }

        private void OnApplicationQuit() => DisposeOutlets();
        #endregion

        #region Handler
        private static float Now() => Time.realtimeSinceStartup;

        private void HandleSessionStarted(SessionStartedData d) =>
            Push(NBackMarkerFormat.Marker(d), NBackMarkerFormat.Event(d, Now()));

        private void HandleTaskStarted(TaskStartedData d)
        {
            // Every event until the next task_start belongs to this block.
            _blockContext = NBackMarkerFormat.BlockContextFrom(d);
            Push(NBackMarkerFormat.Marker(d), NBackMarkerFormat.Event(d, Now()));
        }

        private void HandleStimulusOnset(StimulusOnsetData d) =>
            Push(NBackMarkerFormat.Marker(d), NBackMarkerFormat.Event(d, Now(), _blockContext));

        private void HandleStimulusOffset(StimulusOffsetData d) =>
            Push(NBackMarkerFormat.Marker(d), NBackMarkerFormat.Event(d, Now(), _blockContext));

        private void HandleResponse(ResponseData d) =>
            Push(NBackMarkerFormat.Marker(d), NBackMarkerFormat.Event(d, Now(), _blockContext));

        private void HandleTrialResult(TrialResultData d) =>
            Push(NBackMarkerFormat.Marker(d), NBackMarkerFormat.Event(d, Now(), _blockContext));

        private void HandleTaskFinished(TaskFinishedData d) =>
            Push(NBackMarkerFormat.Marker(d), NBackMarkerFormat.Event(d, Now(), _blockContext));

        private void HandleSessionEnded() =>
            Push(NBackMarkerFormat.SessionEndMarker(), NBackMarkerFormat.SessionEndEvent(Now()));

        private void HandleDisplayChanged(DisplayChangedData d) =>
            Push(NBackMarkerFormat.Marker(d), NBackMarkerFormat.Event(d, Now(), _blockContext));

        private void HandleEffortProbeStarted(EffortProbeStartedData d) =>
            Push(NBackMarkerFormat.Marker(d), NBackMarkerFormat.Event(d, Now()));

        private void HandleEffortRated(EffortRatedData d) =>
            Push(NBackMarkerFormat.Marker(d), NBackMarkerFormat.Event(d, Now()));
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

                // CLAd conventions: float32, positive nominal rate (PhysioLabXR sizes
                // its viz buffer from the preset rate and has no string data type),
                // empty source_id like every CLAdVR outlet.
                var eventInfo = new StreamInfo(
                    "UNbackEventMarkerLSL",
                    "EventMarker",
                    NBackMarkerFormat.EventChannels,
                    75,
                    channel_format_t.cf_float32,
                    "");
                _eventOutlet = new StreamOutlet(eventInfo);
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

        private void Push(string marker, float[] eventSample)
        {
            if (!streamMarkers)
                return;

            if (_markerOutlet == null && _eventOutlet == null)
                return;

            double timestamp = LSL.LSL.local_clock();

            if (_markerOutlet != null)
            {
                // The native LSL string push crashes on null entries.
                _markerSample[0] = marker ?? string.Empty;
                _markerOutlet.push_sample(_markerSample, timestamp);
            }

            if (_eventOutlet != null && eventSample != null)
                _eventOutlet.push_sample(eventSample, timestamp);
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

            if (_eventOutlet != null)
            {
                try
                {
                    _eventOutlet.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[NBackMarkerStreamer] Error disposing event outlet: {e.Message}");
                }
                finally
                {
                    _eventOutlet = null;
                }
            }
        }
        #endregion
    }
}
