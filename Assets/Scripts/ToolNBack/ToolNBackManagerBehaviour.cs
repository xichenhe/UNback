using System;
using System.Collections.Generic;
using System.Linq;
using NBackStreaming;
using UnityEngine;
using UnityEngine.UI;

namespace ToolNBack
{
    /// <summary>
    /// Drives the tool based n-back calibration task: shows machining tool prefabs
    /// (from the _MITmech tool board) at pseudo-random positions on a curved fan
    /// panel around the user, like a VR virtual desktop. The experimenter selects
    /// the working-memory (WM) load level (Low / Optimal / High) in the inspector; the task
    /// publishes the same LSL markers as the text based n-back via NBackEvents.
    /// </summary>
    public class ToolNBackManagerBehaviour : MonoBehaviour
    {
        /// <summary>Resources path of the _MITmech tool board tool prefabs.</summary>
        public const string ToolsResourcePath = "Prefabs/Toolboard/Prefabs/Tools";

        [Header("Experimenter Settings")]
        [Tooltip("Working-memory (WM) load level: Low = 0-back, Optimal = 2-back, High = 3-back.")]
        [SerializeField] private WMLoadLevel _level = WMLoadLevel.Optimal;
        [Tooltip("When set, the custom settings below replace the level preset.")]
        [SerializeField] private bool _overridePreset;
        [SerializeField] private ToolNBackSettings _customSettings = new ToolNBackSettings
        {
            N = 2,
            StimulusDuration = 1.5f,
            InterStimulusDuration = 2f,
            SequenceLength = 40,
            TargetRate = 0.3f,
            ToolPoolSize = 8
        };
        [Tooltip("0 uses a time based seed; any other value reproduces the same sequence and positions.")]
        [SerializeField] private int _randomSeed;
        [Tooltip("0-back: pick the target tool at random (reproducible via Random Seed) instead of always using the first Tool Prefabs entry. The target is previewed during the countdown.")]
        [SerializeField] private bool _randomizeTargetTool = true;
        [Tooltip("Seconds of instruction display before the first trial.")]
        [SerializeField] private float _startDelay = 5f;

        [Header("Curved Panel (fan around the user)")]
        [Tooltip("Height of the panel pivot above the floor in meters — set this to the participant's eye height. Moves this object's Y position (ignored when a custom Panel Anchor is assigned).")]
        [SerializeField] private float _panelPivotHeightMeters = 1.6f;
        [Tooltip("Pivot of the cylinder section the tools appear on (the user's head position); defaults to this transform.")]
        [SerializeField] private Transform _panelAnchor;
        [SerializeField] private float _panelRadius = 1.2f;
        [SerializeField] private float _panelArcDegrees = 110f;
        [SerializeField] private float _panelHeightMin = -0.35f;
        [SerializeField] private float _panelHeightMax = 0.35f;
        [Tooltip("Minimum normalized (u, v) distance between consecutive stimulus positions.")]
        [SerializeField, Range(0f, 0.7f)] private float _minSlotSeparation = 0.25f;

        [Header("Tools")]
        [Tooltip("Stimulus prefabs. When empty, all tool prefabs are loaded from Resources/" + ToolsResourcePath + " (_MITmech tool board).")]
        [SerializeField] private GameObject[] _toolPrefabs = Array.Empty<GameObject>();
        [Tooltip("Uniform display size of the largest tool dimension in meters.")]
        [SerializeField] private float _toolDisplaySize = 0.22f;
        [Tooltip("Slow spin of the displayed tool in degrees per second (0 = static).")]
        [SerializeField] private float _toolSpinSpeed = 40f;

        [Header("Response Feedback (practice blocks only)")]
        [Tooltip("Outline the visible tool green/red when the participant presses. Keep OFF for EEG recording blocks — feedback-evoked potentials scale with error rate and confound the WM-load calibration. Turn on for practice runs.")]
        [SerializeField] private bool _showResponseFeedback;
        [SerializeField] private Color _correctFeedbackColor = new Color(0.15f, 0.9f, 0.2f);
        [SerializeField] private Color _incorrectFeedbackColor = new Color(0.95f, 0.15f, 0.1f);
        [Tooltip("Seconds the outline stays visible after a press (also cleared when the tool hides).")]
        [SerializeField] private float _feedbackDuration = 0.4f;
        [Tooltip("Emission strength of the feedback glow.")]
        [SerializeField, Range(0f, 2f)] private float _feedbackGlowIntensity = 0.6f;

