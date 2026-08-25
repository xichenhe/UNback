using System;
using UnityEngine;

namespace NBackStreaming
{
    /// <summary>
    /// Kind of task currently running in the n-back session.
    /// </summary>
    public enum TaskKind
    {
        Unknown = 0,
        Info = 1,
        Focus = 2,
        NBack = 3
    }

    /// <summary>
    /// Signal-detection classification of a trial or response.
    /// </summary>
    public enum TrialOutcomeKind
    {
        NA = 0,
        Hit = 1,
        Miss = 2,
        FalseAlarm = 3,
        CorrectRejection = 4
    }

    public struct SessionStartedData
    {
        public int TaskCount;
        public float UnityTime;
    }

    public struct TaskStartedData
    {
        public int TaskIndex;
        public TaskKind Kind;
        public int N;
        public int TrialCount;
        public float StimulusDuration;
        public float InterStimulusDuration;
        /// <summary>Concatenated letter sequence, e.g. "SPDXF..." (n-back only).</summary>
        public string Sequence;
        /// <summary>Ground-truth target vector as 0/1 string aligned with Sequence (n-back only).</summary>
        public string Targets;
        /// <summary>Instruction text (info task only).</summary>
        public string InfoText;
        /// <summary>Fixation duration in seconds (focus task only).</summary>
        public float FocusDuration;
    }

    public struct StimulusOnsetData
    {
        public int TrialIndex;
        public string Letter;
        public bool IsTarget;
        public int N;
    }

    public struct StimulusOffsetData
    {
        public int TrialIndex;
        public string Letter;
        public bool Responded;
    }

    public struct ResponseData
    {
        public TaskKind Kind;
        public int TrialIndex;
        public string Letter;
        public bool IsTarget;
        /// <summary>"stimulus", "isi" or "na".</summary>
        public string Phase;
        /// <summary>Reaction time from stimulus onset; valid only when HasRt.</summary>
        public float RtStimulus;
        /// <summary>Reaction time from stimulus onset including ISI presses; valid only when HasRt.</summary>
        public float RtTrial;
        public bool HasRt;
        /// <summary>Hit or FalseAlarm for n-back responses, NA otherwise.</summary>
        public TrialOutcomeKind Classification;
        /// <summary>1-based count of responses within the current trial (repeated presses).</summary>
        public int PressIndex;
    }

    public struct TrialResultData
    {
        public int TrialIndex;
        public string Letter;
        public bool IsTarget;
        public bool Responded;
        public TrialOutcomeKind Outcome;
    }

    public struct TaskFinishedData
    {
        public int TaskIndex;
        public TaskKind Kind;
        public int N;
        public int CorrectCount;
        public float ErrorRate;
    }

    public struct DisplayChangedData
    {
        /// <summary>True when text appeared/changed, false when the display went blank.</summary>
        public bool IsOn;
        public string Text;
        public string Previous;
    }

    /// <summary>
    /// Static event bus decoupling the n-back task logic from LSL streaming and file logging.
    /// Task classes raise events here; NBackMarkerStreamer and NBackEventFileLogger subscribe.
    /// All events are raised synchronously on the Unity main thread.
    /// </summary>
    public static class NBackEvents
    {
        public static event Action<SessionStartedData> SessionStarted;
        public static event Action<TaskStartedData> TaskStarted;
        public static event Action<StimulusOnsetData> StimulusOnset;
        public static event Action<StimulusOffsetData> StimulusOffset;
        public static event Action<ResponseData> ResponseRegistered;
        public static event Action<TrialResultData> TrialResult;
        public static event Action<TaskFinishedData> TaskFinished;
        public static event Action SessionEnded;
        public static event Action<DisplayChangedData> DisplayChanged;

        private static int _pressCountThisTrial;

        public static void RaiseSessionStarted(int taskCount)
        {
            SessionStarted?.Invoke(new SessionStartedData
            {
                TaskCount = taskCount,
                UnityTime = Time.realtimeSinceStartup
            });
        }

