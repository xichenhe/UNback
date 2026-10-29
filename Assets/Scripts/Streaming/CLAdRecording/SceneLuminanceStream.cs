using UnityEngine;

namespace CLAd.Recording
{
    /// <summary>
    /// The CLAdSceneLuminanceLSL contract, shared by UNback, CLAdVR and the PhysioLabXR
    /// CLAd preset: float32, nominal 30 Hz, empty source_id, 13 channels.
    ///
    ///  [0] Unity time (realtimeSinceStartup) of the captured frame
    ///  [1] mean luminance of the whole view, 0–1
    ///  [2] mean luminance of the gaze patch, 0–1; -1 when gaze is invalid or off the view
    ///  [3] gaze valid, 0/1
    ///  [4-5] gaze viewport u, v, 0–1; -1 when invalid or off the view
    ///  [6-8] view (camera) position xyz, Unity world
    ///  [9-12] view rotation quaternion xyzw, Unity world
    /// </summary>
    public static class SceneLuminanceStream
    {
        public const string Name = "CLAdSceneLuminanceLSL";
        public const string Type = "Luminance";
        public const double NominalRateHz = 30.0;
        public const int ChannelCount = 13;

        /// <summary>Channel labels in order; the PhysioLabXR preset uses the same names.</summary>
        public static readonly string[] ChannelNames =
        {
            "unity_time", "view_luminance", "gaze_luminance", "gaze_valid", "gaze_u", "gaze_v",
            "view_pos_x", "view_pos_y", "view_pos_z", "view_rot_x", "view_rot_y", "view_rot_z", "view_rot_w",
        };

        private const float Missing = -1f;

        /// <summary>
        /// Writes one sample. A null <paramref name="gazeViewport"/> with valid gaze means
        /// the eye tracker worked but the gaze point fell outside the captured view.
        /// </summary>
        public static void Fill(float[] sample, double unityTime, float viewLuminance, bool gazeValid,
            Vector2? gazeViewport, float gazeLuminance, Vector3 viewPosition, Quaternion viewRotation)
        {
            bool onView = gazeValid && gazeViewport.HasValue;

            sample[0] = (float)unityTime;
            sample[1] = viewLuminance;
            sample[2] = onView ? gazeLuminance : Missing;
            sample[3] = gazeValid ? 1f : 0f;
            sample[4] = onView ? gazeViewport.Value.x : Missing;
            sample[5] = onView ? gazeViewport.Value.y : Missing;
            sample[6] = viewPosition.x;
            sample[7] = viewPosition.y;
            sample[8] = viewPosition.z;
            sample[9] = viewRotation.x;
            sample[10] = viewRotation.y;
            sample[11] = viewRotation.z;
            sample[12] = viewRotation.w;
        }
    }
}