        [Header("Multi-Block Session")]
        [Tooltip("Run a whole session in one Play session: rest baselines around counterbalanced, repeated WM-load task blocks (plus optional practice), all in one continuous recording. Off = classic single block from the Level field.")]
        [SerializeField] private bool _multiBlockSession;
        [Tooltip("Which of the 6 counterbalanced level orders to use (e.g. the participant number; applied modulo 6).")]
        [SerializeField] private int _counterbalanceIndex;
        [Tooltip("How many recorded task blocks per WM load level.")]
        [SerializeField, Min(1)] private int _repetitionsPerLevel = 2;
        [Tooltip("Opening rest baseline in seconds. Long on purpose: it feeds CLAd-EEG's auto-baseline (needs >= 30 s of valid windows) and the frozen EEG artifact-model fit (90 s window).")]
        [SerializeField, Min(30f)] private float _baselineRestDuration = 90f;
        [Tooltip("Washout rest between blocks in seconds — just enough for pupil and load state to settle; also contributes baseline-labeled windows. The final washout doubles as the closing baseline.")]
        [SerializeField, Min(5f)] private float _interBlockRestDuration = 20f;
        [Tooltip("Insert one practice block (Optimal level, feedback allowed, marked practice=1 in the markers) after the opening rest.")]
        [SerializeField] private bool _includePracticeBlock = true;
        [Tooltip("When non-empty, replaces the generated default plan.")]
        [SerializeField] private List<SessionBlock> _customBlocks = new List<SessionBlock>();

        [Header("UI (created automatically when empty)")]
        [SerializeField] private Text _instructionText;

        /// <summary>The running core task; null before Start.</summary>
        public ToolNBackTask Task { get; private set; }
        /// <summary>The resolved task settings (preset or override).</summary>
        public ToolNBackSettings Settings => _settings;
        /// <summary>Names of the pooled tools; the stimulus markers use these.</summary>
        public string[] ToolNames => _toolNames;

        private ToolNBackSettings _settings;
        private CurvedPanelLayout _layout;
        private GameObject[] _toolInstances;
        private string[] _toolNames;
        private int[] _sequence;
        private Vector2[] _slots;
        private GameObject _visibleInstance;
        private float _delayRemaining;
        private bool _running;
        private int _lastCountdownSecond = -1;
        private int _hits;
        private int _misses;
        private int _falseAlarms;
        private int _correctRejections;
        private ToolFeedbackTint[] _toolTints;
        private ToolFeedbackTint _activeTint;
        private float _feedbackRemaining;
        private bool _feedbackAllowedThisBlock;

        // Multi-block session state.
        private List<SessionBlock> _plan;
        private SessionBlock _currentBlock;
        private int _blockIndex = -1;
        private bool _inRest;
        private float _restRemaining;
        private bool _sessionFinished;
        private int _completedNonRestBlocks;
        private int _sessionHits;
        private int _sessionMisses;
        private int _sessionFalseAlarms;
        private int _sessionCorrectRejections;
        private int _sessionCorrect;
        private int _sessionTrials;

