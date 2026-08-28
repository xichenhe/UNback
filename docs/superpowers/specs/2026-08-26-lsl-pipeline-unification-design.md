# LSL Pipeline Unification — UNback ↔ CLAdVR ↔ PhysioLabXR ↔ CLAdEEG

Date: 2026-08-26. Approved by Xichen in chat (Q&A: keep JSONL; replace
NBackEventCodesLSL with UNbackEventMarkerLSL; implement preprocessing fixes now;
proceed with sections A–D). Addition from approval: **CLAd-EEG runs continuously
across phases 3→4; its mode (collect-labeled vs. estimate) is driven by the
markers it receives.**

## Context (verified findings)

- UNback's `VarjoEyeTrackingLSL` is payload-identical to CLAdVR's (same name,
  type, 39-ch double64 layout, zeroing, back-dated timestamps). Differences:
  source_id (`"uNback-varjo-gaze"` vs `""`), UNback forces gaze frequency/filter
  while CLAdVR uses device defaults.
- CLAdVR's `CLAdEventMarkerLSL` is 2×float32 `[UnityTime, StepNumber]`; semantic
  labels are commented out. CLAdEEG uses it **only** for LSL↔Unity clock offset
  (`grid.py:195-196`); event semantics come from CLAdVR's own `Session_*.jsonl`
  (EventLogger). PhysioLabXR cannot declare/record cf_string streams and needs a
  positive nominal rate for its viz buffer.
- PhysioLabXR (branch `CLAd`) binds streams by NAME; on stream loss it stops the
  widget; clicking Start while a stream is unavailable arms auto-start. One
  recording spans app switches; the OpenBCI script streams EEG continuously.
- CLAdEEG live service (`online_cognitive_lstm.py` on the CognitiveLoadService
  base) consumes `OpenBCI_Cyton_Daisy_15_Channels_Preprocessed` +
  `VarjoEyeTrackingLSL`, publishes `CLAdCognitiveLoadLSL` (9ch float32).

## A. UNback gaze parity (2 changes)

- `VarjoGazeLSLStreamer`: source_id → `""` (matches CLAdVR; PhysioLabXR binds by
  name, but identity fields should not differ).
- Stop forcing `GazeOutputFrequency.MaximumSupported` + filter `Standard`; both
  apps then inherit the same Varjo Base configuration, so gaze/pupil dynamics do
  not change character at the phase-3→4 switch.

## B. `UNbackEventMarkerLSL` (replaces `NBackEventCodesLSL`)

9 channels, cf_float32, nominal 75 Hz (CLAd convention), source_id `""`,
type `EventMarker`:

| Ch | Content |
|---|---|
| 0 | UnityTime (`Time.realtimeSinceStartup`, s) — clock-offset recovery à la CLAdEEG |
| 1 | EventCode 1–10 (unchanged dictionary) |
| 2 | BlockIndex (task_start `index`; −1 n/a) |
| 3 | TrialIndex (−1 n/a) |
| 4 | IsTarget (1/0/−1) |
| 5 | OutcomeCode (0–4) |
| 6 | RT ms (−1 n/a) |
| 7 | N (−1 n/a) |
| 8 | BlockKind: 0 rest / 1 practice / 2 task(nback) / −1 other |

The marker streamer tracks current block index/kind from task_start events and
stamps them on every subsequent sample. Samples pushed as float[], invariant
formatting (deliberately not CLAdVR's culture-sensitive string path).
`NBackMarkersLSL` (string) and the JSONL log remain unchanged — the JSONL is the
semantic record, mirroring CLAdVR's EventLogger pattern.

## C. PhysioLabXR preset + protocol

- New `physiolabxr/_presets/LSLPresets/UNbackEventMarkerLSL.json` (9 named
  channels, NominalSamplingRate 75, DataType float32).
- `physiolabxr/_presets/ExperimentPresets/CLAd.json`: append
  `"UNbackEventMarkerLSL"` to PresetStreamNames.
- Guide: one continuous recording across UNback→CLAdVR; start (or arm) all
  streams at session start; EEG script runs untouched throughout.

## D. Multi-block custom inspector (UNback)

Editor-only `ToolNBackManagerEditor`: when Multi Block Session is on, disable the
single-block-only fields (Level, Override Preset/Custom Settings note) and render
a read-only preview of the generated plan (block list with levels and durations).

## E. Preprocessing must-fixes (`OpenBCIToLSLInterface.py`, PhysioLabXR checkout)

1. **Rank-aware ICA**: Cyton+Daisy has no physical Fz/Cz/Pz (synthesized as
   lateral averages) and CAR removes one more rank; FastICA(n=15) on rank-≤11
   data is ill-posed. Fit ICA on the physical channels only, before CAR, with
   n_components ≤ true rank; synthesize midline and apply CAR after cleaning.
2. **Freeze after baseline**: fit window ≥ 90 s (opening rest + practice supply
   it); no periodic refits — the cleaning function must be identical across load
   blocks or it becomes a load-correlated confound.
3. **Ocular-evidence rejection**: a component is only removed if it is frontal
   AND correlates with concurrent ocular activity derived from
   `VarjoEyeTrackingLSL` (gaze velocity / validity dropouts buffered during the
   fit window); fall back to conservative heuristics with a warning if gaze is
   absent. The previous criteria (frontal + low-frequency) match frontal-midline
   theta — the WM signal itself.
4. **Frontal regression stage default OFF** for recorded blocks (its trigger
   rate scales with load).

Raw 32-ch stream stays in the preset (offline redo always possible). Tests:
synthetic-signal pytest (blink-like component removed; theta-like component
survives; frozen refit; rank handling).

## F. CLAdEEG marker-driven phases

CLAd-EEG stays on across phases. New: subscribe `UNbackEventMarkerLSL`;
calibration mode while UNback markers flow — rest blocks feed the baseline
collector, task blocks accumulate feature windows labeled low/optimal/high by N
(0/2/3), practice ignored; at UNback session_end persist a per-participant
calibration artifact; on CLAdVR markers/session_started switch to estimation
using it. Implemented as a testable module + config/service hook; model-update
policy documented with the code.

## Out of scope (documented, not built)

LLM guidance delegation (design sketch delivered separately); CLAdVR-side
changes (double-drain bug noted for the lab, not fixed here).