        /// <summary>
        /// Raised by TextTaskManager.PrepareTask. Builds the payload by inspecting the task type.
        /// </summary>
        public static void RaiseTaskStarted(int taskIndex, Task.ATask<string> task)
        {
            _pressCountThisTrial = 0;
            TaskStarted?.Invoke(BuildTaskStartedData(taskIndex, task));
        }

        /// <summary>
        /// Overload for task drivers that build the payload themselves
        /// (e.g. the tool based n-back, which has no ATask&lt;string&gt;).
        /// </summary>
        public static void RaiseTaskStarted(TaskStartedData data)
        {
            _pressCountThisTrial = 0;
            TaskStarted?.Invoke(data);
        }

        public static void RaiseStimulusOnset(int trialIndex, string letter, bool isTarget, int n)
        {
            _pressCountThisTrial = 0;
            StimulusOnset?.Invoke(new StimulusOnsetData
            {
                TrialIndex = trialIndex,
                Letter = letter,
                IsTarget = isTarget,
                N = n
            });
        }

        public static void RaiseStimulusOffset(int trialIndex, string letter, bool responded)
        {
            StimulusOffset?.Invoke(new StimulusOffsetData
            {
                TrialIndex = trialIndex,
                Letter = letter,
                Responded = responded
            });
        }

        public static void RaiseResponse(ResponseData data)
        {
            _pressCountThisTrial++;
            data.PressIndex = _pressCountThisTrial;
            ResponseRegistered?.Invoke(data);
        }

        public static void RaiseTrialResult(int trialIndex, string letter, bool isTarget, bool responded)
        {
            TrialResult?.Invoke(new TrialResultData
            {
                TrialIndex = trialIndex,
                Letter = letter,
                IsTarget = isTarget,
                Responded = responded,
                Outcome = Classify(responded, isTarget)
            });
        }

        public static void RaiseTaskFinished(int taskIndex, Task.ATask<string> task)
        {
            TaskFinished?.Invoke(BuildTaskFinishedData(taskIndex, task));
        }

        /// <summary>Overload for task drivers that build the payload themselves.</summary>
        public static void RaiseTaskFinished(TaskFinishedData data)
        {
            TaskFinished?.Invoke(data);
        }

        public static void RaiseSessionEnded()
        {
            SessionEnded?.Invoke();
        }

        public static void RaiseDisplayChanged(bool isOn, string text, string previous)
        {
            DisplayChanged?.Invoke(new DisplayChangedData
            {
                IsOn = isOn,
                Text = text,
                Previous = previous
            });
        }

        /// <summary>
        /// Signal-detection classification: hit / miss / false alarm / correct rejection.
        /// </summary>
        public static TrialOutcomeKind Classify(bool responded, bool isTarget)
        {
            if (responded && isTarget) return TrialOutcomeKind.Hit;
            if (responded && !isTarget) return TrialOutcomeKind.FalseAlarm;
            if (!responded && isTarget) return TrialOutcomeKind.Miss;
            return TrialOutcomeKind.CorrectRejection;
        }

        private static TaskStartedData BuildTaskStartedData(int taskIndex, Task.ATask<string> task)
        {
            var data = new TaskStartedData { TaskIndex = taskIndex, Kind = TaskKind.Unknown, N = -1 };

            if (task is Task.NBackTask nBackTask)
            {
                data.Kind = TaskKind.NBack;
                data.N = nBackTask.N;
                data.TrialCount = nBackTask.CharacterSet.Length;
                data.StimulusDuration = nBackTask.StimulusDuration;
                data.InterStimulusDuration = nBackTask.InterStimulusDuration;

                var sequence = new System.Text.StringBuilder(nBackTask.CharacterSet.Length);
                var targets = new System.Text.StringBuilder(nBackTask.CharacterSet.Length);
                for (int i = 0; i < nBackTask.CharacterSet.Length; i++)
                {
                    sequence.Append(nBackTask.CharacterSet[i]);
                    targets.Append(nBackTask.IsTargetAt(i) ? '1' : '0');
                }
                data.Sequence = sequence.ToString();
                data.Targets = targets.ToString();
            }
            else if (task is Task.FocusTask focusTask)
            {
                data.Kind = TaskKind.Focus;
                data.FocusDuration = focusTask.StimulusDuration;
            }
            else if (task is Task.InfoTask infoTask)
            {
                data.Kind = TaskKind.Info;
                data.InfoText = infoTask.Text;
            }

            return data;
        }