        #region Unity methods
        private void Start()
        {
            if (_panelAnchor == null)
                _panelAnchor = transform;
            ApplyPanelPivotHeight();

            _settings = _overridePreset ? _customSettings : WMLoadPresets.Get(_level);
            _layout = new CurvedPanelLayout(_panelRadius, _panelArcDegrees, _panelHeightMin, _panelHeightMax);

            GameObject[] prefabs = _toolPrefabs != null && _toolPrefabs.Length > 0
                ? _toolPrefabs
                : DeduplicateDrillBits(Resources.LoadAll<GameObject>(ToolsResourcePath).OrderBy(p => p.name).ToArray());

            if (prefabs.Length < 2)
            {
                Debug.LogError($"[ToolNBack] Need at least 2 tool prefabs, found {prefabs.Length} " +
                               $"(assign them in the inspector or keep them in Resources/{ToolsResourcePath}).");
                enabled = false;
                return;
            }

            int poolSize = Mathf.Clamp(_settings.ToolPoolSize, 2, prefabs.Length);
            int seed = _randomSeed == 0 ? Environment.TickCount : _randomSeed;

            // Core index 0 is the designated 0-back target; place a seeded-random tool
            // there so the target differs between runs but reproduces for a fixed seed
            // (independent random streams: sequence = seed, slots = seed + 1, target = seed + 2).
            // In multi-block sessions the target is picked once, so every 0-back block
            // of one session responds to the same tool.
            var pool = (GameObject[])prefabs.Clone();
            if (_randomizeTargetTool)
            {
                int targetIndex = new System.Random(seed + 2).Next(poolSize);
                (pool[0], pool[targetIndex]) = (pool[targetIndex], pool[0]);
            }

            var poolParent = new GameObject("Tool Pool").transform;
            poolParent.SetParent(transform, false);

            _toolNames = new string[poolSize];
            _toolInstances = new GameObject[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                _toolNames[i] = pool[i].name;
                _toolInstances[i] = CreateDisplayInstance(pool[i], poolParent, _toolDisplaySize);
            }

            if (_showResponseFeedback)
                PrepareFeedbackTints();

            EnsureInstructionText();

            if (_multiBlockSession)
            {
                _plan = _customBlocks != null && _customBlocks.Count > 0
                    ? new List<SessionBlock>(_customBlocks)
                    : SessionPlanner.DefaultPlan(_counterbalanceIndex, _repetitionsPerLevel,
                        _baselineRestDuration, _interBlockRestDuration, _includePracticeBlock);
                NBackEvents.RaiseSessionStarted(_plan.Count);
                EnterBlock(0);
                return;
            }

            _feedbackAllowedThisBlock = _showResponseFeedback;
            PrepareBlockTask(seed);
            _delayRemaining = Mathf.Max(0f, _startDelay);

            // 0-back: preview the target tool at panel center while the countdown runs,
            // so participants see the object they must respond to, not just its name.
            if (_settings.N == 0 && _delayRemaining > 0f)
                ShowTargetPreview();
        }

        /// <summary>
        /// Keeps the inspector height field, the transform and the scene gizmo in sync,
        /// both in edit mode (live while typing) and at runtime.
        /// </summary>
        private void OnValidate()
        {
            ApplyPanelPivotHeight();
        }

        private void Update()
        {
            if (_sessionFinished)
                return;

            if (_multiBlockSession && _inRest)
            {
                TickRest(Time.deltaTime);
                return;
            }

            if (Task == null || Task.IsFinished)
                return;

            if (!_running)
            {
                _delayRemaining -= Time.deltaTime;
                if (_delayRemaining > 0f)
                {
                    UpdateCountdownText();
                    SpinVisibleTool();
                    return;
                }

                BeginRunning();
            }

            Task.Run(Time.deltaTime);
            SpinVisibleTool();
            TickFeedback(Time.deltaTime);
        }
        #endregion

        #region Public methods
        /// <summary>Participant response entry point (controller trigger or keyboard).</summary>
        public void HandleOnInput()
        {
            if (Task == null || Task.IsFinished || !_running)
                return;

            Task.RegisterResponse();

            bool isTarget = Task.IsTargetAt(Task.Index);
            NBackEvents.RaiseResponse(new ResponseData
            {
                Kind = TaskKind.NBack,
                TrialIndex = Task.Index,
                Letter = _toolNames[Task.Sequence[Task.Index]],
                IsTarget = isTarget,
                Phase = Task.CurrentTool >= 0 ? "stimulus" : "isi",
                RtStimulus = Task.ReactionTime,
                RtTrial = Task.ReactionTime,
                HasRt = true,
                Classification = isTarget ? TrialOutcomeKind.Hit : TrialOutcomeKind.FalseAlarm
            });

            // Practice-only visual feedback; presses during the blank gap get none.
            // In multi-block sessions recorded task blocks always suppress it —
            // feedback-evoked potentials would confound the WM-load contrast.
            if (_feedbackAllowedThisBlock && Task.CurrentTool >= 0)
                ShowFeedback(Task.CurrentTool, correct: isTarget);
        }
        #endregion

