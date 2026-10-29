using CLAd.Recording;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace ToolNBack.Tests
{
    /// <summary>
    /// Characterises the platform assumption the gaze patch rests on: in an
    /// AsyncGPUReadback of a rendered frame, row 0 is the bottom of the view and the
    /// bytes are in the layout TryGetLayout reports. If this ever broke, the patch
    /// would be read from the mirrored spot of the view without any error.
    /// Needs a graphics device, so it is ignored under -nographics.
    /// </summary>
    public class SceneLuminanceReadbackTests
    {
        [Test]
        public void ReadbackOfARenderedFrame_HasTheTopOfTheViewInTheLastRows()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("no graphics device (-nographics): nothing can be rendered or read back");

            var target = new RenderTexture(16, 16, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            var cameraObject = new GameObject("Readback Test Camera");
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var white = new Material(Shader.Find("Unlit/Color")) { color = Color.white };
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.orthographic = true;
                camera.orthographicSize = 1f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 10f;
                camera.targetTexture = target;

                // White over the top half of the view, black below.
                quad.transform.position = new Vector3(0f, 0.5f, 1f);
                quad.transform.localScale = new Vector3(2f, 1f, 1f);
                quad.GetComponent<Renderer>().sharedMaterial = white;

                camera.Render();
                AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(target, 0);
                request.WaitForCompletion();

                Assert.IsFalse(request.hasError, "the readback failed");
                Assert.IsTrue(SceneLuminanceMath.TryGetLayout(target.graphicsFormat, out PixelLayout layout),
                    $"unexpected render texture format {target.graphicsFormat}");
                byte[] pixels = request.GetData<byte>().ToArray();

                Assert.Greater(SceneLuminanceMath.MeanLuminanceInDisc(pixels, 16, 16, layout, new Vector2(0.5f, 0.75f), 2f),
                    0.95f, "the top of the view must be in the last rows of the readback");
                Assert.Less(SceneLuminanceMath.MeanLuminanceInDisc(pixels, 16, 16, layout, new Vector2(0.5f, 0.25f), 2f),
                    0.05f, "the bottom of the view must be in the first rows of the readback");
            }
            finally
            {
                Object.DestroyImmediate(quad);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(white);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void ReadbackOfARenderedFrame_KeepsRedInTheRedChannel()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("no graphics device (-nographics): nothing can be rendered or read back");

            var target = new RenderTexture(16, 16, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            var cameraObject = new GameObject("Readback Test Camera");
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.red;
                camera.cullingMask = 0;
                camera.targetTexture = target;

                camera.Render();
                AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(target, 0);
                request.WaitForCompletion();

                Assert.IsFalse(request.hasError, "the readback failed");
                Assert.IsTrue(SceneLuminanceMath.TryGetLayout(target.graphicsFormat, out PixelLayout layout));
                byte[] pixels = request.GetData<byte>().ToArray();

                Assert.AreEqual(0.2126f, SceneLuminanceMath.MeanLuminance(pixels, 16, 16, layout), 0.01f,
                    "pure red must weigh as red (0.2126), not as blue (0.0722): the layout must match the bytes");
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }
    }
}