        private static TaskFinishedData BuildTaskFinishedData(int taskIndex, Task.ATask<string> task)
        {
            var data = new TaskFinishedData { TaskIndex = taskIndex, Kind = TaskKind.Unknown, N = -1 };

            if (task is Task.NBackTask nBackTask)
            {
                data.Kind = TaskKind.NBack;
                data.N = nBackTask.N;
                data.CorrectCount = nBackTask.NumberOfCorrectAnswers;
                data.ErrorRate = nBackTask.ErrorRate;
            }
            else if (task is Task.FocusTask)
            {
                data.Kind = TaskKind.Focus;
            }
            else if (task is Task.InfoTask)
            {
                data.Kind = TaskKind.Info;
            }

            return data;
        }
    }

    /// <summary>
    /// Shared formatting of events into LSL marker strings and numeric event-code samples.
    /// Marker format: "event_name|key=value|key=value". See EXPERIMENTER_GUIDE.md for the dictionary.
    /// </summary>
    public static class NBackMarkerFormat
    {
        public const int CodeSessionStart = 1;
        public const int CodeTaskStart = 2;
        public const int CodeStimOn = 3;
        public const int CodeStimOff = 4;
        public const int CodeResponse = 5;
        public const int CodeTrialResult = 6;
        public const int CodeTaskEnd = 7;
        public const int CodeSessionEnd = 8;
        public const int CodeDisplayOn = 9;
        public const int CodeDisplayOff = 10;

        /// <summary>Number of channels in the NBackEventCodesLSL stream.</summary>
        public const int CodeChannels = 6;

        public static string KindName(TaskKind kind)
        {
            switch (kind)
            {
                case TaskKind.Info: return "info";
                case TaskKind.Focus: return "focus";
                case TaskKind.NBack: return "nback";
                default: return "unknown";
            }
        }

        public static string OutcomeName(TrialOutcomeKind outcome)
        {
            switch (outcome)
            {
                case TrialOutcomeKind.Hit: return "hit";
                case TrialOutcomeKind.Miss: return "miss";
                case TrialOutcomeKind.FalseAlarm: return "false_alarm";
                case TrialOutcomeKind.CorrectRejection: return "correct_rejection";
                default: return "na";
            }
        }

        /// <summary>Free text may not contain the field separator; sanitize before embedding.</summary>
        public static string Sanitize(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("|", "/").Replace("\n", " ").Replace("\r", " ");
        }

        /// <summary>Culture-invariant float formatting so markers never contain comma decimals.</summary>
        private static string F(float value, string format) =>
            value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);

        public static string Marker(SessionStartedData d) =>
            $"session_start|tasks={d.TaskCount}|unity_time={F(d.UnityTime, "F3")}";

        public static string Marker(TaskStartedData d)
        {
            switch (d.Kind)
            {
                case TaskKind.NBack:
                    return $"task_start|index={d.TaskIndex}|type=nback|n={d.N}|trials={d.TrialCount}" +
                           $"|stim_dur={F(d.StimulusDuration, "F3")}|isi={F(d.InterStimulusDuration, "F3")}" +
                           $"|sequence={Sanitize(d.Sequence)}|targets={d.Targets}";
                case TaskKind.Focus:
                    return $"task_start|index={d.TaskIndex}|type=focus|duration={F(d.FocusDuration, "F1")}";
                case TaskKind.Info:
                    return $"task_start|index={d.TaskIndex}|type=info|text={Sanitize(d.InfoText)}";
                default:
                    return $"task_start|index={d.TaskIndex}|type=unknown";
            }
        }