        #region Task event handlers
        private void HandleStimulusOnset(int trialIndex, int toolIndex, bool isTarget)
        {
            Vector2 slot = _slots[trialIndex];
            Pose pose = _layout.GetLocalPose(slot.x, slot.y);

            GameObject instance = _toolInstances[toolIndex];
            instance.transform.SetPositionAndRotation(
                _panelAnchor.TransformPoint(pose.position),
                _panelAnchor.rotation * pose.rotation);
            instance.SetActive(true);
            _visibleInstance = instance;

            NBackEvents.RaiseStimulusOnset(trialIndex, _toolNames[toolIndex], isTarget, _settings.N);
        }

        private void HandleStimulusOffset(int trialIndex, int toolIndex, bool responded)
        {
            ClearFeedback();
            if (_visibleInstance != null)
                _visibleInstance.SetActive(false);
            _visibleInstance = null;

            NBackEvents.RaiseStimulusOffset(trialIndex, _toolNames[toolIndex], responded);
        }

        private void HandleTrialResult(int trialIndex, int toolIndex, bool isTarget, bool responded)
        {
            if (isTarget)
            {
                if (responded) _hits++;
                else _misses++;
            }
            else
            {
                if (responded) _falseAlarms++;
                else _correctRejections++;
            }

            NBackEvents.RaiseTrialResult(trialIndex, _toolNames[toolIndex], isTarget, responded);
        }

        private void HandleFinished()
        {
            NBackEvents.RaiseTaskFinished(new TaskFinishedData
            {
                TaskIndex = _multiBlockSession ? _blockIndex : 0,
                Kind = TaskKind.NBack,
                N = _settings.N,
                CorrectCount = Task.NumberOfCorrectAnswers,
                ErrorRate = Task.ErrorRate
            });

            if (!_multiBlockSession)
            {
                NBackEvents.RaiseSessionEnded();

                // Raw percent-correct hides disengagement (never responding already scores
                // all correct rejections), so show the signal-detection breakdown instead.
                int targets = _hits + _misses;
                SetInstruction(
                    "Done!\n" +
                    $"Hits: {_hits}/{targets}   False alarms: {_falseAlarms}\n" +
                    $"Misses: {_misses}   Correct rejections: {_correctRejections}\n" +
                    $"Correct: {Task.NumberOfCorrectAnswers}/{_sequence.Length} (error {Task.ErrorRate:P0})");
                return;
            }

            _running = false;
            _completedNonRestBlocks++;

            // Practice results stay out of the session totals — practice is excluded
            // from analysis and would skew the summary the experimenter reads.
            if (_currentBlock.Kind == SessionBlockKind.Task)
            {
                _sessionHits += _hits;
                _sessionMisses += _misses;
                _sessionFalseAlarms += _falseAlarms;
                _sessionCorrectRejections += _correctRejections;
                _sessionCorrect += Task.NumberOfCorrectAnswers;
                _sessionTrials += _sequence.Length;
            }

            EnterBlock(_blockIndex + 1);
        }
        #endregion

        #region Private methods
        private static GameObject CreateDisplayInstance(GameObject prefab, Transform parent, float displaySize)
        {
            GameObject instance = Instantiate(prefab, parent);
            instance.name = prefab.name;

            // Display only: no XR grabbing, snapping, physics or tutorial logic.
            foreach (MonoBehaviour script in instance.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (script != null)
                    script.enabled = false;
            }
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.useGravity = false;
            }

            NormalizeScale(instance, displaySize);
            instance.SetActive(false);
            return instance;
        }

        /// <summary>Countdown is over: hide the target preview, announce the run, start trials.</summary>
        private void BeginRunning()
        {
            _running = true;

            if (_visibleInstance != null)
            {
                _visibleInstance.SetActive(false);
                _visibleInstance = null;
            }

            SetInstruction(string.Empty);
            if (!_multiBlockSession)
                NBackEvents.RaiseSessionStarted(1); // multi-block raises it once, in Start
            NBackEvents.RaiseTaskStarted(BuildTaskStartedData());
        }

