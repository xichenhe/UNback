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
        NBack = 3,
        /// <summary>Rest baseline block of a multi-block tool n-back session.</summary>
        Rest = 4
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
        /// <summary>Fixation/rest duration in seconds (focus and rest tasks only).</summary>
        public float FocusDuration;
        /// <summary>True for practice n-back blocks (feedback allowed, excluded from analysis).</summary>
        public bool Practice;
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

    /// <summary>The single-item effort rating appeared after a block.</summary>
    public struct EffortProbeStartedData
    {
        /// <summary>Index of the rated block.</summary>
        public int TaskIndex;
        /// <summary>n of the rated block.</summary>
        public int N;
        public int ScaleMin;
        public int ScaleMax;
        /// <summary>True when the rated block is the practice stage.</summary>
        public bool Practice;
    }

    /// <summary>The single-item effort rating closed, answered or timed out.</summary>
    public struct EffortRatedData
    {
        /// <summary>Index of the rated block.</summary>
        public int TaskIndex;
        /// <summary>n of the rated block.</summary>
        public int N;
        public int ScaleMin;
        public int ScaleMax;
        /// <summary>The chosen number; 0 = no answer (timed out).</summary>
        public int Rating;
        /// <summary>Seconds from the scale appearing to the answer; negative without an answer.</summary>
        public float ResponseTime;
        public bool TimedOut;
        /// <summary>True when the rated block is the practice stage.</summary>
        public bool Practice;
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
        public static event Action<EffortProbeStartedData> EffortProbeStarted;
        public static event Action<EffortRatedData> EffortRated;

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

        public static void RaiseEffortProbeStarted(EffortProbeStartedData data)
        {
            EffortProbeStarted?.Invoke(data);
        }

        public static void RaiseEffortRated(EffortRatedData data)
        {
            EffortRated?.Invoke(data);
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
        public const int CodeEffortProbe = 11;
        public const int CodeEffortRating = 12;

        /// <summary>Number of channels in the NBackEventCodesLSL stream.</summary>
        public const int CodeChannels = 6;

        public static string KindName(TaskKind kind)
        {
            switch (kind)
            {
                case TaskKind.Info: return "info";
                case TaskKind.Focus: return "focus";
                case TaskKind.NBack: return "nback";
                case TaskKind.Rest: return "rest";
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
                    // The practice field is appended only when set, so markers of regular
                    // blocks (and the text task) keep the exact historical format.
                    return $"task_start|index={d.TaskIndex}|type=nback|n={d.N}|trials={d.TrialCount}" +
                           $"|stim_dur={F(d.StimulusDuration, "F3")}|isi={F(d.InterStimulusDuration, "F3")}" +
                           $"|sequence={Sanitize(d.Sequence)}|targets={d.Targets}" +
                           (d.Practice ? "|practice=1" : string.Empty);
                case TaskKind.Focus:
                    return $"task_start|index={d.TaskIndex}|type=focus|duration={F(d.FocusDuration, "F1")}";
                case TaskKind.Rest:
                    return $"task_start|index={d.TaskIndex}|type=rest|duration={F(d.FocusDuration, "F1")}";
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

        public static string Marker(EffortProbeStartedData d) =>
            $"effort_probe|index={d.TaskIndex}|n={d.N}|scale={d.ScaleMin}-{d.ScaleMax}" +
            (d.Practice ? "|practice=1" : string.Empty);

        public static string Marker(EffortRatedData d)
        {
            bool answered = d.Rating > 0;
            string rating = answered ? d.Rating.ToString(System.Globalization.CultureInfo.InvariantCulture) : "na";
            string rt = answered && d.ResponseTime >= 0f ? F(d.ResponseTime, "F3") : "na";
            return $"effort_rating|index={d.TaskIndex}|n={d.N}|rating={rating}|rt={rt}" +
                   $"|scale={d.ScaleMin}-{d.ScaleMax}|timeout={(d.TimedOut ? 1 : 0)}" +
                   (d.Practice ? "|practice=1" : string.Empty);
        }

        // --- Numeric event samples for UNbackEventMarkerLSL (CLAd-compatible conventions:
        // channel 0 = Unity time for LSL<->Unity clock-offset recovery, float32, source_id "").
        // Channels: [0] unityTime, [1] eventCode, [2] blockIndex, [3] trialIndex,
        // [4] isTarget, [5] outcomeCode, [6] rtMs, [7] n, [8] blockKind.
        // -1 encodes "not applicable"; blockKind: 0 rest, 1 practice, 2 recorded n-back, -1 other.

        /// <summary>Number of channels in the UNbackEventMarkerLSL stream.</summary>
        public const int EventChannels = 9;

        public const float BlockKindRest = 0f;
        public const float BlockKindPractice = 1f;
        public const float BlockKindTask = 2f;
        public const float BlockKindNA = -1f;

        /// <summary>
        /// Derives the block context a task_start establishes; the marker streamer keeps
        /// it and stamps it on every following event sample of that block.
        /// </summary>
        public static MarkerBlockContext BlockContextFrom(TaskStartedData d)
        {
            float kind;
            switch (d.Kind)
            {
                case TaskKind.Rest: kind = BlockKindRest; break;
                case TaskKind.NBack: kind = d.Practice ? BlockKindPractice : BlockKindTask; break;
                default: kind = BlockKindNA; break;
            }

            return new MarkerBlockContext { BlockIndex = d.TaskIndex, BlockKind = kind };
        }

        public static float[] Event(int eventCode, float unityTime, MarkerBlockContext ctx,
            float trial = -1f, float isTarget = -1f, float outcome = 0f, float rtMs = -1f, float n = -1f) =>
            new[] { unityTime, eventCode, ctx.BlockIndex, trial, isTarget, outcome, rtMs, n, ctx.BlockKind };

        public static float[] Event(SessionStartedData d, float unityTime) =>
            Event(CodeSessionStart, unityTime, MarkerBlockContext.None);

        public static float[] Event(TaskStartedData d, float unityTime) =>
            Event(CodeTaskStart, unityTime, BlockContextFrom(d),
                n: d.Kind == TaskKind.NBack ? d.N : -1f);

        public static float[] Event(StimulusOnsetData d, float unityTime, MarkerBlockContext ctx) =>
            Event(CodeStimOn, unityTime, ctx, d.TrialIndex, d.IsTarget ? 1f : 0f, n: d.N);

        public static float[] Event(StimulusOffsetData d, float unityTime, MarkerBlockContext ctx) =>
            Event(CodeStimOff, unityTime, ctx, d.TrialIndex);

        public static float[] Event(ResponseData d, float unityTime, MarkerBlockContext ctx) =>
            Event(CodeResponse, unityTime, ctx, d.TrialIndex, d.IsTarget ? 1f : 0f,
                (float)d.Classification, d.HasRt ? d.RtStimulus * 1000f : -1f);

        public static float[] Event(TrialResultData d, float unityTime, MarkerBlockContext ctx) =>
            Event(CodeTrialResult, unityTime, ctx, d.TrialIndex, d.IsTarget ? 1f : 0f, (float)d.Outcome);

        public static float[] Event(TaskFinishedData d, float unityTime, MarkerBlockContext ctx) =>
            Event(CodeTaskEnd, unityTime, ctx, n: d.Kind == TaskKind.NBack ? d.N : -1f);

        public static float[] SessionEndEvent(float unityTime) =>
            Event(CodeSessionEnd, unityTime, MarkerBlockContext.None);

        public static float[] Event(DisplayChangedData d, float unityTime, MarkerBlockContext ctx) =>
            Event(d.IsOn ? CodeDisplayOn : CodeDisplayOff, unityTime, ctx);

        // Ratings carry their own block context — the rated block — so they stay correct
        // even when practice was ended before its first round started.
        public static float[] Event(EffortProbeStartedData d, float unityTime) =>
            Event(CodeEffortProbe, unityTime, RatedBlockContext(d.TaskIndex, d.Practice), n: d.N);

        /// <summary>Channel 5 carries the rating (0 = none), channel 6 its response time in ms.</summary>
        public static float[] Event(EffortRatedData d, float unityTime)
        {
            bool answered = d.Rating > 0;
            return Event(CodeEffortRating, unityTime, RatedBlockContext(d.TaskIndex, d.Practice),
                outcome: answered ? d.Rating : 0f,
                rtMs: answered && d.ResponseTime >= 0f ? d.ResponseTime * 1000f : -1f,
                n: d.N);
        }

        private static MarkerBlockContext RatedBlockContext(int taskIndex, bool practice) =>
            new MarkerBlockContext { BlockIndex = taskIndex, BlockKind = practice ? BlockKindPractice : BlockKindTask };
    }

    /// <summary>
    /// Block identity stamped on UNbackEventMarkerLSL samples: which plan entry is
    /// running and whether it is a rest baseline, practice, or recorded task block.
    /// </summary>
    public struct MarkerBlockContext
    {
        public float BlockIndex;
        public float BlockKind;

        public static readonly MarkerBlockContext None =
            new MarkerBlockContext { BlockIndex = -1f, BlockKind = -1f };
    }
}