        public static string Marker(StimulusOnsetData d) =>
            $"stim_on|trial={d.TrialIndex}|letter={Sanitize(d.Letter)}|target={(d.IsTarget ? 1 : 0)}|n={d.N}";

        public static string Marker(StimulusOffsetData d) =>
            $"stim_off|trial={d.TrialIndex}|letter={Sanitize(d.Letter)}|responded={(d.Responded ? 1 : 0)}";

        public static string Marker(ResponseData d)
        {
            string rtStim = d.HasRt ? F(d.RtStimulus, "F3") : "na";
            string rtTrial = d.HasRt ? F(d.RtTrial, "F3") : "na";
            string cls = d.Classification == TrialOutcomeKind.NA ? "na" : OutcomeName(d.Classification);
            return $"response|task_type={KindName(d.Kind)}|trial={d.TrialIndex}|letter={Sanitize(d.Letter)}" +
                   $"|target={(d.IsTarget ? 1 : 0)}|class={cls}|rt_stim={rtStim}|rt_trial={rtTrial}" +
                   $"|phase={d.Phase}|press={d.PressIndex}";
        }

        public static string Marker(TrialResultData d) =>
            $"trial_result|trial={d.TrialIndex}|letter={Sanitize(d.Letter)}|target={(d.IsTarget ? 1 : 0)}" +
            $"|responded={(d.Responded ? 1 : 0)}|outcome={OutcomeName(d.Outcome)}";

        public static string Marker(TaskFinishedData d) =>
            $"task_end|index={d.TaskIndex}|type={KindName(d.Kind)}|n={d.N}|correct={d.CorrectCount}|error_rate={F(d.ErrorRate, "F3")}";

        public static string SessionEndMarker() =>
            $"session_end|unity_time={F(Time.realtimeSinceStartup, "F3")}";

        public static string Marker(DisplayChangedData d) =>
            d.IsOn
                ? $"display_on|text={Sanitize(d.Text)}"
                : $"display_off|prev={Sanitize(d.Previous)}";

        // --- Numeric event-code samples for NBackEventCodesLSL ---
        // Channels: [0] eventCode, [1] trialIndex, [2] isTarget, [3] outcomeCode, [4] rtMs, [5] n.
        // -1 encodes "not applicable" on channels 1-5.

        public static float[] Code(int eventCode, float trial = -1f, float isTarget = -1f,
            float outcome = 0f, float rtMs = -1f, float n = -1f) =>
            new[] { eventCode, trial, isTarget, outcome, rtMs, n };

        public static float[] Code(SessionStartedData d) => Code(CodeSessionStart);

        public static float[] Code(TaskStartedData d) =>
            Code(CodeTaskStart, n: d.Kind == TaskKind.NBack ? d.N : -1f);

        public static float[] Code(StimulusOnsetData d) =>
            Code(CodeStimOn, d.TrialIndex, d.IsTarget ? 1f : 0f, n: d.N);

        public static float[] Code(StimulusOffsetData d) =>
            Code(CodeStimOff, d.TrialIndex, isTarget: -1f);

        public static float[] Code(ResponseData d) =>
            Code(CodeResponse, d.TrialIndex, d.IsTarget ? 1f : 0f, (float)d.Classification,
                d.HasRt ? d.RtStimulus * 1000f : -1f);

        public static float[] Code(TrialResultData d) =>
            Code(CodeTrialResult, d.TrialIndex, d.IsTarget ? 1f : 0f, (float)d.Outcome);

        public static float[] Code(TaskFinishedData d) =>
            Code(CodeTaskEnd, n: d.Kind == TaskKind.NBack ? d.N : -1f);

        public static float[] SessionEndCode() => Code(CodeSessionEnd);

        public static float[] Code(DisplayChangedData d) =>
            Code(d.IsOn ? CodeDisplayOn : CodeDisplayOff);
    }
}