        /// <summary>
        /// (Re)generates sequence and stimulus positions for the current settings and
        /// wires a fresh core task; per-block counters and countdown state reset.
        /// </summary>
        private void PrepareBlockTask(int seed)
        {
            int poolSize = Mathf.Clamp(_settings.ToolPoolSize, 2, _toolInstances.Length);
            _sequence = ToolNBackSequenceGenerator.Generate(
                _settings.SequenceLength, _settings.N, _settings.TargetRate, poolSize, seed);
            _slots = ToolNBackSequenceGenerator.GenerateSlots(
                _settings.SequenceLength, _minSlotSeparation, seed + 1);

            if (Task != null)
            {
                Task.StimulusOnset -= HandleStimulusOnset;
                Task.StimulusOffset -= HandleStimulusOffset;
                Task.TrialResult -= HandleTrialResult;
                Task.Finished -= HandleFinished;
            }

            Task = new ToolNBackTask(_sequence, _settings.N,
                _settings.StimulusDuration, _settings.InterStimulusDuration);
            Task.StimulusOnset += HandleStimulusOnset;
            Task.StimulusOffset += HandleStimulusOffset;
            Task.TrialResult += HandleTrialResult;
            Task.Finished += HandleFinished;

            _hits = _misses = _falseAlarms = _correctRejections = 0;
            _lastCountdownSecond = -1;
            _running = false;
        }

        /// <summary>Advances the multi-block session to plan entry <paramref name="index"/>.</summary>
        private void EnterBlock(int index)
        {
            _blockIndex = index;
            if (index >= _plan.Count)
            {
                FinishSession();
                return;
            }

            SessionBlock block = _plan[index];
            _currentBlock = block;

            if (block.Kind == SessionBlockKind.Rest)
            {
                _inRest = true;
                _restRemaining = block.RestDuration;
                _lastCountdownSecond = -1;
                ClearFeedback();
                if (_visibleInstance != null)
                {
                    _visibleInstance.SetActive(false);
                    _visibleInstance = null;
                }
                NBackEvents.RaiseTaskStarted(new TaskStartedData
                {
                    TaskIndex = index,
                    Kind = TaskKind.Rest,
                    N = -1,
                    FocusDuration = block.RestDuration
                });
                UpdateRestText();
                return;
            }

            _inRest = false;
            _settings = _overridePreset ? _customSettings : WMLoadPresets.Get(block.Level);
            int derived = SessionPlanner.DeriveBlockSeed(_randomSeed, index);
            PrepareBlockTask(derived == 0 ? Environment.TickCount : derived);
            _feedbackAllowedThisBlock = block.Kind == SessionBlockKind.Practice && _showResponseFeedback;
            _delayRemaining = Mathf.Max(0f, _startDelay);

            if (_settings.N == 0 && _delayRemaining > 0f)
                ShowTargetPreview();
            if (_delayRemaining > 0f)
                UpdateCountdownText();
        }

        /// <summary>Counts down a rest baseline; advances to the next block when it ends.</summary>
        private void TickRest(float deltaTime)
        {
            if (_sessionFinished || !_inRest)
                return;

            _restRemaining -= deltaTime;
            if (_restRemaining > 0f)
            {
                UpdateRestText();
                return;
            }

            _inRest = false;
            NBackEvents.RaiseTaskFinished(new TaskFinishedData
            {
                TaskIndex = _blockIndex,
                Kind = TaskKind.Rest,
                N = -1
            });
            EnterBlock(_blockIndex + 1);
        }

        private void UpdateRestText()
        {
            int second = Mathf.CeilToInt(Mathf.Max(0f, _restRemaining));
            if (second == _lastCountdownSecond)
                return;
            _lastCountdownSecond = second;

            SetInstruction(
                "Rest\nRelax, keep still and keep the headset on.\n\n" +
                $"Continuing in {second}...");
        }

        /// <summary>Last plan entry done: end the session and show the aggregate summary.</summary>
        private void FinishSession()
        {
            _sessionFinished = true;
            _inRest = false;
            NBackEvents.RaiseSessionEnded();

            int targets = _sessionHits + _sessionMisses;
            SetInstruction(
                "Session complete!\n" +
                $"Hits: {_sessionHits}/{targets}   False alarms: {_sessionFalseAlarms}\n" +
                $"Misses: {_sessionMisses}   Correct rejections: {_sessionCorrectRejections}\n" +
                $"Correct: {_sessionCorrect}/{_sessionTrials} (recorded task blocks; practice excluded)");
        }

