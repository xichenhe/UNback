using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

/// <summary>
/// Adds the OpenXR loader as the Standalone fallback AFTER the Varjo loader and
/// enables the Valve Index Controller interaction profile — mirroring CLAdVR.
///
/// Why: on this lab machine the native Varjo plugin receives controller poses
/// but zero button data from the SteamVR bridge, while OpenXR delivers Index
/// buttons correctly. Varjo must stay FIRST: recorded sessions need the native
/// Varjo eye-tracking API (pupil diameter for the effort/arousal channels),
/// which is unavailable under OpenXR. With Varjo Base running, Varjo still
/// initializes (recording mode, keyboard responses); with Varjo Base closed or
/// the Varjo loader unticked, OpenXR initializes and controller buttons work
/// (practice/demo mode, no gaze streaming).
/// Run from the menu, or -executeMethod OpenXRSetup.RunBatch.
/// </summary>
public static class OpenXRSetup
{
    private const string OpenXRLoaderTypeName = "UnityEngine.XR.OpenXR.OpenXRLoader";

    [MenuItem("NBack VR/Enable OpenXR Fallback (Index Controllers)")]
    public static void Run()
    {
        int errors = Apply();
        Debug.Log(errors == 0
            ? "[OpenXRSetup] done — check Project Settings > XR Plug-in Management (Varjo first, OpenXR second)"
            : $"[OpenXRSetup] finished with {errors} error(s), see Console");
    }

    public static void RunBatch()
    {
        int errors = Apply();
        EditorApplication.Exit(errors == 0 ? 0 : 1);
    }

    private static int Apply()
    {
        int errors = 0;

        if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,
                out XRGeneralSettingsPerBuildTarget perTarget) || perTarget == null)
        {
            Debug.LogError("[OpenXRSetup] XR Plug-in Management settings not found.");
            return 1;
        }

        XRGeneralSettings standalone = perTarget.SettingsForBuildTarget(BuildTargetGroup.Standalone);
        if (standalone == null || standalone.Manager == null)
        {
            Debug.LogError("[OpenXRSetup] no Standalone XR manager settings.");
            return 1;
        }

        // Appends after the existing Varjo loader; no-op if already assigned.
        if (standalone.Manager.activeLoaders.Count == 0 ||
            standalone.Manager.activeLoaders[standalone.Manager.activeLoaders.Count - 1]?.GetType().FullName != OpenXRLoaderTypeName)
        {
            if (XRPackageMetadataStore.AssignLoader(standalone.Manager, OpenXRLoaderTypeName, BuildTargetGroup.Standalone))
            {
                Debug.Log("[OpenXRSetup] OpenXR loader assigned as Standalone fallback.");
            }
            else
            {
                Debug.LogError("[OpenXRSetup] failed to assign the OpenXR loader.");
                errors++;
            }
        }
        else
        {
            Debug.Log("[OpenXRSetup] OpenXR loader already assigned.");
        }

        FeatureHelpers.RefreshFeatures(BuildTargetGroup.Standalone);
        OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
        if (settings == null)
        {
            Debug.LogError("[OpenXRSetup] no OpenXR settings for Standalone.");
            return errors + 1;
        }

        var index = settings.GetFeature<ValveIndexControllerProfile>();
        if (index == null)
        {
            Debug.LogError("[OpenXRSetup] Valve Index Controller profile feature not found.");
            errors++;
        }
        else if (!index.enabled)
        {
            index.enabled = true;
            EditorUtility.SetDirty(index);
            Debug.Log("[OpenXRSetup] Valve Index Controller interaction profile enabled.");
        }
        else
        {
            Debug.Log("[OpenXRSetup] Valve Index Controller profile already enabled.");
        }

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        return errors;
    }
}
