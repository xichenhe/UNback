using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ToolNBack;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ToolNBack.Tests
{
    /// <summary>
    /// The calibration session the experimenter actually runs — the MultiBlock scene's
    /// inspector values plus the WM-load presets — must record every condition in four
    /// short, interleaved blocks with an effort rating after each, in about 17 minutes.
    /// </summary>
    public class SessionLengthTests
    {
        private const string MultiBlockScenePath = "Assets/Scenes/VR-Tool-NBack-MultiBlock.unity";

        private static readonly Type ManagerType =
            Type.GetType("ToolNBack.ToolNBackManagerBehaviour, Assembly-CSharp");

        [Test]
        public void ShippedMultiBlockSession_RunsFourShortBlocksPerCondition_InAboutSeventeenMinutes()
        {
            EditorSceneManager.OpenScene(MultiBlockScenePath, OpenSceneMode.Single);
            var manager = (MonoBehaviour)UnityEngine.Object.FindFirstObjectByType(
                ManagerType, FindObjectsInactive.Include);
            Assert.NotNull(manager, "the MultiBlock scene must contain the Tool NBack manager");

            var serialized = new SerializedObject(manager);
            Assert.IsTrue(serialized.FindProperty("_multiBlockSession").boolValue);
            Assert.AreEqual(0, serialized.FindProperty("_customBlocks").arraySize,
                "the generated default plan is the shipped design");
            Assert.IsFalse(serialized.FindProperty("_overridePreset").boolValue,
                "blocks must use the WM-load presets");
            SerializedProperty probe = serialized.FindProperty("_effortProbeAfterBlocks");
            Assert.NotNull(probe, "the manager must expose the effort-rating switch");
            Assert.IsTrue(probe.boolValue, "the session asks the effort rating after every block");

            List<SessionBlock> plan = SessionPlanner.DefaultPlan(
                serialized.FindProperty("_counterbalanceIndex").intValue,
                serialized.FindProperty("_repetitionsPerLevel").intValue,
                serialized.FindProperty("_baselineRestDuration").floatValue,
                serialized.FindProperty("_interBlockRestDuration").floatValue,
                serialized.FindProperty("_includePracticeBlock").boolValue);
            float minutes = SessionPlanner.EstimateDurationSeconds(
                plan, serialized.FindProperty("_startDelay").floatValue, WMLoadPresets.Get,
                EffortProbe.EstimatedSecondsPerProbe) / 60f;

            foreach (WMLoadLevel level in new[] { WMLoadLevel.Low, WMLoadLevel.Optimal, WMLoadLevel.High })
                Assert.AreEqual(4, plan.Count(b => b.Kind == SessionBlockKind.Task && b.Level == level),
                    $"{level} must be recorded in four blocks");
            Assert.That(minutes, Is.InRange(16f, 18f), $"the session lasts {minutes:F2} min");
        }
    }
}
