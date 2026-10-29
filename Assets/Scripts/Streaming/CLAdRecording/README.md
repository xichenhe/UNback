# CLAd scene luminance recording (`CLAdSceneLuminanceLSL`)

This folder records how bright the scene is, over the whole view and where the participant
is looking, as an LSL stream. The CLAd pilot needs it to correct pupil size for brightness
later. It only records: nothing reads the stream live.

## One folder, two projects

The same files live in two Unity projects. Keep them identical:

- UNback: `Assets/Scripts/Streaming/CLAdRecording/`
- CLAdVR: `Assets/Scripts/CLAdRecording/`

Change both copies together and compare them with `diff -r -x "*.meta" <one> <other>`.
The folder is its own assembly (`CLAd.Recording`). It references only the LSL package
(`labstreaminglayer.LSL4Unity.Runtime`), and nothing else in either project references it.
It works in UNback's Built-in render pipeline and in CLAdVR's URP. Its tests live in UNback:
`Assets/Tests/EditMode/SceneLuminance*Tests.cs`, `LslGazeSourceTests.cs` and
`Assets/Tests/PlayMode/SceneLuminanceStreamerPlayTests.cs`.

## What it does

- After the first scene loads, `SceneLuminanceBootstrap` creates one object,
  **CLAd Scene Luminance**, under DontDestroyOnLoad. No scene, prefab or existing script is
  changed, and the object survives scene changes.
- Up to 30 times a second a hidden 128 × 128 camera copies the main camera (`Camera.main`):
  its pose, clip planes, culling mask and clear settings, with a 90° vertical field of view
  while a headset is active. It renders for one frame, the image is read back with
  `AsyncGPUReadback`, and the luminance is computed when the readback arrives. No frame
  waits for it, and when the GPU is slow a capture is skipped rather than queued, so a
  capture takes at least three rendered frames (30 Hz at best at 90 fps).
  Post-processing is not applied to the capture.
- Gaze comes from the app's own `VarjoEyeTrackingLSL` stream through an LSL inlet
  (channel 9 = combined gaze valid, channels 10–12 = combined forward). It calls **no Varjo
  API**, so it cannot take samples away from the 200 Hz pupil stream. Only a stream published
  from this machine is read: one from another machine on the network would be someone else's
  eyes, and the Console says once if it ignores one. Gaze counts as invalid when channel 9 is
  0, when the newest sample is older than 0.1 s, or when the stream isn't there. When the gaze stream goes away and comes back (a scene change, Play mode stopped and
  started), the inlet re-attaches by itself.

## The stream

`CLAdSceneLuminanceLSL`, type `Luminance`, float32, nominal 30 Hz, empty source_id,
13 channels:

| Ch | Name | Content |
|---|---|---|
| 0 | `unity_time` | Unity time (`realtimeSinceStartup`) of the captured frame |
| 1 | `view_luminance` | mean luminance of the whole view, 0–1 |
| 2 | `gaze_luminance` | mean luminance of a 2° disc around the gaze point, 0–1; -1 when gaze is invalid or off the view |
| 3 | `gaze_valid` | 1 when the eye tracker gave a valid, fresh gaze sample, else 0 |
| 4–5 | `gaze_u`, `gaze_v` | gaze point in the captured view, 0–1 (u right, v up); -1 when invalid or off the view |
| 6–8 | `view_pos_x/y/z` | camera position, Unity world, metres |
| 9–12 | `view_rot_x/y/z/w` | camera rotation quaternion, Unity world |

Values are relative screen brightness (Rec. 709 luminance of the sRGB-decoded image, 0–1),
not cd/m². The LSL timestamp is the moment the captured frame was rendered, not the moment
the readback arrived. The PhysioLabXR `CLAd` experiment preset records this stream.

## Settings

The defaults are the field initialisers of `SceneLuminanceStreamer`: capture size 128,
capture rate up to 30 Hz, gaze patch radius 2°, headset field of view 90°, maximum gaze age
0.1 s, gaze stream `VarjoEyeTrackingLSL`. Change them in both copies.

To pause the stream during a session, untick the **CLAd Scene Luminance** object (under
DontDestroyOnLoad in the Hierarchy): capturing stops at once and the stream goes quiet. To
remove it for good, delete this folder — in CLAdVR that is all. In UNback, also delete the
tests listed above and the `CLAd.Recording` references in the EditMode and PlayMode test
assembly definitions, or UNback no longer compiles and cannot enter Play mode.

## Hand-off to the CLAdVR team (Yinqi, Bhavya)

These files were added to the local CLAdVR workspace on 2026-10-21 and are **not checked in**.

1. Check this folder in, with the `.meta` files Unity creates for it, on the Plastic branch
   the pilot runs from. Without it, the pilot's drilling phase records no brightness.
2. Once the pilot's drill scene exists, play it the way the pilot will run it (Editor Play
   mode or a build) with PhysioLabXR open, and check `CLAdSceneLuminanceLSL`:
   - after gaze calibration, `gaze_valid` stays at 1 while looking around;
   - with the head still, look up at something bright and down at something dark:
     `gaze_v` goes above and then below 0.5, and `gaze_luminance` follows the gaze (bright
     up, dark down) more than `view_luminance` does. If it follows the opposite way, the
     data are mirrored: tell Xichen before the pilot;
   - the Console shows no repeating "CLAd scene luminance" or camera messages.
3. Watch the frame time on capture frames in the machining scene. Each capture re-renders the
   scene from the main camera, shadow maps and depth passes included, so its cost hardly
   depends on the 128-pixel size. If frames drop, lower `captureRateHz` in
   `SceneLuminanceStreamer` (both copies; the drops get rarer, not smaller) and tell Xichen,
   so the analysis knows.

Questions: Xichen He (CLAd_EEG).
