using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace CLAd.Recording
{
    /// <summary>Byte order of one 4-byte pixel in a GPU readback.</summary>
    public enum PixelLayout
    {
        Rgba,
        Bgra,
    }

    /// <summary>
    /// Pixel maths for CLAdSceneLuminanceLSL, kept free of rendering so it can be tested.
    ///
    /// Readback pixels are 4 bytes each; row 0 is the bottom of the view (viewport v = 0)
    /// and pixel (x, y) is centred at viewport ((x + 0.5) / width, (y + 0.5) / height).
    /// The bytes are display-encoded (sRGB) in both Gamma and Linear projects, so they are
    /// decoded before Rec. 709 luminance is taken. Results are relative brightness, 0–1,
    /// not cd/m².
    /// </summary>
    public static class SceneLuminanceMath
    {
        private static readonly float[] SrgbToLinearTable = BuildSrgbToLinearTable();

        /// <summary>Linear value (0–1) of an sRGB-encoded byte (IEC 61966-2-1).</summary>
        public static float SrgbToLinear(byte encoded) => SrgbToLinearTable[encoded];

        /// <summary>Rec. 709 relative luminance of linear RGB.</summary>
        public static float RelativeLuminance(float linearR, float linearG, float linearB) =>
            0.2126f * linearR + 0.7152f * linearG + 0.0722f * linearB;

        /// <summary>The byte order of 8-bit RGBA formats; false for anything else.</summary>
        public static bool TryGetLayout(GraphicsFormat format, out PixelLayout layout)
        {
            switch (format)
            {
                case GraphicsFormat.R8G8B8A8_SRGB:
                case GraphicsFormat.R8G8B8A8_UNorm:
                    layout = PixelLayout.Rgba;
                    return true;
                case GraphicsFormat.B8G8R8A8_SRGB:
                case GraphicsFormat.B8G8R8A8_UNorm:
                    layout = PixelLayout.Bgra;
                    return true;
                default:
                    layout = default;
                    return false;
            }
        }

        /// <summary>Mean per-pixel luminance of the whole image; -1 for an empty image.</summary>
        public static float MeanLuminance(byte[] pixels, int width, int height, PixelLayout layout)
        {
            int count = width * height;
            if (count <= 0)
                return -1f;

            double sum = 0;
            for (int i = 0; i < count; i++)
                sum += PixelLuminance(pixels, i * 4, layout);
            return (float)(sum / count);
        }

        /// <summary>
        /// Mean per-pixel luminance of the pixels whose centres lie within
        /// <paramref name="radiusPixels"/> of a viewport point; -1 when none do.
        /// </summary>
        public static float MeanLuminanceInDisc(byte[] pixels, int width, int height, PixelLayout layout,
            Vector2 centerViewport, float radiusPixels)
        {
            float cx = centerViewport.x * width;
            float cy = centerViewport.y * height;
            int xMin = Mathf.Max(0, Mathf.FloorToInt(cx - radiusPixels));
            int xMax = Mathf.Min(width - 1, Mathf.CeilToInt(cx + radiusPixels));
            int yMin = Mathf.Max(0, Mathf.FloorToInt(cy - radiusPixels));
            int yMax = Mathf.Min(height - 1, Mathf.CeilToInt(cy + radiusPixels));
            float radiusSquared = radiusPixels * radiusPixels;

            double sum = 0;
            int count = 0;
            for (int y = yMin; y <= yMax; y++)
            {
                float dy = y + 0.5f - cy;
                for (int x = xMin; x <= xMax; x++)
                {
                    float dx = x + 0.5f - cx;
                    if (dx * dx + dy * dy > radiusSquared)
                        continue;
                    sum += PixelLuminance(pixels, (y * width + x) * 4, layout);
                    count++;
                }
            }
            return count > 0 ? (float)(sum / count) : -1f;
        }

        /// <summary>
        /// Pixel radius of a disc of <paramref name="radiusDegrees"/> visual angle at the
        /// centre of a perspective image; never below one pixel.
        /// </summary>
        public static float PatchRadiusPixels(float radiusDegrees, float verticalFovDegrees, int heightPixels)
        {
            float radius = heightPixels * 0.5f * Mathf.Tan(radiusDegrees * Mathf.Deg2Rad)
                           / Mathf.Tan(verticalFovDegrees * 0.5f * Mathf.Deg2Rad);
            return Mathf.Max(1f, radius);
        }

        /// <summary>
        /// Projects a gaze direction given in the camera's local frame (x right, y up,
        /// z forward; Varjo's HMD space) into the viewport of a camera with that vertical
        /// field of view and aspect. False when it points behind the camera or off the view.
        /// </summary>
        public static bool TryGazeToViewport(Vector3 hmdForward, float verticalFovDegrees, float aspect,
            out Vector2 viewport)
        {
            viewport = default;
            if (hmdForward.z <= 1e-6f)
                return false;

            float tanHalfFov = Mathf.Tan(verticalFovDegrees * 0.5f * Mathf.Deg2Rad);
            float u = 0.5f + 0.5f * (hmdForward.x / hmdForward.z) / (tanHalfFov * aspect);
            float v = 0.5f + 0.5f * (hmdForward.y / hmdForward.z) / tanHalfFov;
            if (u < 0f || u > 1f || v < 0f || v > 1f)
                return false;

            viewport = new Vector2(u, v);
            return true;
        }

        private static float PixelLuminance(byte[] pixels, int offset, PixelLayout layout)
        {
            byte red = layout == PixelLayout.Rgba ? pixels[offset] : pixels[offset + 2];
            byte blue = layout == PixelLayout.Rgba ? pixels[offset + 2] : pixels[offset];
            return RelativeLuminance(SrgbToLinear(red), SrgbToLinear(pixels[offset + 1]), SrgbToLinear(blue));
        }

        private static float[] BuildSrgbToLinearTable()
        {
            var table = new float[256];
            for (int i = 0; i < table.Length; i++)
            {
                double encoded = i / 255.0;
                table[i] = (float)(encoded <= 0.04045
                    ? encoded / 12.92
                    : Math.Pow((encoded + 0.055) / 1.055, 2.4));
            }
            return table;
        }
    }
}
