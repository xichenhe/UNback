using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ToolNBack.Tests
{
    /// <summary>
    /// The instruction text must sit on a semi-transparent dark backdrop so the
    /// white text stays legible instead of floating bare over the scene.
    /// </summary>
    public class InstructionPanelTests
    {
        private static readonly Type ManagerType =
            Type.GetType("ToolNBack.ToolNBackManagerBehaviour, Assembly-CSharp");

        private GameObject _root;
        private MonoBehaviour _manager;

        [SetUp]
        public void SetUp()
        {
            Assert.NotNull(ManagerType, "ToolNBackManagerBehaviour must exist in Assembly-CSharp");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _root = new GameObject("Tool NBack");
            _manager = (MonoBehaviour)_root.AddComponent(ManagerType);

            var serialized = new SerializedObject(_manager);
            serialized.FindProperty("_startDelay").floatValue = 0f;
            serialized.FindProperty("_randomSeed").intValue = 123;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            ManagerType.GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_manager, null);
        }

        [Test]
        public void InstructionText_HasSemiTransparentBackdropBehindIt()
        {
            Transform canvas = _root.transform.Find("Instruction Canvas");
            Assert.NotNull(canvas, "Start must create the instruction canvas");

            Transform backdropTransform = canvas.Find("Instruction Backdrop");
            Assert.NotNull(backdropTransform, "the canvas must contain a backdrop panel");

            var backdrop = backdropTransform.GetComponent<Image>();
            Assert.NotNull(backdrop, "the backdrop must render as a UI Image");

            SerializedProperty colorProperty = new SerializedObject(_manager)
                .FindProperty("_instructionBackdropColor");
            Assert.NotNull(colorProperty, "the backdrop color must be experimenter-tunable");
            Assert.AreEqual(colorProperty.colorValue, backdrop.color,
                "the backdrop must use the configured color");
            Assert.Greater(backdrop.color.a, 0f, "an invisible backdrop leaves the text floating");
            Assert.Less(backdrop.color.a, 1f, "the backdrop must stay semi-transparent");

            Transform text = canvas.Find("Instruction Text");
            Assert.NotNull(text, "the instruction text must stay at its known path");
            Assert.Less(backdropTransform.GetSiblingIndex(), text.GetSiblingIndex(),
                "the backdrop must render behind the text (UI draws in hierarchy order)");
        }

        [Test]
        public void Backdrop_HiddenWhileInstructionTextIsEmpty()
        {
            // Trials run with a blanked instruction (BeginRunning sets ""); the
            // backdrop must vanish with the text or a dark panel floats in view
            // for the whole recorded block.
            MethodInfo setInstruction = ManagerType.GetMethod("SetInstruction",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Transform backdrop = _root.transform.Find("Instruction Canvas/Instruction Backdrop");
            Assert.NotNull(backdrop, "the canvas must contain a backdrop panel");

            setInstruction.Invoke(_manager, new object[] { "Get ready" });
            Assert.IsTrue(backdrop.gameObject.activeSelf,
                "the backdrop must show while instructions are displayed");

            setInstruction.Invoke(_manager, new object[] { string.Empty });
            Assert.IsFalse(backdrop.gameObject.activeSelf,
                "the backdrop must hide when the instruction text is blanked for the trials");
        }

        [Test]
        public void InstructionText_NeverTruncatesVertically()
        {
            // Legacy Text defaults to verticalOverflow = Truncate, which silently
            // drops the bottom line ("Starting in N...") when long tool names wrap.
            var text = _root.transform.Find("Instruction Canvas/Instruction Text")
                .GetComponent<Text>();
            Assert.AreEqual(VerticalWrapMode.Overflow, text.verticalOverflow,
                "long instructions must overflow instead of losing their last line");
        }

        [Test]
        public void Backdrop_CoversTheWholeCanvas()
        {
            Transform backdropTransform = _root.transform.Find("Instruction Canvas/Instruction Backdrop");
            Assert.NotNull(backdropTransform, "the canvas must contain a backdrop panel");

            var rect = (RectTransform)backdropTransform;
            Assert.AreEqual(Vector2.zero, rect.anchorMin);
            Assert.AreEqual(Vector2.one, rect.anchorMax);
            Assert.AreEqual(Vector2.zero, rect.offsetMin);
            Assert.AreEqual(Vector2.zero, rect.offsetMax);
        }
    }
}