        /// <summary>Shows the designated 0-back target tool at panel center during the countdown.</summary>
        private void ShowTargetPreview()
        {
            Pose pose = _layout.GetLocalPose(0.5f, 0.5f);
            GameObject target = _toolInstances[0];
            target.transform.SetPositionAndRotation(
                _panelAnchor.TransformPoint(pose.position),
                _panelAnchor.rotation * pose.rotation);
            target.SetActive(true);
            _visibleInstance = target;
        }

        private void SpinVisibleTool()
        {
            if (_visibleInstance != null && _toolSpinSpeed != 0f)
                _visibleInstance.transform.Rotate(0f, _toolSpinSpeed * Time.deltaTime, 0f, Space.Self);
        }

        /// <summary>
        /// Adds one renderer-level tint per tool instance. Feedback deliberately uses
        /// plain material rendering (no screen-space outline effect) so it behaves
        /// identically in every Varjo view and needs no readable meshes.
        /// </summary>
        private void PrepareFeedbackTints()
        {
            _toolTints = new ToolFeedbackTint[_toolInstances.Length];
            for (int i = 0; i < _toolInstances.Length; i++)
            {
                var tint = _toolInstances[i].AddComponent<ToolFeedbackTint>();
                tint.GlowIntensity = _feedbackGlowIntensity;
                tint.Initialize();
                _toolTints[i] = tint;
            }
        }

        private void ShowFeedback(int toolIndex, bool correct)
        {
            if (_toolTints == null)
                return; // feedback was toggled on after Start; tints were never prepared

            ClearFeedback();
            _activeTint = _toolTints[toolIndex];
            _activeTint.Apply(correct ? _correctFeedbackColor : _incorrectFeedbackColor);
            _feedbackRemaining = _feedbackDuration;
        }

        private void ClearFeedback()
        {
            if (_activeTint != null)
                _activeTint.Clear();
            _activeTint = null;
            _feedbackRemaining = 0f;
        }

        private void TickFeedback(float deltaTime)
        {
            if (_activeTint == null)
                return;

            _feedbackRemaining -= deltaTime;
            if (_feedbackRemaining <= 0f)
                ClearFeedback();
        }

        /// <summary>
        /// Places the panel pivot at the experimenter-configured height. When a custom
        /// Panel Anchor transform is assigned, its position is respected instead.
        /// </summary>
        private void ApplyPanelPivotHeight()
        {
            if (_panelAnchor != null && _panelAnchor != transform)
                return;

            Vector3 position = transform.position;
            if (!Mathf.Approximately(position.y, _panelPivotHeightMeters))
                transform.position = new Vector3(position.x, _panelPivotHeightMeters, position.z);
        }

        /// <summary>
        /// The tool board ships 9 nearly identical drill bits; as n-back stimuli they are
        /// indistinguishable, so auto-loading keeps only the first "Drill Bit ..." prefab.
        /// Assign Tool Prefabs explicitly in the inspector to override this.
        /// </summary>
        private static GameObject[] DeduplicateDrillBits(GameObject[] prefabs)
        {
            var result = new List<GameObject>(prefabs.Length);
            bool drillBitTaken = false;
            foreach (GameObject prefab in prefabs)
            {
                if (prefab.name.StartsWith("Drill Bit"))
                {
                    if (drillBitTaken)
                        continue;
                    drillBitTaken = true;
                }
                result.Add(prefab);
            }

            return result.ToArray();
        }

        /// <summary>Scales the instance so its largest bounds dimension equals displaySize meters.</summary>
        private static void NormalizeScale(GameObject instance, float displaySize)
        {
            if (displaySize <= 0f)
                return;

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (maxDimension > 1e-5f)
                instance.transform.localScale *= displaySize / maxDimension;
        }

        private TaskStartedData BuildTaskStartedData()
        {
            var targets = new char[_sequence.Length];
            for (int i = 0; i < _sequence.Length; i++)
                targets[i] = Task.IsTargetAt(i) ? '1' : '0';

            return new TaskStartedData
            {
                TaskIndex = _multiBlockSession ? _blockIndex : 0,
                Kind = TaskKind.NBack,
                N = _settings.N,
                TrialCount = _sequence.Length,
                StimulusDuration = _settings.StimulusDuration,
                InterStimulusDuration = _settings.InterStimulusDuration,
                Sequence = string.Join(",", _sequence),
                Targets = new string(targets),
                Practice = _multiBlockSession && _currentBlock.Kind == SessionBlockKind.Practice
            };
        }

