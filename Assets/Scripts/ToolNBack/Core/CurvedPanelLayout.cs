using System;
using UnityEngine;

namespace ToolNBack
{
    /// <summary>
    /// Maps normalized panel coordinates (u, v) in [0, 1] x [0, 1] onto a vertical
    /// cylinder section curved around a pivot (the user's head), like the curved
    /// virtual desktop panels on Meta Quest. u sweeps the horizontal arc
    /// (0 = left edge, 0.5 = straight ahead, 1 = right edge), v the height
    /// (0 = bottom, 1 = top). All results are in pivot-local space with +Z forward.
    /// </summary>
    public class CurvedPanelLayout
    {
        public float Radius { get; }
        public float ArcDegrees { get; }
        public float HeightMin { get; }
        public float HeightMax { get; }

        public CurvedPanelLayout(float radius, float arcDegrees, float heightMin, float heightMax)
        {
            Radius = radius;
            ArcDegrees = arcDegrees;
            HeightMin = heightMin;
            HeightMax = heightMax;
        }

        /// <summary>Pivot-local position of panel coordinate (u, v) on the cylinder section.</summary>
        public Vector3 GetLocalPosition(float u, float v)
        {
            float yaw = YawRadians(u);
            return new Vector3(
                Mathf.Sin(yaw) * Radius,
                Mathf.Lerp(HeightMin, HeightMax, Mathf.Clamp01(v)),
                Mathf.Cos(yaw) * Radius);
        }

        /// <summary>Pivot-local rotation at (u, v): forward points horizontally at the pivot, up stays world up.</summary>
        public Quaternion GetLocalRotation(float u, float v)
        {
            // The point sits at yaw degrees around +Y; turning 180 degrees further
            // makes +Z point back at the cylinder axis (the user).
            return Quaternion.Euler(0f, YawRadians(u) * Mathf.Rad2Deg + 180f, 0f);
        }

        /// <summary>Combined pivot-local pose at (u, v).</summary>
        public Pose GetLocalPose(float u, float v)
        {
            return new Pose(GetLocalPosition(u, v), GetLocalRotation(u, v));
        }

        private float YawRadians(float u)
        {
            return (Mathf.Clamp01(u) - 0.5f) * ArcDegrees * Mathf.Deg2Rad;
        }
    }
}
