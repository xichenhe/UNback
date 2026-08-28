# UNback — VR working-memory load calibration (Varjo XR-3 + LSL + EEG)

UNback presents **n-back working-memory (WM) tasks inside a Varjo XR-3 headset** and publishes synchronized task markers, eye tracking, and head pose as **Lab Streaming Layer (LSL)** streams, so that a per-participant **EEG WM-load classifier** can be calibrated in-headset immediately before a VR training study (the CLAdVR machining training). All task events are also written to a local JSONL backup log, so task data survives even if LSL recording fails.

> **Terminology — why "WM load", not "cognitive load".** The n-back manipulates **working-memory load** specifically. A classifier calibrated with it is a **WM-load classifier**, not a general "cognitive workload" classifier: EEG workload classifiers transfer poorly across dissimilar tasks, so scores from this calibration are reported as WM load and must be validated on the target task before any broader claim. Code, docs, and markers in this project use *WM load* for the Low / Optimal / High manipulation throughout.

## The two tasks

| Scene | Stimuli | Purpose |
|---|---|---|
| `VR-N-Back` | Letters on a world-space canvas | Text n-back (Braver-style) presented in VR |
| `VR-Tool-NBack` | Machining tool models from the `_MITmech` tool board, on a curved fan panel around the user | WM-load calibration matched to the CLAdVR training environment |

In the tool task the experimenter selects a **WM load level** — Low (0-back), Optimal (2-back), High (3-back). All three levels share identical timing, trial count, and stimulus pool, so **N is the only difference between conditions** (equal block durations and near-equal event counts for EEG epoching, no stimulus-rate confound). Tools appear at pseudo-random positions on the curved panel, so gaze and head movement during calibration resemble the training task the classifier will be used in.

For full recording sessions, the `VR-Tool-NBack-MultiBlock` scene runs a complete **multi-block session** in one Play session and one continuous recording: marker-bracketed rest baselines, an optional practice block (feedback allowed, marked `practice=1`), and counterbalanced, repeated task blocks per level with distinct reproducible per-block sequences (guide section 3c).

## Data streams

While a task runs, the app publishes four LSL streams — `NBackMarkersLSL` (human-readable event markers), `UNbackEventMarkerLSL` (the same events as 9-channel numeric samples following the CLAd conventions: channel 0 carries Unity time for clock alignment, block index/kind channels let purely numeric pipelines label calibration windows), `VarjoEyeTrackingLSL` (up to 200 Hz gaze, per-eye pupil and iris diameter — identical in name, layout, and timestamps to CLAdVR's stream, so one PhysioLabXR preset spans both apps), and `NBackHeadPoseLSL` (head pose per frame). The lab records with **PhysioLabXR** (experiment preset `CLAd`) in one continuous recording across the UNback calibration and CLAdVR training phases; the CLAd-EEG live service consumes the marker stream to collect labeled low/optimal/high calibration data during phase 3 and freezes into estimation for phase 4. Marker strings carry the complete ground truth (full stimulus sequence, target vector, per-trial outcomes, reaction times).

## Getting started

- **Unity 6000.3.1f1**, Varjo Base ≥ 4.6; all packages (Varjo XR plugin, LSL plugin, XR Interaction Toolkit) resolve automatically from `Packages/manifest.json`.
- Editor menu **NBack VR > Build or Repair VR-N-Back Scene** builds/repairs the text-task scene; **NBack VR > Build Tool NBack Scene** builds the tool task; **NBack VR > Validate Setup** prints a PASS/FAIL report.
- See **[EXPERIMENTER_GUIDE.md](EXPERIMENTER_GUIDE.md)** for one-time setup, task configuration, the session-day run checklist, the full stream/marker reference used for analysis, and troubleshooting.

## Origins and credits

The text-based task is built on [wgnrto/uNback](https://github.com/wgnrto/uNback), a Unity package implementing the n-back procedure described by [T. S. Braver. 1997. "A Parametric Study of Prefrontal Cortex Involvement in Human Working Memory". NeuroImage. DOI: 10.1006/nimg.1996.0247](https://www.sciencedirect.com/science/article/pii/S1053811996902475), and used in [M. Lanzer. 2023. "Interaction Effects of Pedestrian Behavior, Smartphone Distraction and External Communication of Automated Vehicles on Crossing and Gaze Behavior". CHI'23. DOI: 10.1145/3544548.3581303](https://dl.acm.org/doi/full/10.1145/3544548.3581303). The original package (archived in `Package/nbacktask.unitypackage`) is a desktop text n-back without data logging:

![Original uNback text n-back task](https://github.com/wgnrto/uNback/assets/24171719/99c7b0ba-3f9e-4a7c-8358-11c99123f0ce)

This project departs from it substantially:

- **VR presentation** on a Varjo XR-3 (XR rig, world-space canvas, controller-trigger responses) instead of a desktop screen.
- **Full data pipeline**: LSL marker/event/gaze/head-pose streams with a stable, documented analysis contract, plus a crash-safe JSONL backup log (the original had no logging).
- **Seeded sequence generation** with exact target counts, for reproducible sequences per participant/condition.
- **Per-trial scoring including the final trial** (`trial_result` markers; documented divergence from the original's `task_end` summary semantics).
- **The tool-based WM-load calibration task**: 3D machining-tool stimuli on a curved panel with Low / Optimal / High presets, practice-only response feedback, and a signal-detection summary (hits, misses, false alarms, correct rejections).

For custom time-dependent tasks, the original's extension point still exists: inherit from `Assets/Scripts/Task/ATask<T>`.