        private void EnsureInstructionText()
        {
            if (_instructionText != null)
                return;

            var canvasGo = new GameObject("Instruction Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(900f, 400f);
            canvasGo.transform.localScale = Vector3.one * 0.001f;

            // Below the panel center, facing the user (canvas +Z must point away).
            Vector3 local = _layout.GetLocalPosition(0.5f, 0f) + Vector3.down * 0.3f;
            canvasGo.transform.SetPositionAndRotation(
                _panelAnchor.TransformPoint(local),
                _panelAnchor.rotation * _layout.GetLocalRotation(0.5f, 0f) * Quaternion.Euler(0f, 180f, 0f));

            var textGo = new GameObject("Instruction Text");
            textGo.transform.SetParent(canvasGo.transform, false);
            _instructionText = textGo.AddComponent<Text>();
            _instructionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _instructionText.fontSize = 48;
            _instructionText.alignment = TextAnchor.MiddleCenter;
            _instructionText.color = Color.white;

            var textRect = _instructionText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private void UpdateCountdownText()
        {
            int second = Mathf.CeilToInt(_delayRemaining);
            if (second == _lastCountdownSecond)
                return;
            _lastCountdownSecond = second;

            SetInstruction($"{BuildInstruction()}\n\nStarting in {second}...");
        }

        private string BuildInstruction()
        {
            string levelName = _overridePreset
                ? "Custom"
                : (_multiBlockSession ? _currentBlock.Level : _level).ToString();
            string prefix = _multiBlockSession && _currentBlock.Kind == SessionBlockKind.Practice
                ? "PRACTICE - "
                : string.Empty;

            if (_settings.N == 0)
                return $"{prefix}Tool N-Back ({levelName}, 0-back)\nRemember the tool shown above:\nrespond whenever the {_toolNames[0]} appears.";

            return $"{prefix}Tool N-Back ({levelName}, {_settings.N}-back)\n" +
                   $"Respond when the tool matches the one shown {_settings.N} tools earlier.";
        }

        private void SetInstruction(string text)
        {
            if (_instructionText != null)
                _instructionText.text = text;
        }
        #endregion

#if UNITY_EDITOR
        #region Editor gizmos
        // The tools only exist at runtime, so draw the curved panel in the scene view
        // to show where stimuli will appear.
        private void OnDrawGizmos()
        {
            DrawPanelGizmo(selected: false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawPanelGizmo(selected: true);
        }

        private void DrawPanelGizmo(bool selected)
        {
            Transform anchor = _panelAnchor != null ? _panelAnchor : transform;
            var layout = new CurvedPanelLayout(_panelRadius, _panelArcDegrees, _panelHeightMin, _panelHeightMax);
            const int steps = 24;

            Gizmos.color = selected ? new Color(0f, 0.85f, 1f, 0.9f) : new Color(0f, 0.85f, 1f, 0.35f);

            float[] rows = selected ? new[] { 0f, 0.5f, 1f } : new[] { 0.5f };
            foreach (float v in rows)
            {
                Vector3 previous = anchor.TransformPoint(layout.GetLocalPosition(0f, v));
                for (int i = 1; i <= steps; i++)
                {
                    Vector3 next = anchor.TransformPoint(layout.GetLocalPosition(i / (float)steps, v));
                    Gizmos.DrawLine(previous, next);
                    previous = next;
                }
            }

            if (!selected)
                return;

            foreach (float u in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
            {
                Gizmos.DrawLine(
                    anchor.TransformPoint(layout.GetLocalPosition(u, 0f)),
                    anchor.TransformPoint(layout.GetLocalPosition(u, 1f)));
            }

            // Example stimulus at panel center, at the configured display size.
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.9f);
            Gizmos.DrawWireSphere(
                anchor.TransformPoint(layout.GetLocalPosition(0.5f, 0.5f)), _toolDisplaySize * 0.5f);
        }
        #endregion
#endif
    }
}
