using System;
using System.Collections.Generic;
using System.Text;
using ToolNBack;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector for <see cref="ToolNBackManagerBehaviour"/>: in multi-block mode the
/// single-block-only fields are greyed out (Level is ignored there — the plan
/// contains all levels) and the generated session plan is previewed read-only,
/// so what will run is visible before pressing Play.
/// </summary>
[CustomEditor(typeof(ToolNBackManagerBehaviour))]
public class ToolNBackManagerEditor : Editor
{
    private static readonly HashSet<string> SingleBlockOnlyFields = new HashSet<string>
    {
        "_level"
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty multiBlock = serializedObject.FindProperty("_multiBlockSession");
        bool isMultiBlock = multiBlock != null && multiBlock.boolValue;

        if (isMultiBlock && Application.isPlaying)
            DrawExperimenterControls();

        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (property.propertyPath == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(property, true);
                continue;
            }

            bool greyOut = isMultiBlock && SingleBlockOnlyFields.Contains(property.name);
            using (new EditorGUI.DisabledScope(greyOut))
                EditorGUILayout.PropertyField(property, true);

            if (greyOut && property.name == "_level")
                EditorGUILayout.HelpBox(
                    "Multi-block session: Level is ignored — the plan below contains all " +
                    "three WM load levels in the counterbalanced order.", MessageType.Info);
        }

        if (isMultiBlock)
            DrawPlanPreview();

        serializedObject.ApplyModifiedProperties();
    }

    // Keeps the practice button and its status live while Play mode runs.
    public override bool RequiresConstantRepaint() => Application.isPlaying;

    private void DrawExperimenterControls()
    {
        var manager = (ToolNBackManagerBehaviour)target;
        bool practicing = manager.IsPracticing;

        EditorGUILayout.LabelField("Experimenter Controls", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(!practicing))
        {
            if (GUILayout.Button("End Practice → Next Stage", GUILayout.Height(36)))
                manager.EndPractice();
        }
        EditorGUILayout.HelpBox(
            practicing
                ? $"Practice round {manager.PracticeRound} is running. Practice repeats until you press the button."
                : "The button is active during the practice stage.",
            practicing ? MessageType.Warning : MessageType.None);
        if (manager.IsAskingEffort)
            EditorGUILayout.HelpBox(
                "Effort rating on screen. If the participant says the number, click the Game view " +
                "and type it with the digit keys 1–9.", MessageType.Info);
        EditorGUILayout.Space(8);
    }

    private void DrawPlanPreview()
    {
        SerializedProperty custom = serializedObject.FindProperty("_customBlocks");
        List<SessionBlock> plan;
        string source;
        if (custom != null && custom.arraySize > 0)
        {
            plan = ReadCustomBlocks(custom);
            source = "custom blocks";
        }
        else
        {
            plan = SessionPlanner.DefaultPlan(
                serializedObject.FindProperty("_counterbalanceIndex").intValue,
                serializedObject.FindProperty("_repetitionsPerLevel").intValue,
                serializedObject.FindProperty("_baselineRestDuration").floatValue,
                serializedObject.FindProperty("_interBlockRestDuration").floatValue,
                serializedObject.FindProperty("_includePracticeBlock").boolValue);
            source = "generated default plan";
        }

        bool ratings = serializedObject.FindProperty("_effortProbeAfterBlocks").boolValue;
        string rating = ratings ? " → effort rating" : string.Empty;

        var text = new StringBuilder();
        text.Append("Session plan (").Append(source).Append("), ")
            .Append(plan.Count).Append(" blocks:\n");
        for (int i = 0; i < plan.Count; i++)
        {
            SessionBlock block = plan[i];
            text.Append(i).Append(": ");
            switch (block.Kind)
            {
                case SessionBlockKind.Rest:
                    text.Append("Rest ").Append(block.RestDuration.ToString("F0")).Append(" s");
                    break;
                case SessionBlockKind.Practice:
                    text.Append("Practice — ").Append(block.Level)
                        .Append(" (").Append(NOf(block.Level)).Append("-back, feedback allowed, repeats until you end it)")
                        .Append(rating);
                    break;
                default:
                    text.Append("Task — ").Append(block.Level)
                        .Append(" (").Append(NOf(block.Level)).Append("-back)").Append(rating);
                    break;
            }
            if (i < plan.Count - 1)
                text.Append('\n');
        }

        float seconds = SessionPlanner.EstimateDurationSeconds(
            plan, serializedObject.FindProperty("_startDelay").floatValue, SettingsSource(),
            ratings ? EffortProbe.EstimatedSecondsPerProbe : 0f);
        text.Append("\n\nEstimated length: ").Append(TimeSpan.FromSeconds(seconds).ToString(@"m\:ss")).Append(" min");
        if (plan.Exists(block => block.Kind == SessionBlockKind.Practice))
            text.Append(" (with one practice round; each extra round adds its length)");
        if (ratings)
            text.Append($"\nEffort ratings counted at about {EffortProbe.EstimatedSecondsPerProbe:F0} s each.");

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(text.ToString(), MessageType.None);
    }

    /// <summary>The settings blocks will run with: the level presets, or the custom settings when overridden.</summary>
    private Func<WMLoadLevel, ToolNBackSettings> SettingsSource()
    {
        if (!serializedObject.FindProperty("_overridePreset").boolValue)
            return WMLoadPresets.Get;

        SerializedProperty custom = serializedObject.FindProperty("_customSettings");
        var settings = new ToolNBackSettings
        {
            SequenceLength = custom.FindPropertyRelative("SequenceLength").intValue,
            StimulusDuration = custom.FindPropertyRelative("StimulusDuration").floatValue,
            InterStimulusDuration = custom.FindPropertyRelative("InterStimulusDuration").floatValue
        };
        return _ => settings;
    }

    private static int NOf(WMLoadLevel level) => WMLoadPresets.Get(level).N;

    private static List<SessionBlock> ReadCustomBlocks(SerializedProperty custom)
    {
        var plan = new List<SessionBlock>(custom.arraySize);
        for (int i = 0; i < custom.arraySize; i++)
        {
            SerializedProperty element = custom.GetArrayElementAtIndex(i);
            plan.Add(new SessionBlock
            {
                Kind = (SessionBlockKind)element.FindPropertyRelative("Kind").enumValueIndex,
                Level = (WMLoadLevel)element.FindPropertyRelative("Level").enumValueIndex,
                RestDuration = element.FindPropertyRelative("RestDuration").floatValue
            });
        }
        return plan;
    }
}
