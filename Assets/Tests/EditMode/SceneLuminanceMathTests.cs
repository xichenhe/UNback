using System;
using CLAd.Recording;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace ToolNBack.Tests
{
    /// <summary>
    /// The pixel maths behind CLAdSceneLuminanceLSL: screen bytes are display-encoded
    /// (sRGB), luminance is Rec. 709 on linear values, row 0 of a readback is the
    /// bottom of the view, and the gaze patch is a disc of fixed visual angle.
    /// </summary>
    public class SceneLuminanceMathTests
    {
        private static readonly float Sin20 = Mathf.Sin(20f * Mathf.Deg2Rad);
        private static readonly float Cos20 = Mathf.Cos(20f * Mathf.Deg2Rad);

        /// <summary>An RGBA image, row 0 first (the bottom of the view).</summary>
        private static byte[] Image(int width, int height, Func<int, int, byte> grey)
        {
            var pixels = new byte[width * height * 4];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int i = (y * width + x) * 4;
                byte value = grey(x, y);
                pixels[i] = value;
                pixels[i + 1] = value;
                pixels[i + 2] = value;
                pixels[i + 3] = 255;
            }
            return pixels;
        }

        [TestCase((byte)0, 0f)]
        [TestCase((byte)10, 0.0030353f)]
        [TestCase((byte)128, 0.2158605f)]
        [TestCase((byte)255, 1f)]
        public void SrgbToLinear_FollowsTheSrgbCurve(byte encoded, float linear)
        {
            Assert.AreEqual(linear, SceneLuminanceMath.SrgbToLinear(encoded), 1e-5f);
        }

        [Test]
        public void RelativeLuminance_UsesRec709Weights()
        {
            Assert.AreEqual(0.2126f, SceneLuminanceMath.RelativeLuminance(1f, 0f, 0f), 1e-6f);
            Assert.AreEqual(0.7152f, SceneLuminanceMath.RelativeLuminance(0f, 1f, 0f), 1e-6f);
            Assert.AreEqual(0.0722f, SceneLuminanceMath.RelativeLuminance(0f, 0f, 1f), 1e-6f);
            Assert.AreEqual(1f, SceneLuminanceMath.RelativeLuminance(1f, 1f, 1f), 1e-5f);
        }

        [Test]
        public void MeanLuminance_OfAUniformGreyImage_IsThatGreysLuminance()
        {
            byte[] pixels = Image(4, 4, (x, y) => 128);

            Assert.AreEqual(0.2158605f, SceneLuminanceMath.MeanLuminance(pixels, 4, 4, PixelLayout.Rgba), 1e-5f,
                "mid grey is display-encoded: averaging the raw bytes would report 0.5");
        }

        [Test]
        public void MeanLuminance_ReadsBgraLayout()
        {
            var pixels = new byte[2 * 2 * 4];
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = 255;
                pixels[i + 3] = 255;
            }

            Assert.AreEqual(0.2126f, SceneLuminanceMath.MeanLuminance(pixels, 2, 2, PixelLayout.Rgba), 1e-5f);
            Assert.AreEqual(0.0722f, SceneLuminanceMath.MeanLuminance(pixels, 2, 2, PixelLayout.Bgra), 1e-5f,
                "in BGRA the first byte is blue");
        }

        [Test]
        public void MeanLuminanceInDisc_RowZeroIsTheBottomOfTheView()
        {
            byte[] pixels = Image(8, 8, (x, y) => y < 4 ? (byte)255 : (byte)0);

            Assert.AreEqual(1f, SceneLuminanceMath.MeanLuminanceInDisc(pixels, 8, 8, PixelLayout.Rgba,
                new Vector2(0.5f, 0.2f), 1.5f), 1e-5f, "low viewport v must read the first rows");
            Assert.AreEqual(0f, SceneLuminanceMath.MeanLuminanceInDisc(pixels, 8, 8, PixelLayout.Rgba,
                new Vector2(0.5f, 0.8f), 1.5f), 1e-5f);
        }

        [Test]
        public void MeanLuminanceInDisc_CountsOnlyPixelsInsideTheRadius()
        {
            byte[] pixels = Image(8, 8, (x, y) => x < 4 ? (byte)255 : (byte)0);

            // The disc of radius 1 around the image centre holds exactly the four central pixels.
            Assert.AreEqual(0.5f, SceneLuminanceMath.MeanLuminanceInDisc(pixels, 8, 8, PixelLayout.Rgba,
                new Vector2(0.5f, 0.5f), 1f), 1e-5f);
        }

        [Test]
        public void MeanLuminanceInDisc_WhollyOutsideTheImage_IsMinusOne()
        {
            byte[] pixels = Image(8, 8, (x, y) => 255);

            Assert.AreEqual(-1f, SceneLuminanceMath.MeanLuminanceInDisc(pixels, 8, 8, PixelLayout.Rgba,
                new Vector2(1.5f, 0.5f), 1f));
        }

        [Test]
        public void PatchRadiusPixels_ScalesWithTheVerticalFieldOfView()
        {
            Assert.AreEqual(2.235f, SceneLuminanceMath.PatchRadiusPixels(2f, 90f, 128), 0.01f);
            Assert.AreEqual(1f, SceneLuminanceMath.PatchRadiusPixels(0.1f, 90f, 128), 1e-6f,
                "a patch narrower than a pixel still reads the pixel under the gaze");
        }

        [Test]
        public void TryGazeToViewport_MapsStraightAheadToTheCentre()
        {
            Assert.IsTrue(SceneLuminanceMath.TryGazeToViewport(Vector3.forward, 90f, 1f, out Vector2 viewport));
            Assert.AreEqual(0.5f, viewport.x, 1e-6f);
            Assert.AreEqual(0.5f, viewport.y, 1e-6f);
        }

        [Test]
        public void TryGazeToViewport_MapsTwentyDegreesUpAndRight()
        {
            Assert.IsTrue(SceneLuminanceMath.TryGazeToViewport(new Vector3(0f, Sin20, Cos20), 90f, 1f, out Vector2 up));
            Assert.AreEqual(0.5f, up.x, 1e-5f);
            Assert.AreEqual(0.68199f, up.y, 1e-4f);

            Assert.IsTrue(SceneLuminanceMath.TryGazeToViewport(new Vector3(Sin20, 0f, Cos20), 90f, 1f, out Vector2 right));
            Assert.AreEqual(0.68199f, right.x, 1e-4f);
            Assert.AreEqual(0.5f, right.y, 1e-5f);
        }

        [Test]
        public void TryGazeToViewport_RejectsBehindOutsideAndZero()
        {
            float sin60 = Mathf.Sin(60f * Mathf.Deg2Rad), cos60 = Mathf.Cos(60f * Mathf.Deg2Rad);

            Assert.IsFalse(SceneLuminanceMath.TryGazeToViewport(Vector3.back, 90f, 1f, out _), "behind the viewer");
            Assert.IsFalse(SceneLuminanceMath.TryGazeToViewport(new Vector3(sin60, 0f, cos60), 90f, 1f, out _),
                "60 degrees to the side is outside a 90-degree view");
            Assert.IsFalse(SceneLuminanceMath.TryGazeToViewport(Vector3.zero, 90f, 1f, out _), "no direction");
        }

        [Test]
        public void TryGetLayout_KnowsTheEightBitFormats()
        {
            Assert.IsTrue(SceneLuminanceMath.TryGetLayout(GraphicsFormat.R8G8B8A8_SRGB, out PixelLayout rgba));
            Assert.AreEqual(PixelLayout.Rgba, rgba);
            Assert.IsTrue(SceneLuminanceMath.TryGetLayout(GraphicsFormat.B8G8R8A8_UNorm, out PixelLayout bgra));
            Assert.AreEqual(PixelLayout.Bgra, bgra);
            Assert.IsFalse(SceneLuminanceMath.TryGetLayout(GraphicsFormat.R16G16B16A16_SFloat, out _));
        }
    }
}
