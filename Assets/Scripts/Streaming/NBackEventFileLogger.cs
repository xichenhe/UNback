using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace NBackStreaming
{
    /// <summary>
    /// Writes every <see cref="NBackEvents"/> event (including display truth) as
    /// one JSON line (JSONL) to
    /// <c>Application.persistentDataPath/NBackSessions/Session_yyyyMMdd_HHmmss.jsonl</c>.
    /// Each line carries the envelope fields <c>event</c>, <c>iso</c> (local ISO 8601),
    /// <c>unity_time</c>, <c>lsl_time</c> (JSON null when liblsl is unavailable) and
    /// <c>marker</c> (the LSL marker string), followed by event-specific fields.
    /// The writer flushes after every line so data survives crashes.
    /// Works independently of LSL streaming.
    /// </summary>
    public class NBackEventFileLogger : MonoBehaviour
    {
        /// <summary>
        /// Optional log directory override. Empty uses
        /// <c>Application.persistentDataPath/NBackSessions</c>.
        /// </summary>
        public string customLogDirectory = string.Empty;

        #region Private variables
        private StreamWriter _writer;
        private bool _lslClockFailed;
        #endregion

        #region Unity methods
        private void Awake()
        {
            OpenLogFile();

            NBackEvents.SessionStarted += HandleSessionStarted;
            NBackEvents.TaskStarted += HandleTaskStarted;
            NBackEvents.StimulusOnset += HandleStimulusOnset;
            NBackEvents.StimulusOffset += HandleStimulusOffset;
            NBackEvents.ResponseRegistered += HandleResponse;
            NBackEvents.TrialResult += HandleTrialResult;
            NBackEvents.TaskFinished += HandleTaskFinished;
            NBackEvents.SessionEnded += HandleSessionEnded;
            NBackEvents.DisplayChanged += HandleDisplayChanged;
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

            CloseLogFile();
        }

        private void OnApplicationQuit() => CloseLogFile();
        #endregion

        #region Handler
        private void HandleSessionStarted(SessionStartedData d)
        {
            WriteEvent("session_start", NBackMarkerFormat.Marker(d), sb =>
            {
                AppendInt(sb, "tasks", d.TaskCount);
            });
        }

        private void HandleTaskStarted(TaskStartedData d)
        {
            WriteEvent("task_start", NBackMarkerFormat.Marker(d), sb =>
            {
                AppendInt(sb, "index", d.TaskIndex);
                AppendString(sb, "type", NBackMarkerFormat.KindName(d.Kind));

                switch (d.Kind)
                {
                    case TaskKind.NBack:
                        AppendInt(sb, "n", d.N);
                        AppendInt(sb, "trials", d.TrialCount);
                        AppendFloat(sb, "stim_dur", d.StimulusDuration);
                        AppendFloat(sb, "isi", d.InterStimulusDuration);
                        AppendString(sb, "sequence", d.Sequence);
                        AppendString(sb, "targets", d.Targets);
                        break;
                    case TaskKind.Info:
                        AppendString(sb, "text", d.InfoText);
                        break;
                    case TaskKind.Focus:
                        AppendFloat(sb, "duration", d.FocusDuration);
                        break;
                }
            });
        }

        private void HandleStimulusOnset(StimulusOnsetData d)
        {
            WriteEvent("stim_on", NBackMarkerFormat.Marker(d), sb =>
            {
                AppendInt(sb, "trial", d.TrialIndex);
                AppendString(sb, "letter", d.Letter);
                AppendBool(sb, "target", d.IsTarget);
                AppendInt(sb, "n", d.N);
            });
        }

        private void HandleStimulusOffset(StimulusOffsetData d)
        {
            WriteEvent("stim_off", NBackMarkerFormat.Marker(d), sb =>
            {
                AppendInt(sb, "trial", d.TrialIndex);
                AppendString(sb, "letter", d.Letter);
                AppendBool(sb, "responded", d.Responded);
            });
        }

        private void HandleResponse(ResponseData d)
        {
            WriteEvent("response", NBackMarkerFormat.Marker(d), sb =>
            {
                AppendString(sb, "task_type", NBackMarkerFormat.KindName(d.Kind));
                AppendInt(sb, "trial", d.TrialIndex);
                AppendString(sb, "letter", d.Letter);
                AppendBool(sb, "target", d.IsTarget);
                AppendString(sb, "class", NBackMarkerFormat.OutcomeName(d.Classification));

                if (d.HasRt)
                {
                    AppendFloat(sb, "rt_stim", d.RtStimulus);
                    AppendFloat(sb, "rt_trial", d.RtTrial);
                }
                else
                {
                    AppendNull(sb, "rt_stim");
                    AppendNull(sb, "rt_trial");
                }

                AppendString(sb, "phase", d.Phase);
                AppendInt(sb, "press", d.PressIndex);
            });
        }

        private void HandleTrialResult(TrialResultData d)
        {
            WriteEvent("trial_result", NBackMarkerFormat.Marker(d), sb =>
            {
                AppendInt(sb, "trial", d.TrialIndex);
                AppendString(sb, "letter", d.Letter);
                AppendBool(sb, "target", d.IsTarget);
                AppendBool(sb, "responded", d.Responded);
                AppendString(sb, "outcome", NBackMarkerFormat.OutcomeName(d.Outcome));
            });
        }

        private void HandleTaskFinished(TaskFinishedData d)
        {
            WriteEvent("task_end", NBackMarkerFormat.Marker(d), sb =>
            {
                AppendInt(sb, "index", d.TaskIndex);
                AppendString(sb, "type", NBackMarkerFormat.KindName(d.Kind));
                AppendInt(sb, "n", d.N);
                AppendInt(sb, "correct", d.CorrectCount);
                AppendFloat(sb, "error_rate", d.ErrorRate);
            });
        }

        private void HandleSessionEnded() =>
            WriteEvent("session_end", NBackMarkerFormat.SessionEndMarker(), null);

        private void HandleDisplayChanged(DisplayChangedData d)
        {
            WriteEvent(d.IsOn ? "display_on" : "display_off", NBackMarkerFormat.Marker(d), sb =>
            {
                AppendBool(sb, "is_on", d.IsOn);
                AppendString(sb, "text", d.Text);
                AppendString(sb, "prev", d.Previous);
            });
        }
        #endregion

        #region Private methods
        private void OpenLogFile()
        {
            try
            {
                string directory = string.IsNullOrEmpty(customLogDirectory)
                    ? Path.Combine(Application.persistentDataPath, "NBackSessions")
                    : customLogDirectory;
                Directory.CreateDirectory(directory);

                string fileName = "Session_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".jsonl";
                string path = Path.Combine(directory, fileName);
                _writer = new StreamWriter(path, false, new UTF8Encoding(false));

                WriteEvent("log_opened", null, sb =>
                {
                    AppendString(sb, "app", "uNback");
                    AppendString(sb, "unity_version", Application.unityVersion);
                    AppendString(sb, "platform", Application.platform.ToString());
                    AppendString(sb, "file", path);
                });

                Debug.Log($"[NBackEventFileLogger] Logging events to {path}");
            }
            catch (Exception e)
            {
                _writer = null;
                Debug.LogError($"[NBackEventFileLogger] Could not open log file; event file logging disabled: {e.Message}");
            }
        }

        private void CloseLogFile()
        {
            if (_writer == null)
                return;

            WriteEvent("log_closed", null, null);

            try
            {
                _writer.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NBackEventFileLogger] Error closing log file: {e.Message}");
            }
            finally
            {
                _writer = null;
            }
        }

        /// <summary>
        /// Writes one JSONL line: envelope (event/iso/unity_time/lsl_time/marker)
        /// plus event-specific fields appended by <paramref name="appendFields"/>.
        /// </summary>
        private void WriteEvent(string eventName, string marker, Action<StringBuilder> appendFields)
        {
            if (_writer == null)
                return;

            var sb = new StringBuilder(256);
            sb.Append("{\"event\":\"").Append(Escape(eventName)).Append('"');
            sb.Append(",\"iso\":\"").Append(DateTime.Now.ToString("o", CultureInfo.InvariantCulture)).Append('"');
            sb.Append(",\"unity_time\":").Append(Time.realtimeSinceStartupAsDouble.ToString("F4", CultureInfo.InvariantCulture));

            double? lslTime = TryGetLslTime();
            sb.Append(",\"lsl_time\":").Append(lslTime.HasValue
                ? lslTime.Value.ToString("F6", CultureInfo.InvariantCulture)
                : "null");

            if (marker != null)
                sb.Append(",\"marker\":\"").Append(Escape(marker)).Append('"');

            appendFields?.Invoke(sb);
            sb.Append('}');

            try
            {
                _writer.WriteLine(sb.ToString());
                _writer.Flush();
            }
            catch (Exception e)
            {
                Debug.LogError($"[NBackEventFileLogger] Write failed; event file logging disabled: {e.Message}");
                try
                {
                    _writer.Dispose();
                }
                catch (Exception)
                {
                    // Writer is already unusable; nothing left to release.
                }
                _writer = null;
            }
        }

        private double? TryGetLslTime()
        {
            if (_lslClockFailed)
                return null;

            try
            {
                return LSL.LSL.local_clock();
            }
            catch (Exception)
            {
                _lslClockFailed = true;
                return null;
            }
        }

        private static void AppendString(StringBuilder sb, string key, string value) =>
            sb.Append(",\"").Append(key).Append("\":\"").Append(Escape(value)).Append('"');

        private static void AppendInt(StringBuilder sb, string key, int value) =>
            sb.Append(",\"").Append(key).Append("\":").Append(value.ToString(CultureInfo.InvariantCulture));

        private static void AppendFloat(StringBuilder sb, string key, float value) =>
            sb.Append(",\"").Append(key).Append("\":").Append(value.ToString("F3", CultureInfo.InvariantCulture));

        private static void AppendBool(StringBuilder sb, string key, bool value) =>
            sb.Append(",\"").Append(key).Append("\":").Append(value ? "true" : "false");

        private static void AppendNull(StringBuilder sb, string key) =>
            sb.Append(",\"").Append(key).Append("\":null");

        private static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var sb = new StringBuilder(text.Length + 8);
            foreach (char c in text)
            {
                switch (c)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (c < ' ')
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
        #endregion
    }
}
