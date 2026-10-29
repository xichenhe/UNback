# UNback VR Experimenter Guide (Varjo XR-3 + LSL + EEG)

This guide is for the person running study sessions. It assumes no knowledge of how the
software was built. Read "One-time setup" once per lab PC; use the "Session-day run
checklist" for every participant; use the "Stream reference and marker dictionary" and
"Slicing guide" when analyzing the data.

---

## 1. Overview

The participant wears a **Varjo XR-3** headset and sees the text-based n-back task on a
floating panel. They respond by pressing **Enter** on a keyboard or **pulling the trigger
on a VR controller**. While the task runs, the Unity application publishes five
**Lab Streaming Layer (LSL)** streams:

| What | Stream name |
|---|---|
| Human-readable task event markers | `NBackMarkersLSL` |
| The same events as numeric samples (9 channels, CLAd conventions) | `UNbackEventMarkerLSL` |
| Varjo eye tracking (up to 200 Hz, 39 channels, identical to CLAdVR's) | `VarjoEyeTrackingLSL` |
| Headset pose, once per rendered frame (9 channels) | `NBackHeadPoseLSL` |
| Scene brightness over the view and at the gaze point (up to 30 Hz, 13 channels, identical to CLAdVR's) | `CLAdSceneLuminanceLSL` |

Alongside these, the **motion recorder** (CLAd_EEG project,
`scripts/motion_lsl_recorder.py`) publishes headset and controller motion from SteamVR as
`CLAdMotionLSL`, and the **OpenBCI-to-LSL script** (PhysioLabXR checkout,
`physiolabxr/examples/OpenBCIExamples/OpenBCIToLSLInterface.py`) publishes the raw and
preprocessed EEG as LSL streams, and **PhysioLabXR** (experiment preset **CLAd**)
records everything into one time-synchronized recording — the same preset covers the
UNback calibration and the CLAdVR training phases, so one continuous recording spans
both (see 4b). LabRecorder/XDF remains a supported alternative recorder. Independently of LSL, the app writes a **JSONL backup log** of every task
event to disk, so task data survives even if LSL recording fails.

```
  Varjo XR-3 HMD  <====>  Varjo Base (compositor, tracking, gaze)
        ^
        | display / gaze / pose
        v
+--------------------------------------+
|  Unity app (uNback, VR-N-Back scene) |         LSL (localhost / LAN)
|                                      |
|   NBackMarkersLSL      (markers) ----+----------------+
|   UNbackEventMarkerLSL (events)  ----+-------------+  |
|   VarjoEyeTrackingLSL  (gaze)    ----+----------+  |  |
|   NBackHeadPoseLSL     (pose)    ----+-------+  |  |  |
|                                      |       |  |  |  |
|   JSONL backup -> NBackSessions/     |       v  v  v  v
+--------------------------------------+     +---------------+
                                             |  LabRecorder  | --> session.xdf
  OpenBCI EEG cap --> OpenBCI GUI -----------+---------------+
                      (EEG LSL stream)          ^
                                                |
  (optional) CLAdVR-side LSL streams -----------+
  (optional) PhysioLabXR as live visualizer / alternative recorder
```

---

## 2. One-time setup (per lab PC)

### 2.1 Required software

| Software | Version / source | Purpose |
|---|---|---|
| Unity Hub + **Unity 6000.3.1f1** | Install the editor via Unity Hub | Runs the task |
| **Varjo Base** | **>= 4.6**, from varjo.com | XR-3 driver/compositor, gaze calibration |
| **VarjoUnityXRPlugin 3.7.3** | Installed automatically via the project's `Packages/manifest.json` (git URL) — no manual step | Unity <-> Varjo bridge |
| **OpenBCI GUI** | openbci.com | EEG acquisition, publishes the EEG LSL stream |
| **SteamVR** | Steam | Tracks the Index controllers; the motion recorder reads all poses from it |
| **CLAd_EEG Python environment** | The environment that runs the CLAd-EEG service (e.g. the `clad_eeg` conda env), plus `pip install openvr` once | Runs the motion recorder (`pylsl` + `openvr`) |
| **LabRecorder** | From the labstreaminglayer GitHub releases | Records all LSL streams to XDF |
| **PhysioLabXR** (optional) | physiolabxr.org | Live visualization of the streams, or as an alternative recorder |

Also installed automatically via the manifest: the LSL Unity plugin
(`com.physiolabxr.unityplugin`, which ships `lsl.dll`), XR Interaction Toolkit 3.3.1 and
XR Plug-in Management 4.4.0.

### 2.2 Opening the project the first time

1. In Unity Hub, add the project folder and open it with **Unity 6000.3.1f1**.
2. The first open takes a while: Unity resolves the git packages (Varjo plugin, LSL
   plugin) and recompiles. Wait until the progress bar in the bottom-right disappears and
   the Console shows no compile errors. An internet connection is required for this first
   package resolution.
3. Run the menu item **NBack VR > Enable Varjo XR Loader**. This checks the Varjo loader
   in *Project Settings > XR Plug-in Management* for Standalone. If it reports an error,
   do it manually: *Edit > Project Settings > XR Plug-in Management > Windows tab > tick
   "Varjo"*.
4. Run **NBack VR > Build or Repair VR-N-Back Scene**. This opens
   `Assets/Scenes/VR-N-Back.unity` and (idempotently — safe to re-run any time) creates or
   repairs: the XR rig ("XR Origin (VR)" with a head-tracked camera), the VR-capable
   EventSystem, the task canvas placed at eye height (position `(0, 1.6, 1.5)`, i.e. 1.5 m
   in front of the participant), and the "LSL Streaming" object holding all streaming
   components. It also adds the scene to Build Settings.
5. Run **NBack VR > Validate Setup** and read the PASS/FAIL report in the Console. It
   checks: required packages present, Varjo loader assigned, scene wiring complete, and
   the input setting (`activeInputHandler: 2`) that makes both keyboard and VR controllers
   work.

### 2.3 Settings that must NOT be changed

- **Graphics API must stay Direct3D 11 or Direct3D 12** (the project default). Varjo does
  not work with other APIs on Windows.
- **Keep the Built-in Render Pipeline. Do NOT add/convert to URP.** URP is broken in
  combination with the Varjo plugin on Unity 6 (Varjo known issues).
- Keep `Active Input Handling` = *Both* (the project is configured this way; the old
  keyboard input path and the new VR controller input path both depend on it).

---

## 3. Configuring the n-back task

Everything task-related lives on the **TextTaskCanvas** object (a prefab instance in the
`VR-N-Back` scene). Select it in the Hierarchy and look at the Inspector.

### 3.1 Component tour (TextTaskCanvas root)

| Component | What it does / what you edit |
|---|---|
| `InfoBehaviour` | The instruction screen shown first. Edit its text field to change participant instructions. It stays on screen until the participant responds (Enter / trigger). |
| `FocusBehaviour` | The fixation screen ("+") shown before a block. Edit its duration (seconds). |
| `NBackBehaviour` | The n-back block itself — see 3.2. |
| `TaskManagerBehaviour` | Runs the queue of tasks. No per-study settings. |
| `KeyboardInputBehaviour` | Maps the **Enter/Return key** to a response. |
| Child object `Text` | The legacy UI Text element that displays everything (Arial, 64 pt). Leave as is. |

The **"LSL Streaming"** scene object holds `NBackMarkerStreamer`, `NBackEventFileLogger`,
`VarjoGazeLSLStreamer`, `HeadPoseLSLStreamer` and `ControllerResponseInput`. Defaults are
correct; things you may want to touch:

- `VarjoGazeLSLStreamer.calibrationKey` — key that triggers gaze calibration in-app
  (default **F9**).
- `NBackEventFileLogger.customLogDirectory` — leave empty for the default backup-log
  location (see 4, step 12).
- `NBackMarkerStreamer.streamMarkers` — untick only to silence marker streaming (the JSONL
  backup still logs everything).

### 3.2 NBackBehaviour fields

| Field | Default | Meaning |
|---|---|---|
| `_n` | 0 | The n of the n-back. `0` = press on the letter **X**; `n >= 1` = press when the letter matches the one n positions back. |
| `_characterSet` | 40 letters, 12 of them X | The exact letter sequence shown, in order. Edit it directly for a hand-crafted sequence (one letter per entry). |
| `_stimulusDuration` | 0.5 | Seconds each letter is shown. |
| `_interStimulusDuration` | 2.5 | Seconds of blank screen between letters (this is also the response window's tail — responses count during the letter *and* the following blank). |
| **Sequence Generation (optional)** | | |
| `_generateSequence` | off | If ticked, `_characterSet` is ignored and a sequence is generated at runtime (and fully recorded in the `task_start` marker, so nothing is lost for analysis). |
| `_sequenceLength` | 40 | Number of trials in the generated sequence. |
| `_targetRate` | 0.3 | Fraction of eligible positions that are targets. |
| `_randomSeed` | 0 | Seed for reproducible sequences. **0 = a time-based seed** (different every run). Use a fixed non-zero seed per participant/condition if you need reproducibility. |

With generation off and defaults untouched, the task behaves exactly like the original
uNback package.

### 3.3 Multi-block sessions

`TaskManagerBehaviour` collects **all** `TextTaskBehaviour` components in its children
**in hierarchy order** at startup and runs them one after another. The standard pattern
per block is **Info → Focus → NBack**:

- Info screen ("Press the trigger when ...") — advances on Enter/trigger,
- Focus fixation ("+") — fixed duration,
- the n-back block.

To build e.g. a 0-back block followed by a 2-back block, add a second set of
`InfoBehaviour`/`FocusBehaviour`/`NBackBehaviour` components (on the canvas root or on
child objects — child objects are tidier and their top-to-bottom hierarchy order defines
the run order) and set `_n = 2` on the second `NBackBehaviour`. The session ends
automatically after the last task (a `session_end` marker is emitted).

### 3.4 Responding and feedback

- A response is **Enter/Return** on the keyboard **or** a **VR controller trigger**
  (left or right hand) **or** the controller's **primary button**. All are equivalent.
- On an **incorrect** response the displayed text turns **red** as immediate feedback to
  the participant.
- Repeated presses within one trial are all recorded (see the `press=` field of the
  `response` marker) but do not change scoring.

---

## 3b. Tool-based n-back (VR-Tool-NBack scene)

Alternative to the text task for calibrating the EEG **working-memory (WM) load**
classifier used with the CLAdVR machining training: instead of letters, the stimuli
are the machining **tool models from the `_MITmech` tool board** (chuck, chuck key, drill bits, edge finder, mallet, calipers, ...). Each
tool appears at a **pseudo-random position on a curved fan panel** around the user
(cylinder section, like a VR virtual desktop), so the participant also moves gaze and
head. Responses and all LSL markers/JSONL logging work exactly like the text task
(the `letter=` field of the markers carries the tool name instead of a letter).

- Open `Assets/Scenes/VR-Tool-NBack.unity`. To (re)create it, run
  **NBack VR > Build Tool NBack Scene** — it clones the VR-N-Back scene, keeps the XR
  rig and LSL streamers, and swaps the text task for the tool task.
- Select the **Tool NBack** object. On **ToolNBackManagerBehaviour** set the
  experimenter parameter **Level** (the WM load level):

  | Level | Task | Pace (stimulus / ISI) | Trials | Duration |
  |---|---|---|---|---|
  | `Low` | 0-back — respond whenever the designated target tool appears | 1.5 s / 2.0 s | 13 | 45.5 s |
  | `Optimal` | 2-back — respond when the tool matches the one 2 back | 1.5 s / 2.0 s | 13 | 45.5 s |
  | `High` | 3-back — respond when the tool matches the one 3 back | 1.5 s / 2.0 s | 13 | 45.5 s |

  All three levels deliberately share identical timing and trial count, so **N — the
  WM load — is the only difference between conditions**: every block records the same
  duration (equal spectral epochs), stimulus/target event counts are near-equal
  (4/3/3 targets per block), and the EEG contrast between levels cannot be confounded by
  stimulus rate. Blocks are short on purpose: the multi-block session (3c) runs four
  of them per level.

  Tick **Override Preset** to tune N, durations, trial count, target rate and tool
  pool size manually. **Random Seed** = 0 gives a fresh sequence each run; any other
  value reproduces the identical sequence and stimulus positions.
- The curved panel (radius, horizontal arc, height band, minimum distance between
  consecutive stimulus positions) and the tool display size/spin are also inspector
  parameters on the same component. **Panel Pivot Height Meters** sets the height of
  the panel center above the floor in meters (default 1.6) — set it to the
  participant's eye height; the scene-view gizmo follows the value live.
- Pressing Play **without** the headset: the rig uses a floor tracking origin, so the
  camera would normally sit on the floor. A `DesktopViewFallback` on the rig's Camera
  Offset raises the camera to the same eye height when no XR device is active, so the
  Game view faces the panel; with the headset connected it does nothing.
- **Tool Prefabs** ships with a curated 10-tool list (Mallet, Chuck, Brush, Edge Finder,
  Parallels, Chuck Key, Deburring Tool, one Drill Bit, Center Drill, Block). Only one of
  the nine drill-bit variants is included on purpose — the bits are too similar to tell
  apart as memory stimuli. The first *Tool Pool Size* entries (default 8) form the
  stimulus pool, so the list order matters. Edit the list in the inspector to change the
  pool; if you empty it, all tool prefabs are auto-loaded from `_MITmech` with the drill
  bits again collapsed to one.
- **0-back target**: by default the target tool is picked at random from the pool each
  run (**Randomize Target Tool**; reproducible with a fixed Random Seed) and the actual
  3D tool is shown spinning at the panel center during the countdown, above the written
  instruction, so the participant sees exactly what to respond to. Untick the option to
  always use the first Tool Prefabs entry instead. The chosen target's name appears in
  the instruction text and in every stimulus marker (`letter=`).
- Participant responds like in the text task: **controller trigger / primary button**,
  or **Enter/Space** on the keyboard.
- **Run modes (XR loader, since 2026-08-29):** the project has two Standalone XR
  loaders, tried in order. **Varjo (first) = recording mode**: full native eye
  tracking (gaze + pupil for the LSL stream), but on the current lab
  Varjo-Base/SteamVR combination the Index controller **buttons never arrive**
  (poses only) — use the keyboard for responses. **OpenXR (fallback) = practice/
  demo mode**: initializes when Varjo Base is closed (or the Varjo loader is
  unticked in Project Settings > XR Plug-in Management); Index controller
  buttons work via the Valve Index interaction profile, but the Varjo gaze/pupil
  stream is unavailable — never record calibration sessions in this mode.
- **Show Response Feedback** (ON by default in this scene since 2026-08-28, treating
  it as the practice/demo scene): flashes the visible tool with a green
  glow on a correct press and red on an incorrect press for ~0.4 s (colors, duration
  and glow intensity are inspector fields). The flash is a plain material tint on the
  tool itself — screen-space outline effects are deliberately avoided because they
  mismatch the Varjo XR-3's multi-view stereo rendering and spam per-frame errors.
  **If you record EEG with THIS single-block scene, untick the box first** — feedback
  flashes evoke their own EEG response, and because error rates grow with N, that
  response would differ by condition and confound the WM-load calibration. Recorded
  sessions should use the multi-block scene (section 3c), which suppresses feedback in
  recorded task blocks in code regardless of this checkbox. Note that only presses get
  feedback; a missed target shows nothing.
- On-screen instructions (with a countdown, default 5 s) are shown before the run and
  a summary at the end with the signal-detection breakdown: hits/targets, false
  alarms, misses, correct rejections, plus the raw correct count. Judge engagement by
  **hits and false alarms**, not percent correct — a participant who never responds
  still scores ~72% "correct" from correct rejections alone (all targets missed), and
  the summary makes that visible as `Hits: 0/…`.

---

## 3c. Multi-block sessions (VR-Tool-NBack-MultiBlock scene)

One Play session — and therefore **one continuous recording** — that runs rest
baselines, an optional practice block, and counterbalanced, repeated WM-load task
blocks back to back. Open `Assets/Scenes/VR-Tool-NBack-MultiBlock.unity`: it is the
tool scene with **Multi Block Session** ticked on the **Tool NBack** object.

- **Default plan**: a long opening rest baseline (**Baseline Rest Duration**,
  default 90 s — it feeds CLAd-EEG's auto-baseline and the frozen EEG
  artifact-model fit) → practice (Optimal level, feedback allowed) → then
  `Repetitions Per Level` (4) x the three levels in the participant's counterbalanced
  order, **each repetition rotated by one** (A-B-C, B-C-A, C-A-B, A-B-C) — average
  serial positions stay near-balanced within the participant (the classifier is
  calibrated per person, so fatigue/drift must not correlate with level inside
  one session) and no level ever runs in two consecutive blocks.
  **Why four 45-s blocks instead of two 91-s blocks:** the task time per level is the
  same, but with four blocks a classifier can be validated by holding out whole
  blocks, so slow drift between blocks cannot pass for load.
  After the practice stage and after every recorded block the participant gives an
  **effort rating** (below), then a short washout rest follows (**Inter Block Rest
  Duration**, default 15 s — with the few seconds of the rating the gap between
  blocks stays about 20 s, long enough for tonic pupil and load state to settle; the
  final washout doubles as the closing baseline). With 4 repetitions: 1 practice +
  12 task blocks + 13 ratings + 14 rests, about 17 minutes with one practice round —
  the inspector's plan preview shows the estimated length (ratings counted at about
  6 s each) whenever you change repetitions or rest durations.
- **Effort rating** (Effort Probe section of the inspector, on by default): a
  single-item mental-effort rating, the Paas 9-point scale — "How much mental effort
  did you invest in the task you just finished?", 1 = *very, very low mental effort*,
  9 = *very, very high mental effort*. It appears in front of the participant right
  after the block ends, while the block is fresh. **Use exactly the same question and
  scale for the drilling task's effort probes in CLAdVR**, so both tasks share one
  rating scale (question, anchors and number of points are inspector fields).
  - **Participant controls:** push the thumbstick left/right — the first push lands
    on 5, further pushes step by one — then press A or the trigger to confirm. A
    press within 0.5 s of the scale appearing is ignored (it is a leftover n-back
    response), and a press with no number chosen does nothing.
  - **Keyboard:** left/right arrows (or A/D) and Enter/Space do the same. In
    recording mode the Index buttons may not arrive (3b, Run modes): the scale's hint
    line also invites the participant to **say the number** — type it with the digit
    keys **1–9** (number row or numpad, Game view focused); a digit answers at once.
  - **Timeout** (default 20 s, 0 = wait): an unanswered rating is skipped and logged
    as missing (`rating=na`), and the session continues.
  - If the stick does not move the selection in-headset, tick **Log Controller
    Buttons** on the Tool NBack Response Input, push the stick, and add the logged
    path to its **Extra Navigation Bindings**.
  - Rating windows are not training data: they follow `task_end`, so CLAd-EEG leaves
    them unlabeled. Untick **Effort Probe After Blocks** to run the session without
    ratings.
- **Counterbalance Index**: set it to the participant number. It selects one of the
  6 level orders (applied modulo 6) — write it down with the session.
- **Random Seed**: same semantics as the single-block scene. A fixed non-zero seed
  now derives a *distinct but reproducible* seed per block, so repeated blocks of
  one level get different sequences, yet the whole session reproduces exactly for
  the same seed + counterbalance settings. Seed 0 stays time-based (unreproducible).
- The **0-back target tool is chosen once per session** (seeded), so every Low
  block responds to the same tool; it is previewed during those blocks' countdowns.
- **Show Response Feedback** affects only the practice block. Recorded task blocks
  never flash feedback, even when the box is ticked — feedback-evoked potentials
  scale with error rate and would confound the WM-load contrast.
- **Practice is experimenter-paced.** It repeats in rounds (13 trials each, a fresh
  sequence and the countdown between rounds, shown as "PRACTICE (round N)") until
  you end it: in Play mode select the **Tool NBack** object and click **End Practice
  → Next Stage** at the top of its inspector. The session then continues with the
  effort rating (it doubles as the participant's first go at the scale), the
  washout rest and the recorded blocks. The button does nothing outside practice,
  so a recorded block can never be cut short.
- Rest screens show "Rest — relax, keep still" with a countdown; each task block is
  preceded by its instruction + countdown; the session ends with an aggregate
  signal-detection summary over the recorded task blocks (practice excluded).
- **Custom Blocks** (inspector list): when non-empty it replaces the default plan —
  compose any order of Rest / Practice / Task entries (with level and rest duration)
  by hand.

Markers: `session_start|tasks=<number of blocks>` fires once when Play starts (the
plan is prepared), unlike the single-block scene where it fires when the countdown
ends. Every block — rests included — is bracketed by `task_start`/`task_end`
carrying its block index (see 5.2). Trial indices restart at 0 in every task block;
segment by `task_start`/`task_end` exactly as for multi-block text sessions.

---

## 4. Session-day run checklist

Do these **in order**. Streams only exist while the Unity app is in Play mode, so the
order matters.

1. **Power on the Varjo XR-3** and start **Varjo Base**. Wait until Varjo Base shows
   green/OK status for the headset (tracking + display). Make sure **SteamVR** is running
   too (it tracks the Index controllers), then start the **motion recorder** from the
   CLAd_EEG project, in the same Python environment as the CLAd-EEG service (2.1):
   `python scripts/motion_lsl_recorder.py`. It publishes headset and controller poses as
   `CLAdMotionLSL` and waits for SteamVR by itself; leave its window open until the
   session ends.
2. **Fit the EEG cap**, connect the OpenBCI board, and open the **OpenBCI GUI**. Start the
   data stream, then start its **LSL output** (Networking widget → protocol LSL → Start).
   **Name the stream and write the name down** — you will need it to find the EEG in
   LabRecorder and in analysis (it is the only stream whose name is not fixed by this
   project).
3. **Launch the task**: open the project in Unity and press **Play** with the
   `VR-N-Back` scene open (or start the standalone build if you made one). The four
   `NBack*`/`Varjo*` LSL streams and `CLAdSceneLuminanceLSL` (scene brightness, 5.7) are
   created at startup.
4. Put the headset on the participant and run **Varjo gaze calibration**: press **F9**
   (with the Unity/app window focused) or trigger calibration from Varjo Base. The
   participant follows the dots. **No gaze data is streamed until calibration succeeds.**
5. Open **PhysioLabXR** and add the **CLAd** experiment preset (it contains
   `VarjoEyeTrackingLSL`, both OpenBCI EEG streams, `CLAdEventMarkerLSL`,
   `MachineStateLSL`, `UNbackEventMarkerLSL`, `CLAdMotionLSL`,
   `CLAdSceneLuminanceLSL`, and the CLAd-EEG service's two outputs: `CLAdCognitiveLoadLSL`,
   its live estimate, and `CLAdLoadStateLSL`, the load state the CLAdVR Instructor reads).
   Click **Start on every stream** —
   streams that are not live yet (the CLAdVR-only ones during calibration) arm
   themselves and start automatically the moment they appear. If a stream shows as
   missing entirely, see Troubleshooting (section 8) before continuing.
   *(LabRecorder alternative: tick `NBackMarkersLSL`, `UNbackEventMarkerLSL`,
   `VarjoEyeTrackingLSL`, `NBackHeadPoseLSL`, `CLAdSceneLuminanceLSL`, `CLAdMotionLSL`,
   and the EEG streams, then record to XDF.)*
6. Set the recording metadata in PhysioLabXR's Recording tab (experiment `CLAd`,
   subject = participant ID, session number).
7. **Start the recording NOW — before the participant starts the task.** The
   `session_start` and first `task_start` markers must be inside the recording.
8. Give the participant their instructions verbally; the on-screen Info text is shown in
   the headset as a reminder.
9. The participant presses **Enter or the trigger** to advance past the Info screen; the
   fixation cross and then the task follow automatically.
10. During the task: monitor the PhysioLabXR stream plots (samples flowing on every
    stream, EEG signal quality visible on the preprocessed stream). Before the first
    block, check the two pilot streams once: wave each controller and watch its
    `tracked` flag and velocity move in `CLAdMotionLSL`; in `CLAdSceneLuminanceLSL`,
    `gaze_valid` should stay at 1 while the participant looks around. Then have them
    keep the head still and look up at something bright and down at something dark:
    `gaze_v` must go above and then below 0.5, and `gaze_luminance` must follow the
    gaze (bright up, dark down) more than `view_luminance` does. If it follows the
    opposite way, stop and report it — the brightness data would be mirrored.
11. The session **ends by itself after the last task** — the app emits a `session_end`
    marker. **If the CLAdVR training phase follows, do NOT stop the recording** —
    continue with the switch procedure in 4b. Only stop when the whole session is over.
12. Where the data is:
    - **PhysioLabXR recording** (`.dats`, converted on stop to your configured format)
      — in the configured recordings folder (default `~/Documents/Recordings`),
      named `MM_DD_YYYY_HH_MM_SS-Exp_CLAd-Sbj_<id>-Ssn_<n>`. This is the primary data.
    - **JSONL backup of all task events** — written automatically to
      `%USERPROFILE%\AppData\LocalLow\DefaultCompany\nbacktask\NBackSessions\Session_<yyyyMMdd_HHmmss>.jsonl`
      (the folder name comes from the Unity product name, which is `nbacktask`)
      (one file per app start, one JSON object per line, flushed continuously). Keep it
      with the XDF; it is your fallback if LSL recording failed.

---

## 4b. Continuous calibrate-then-drill sessions (UNback → CLAdVR)

The full study runs phases 3 and 4 in **one continuous recording with the headset
kept on**, and with the **CLAd-EEG service running throughout**:

1. Before the participant starts: launch the OpenBCI-to-LSL script and the motion
   recorder (`python scripts/motion_lsl_recorder.py`, CLAd_EEG project; section 4
   step 1) — both run independently of Unity and never stop — start PhysioLabXR with
   the CLAd preset (all streams started/armed, recording running — section 4), and launch the
   CLAd-EEG live service:
   `python scripts/online_cognitive_lstm.py --participant <ID>` (CLAdEEG project).
2. Run the UNback multi-block session (`VR-Tool-NBack-MultiBlock`, section 3c).
   Once the participant has the hang of the practice, click **End Practice → Next
   Stage** in the Tool NBack inspector — practice does not end on its own. CLAd-EEG reads `UNbackEventMarkerLSL` and collects **labeled** calibration data:
   rest blocks feed its baseline, task blocks feed the low/optimal/high training
   pools; practice is ignored. UNback's `session_end` **freezes the model**.
3. **Keep the headset on the participant.** Stop Unity Play mode in UNback and open
   the CLAdVR project/build. The Varjo gaze stream, the marker streams and
   `CLAdSceneLuminanceLSL` reappear under the same names (`CLAdMotionLSL` never stops;
   check that both controllers still show `tracked` = 1 in it before drilling)
   — PhysioLabXR's armed widgets pick them up automatically;
   glance at the stream plots to confirm data is flowing again. CLAd-EEG follows
   on its own: its console logs `... was lost` when UNback stops, then
   `Attached pupil stream` / `Attached event stream` once CLAdVR is playing. Do a quick EEG
   signal check (electrodes can shift; the calibration is only as good as the
   contact quality that outlives it).
4. Run the drilling task. CLAd-EEG is now in **estimation mode**, publishing
   `CLAdCognitiveLoadLSL` from the phase-3 calibration; it never refits during the
   task, and no CLAdVR marker resets the calibration.
5. When the drilling task ends: stop the PhysioLabXR recording, stop the CLAd-EEG
   service and the motion recorder (Ctrl+C in each window), then take the headset and
   cap off.

Two rules this protocol depends on: the EEG script and CLAd-EEG service are never
restarted mid-session (the calibration lives in the service's memory), and the
Unity switch happens between — never during — recorded blocks.

**Pilot sessions** run this exact protocol, in this exact order: UNback, then the
60-s rest in CLAdVR's drill scene, then drilling. The pilot's job is to test whether
the n-back calibration carries over to drilling across the real gap (the model is
frozen about a minute before drilling starts; by the end of drilling its
calibration data is up to about 40 minutes old), so do not shorten, reorder or
pause between the phases. The drilling task's effort probes must use the same
question and 1–9 scale as UNback's ratings (3c), so both tasks share one scale.

---

## 5. Stream reference and marker dictionary (analysis contract)

Everything in this section is exact and stable — analysis code should be written against
it.

### 5.1 The Unity streams

| Stream name | type | channels | nominal rate | format | source_id |
|---|---|---|---|---|---|
| `NBackMarkersLSL` | Markers | 1 | irregular (0) | string | `uNback-markers` |
| `UNbackEventMarkerLSL` | EventMarker | 9 | 75 (declared; events are sporadic) | float32 | (empty) |
| `VarjoEyeTrackingLSL` | GazeData | 39 | 200 | double64 | (empty, like CLAdVR) |
| `NBackHeadPoseLSL` | HeadPose | 9 | irregular (0) | double64 | `uNback-headpose` |
| `CLAdSceneLuminanceLSL` | Luminance | 13 | 30 | float32 | (empty; CLAdVR publishes the same stream) |

`CLAdMotionLSL` (5.8) is not a Unity stream: the CLAd_EEG motion recorder publishes it
from SteamVR for the whole session. Nor is `CLAdCognitiveLoadLSL`, the CLAd-EEG service's
live estimate: 9 float32 channels (`COGNITIVE_LOAD_OUTPUT_LABELS` in the CLAd_EEG
project's `scripts/config.py`), one sample per 1-s step. The stream declares an irregular
rate; the PhysioLabXR preset records it at 1 Hz. Beside it the service publishes
`CLAdLoadStateLSL` (13 float32 channels, 1 Hz), the CLAdVR Instructor's input: the held
underload/optimal/overload state plus the flags that say when it may be used. Its channels
and rules are in the CLAd_EEG project's `docs/clad-load-state-lsl.md`.

`NBackMarkersLSL` and `UNbackEventMarkerLSL` describe the **same events**; each marker
string and its numeric event sample are pushed with the **same LSL timestamp**. Use the
string stream for readability (LabRecorder/debugging) and the numeric stream for
PhysioLabXR and analysis pipelines — PhysioLabXR cannot record string streams, which
is why the numeric stream follows the CLAd conventions (float32, positive declared
rate, channel 0 = Unity time).

### 5.2 Marker string dictionary (`NBackMarkersLSL`)

Format: an event name, then `|`-separated `key=value` fields. Floats have 3 decimals
unless noted. `<c>` is a single letter. Sequences are concatenated letters (no commas);
target vectors are strings of `0`/`1` aligned with the sequence. Free text has `|` and
newlines replaced before embedding.

| Marker | When |
|---|---|
| `session_start\|tasks=<int>\|unity_time=<f3>` | App entered Play, task queue prepared. `tasks` = number of tasks in the session. |
| `task_start\|index=<int>\|type=nback\|n=<int>\|trials=<int>\|stim_dur=<f3>\|isi=<f3>\|sequence=<e.g. SPDXF...>\|targets=<e.g. 00101...>` | An n-back block starts. Contains the **complete ground truth**: full letter sequence and 0/1 target vector. |
| `task_start\|index=<int>\|type=info\|text=<...>` | Instruction screen shown. |
| `task_start\|index=<int>\|type=focus\|duration=<f1>` | Fixation screen shown. |
| `task_start\|index=<int>\|type=rest\|duration=<f1>` | A rest baseline begins (multi-block tool sessions, section 3c). Closed by a `task_end\|...\|type=rest`. |
| `stim_on\|trial=<int>\|letter=<c>\|target=<0\|1>\|n=<int>` | A letter appears. |
| `stim_off\|trial=<int>\|letter=<c>\|responded=<0\|1>` | The letter disappears (ISI begins). `responded` = whether a press already occurred during the stimulus. |
| `response\|task_type=<nback\|info\|focus>\|trial=<int>\|letter=<c>\|target=<0\|1>\|class=<hit\|false_alarm\|na>\|rt_stim=<f3\|na>\|rt_trial=<f3\|na>\|phase=<stimulus\|isi\|na>\|press=<int>` | Every keypress/trigger pull. `rt_stim` = seconds since stimulus onset (when available); `rt_trial` additionally covers ISI presses when available. `press` = 1-based count of presses within the current trial. |
| `trial_result\|trial=<int>\|letter=<c>\|target=<0\|1>\|responded=<0\|1>\|outcome=<hit\|miss\|false_alarm\|correct_rejection>` | Trial scored at the end of its response window. **One per trial, including the final trial** — this is the complete per-trial scoring record. |
| `task_end\|index=<int>\|type=<...>\|n=<int>\|correct=<int>\|error_rate=<f3>` | Task finished. `correct`/`error_rate` keep the historical semantics (see 7). |
| `effort_probe\|index=<int>\|n=<int>\|scale=<min>-<max>` (+ `\|practice=1`) | The effort rating appeared after block `index` (multi-block tool sessions, 3c). |
| `effort_rating\|index=<int>\|n=<int>\|rating=<int\|na>\|rt=<f3\|na>\|scale=<min>-<max>\|timeout=<0\|1>` (+ `\|practice=1`) | The rating closed. `rating` = the chosen number; `rt` = seconds from the scale appearing to the answer; `rating=na`, `rt=na`, `timeout=1` when no answer came before the timeout. |
| `session_end\|unity_time=<f3>` | Last task finished. |
| `display_on\|text=<...>` | Display-truth: the on-screen text changed to a non-empty value (fires for letters, instructions, fixation cross alike). |
| `display_off\|prev=<...>` | Display-truth: the screen went blank; `prev` is the text that was removed. |

Notes:
- **Trial indices are 0-based and restart at 0 in every n-back block.** In multi-block
  sessions, segment by `task_start` / `task_end` (using their `index=` field) before
  matching trials.
- **Practice blocks** (multi-block tool sessions) append `|practice=1` to their n-back
  `task_start` marker; regular blocks carry no `practice` field, so the historical
  format is unchanged. Exclude `practice=1` blocks from calibration analysis.
  Practice can span several rounds: each round is its own `task_start`/`task_end`
  pair with the same `index=`. When the experimenter ends practice mid-round, the
  open trial still gets its `stim_off` and the round its `task_end` before the rest.
- `display_on`/`display_off` are emitted by an end-of-frame comparison of the actual UI
  text, independent of task logic — they are the ground truth of what was on screen.
- **Effort ratings** sit between a block's `task_end` and the next rest's
  `task_start`; their `index=` is the rated block. The practice stage gets one
  rating (`practice=1`) after the experimenter ends it.

### 5.3 Numeric event samples (`UNbackEventMarkerLSL`, 9 channels, float32)

Channel layout (`-1` means "not applicable" except on channels 0 and 5):

| Ch | Content |
|---|---|
| 0 | **Unity time** (`realtimeSinceStartup`, s) — for LSL↔Unity clock-offset recovery, exactly like `CLAdEventMarkerLSL` channel 0 (offset = median(ch0 − LSL timestamps)) |
| 1 | event code (below) |
| 2 | block index (the `index=` of the surrounding `task_start`; -1 before the first block) |
| 3 | trial index (0-based; -1 if n/a) |
| 4 | is target (1/0; -1 if n/a) |
| 5 | outcome code (below; 0 = n/a) |
| 6 | reaction time in **ms** (-1 if n/a) |
| 7 | n of the n-back (-1 if n/a) |
| 8 | **block kind**: `0` rest baseline, `1` practice, `2` recorded task block, `-1` other |

Channels 2 and 8 carry the block context of the most recent `task_start`, stamped on
every event of that block — a purely numeric pipeline can segment and label
calibration windows (rest / practice / low / optimal / high via channel 7) without
parsing any strings. This is what drives CLAd-EEG's phase-3 calibration mode.

Event codes (channel 1):

| Code | Event | Channels also filled |
|---|---|---|
| 1 | session_start | — |
| 2 | task_start | 2, 8 (block context), 7 (n, n-back only) |
| 3 | stim_on | 2, 3, 4, 7, 8 |
| 4 | stim_off | 2, 3, 8 |
| 5 | response | 2, 3, 4, 5 (hit/false alarm), 6 (rt_stim in ms, when available), 8 |
| 6 | trial_result | 2, 3, 4, 5, 8 |
| 7 | task_end | 2, 8, 7 (n, n-back only) |
| 8 | session_end | — |
| 9 | display_on | — |
| 10 | display_off | — |
| 11 | effort_probe | 2, 7, 8 (the rated block) |
| 12 | effort_rating | 2, 7, 8 (the rated block), **5 = the rating** (0 = none, timed out), 6 (response time in ms, -1 = none) |

Outcome codes (channel 5): `0` n/a, `1` hit, `2` miss, `3` false_alarm,
`4` correct_rejection. On `effort_rating` (code 12) channel 5 carries the rating
instead, so read it by event code.

**Historical note:** this stream replaces the earlier 6-channel `NBackEventCodesLSL`
(no data was ever collected against that contract). The event and outcome code
dictionaries are unchanged.

### 5.4 `VarjoEyeTrackingLSL` (39 channels, double64, nominal 200 Hz)

Channel layout (byte-compatible with the CLAdVR gaze stream). Gaze vectors are in the
HMD/tracking frame as provided by the Varjo SDK. Blocks are zeroed when the corresponding
validity/status flag is Invalid.

| Ch | Content |
|---|---|
| 0 | Varjo frame number |
| 1 | capture timestamp, LSL time domain (same value used as the sample's timestamp) |
| 2 | wall-clock time (`DateTime.Now` ticks / ticks-per-millisecond) |
| 3–5 | HMD camera local position x, y, z |
| 6–8 | HMD camera local rotation quaternion x, y, z (w intentionally omitted for CLAdVR compatibility — full pose is in `NBackHeadPoseLSL`) |
| 9 | combined gaze valid (1/0) |
| 10–12 | combined gaze forward x, y, z |
| 13–15 | combined gaze origin x, y, z |
| 16 | IPD (mm) |
| 17 | left eye valid (1/0) |
| 18–20 | left gaze forward x, y, z |
| 21–23 | left gaze origin x, y, z |
| 24 | left pupil/iris diameter ratio |
| 25 | left pupil diameter (mm) |
| 26 | left iris diameter (mm) |
| 27 | right eye valid (1/0) |
| 28–30 | right gaze forward x, y, z |
| 31–33 | right gaze origin x, y, z |
| 34 | right pupil/iris diameter ratio |
| 35 | right pupil diameter (mm) |
| 36 | right iris diameter (mm) |
| 37 | focus distance (m) |
| 38 | focus stability |

**Timestamps:** gaze samples arrive from the Varjo SDK in per-frame batches; each sample's
LSL timestamp is **back-dated to its capture time** (`local_clock() -` the age of the
sample per Varjo's clock), not the time it was pushed. So gaze timestamps are already
aligned to acquisition — use them directly.

### 5.5 `NBackHeadPoseLSL` (9 channels, double64, one sample per rendered frame)

| Ch | Content |
|---|---|
| 0 | Unity time (`realtimeSinceStartup`, seconds, app-relative — for cross-checking with the JSONL log) |
| 1–3 | HMD world position x, y, z |
| 4–7 | HMD world rotation quaternion x, y, z, w |
| 8 | tracked flag (1) |

### 5.6 JSONL backup log

One JSON object per line, e.g.
`{"event":"stim_on","iso":"<ISO-8601 local>","unity_time":<sec>,"lsl_time":<LSL clock or null>,"marker":"<the marker string>", ...event fields...}`.
The `lsl_time` field lets you re-align the backup with the XDF if needed. First line is a
`log_opened` header.

### 5.7 `CLAdSceneLuminanceLSL` (13 channels, float32, nominal 30 Hz)

Scene brightness for correcting pupil size later. Published by the shared
`Assets/Scripts/Streaming/CLAdRecording/` folder (the same files sit in CLAdVR, so the
stream continues through drilling). A hidden 128 × 128 copy of the main camera renders up
to 30 times a second (90° vertical field of view while the headset is active) and is read
back without blocking a frame. A capture takes at least three rendered frames, so the real
rate depends on the frame rate and the GPU's readback delay (30 Hz at best at 90 fps):
use the timestamps, not the nominal rate. Gaze comes from this app's own
`VarjoEyeTrackingLSL` through an LSL inlet, never through a Varjo API, so the pupil stream
loses no samples; a `VarjoEyeTrackingLSL` published from another machine is ignored.

| Ch | Content |
|---|---|
| 0 | Unity time (`realtimeSinceStartup`) of the captured frame |
| 1 | `view_luminance`: mean luminance of the whole view, 0–1 |
| 2 | `gaze_luminance`: mean luminance of a 2° disc around the gaze point, 0–1; -1 when gaze is invalid or off the view |
| 3 | `gaze_valid`: 1 when `VarjoEyeTrackingLSL` gave a valid combined gaze no older than 0.1 s, else 0 |
| 4–5 | `gaze_u`, `gaze_v`: gaze point in the captured view, 0–1 (u right, v up); -1 when invalid or off the view |
| 6–8 | camera position x, y, z (Unity world, metres) |
| 9–12 | camera rotation quaternion x, y, z, w (Unity world) |

Values are relative screen brightness (Rec. 709 luminance of the sRGB-decoded image), not
cd/m². The LSL timestamp is the moment the captured frame was rendered. Channel 0 is
float32, as in `UNbackEventMarkerLSL`: in Editor Play mode `realtimeSinceStartup` counts
from the Editor's launch, so its resolution coarsens with Editor uptime (about 8 ms after a
day). Restart the Editor before a session day; LSL timestamps are unaffected.

### 5.8 `CLAdMotionLSL` (42 channels, float32, nominal 90 Hz)

Headset and controller motion for down-weighting EEG during fast movement. Published by
`scripts/motion_lsl_recorder.py` in the CLAd_EEG project, which reads SteamVR as a
background application (it never launches SteamVR) and runs for the whole session.

Three blocks of 14 channels, in the order `head`, `left`, `right` (channel names
`<device>_<field>`):

| Offset | Field | Unit |
|---|---|---|
| 0 | `tracked`: 1 when the device is connected and its pose valid; otherwise this block is all zeros | — |
| 1–3 | `pos_x`, `pos_y`, `pos_z` | m |
| 4–7 | `rot_x`, `rot_y`, `rot_z`, `rot_w` (quaternion) | — |
| 8–10 | `vel_x`, `vel_y`, `vel_z` (linear velocity) | m/s |
| 11–13 | `angvel_x`, `angvel_y`, `angvel_z` (angular velocity) | rad/s |

All values are exactly as SteamVR reports them in its standing universe (right-handed:
+x right, +y up, −z forward) — not Unity's left-handed frame. OpenVR does not document the
frame of the angular velocity for certain, so use its magnitude (or check its frame once
against the rotation channels) before relying on its axes. Controllers are found by
hand role on every sample, so a controller that is switched off shows `tracked` = 0 at
once. There is no time channel: the LSL timestamp is the moment the poses were read.
While SteamVR is not running the stream is silent and the recorder retries every 3 s.

---

## 6. Slicing guide for analysis (Python + pyxdf)

Install: `pip install pyxdf numpy`. Minimal example — load the XDF, find streams by name,
epoch EEG around target vs. non-target stimulus onsets, and join per-trial outcomes:

```python
import numpy as np
import pyxdf

streams, header = pyxdf.load_xdf("session.xdf")

def by_name(name):
    return next(s for s in streams if s["info"]["name"][0] == name)

markers = by_name("NBackMarkersLSL")
eeg     = by_name("obci_eeg1")          # <-- your OpenBCI stream name (noted on session day)

# Parse "event|key=value|key=value" marker strings
def parse(m):
    parts = m.split("|")
    d = {"event": parts[0]}
    for p in parts[1:]:
        k, _, v = p.partition("=")
        d[k] = v
    return d

events = [(t, parse(s[0])) for t, s in zip(markers["time_stamps"],
                                           markers["time_series"])]

eeg_t = eeg["time_stamps"]                       # LSL timestamps, shared clock
eeg_x = np.asarray(eeg["time_series"])           # (samples, channels)

def epoch(t0, tmin=-0.2, tmax=0.8):
    i0 = np.searchsorted(eeg_t, t0 + tmin)
    i1 = np.searchsorted(eeg_t, t0 + tmax)
    return eeg_x[i0:i1]

# Stimulus-locked epochs, split by target status
target_epochs    = [epoch(t) for t, e in events
                    if e["event"] == "stim_on" and e["target"] == "1"]
nontarget_epochs = [epoch(t) for t, e in events
                    if e["event"] == "stim_on" and e["target"] == "0"]

# Per-trial behavioral outcomes (hit/miss/false_alarm/correct_rejection)
outcomes = {int(e["trial"]): e["outcome"] for t, e in events
            if e["event"] == "trial_result"}
```

For **multi-block** sessions, first split `events` at `task_start`/`task_end` markers
(trial indices restart per block), then apply the same recipe per block. Reaction times
come from the `response` markers (`rt_stim`), or from channel 6 of `UNbackEventMarkerLSL`.

**`display_on`/`display_off` vs `stim_on`/`stim_off`:** these fire on the same frame —
`stim_on` is the semantic event from the task logic, `display_on` is measured from the
actual UI text at end of frame. For most analyses `stim_on` is what you want. For strict
photon-adjacent alignment use the `display_*` markers, and remember both still precede
light hitting the eye by roughly one frame of rendering plus the HMD's display latency.

---

## 7. Timing notes and known quirks

- **Always use the LSL timestamps.** Never reconstruct event times from the nominal
  stimulus/ISI durations: phase durations are frame-quantized (the internal timer only
  fires on a frame boundary, and overshoot is clamped), so real durations are the nominal
  value rounded up to the frame grid.
- Markers are emitted in the same call stack as the visual change, i.e. same-frame with
  the text commit; photon onset in the HMD follows ~1+ frame later (see section 6).
- Gaze samples are back-dated to capture time (section 5.4) — no extra correction needed.
- **Known quirk (kept intentionally for historical comparability):** the **final trial**
  of an n-back block is excluded from the `correct=` and `error_rate=` values in the
  `task_end` marker. The `trial_result` markers do **not** have this gap — every trial,
  including the last, gets one. **Use `trial_result` for per-trial scoring**; use
  `task_end`'s summary only when comparing against legacy uNback results.
- **Pilot-check your markers once** before the first real participant: record a short
  session and confirm every trial you care about has the markers your pipeline needs
  (note that trial 0's letter is already on screen when the block starts, so its onset
  coincides with `task_start` — the `display_on` markers cover every visual change if you
  need a per-trial display-truth anchor).

---

## 8. Troubleshooting

| Symptom | Likely cause → fix |
|---|---|
| _MITmech tools/models render magenta | Their materials were authored for URP (CLAdVR), but this project uses the Built-in RP → run **NBack VR > Convert MITmech URP Materials to Built-in** once and let it finish; the Console prints a per-material summary and an audit PASSED line. |
| Black screen / no image in the HMD | Varjo loader not enabled → run **NBack VR > Enable Varjo XR Loader** (or tick Varjo in *Project Settings > XR Plug-in Management*). If OpenXR is also installed/checked it can grab the XR session → untick everything except Varjo. If someone added URP → remove it; the project must stay on the Built-in Render Pipeline. |
| Task canvas not visible in the headset | Rig or canvas moved. The rig belongs at the origin and the canvas at `(0, 1.6, 1.5)` → run **NBack VR > Build or Repair VR-N-Back Scene** to restore. |
| No gaze data in `VarjoEyeTrackingLSL` | Gaze not calibrated (streaming is gated on successful calibration) → press **F9** with the app focused, or calibrate from Varjo Base, then check again. |
| `CLAdMotionLSL` shows no data | SteamVR isn't running (the recorder's window says it is retrying) → start SteamVR; the recorder connects by itself. If the window is gone, start `python scripts/motion_lsl_recorder.py` again (CLAd_EEG project). |
| `CLAdMotionLSL` head block stays at `tracked` = 0 while the controllers work | SteamVR doesn't report the Varjo headset to background apps → nothing to fix during a session: head pose is still recorded in `CLAdSceneLuminanceLSL` (channels 6–12) and `NBackHeadPoseLSL`. |
| `gaze_valid` stays 0 in `CLAdSceneLuminanceLSL` | No gaze calibration yet, or `VarjoEyeTrackingLSL` isn't up on this PC → calibrate (F9) and check the gaze stream; brightness channels 0–1 and 6–12 keep recording meanwhile. Only this PC's gaze stream counts: the Unity Console says once if it ignored one from another machine. |
| No LSL streams visible in LabRecorder | Unity not in Play mode; or `lsl.dll` missing (package didn't resolve — check the Console/`Editor.log` for `DllNotFoundException`); or Windows Firewall blocking Unity/LabRecorder → allow both on private networks. Task events are still saved to the JSONL backup even when LSL is down. |
| Enter key does nothing | `Active Input Handling` was reset → it must be **Both** (`activeInputHandler: 2` in `ProjectSettings/ProjectSettings.asset`). Run **NBack VR > Validate Setup** to confirm. |
| Varjo rendering misbehaves on Unity 6 (persistent glitches after the above) | Fallback plan: open the project with **Unity 2022.3.44f1** (installed on the lab PC; the same stack CLAdVR uses). Package versions are pinned, so the project opens there as well. |

When in doubt, run **NBack VR > Validate Setup** — it prints a PASS/FAIL line for each
requirement — and check `%LOCALAPPDATA%\Unity\Editor\Editor.log` for errors.
