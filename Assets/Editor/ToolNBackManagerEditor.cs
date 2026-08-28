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
                        .Append(" (").Append(NOf(block.Level)).Append("-back, feedback allowed)");
                    break;
                default:
                    text.Append("Task — ").Append(block.Level)
                        .Append(" (").Append(NOf(block.Level)).Append("-back)");
                    break;
            }
            if (i < plan.Count - 1)
                text.Append('\n');
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(text.ToString(), MessageType.None);
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
