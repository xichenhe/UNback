# LLM Guidance Delegation — Exploration (Leo's open question)

Date: 2026-08-26. Status: design exploration, nothing built. Grounded in what the
four repos contain today after the LSL pipeline unification.

## What already exists to build on

- **CLAd-EEG publishes a live state stream**: `CLAdCognitiveLoadLSL` (9×float32,
  ~1 Hz): fusion_score, eeg_probability, pupil_probability, pupil_z,
  pupil_mean_mm, step_number, unity_time, baseline_ready, load_value_01. The
  JSONL payload beside it is richer: LSTM quantiles p10/p50/p90 (a native
  **confidence interval** on the WM estimate), EEG quality flags, gaze
  eccentricity, estimation mode.
- After phase 3 the estimate is **calibrated per participant** against real
  low/optimal/high WM states — it is a WM-load axis, not generic "workload"
  (exactly the claim discipline Leo asked for).
- CLAdVR has a step machine (step_changed events), error/warning events, machine
  state — the task context an instruction policy needs.
- **No LLM-command code exists in any repo.** The loop closes on fresh ground.

## Leo's decomposition → the state vector

| Axis | Source | Status |
|---|---|---|
| Executive-WM demand | calibrated EEG head (low/optimal/high + p10-p90 spread as confidence) | live after phase 3; discrete label currently blanked in the payload — unblank or threshold `load_value_01` |
| Perceptual demand | gaze features from `VarjoEyeTrackingLSL`: fixation duration, saccade rate/amplitude, ambient/focal coefficient K, eccentricity | eccentricity computed today; K/fixation stats are straightforward additions to the same consumer |
| Effort/arousal | `pupil_z` (already a channel) | live; luminance regression still needed before trusting it hard (bright-scene caveat from the audit) |
| Tonic state/fatigue | HRV (needs the ECG lead) | not yet; interim proxy: time-on-task + baseline drift |

## Recommended architecture: a small Python broker

Three options considered; recommend **B**.

- **A. Unity-side agent** (CLAdVR calls the LLM directly): keeps everything in
  one app, but LLM iteration in C#/Unity is slow and the prompt logic would be
  buried in the game.
- **B. Python broker (recommended)**: `scaffolding_agent.py` beside CLAd-EEG.
  Subscribes `CLAdCognitiveLoadLSL` + `CLAdEventMarkerLSL` + `MachineStateLSL`;
  maintains the state vector; a deterministic gate decides *when* to consult the
  LLM; the LLM returns a structured directive; the broker publishes it as a new
  LSL stream (`CLAdScaffoldingLSL`, string/JSON or coded) that CLAdVR renders.
  Unity stays a dumb renderer of directives; prompts/policies iterate in Python;
  everything is in the recording for post-hoc analysis.
- **C. Rules first, LLM later**: same directive schema, policy table instead of
  an LLM for v1. Compatible with B — B's gate IS a rule layer; the LLM can be
  swapped in per intervention point.

## The delegation policy sketch

Per step transition (and on error/warning), the broker assembles:

```json
{
  "step": 29, "step_name": "Drilling Speed",
  "wm_load": "high", "wm_confidence": 0.72,
  "perceptual_demand": "low",
  "arousal_z": 1.1,
  "time_on_task_min": 14,
  "recent": ["warning: rpm", "error dismissed"]
}
```

**Deterministic gate before any LLM call** (protects both the participant and
the token budget): never interrupt mid-step while WM is high unless an error is
active; consult the LLM only at step transitions, sustained-high-WM episodes
(the service's existing hysteresis/dwell machinery defines "sustained"), or
errors. Expected call rate: ~1 per step, latency 1–3 s — irrelevant at step
granularity.

**Directive schema** CLAdVR understands:
`{mode: chunk_instruction | declutter | verbal_hint | pause_prompt | fade_scaffolding | proceed, text, intensity}`

The mapping Leo sketched, made concrete:

| WM load | Perceptual | Policy |
|---|---|---|
| high | low | *"executively expensive, perceptually cheap"* → offload memory: chunk the instruction, show a persistent checklist |
| low | high | *"perceptually overloaded, executively cheap"* → declutter visuals, switch guidance to audio |
| high | high | slow down: pause prompt, one action at a time |
| low | low | fade scaffolding — raise autonomy/challenge |

## Validation before trusting it

Log every directive with its input state (the broker's own JSONL); NASA-TLX per
phase; A/B scaffolding-on vs. -off on the drilling task. The WM axis itself gets
validated against the phase-3 labels before its outputs steer anything —
transfer is measured, not assumed.

## Open items

Luminance correction for the pupil axis; ECG/HRV for the fatigue axis; deciding
whether the discrete class labels get unblanked in `CLAdCognitiveLoadLSL` (adds
channels → version the stream id) or thresholded broker-side (no contract
change; recommended first step).
