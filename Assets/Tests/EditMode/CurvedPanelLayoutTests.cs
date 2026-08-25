using NUnit.Framework;
using ToolNBack;
using UnityEngine;

namespace ToolNBack.Tests
{
    public class CurvedPanelLayoutTests
    {
        private const float Radius = 1.2f;
        private const float Arc = 110f;
        private const float HeightMin = -0.35f;
        private const float HeightMax = 0.35f;
        private const float Tolerance = 1e-4f;

        private static CurvedPanelLayout NewLayout() =>
            new CurvedPanelLayout(Radius, Arc, HeightMin, HeightMax);

        [Test]
        public void Position_LiesOnCylinder_ForAnyUV()
        {
            var layout = NewLayout();
            for (float u = 0f; u <= 1f; u += 0.25f)
            {
                for (float v = 0f; v <= 1f; v += 0.25f)
                {
                    Vector3 p = layout.GetLocalPosition(u, v);
                    float horizontalDistance = Mathf.Sqrt(p.x * p.x + p.z * p.z);
                    Assert.AreEqual(Radius, horizontalDistance, Tolerance,
                        $"panel point (u={u}, v={v}) must sit on the cylinder");
                }
            }
        }

        [Test]
        public void Position_CenterIsStraightAhead()
        {
            Vector3 p = NewLayout().GetLocalPosition(0.5f, 0.5f);

            Assert.AreEqual(0f, p.x, Tolerance);
            Assert.AreEqual(Radius, p.z, Tolerance);
            Assert.AreEqual((HeightMin + HeightMax) / 2f, p.y, Tolerance);
        }

        [Test]
        public void Position_UEdgesMapToArcExtremes()
        {
            var layout = NewLayout();
            float halfArcRad = Arc * 0.5f * Mathf.Deg2Rad;

            Vector3 left = layout.GetLocalPosition(0f, 0.5f);
            Assert.AreEqual(Radius * Mathf.Sin(-halfArcRad), left.x, Tolerance);
            Assert.AreEqual(Radius * Mathf.Cos(halfArcRad), left.z, Tolerance);

            Vector3 right = layout.GetLocalPosition(1f, 0.5f);
            Assert.AreEqual(Radius * Mathf.Sin(halfArcRad), right.x, Tolerance);
            Assert.AreEqual(Radius * Mathf.Cos(halfArcRad), right.z, Tolerance);
        }

        [Test]
        public void Position_VMapsHeightLinearly()
        {
            var layout = NewLayout();

            Assert.AreEqual(HeightMin, layout.GetLocalPosition(0.5f, 0f).y, Tolerance);
            Assert.AreEqual(HeightMax, layout.GetLocalPosition(0.5f, 1f).y, Tolerance);
        }

        [Test]
        public void Position_InputsAreClamped()
        {
            var layout = NewLayout();

            Assert.AreEqual(layout.GetLocalPosition(0f, 0f), layout.GetLocalPosition(-1f, -5f));
            Assert.AreEqual(layout.GetLocalPosition(1f, 1f), layout.GetLocalPosition(2f, 7f));
        }

        [Test]
        public void Rotation_FacesPivotHorizontally()
        {
            var layout = NewLayout();
            for (float u = 0f; u <= 1f; u += 0.25f)
            {
                Vector3 p = layout.GetLocalPosition(u, 0.8f);
                Vector3 expectedForward = -new Vector3(p.x, 0f, p.z).normalized;

                Vector3 forward = layout.GetLocalRotation(u, 0.8f) * Vector3.forward;
                Assert.Less(Vector3.Angle(expectedForward, forward), 0.01f,
                    $"tool at u={u} must face the user (pivot)");
            }
        }

        [Test]
        public void Rotation_KeepsWorldUp()
        {
            Vector3 up = NewLayout().GetLocalRotation(0.1f, 0.9f) * Vector3.up;
            Assert.Less(Vector3.Angle(Vector3.up, up), 0.01f);
        }

        [Test]
        public void Pose_CombinesPositionAndRotation()
        {
            var layout = NewLayout();
            Pose pose = layout.GetLocalPose(0.25f, 0.75f);

            Assert.AreEqual(layout.GetLocalPosition(0.25f, 0.75f), pose.position);
            Assert.AreEqual(layout.GetLocalRotation(0.25f, 0.75f), pose.rotation);
        }
    }
}
