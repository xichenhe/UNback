using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Converts the URP materials imported with the _MITmech folder (authored in the
/// CLAdVR project, Unity 2022.3 + URP 14) to Built-in Render Pipeline shaders.
/// This project deliberately stays on the Built-in RP (Varjo), so URP's Lit/Unlit
/// shaders do not exist here and the materials render magenta
/// (Hidden/InternalErrorShader). Conversion happens in place, preserving material
/// GUIDs so every prefab reference keeps working.
///
/// Mapping summary (URP Lit -> Standard / Standard (Specular setup)):
/// - _BaseMap/_BaseColor -> _MainTex/_Color (with tiling/offset)
/// - metallic or specular workflow chosen from the saved _WorkflowMode
/// - _Surface + _Blend -> Opaque / Fade / Transparent(premultiplied) / saved blend
///   factors for additive-style materials; alpha clip -> Cutout (blend wins when both)
/// - emission only when the source material actually had the _EMISSION keyword
/// - bump/occlusion/parallax/detail-free extras carried over; cull overrides are
///   reported, since Standard cannot express them
///
/// Run from the menu or via -executeMethod MITmechMaterialConverter.BatchConvertAndAudit.
/// </summary>
public static class MITmechMaterialConverter
{
    private const string RootFolder = "Assets/_MITmech";
    /// <summary>URP "Universal Render Pipeline/Lit" (com.unity.render-pipelines.universal).</summary>
    private const string UrpLitGuid = "933532a4fcc9baf4fa0491de14d08ed7";
    /// <summary>URP "Universal Render Pipeline/Unlit".</summary>
    private const string UrpUnlitGuid = "650dd9526735d5b46b79224bc6e94025";

    private static readonly Regex ShaderGuidRegex =
        new Regex(@"m_Shader:\s*\{fileID:\s*-?\d+,\s*guid:\s*([0-9a-f]{32})", RegexOptions.Compiled);

    /// <summary>Set when the serialized property layout does not match expectations; fails the batch run.</summary>
    private static bool _layoutFailure;

    [MenuItem("NBack VR/Convert MITmech URP Materials to Built-in")]
    public static void Convert()
    {
        (int lit, int unlit) = ConvertAll();
        Debug.Log($"[MaterialConverter] Converted {lit} URP Lit and {unlit} URP Unlit material(s) under {RootFolder}.");
        Audit();
    }

    /// <summary>Batch entry point: converts, audits, exits non-zero when broken materials remain.</summary>
    public static void BatchConvertAndAudit()
    {
        (int lit, int unlit) = ConvertAll();
        Debug.Log($"[MaterialConverter] Converted {lit} URP Lit and {unlit} URP Unlit material(s).");
        int broken = Audit();
        EditorApplication.Exit(broken == 0 && !_layoutFailure ? 0 : 1);
    }

    /// <summary>Audit only (no conversion), for re-checking. Exits with the broken count in batch mode.</summary>
    public static void BatchAudit()
    {
        int broken = Audit();
        EditorApplication.Exit(broken == 0 ? 0 : 1);
    }

    private static (int lit, int unlit) ConvertAll()
    {
        _layoutFailure = false;
        int litCount = 0;
        int unlitCount = 0;
        var seenPaths = new HashSet<string>();

        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { RootFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!seenPaths.Add(path))
                continue;
            if (!path.EndsWith(".mat"))
                continue; // embedded/imported sub-asset materials are not rewritable .mat files

            // The shader reference is unreadable through the API once it is broken,
            // so identify the original URP shader from the serialized YAML.
            Match match = ShaderGuidRegex.Match(File.ReadAllText(path));
            if (!match.Success)
                continue;

            string shaderGuid = match.Groups[1].Value;
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
                continue;

            if (shaderGuid == UrpLitGuid)
            {
                ConvertLitToStandard(material, path);
                litCount++;
            }
            else if (shaderGuid == UrpUnlitGuid)
            {
                ConvertUnlitToBuiltin(material);
                unlitCount++;
            }
        }

        AssetDatabase.SaveAssets();
        return (litCount, unlitCount);
    }

    private static int Audit()
    {
        int broken = 0;
        int total = 0;
        int embedded = 0;
        var seenPaths = new HashSet<string>();

        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { RootFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!seenPaths.Add(path))
                continue;
            if (!path.EndsWith(".mat"))
            {
                embedded++; // e.g. materials embedded in imported models; not converter-owned
                continue;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
                continue;

            total++;
            if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
            {
                broken++;
                Debug.LogError($"[MaterialConverter] STILL BROKEN: {path}");
            }
        }

        if (embedded > 0)
            Debug.Log($"[MaterialConverter] Note: {embedded} embedded (non-.mat) material asset(s) were not audited.");

        Debug.Log(broken == 0
            ? $"[MaterialConverter] Audit PASSED: all {total} .mat materials under {RootFolder} have a valid shader."
            : $"[MaterialConverter] Audit FAILED: {broken}/{total} materials still have no valid shader.");
        return broken;
    }

    #region URP Lit -> Standard
    /// <summary>Everything we need from the URP material, read before the shader is switched.</summary>
    private sealed class UrpLitData
    {
        public Texture BaseMap, BumpMap, MetallicGlossMap, SpecGlossMap, OcclusionMap, EmissionMap, ParallaxMap;
        public Vector2 BaseMapScale = Vector2.one, BaseMapOffset = Vector2.zero;
        public Color? BaseColor, EmissionColor, SpecColor;
        public float? Metallic, Smoothness, Surface, Blend, SrcBlend, DstBlend, AlphaClip, Cutoff,
            BumpScale, OcclusionStrength, Parallax, Cull, WorkflowMode,
            SpecularHighlights, EnvironmentReflections, SmoothnessTextureChannel;
        public HashSet<string> Keywords;

        public bool ReadAnything =>
            BaseMap != null || BaseColor.HasValue || Metallic.HasValue || Smoothness.HasValue || Surface.HasValue;
    }

    private static void ConvertLitToStandard(Material material, string path)
    {
        UrpLitData data = ReadUrpLitData(material);
        if (!data.ReadAnything)
            Debug.LogWarning($"[MaterialConverter] No saved URP properties readable on {path} — " +
                             "converted with Standard defaults; check it visually.");

        // URP _WorkflowMode: 0 = Specular, 1 = Metallic (metallic when absent).
        bool specularWorkflow = data.WorkflowMode.HasValue && data.WorkflowMode.Value < 0.5f;
        material.shader = Shader.Find(specularWorkflow ? "Standard (Specular setup)" : "Standard");
        material.shaderKeywords = new string[0]; // drop stale URP keywords

        if (data.BaseMap != null)
        {
            material.SetTexture("_MainTex", data.BaseMap);
            material.SetTextureScale("_MainTex", data.BaseMapScale);
            material.SetTextureOffset("_MainTex", data.BaseMapOffset);
        }
        if (data.BaseColor.HasValue)
            material.SetColor("_Color", data.BaseColor.Value);

        if (specularWorkflow)
        {
            if (data.SpecColor.HasValue)
                material.SetColor("_SpecColor", data.SpecColor.Value);
            if (data.SpecGlossMap != null)
            {
                material.SetTexture("_SpecGlossMap", data.SpecGlossMap);
                material.EnableKeyword("_SPECGLOSSMAP");
            }
        }
        else
        {
            if (data.Metallic.HasValue)
                material.SetFloat("_Metallic", data.Metallic.Value);
            if (data.MetallicGlossMap != null)
            {
                material.SetTexture("_MetallicGlossMap", data.MetallicGlossMap);
                material.EnableKeyword("_METALLICGLOSSMAP");
            }
        }

        if (data.Smoothness.HasValue)
        {
            material.SetFloat("_Glossiness", data.Smoothness.Value);
            material.SetFloat("_GlossMapScale", data.Smoothness.Value);
        }
        if (data.SmoothnessTextureChannel.HasValue && data.SmoothnessTextureChannel.Value > 0.5f)
            material.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");

        if (data.BumpMap != null)
        {
            material.SetTexture("_BumpMap", data.BumpMap);
            material.EnableKeyword("_NORMALMAP");
            if (data.BumpScale.HasValue)
                material.SetFloat("_BumpScale", data.BumpScale.Value);
        }
        if (data.OcclusionMap != null)
        {
            material.SetTexture("_OcclusionMap", data.OcclusionMap);
            if (data.OcclusionStrength.HasValue)
                material.SetFloat("_OcclusionStrength", data.OcclusionStrength.Value);
        }
        if (data.ParallaxMap != null)
        {
            material.SetTexture("_ParallaxMap", data.ParallaxMap);
            material.EnableKeyword("_PARALLAXMAP");
            if (data.Parallax.HasValue)
                material.SetFloat("_Parallax", Mathf.Clamp(data.Parallax.Value, 0.005f, 0.08f));
        }

        // Emission only when the source material actually had it enabled — stale
        // _EmissionColor values survive in the saved properties when emission is off.
        if (data.Keywords.Contains("_EMISSION"))
        {
            if (data.EmissionMap != null)
                material.SetTexture("_EmissionMap", data.EmissionMap);
            material.SetColor("_EmissionColor", data.EmissionColor ?? Color.black);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }

        // Disabled highlights/reflections (same float+keyword pattern on Standard;
        // URP names the reflections float _EnvironmentReflections).
        if (IsZero(data.SpecularHighlights))
        {
            material.SetFloat("_SpecularHighlights", 0f);
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        }
        if (IsZero(data.EnvironmentReflections))
        {
            material.SetFloat("_GlossyReflections", 0f);
            material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
        }

        // Standard has no cull-mode property; do not lose that silently.
        if (data.Cull.HasValue && (int)data.Cull.Value != 2)
            Debug.LogWarning($"[MaterialConverter] {path}: URP render face override (_Cull={(int)data.Cull.Value}) " +
                             "cannot be expressed with the Standard shader — material is now back-face culled. " +
                             "Handle manually if the double-sided/inverted look matters.");

        // Surface/blend mode. Blend wins over clip when both are set: URP applies them
        // together and blending is the visually dominant effect (Standard can't do both).
        if (data.Surface.HasValue && data.Surface.Value > 0.5f)
            SetupTransparent(material, data);
        else if (data.AlphaClip.HasValue && data.AlphaClip.Value > 0.5f)
            SetupCutout(material, data.Cutoff ?? 0.5f);
        else
            SetupOpaque(material);

        EditorUtility.SetDirty(material);
    }

    private static UrpLitData ReadUrpLitData(Material material)
    {
        var saved = new SavedProperties(material);
        var data = new UrpLitData { Keywords = saved.GetKeywords() };

        if (saved.TryGetTexture("_BaseMap", out Texture baseMap, out Vector2 scale, out Vector2 offset))
        {
            data.BaseMap = baseMap;
            data.BaseMapScale = scale;
            data.BaseMapOffset = offset;
        }
        saved.TryGetTexture("_BumpMap", out data.BumpMap, out _, out _);
        saved.TryGetTexture("_MetallicGlossMap", out data.MetallicGlossMap, out _, out _);
        saved.TryGetTexture("_SpecGlossMap", out data.SpecGlossMap, out _, out _);
        saved.TryGetTexture("_OcclusionMap", out data.OcclusionMap, out _, out _);
        saved.TryGetTexture("_EmissionMap", out data.EmissionMap, out _, out _);
        saved.TryGetTexture("_ParallaxMap", out data.ParallaxMap, out _, out _);

        data.BaseColor = saved.GetColor("_BaseColor");
        data.EmissionColor = saved.GetColor("_EmissionColor");
        data.SpecColor = saved.GetColor("_SpecColor");

        data.Metallic = saved.GetFloat("_Metallic");
        data.Smoothness = saved.GetFloat("_Smoothness");
        data.Surface = saved.GetFloat("_Surface");
        data.Blend = saved.GetFloat("_Blend");
        data.SrcBlend = saved.GetFloat("_SrcBlend");
        data.DstBlend = saved.GetFloat("_DstBlend");
        data.AlphaClip = saved.GetFloat("_AlphaClip");
        data.Cutoff = saved.GetFloat("_Cutoff");
        data.BumpScale = saved.GetFloat("_BumpScale");
        data.OcclusionStrength = saved.GetFloat("_OcclusionStrength");
        data.Parallax = saved.GetFloat("_Parallax");
        data.Cull = saved.GetFloat("_Cull");
        data.WorkflowMode = saved.GetFloat("_WorkflowMode");
        data.SpecularHighlights = saved.GetFloat("_SpecularHighlights");
        data.EnvironmentReflections = saved.GetFloat("_EnvironmentReflections");
        data.SmoothnessTextureChannel = saved.GetFloat("_SmoothnessTextureChannel");
        return data;
    }

    private static bool IsZero(float? value) => value.HasValue && value.Value < 0.5f;
    #endregion

    #region URP Unlit -> Built-in Unlit
    private static void ConvertUnlitToBuiltin(Material material)
    {
        var saved = new SavedProperties(material);
        saved.TryGetTexture("_BaseMap", out Texture baseMap, out Vector2 scale, out Vector2 offset);
        float? surface = saved.GetFloat("_Surface");
        bool transparent = surface.HasValue && surface.Value > 0.5f;

        if (baseMap != null)
        {
            material.shader = Shader.Find(transparent ? "Unlit/Transparent" : "Unlit/Texture");
            material.shaderKeywords = new string[0];
            material.SetTexture("_MainTex", baseMap);
            material.SetTextureScale("_MainTex", scale);
            material.SetTextureOffset("_MainTex", offset);
        }
        else
        {
            material.shader = Shader.Find("Unlit/Color");
            material.shaderKeywords = new string[0];
            Color? baseColor = saved.GetColor("_BaseColor");
            if (baseColor.HasValue)
                material.SetColor("_Color", baseColor.Value);
        }

        EditorUtility.SetDirty(material);
    }
    #endregion

    #region Standard shader render modes
    private static void SetupOpaque(Material material)
    {
        material.SetOverrideTag("RenderType", "");
        material.SetFloat("_Mode", 0f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        material.SetInt("_ZWrite", 1);
        material.renderQueue = -1;
    }

    private static void SetupCutout(Material material, float cutoff)
    {
        material.SetOverrideTag("RenderType", "TransparentCutout");
        material.SetFloat("_Mode", 1f);
        material.SetFloat("_Cutoff", cutoff);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        material.SetInt("_ZWrite", 1);
        material.EnableKeyword("_ALPHATEST_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
    }

    /// <summary>
    /// URP _Blend: 0 Alpha, 1 Premultiply, 2 Additive, 3 Multiply. Premultiply maps to
    /// Standard's Transparent mode; additive/multiply copy the saved blend factors
    /// verbatim (Standard's passes declare "Blend [_SrcBlend] [_DstBlend]").
    /// </summary>
    private static void SetupTransparent(Material material, UrpLitData data)
    {
        int blend = (int)(data.Blend ?? 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_ZWrite", 0);
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        switch (blend)
        {
            case 1: // Premultiplied -> Standard "Transparent"
                material.SetFloat("_Mode", 3f);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                break;
            case 2: // Additive
            case 3: // Multiply
                material.SetFloat("_Mode", 2f);
                material.SetInt("_SrcBlend", (int)(data.SrcBlend ?? (float)UnityEngine.Rendering.BlendMode.SrcAlpha));
                material.SetInt("_DstBlend", (int)(data.DstBlend ?? (float)UnityEngine.Rendering.BlendMode.One));
                material.EnableKeyword("_ALPHABLEND_ON");
                break;
            default: // Alpha -> Standard "Fade"
                material.SetFloat("_Mode", 2f);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_ALPHABLEND_ON");
                break;
        }
    }
    #endregion

    /// <summary>
    /// Reads a material's serialized saved properties directly. Required because the
    /// regular Material getters cannot address properties of a missing shader.
    /// </summary>
    private sealed class SavedProperties
    {
        private readonly SerializedObject _serialized;

        public SavedProperties(Material material)
        {
            _serialized = new SerializedObject(material);
        }

        /// <summary>
        /// All serialized keywords. With a missing shader the previously valid keywords
        /// migrate to m_InvalidKeywords, so both lists (and the legacy single-string
        /// form) are merged.
        /// </summary>
        public HashSet<string> GetKeywords()
        {
            var keywords = new HashSet<string>();

            foreach (string arrayPath in new[] { "m_ValidKeywords", "m_InvalidKeywords" })
            {
                SerializedProperty array = _serialized.FindProperty(arrayPath);
                if (array == null || !array.isArray)
                    continue;
                for (int i = 0; i < array.arraySize; i++)
                    keywords.Add(array.GetArrayElementAtIndex(i).stringValue);
            }

            SerializedProperty legacy = _serialized.FindProperty("m_ShaderKeywords");
            if (legacy != null && legacy.propertyType == SerializedPropertyType.String &&
                !string.IsNullOrEmpty(legacy.stringValue))
            {
                foreach (string keyword in legacy.stringValue.Split(' '))
                    keywords.Add(keyword);
            }

            return keywords;
        }

        public bool TryGetTexture(string name, out Texture texture, out Vector2 scale, out Vector2 offset)
        {
            texture = null;
            scale = Vector2.one;
            offset = Vector2.zero;

            SerializedProperty entry = FindEntry("m_SavedProperties.m_TexEnvs", name);
            if (entry == null)
                return false;

            texture = entry.FindPropertyRelative("m_Texture").objectReferenceValue as Texture;
            scale = entry.FindPropertyRelative("m_Scale").vector2Value;
            offset = entry.FindPropertyRelative("m_Offset").vector2Value;
            return texture != null;
        }

        public float? GetFloat(string name)
        {
            SerializedProperty entry = FindEntry("m_SavedProperties.m_Floats", name);
            return entry?.floatValue;
        }

        public Color? GetColor(string name)
        {
            SerializedProperty entry = FindEntry("m_SavedProperties.m_Colors", name);
            return entry?.colorValue;
        }

        private SerializedProperty FindEntry(string arrayPath, string name)
        {
            SerializedProperty array = _serialized.FindProperty(arrayPath);
            if (array == null)
            {
                ReportLayoutFailure($"property array '{arrayPath}' not found");
                return null;
            }

            for (int i = 0; i < array.arraySize; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                SerializedProperty key = element.FindPropertyRelative("first");
                if (key == null)
                {
                    ReportLayoutFailure($"pair layout ('first'/'second') not found in '{arrayPath}'");
                    return null;
                }

                if (key.stringValue == name)
                    return element.FindPropertyRelative("second");
            }

            return null;
        }

        private void ReportLayoutFailure(string what)
        {
            if (_layoutFailure)
                return;
            _layoutFailure = true;
            Debug.LogError($"[MaterialConverter] SERIALIZATION LAYOUT MISMATCH: {what}. " +
                           "Conversion cannot read the saved URP properties — aborting with failure " +
                           "so materials are not silently reset to defaults.");
        }
    }
}
