using UnityEngine;

namespace ToolNBack
{
    /// <summary>
    /// Renderer-level response feedback for a tool instance: flashes all its renderers
    /// with an emissive/albedo tint via MaterialPropertyBlocks. Deliberately avoids
    /// screen-space outline effects — QuickOutline needs Read/Write-enabled meshes and
    /// camera-buffer outlines (EPO) mismatch the Varjo XR-3's four differently sized
    /// stereo views. Plain material rendering works in every view.
    /// </summary>
    public class ToolFeedbackTint : MonoBehaviour
    {
        /// <summary>Currently applied tint, or null when idle. Exposed for tests/state checks.</summary>
        public Color? ActiveColor { get; private set; }

        /// <summary>Emission strength of the flash.</summary>
        public float GlowIntensity { get; set; } = 0.6f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;

        /// <summary>
        /// Caches renderers and instantiates their materials once so the _EMISSION
        /// keyword can be enabled per instance (keywords cannot be set through
        /// property blocks, and enabling them on shared materials would edit assets).
        /// </summary>
        public void Initialize()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _block = new MaterialPropertyBlock();

            foreach (Renderer toolRenderer in _renderers)
            {
                foreach (Material material in toolRenderer.materials) // instantiates copies
                {
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                }
            }
        }

        public void Apply(Color color)
        {
            if (_renderers == null)
                return;

            ActiveColor = color;
            foreach (Renderer toolRenderer in _renderers)
            {
                toolRenderer.GetPropertyBlock(_block);
                _block.SetColor(EmissionColorId, color * GlowIntensity);
                _block.SetColor(ColorId, Color.Lerp(Color.white, color, 0.5f));
                toolRenderer.SetPropertyBlock(_block);
            }
        }

        public void Clear()
        {
            ActiveColor = null;
            if (_renderers == null)
                return;

            foreach (Renderer toolRenderer in _renderers)
                toolRenderer.SetPropertyBlock(null);
        }
    }
}
