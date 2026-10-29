using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ToolNBack.Tests
{
    /// <summary>
    /// The response actions must bind the control names the native Varjo XR plugin
    /// actually exposes: its VarjoIndexController/VarjoViveWand layouts name the
    /// primary face button "primary" and carry no "primaryButton" control or alias,
    /// so the OpenXR-style "primaryButton" binding alone never fires in-headset.
    /// </summary>
    public class ResponseInputBindingTests
    {
        [TestCase("ToolNBack.ToolNBackResponseInput, Assembly-CSharp")]
        [TestCase("NBackStreaming.ControllerResponseInput, Assembly-CSharp")]
        public void ResponseAction_BindsVarjoFaceButtonAndTriggers(string typeName)
        {
            Type type = Type.GetType(typeName);
            Assert.NotNull(type, $"{typeName} must exist in Assembly-CSharp");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var component = (MonoBehaviour)new GameObject("Response Input Test").AddComponent(type);

            MethodInfo onEnable = type.GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo onDisable = type.GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(onEnable, "the input component must create its action in OnEnable");

            onEnable.Invoke(component, null);
            try
            {
                var action = (InputAction)type
                    .GetField("_responseAction", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(component);
                Assert.NotNull(action, "OnEnable must create the response action");

                string[] paths = action.bindings.Select(b => b.path).ToArray();
                CollectionAssert.Contains(paths, "<XRController>/primary",
                    "Varjo controller layouts name the face button 'primary' — without this binding the Index A button does nothing");
                CollectionAssert.Contains(paths, "<XRController>{RightHand}/triggerPressed");
                CollectionAssert.Contains(paths, "<XRController>{LeftHand}/triggerPressed");
            }
            finally
            {
                onDisable.Invoke(component, null);
            }
        }

        [TestCase("ToolNBack.ToolNBackResponseInput, Assembly-CSharp")]
        [TestCase("NBackStreaming.ControllerResponseInput, Assembly-CSharp")]
        public void ExtraBindings_FromInspector_AreAddedToTheAction(string typeName)
        {
            Type type = Type.GetType(typeName);
            Assert.NotNull(type, $"{typeName} must exist in Assembly-CSharp");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var component = (MonoBehaviour)new GameObject("Extra Binding Test").AddComponent(type);

            // The experimenter adds lab-specific paths in the Inspector without a rebuild.
            var serialized = new SerializedObject(component);
            SerializedProperty extras = serialized.FindProperty("_extraBindings");
            Assert.NotNull(extras, "the input component must expose _extraBindings in the Inspector");
            extras.arraySize = 2;
            extras.GetArrayElementAtIndex(0).stringValue = "<XRController>/secondary";
            extras.GetArrayElementAtIndex(1).stringValue = "  ";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            MethodInfo onEnable = type.GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo onDisable = type.GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic);
            onEnable.Invoke(component, null);
            try
            {
                var action = (InputAction)type
                    .GetField("_responseAction", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(component);
                string[] paths = action.bindings.Select(b => b.path).ToArray();
                CollectionAssert.Contains(paths, "<XRController>/secondary",
                    "inspector-provided binding paths must be added to the response action");
                Assert.IsFalse(paths.Any(string.IsNullOrWhiteSpace),
                    "blank inspector entries must be skipped, not added as empty bindings");
            }
            finally
            {
                onDisable.Invoke(component, null);
            }
        }

        [Test]
        public void RatingScale_IsDrivenByEitherStick_OrTheArrowKeys()
        {
            Type type = Type.GetType("ToolNBack.ToolNBackResponseInput, Assembly-CSharp");
            Assert.NotNull(type, "ToolNBackResponseInput must exist in Assembly-CSharp");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var component = (MonoBehaviour)new GameObject("Rating Input Test").AddComponent(type);

            // Lab-specific stick paths (found with Log Controller Buttons) are added without a rebuild.
            var serialized = new SerializedObject(component);
            SerializedProperty extras = serialized.FindProperty("_extraNavigationBindings");
            Assert.NotNull(extras, "the input component must expose _extraNavigationBindings in the Inspector");
            extras.arraySize = 1;
            extras.GetArrayElementAtIndex(0).stringValue = "<XRController>/secondary2DAxis";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            MethodInfo onEnable = type.GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo onDisable = type.GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic);
            onEnable.Invoke(component, null);
            try
            {
                string[] stick = BindingPaths(type, component, "_probeStickAction");
                CollectionAssert.Contains(stick, "<XRController>/primary2DAxis",
                    "OpenXR and most XR plugins expose the thumbstick as primary2DAxis");
                CollectionAssert.Contains(stick, "<XRController>/thumbstick");
                CollectionAssert.Contains(stick, "<XRController>/secondary2DAxis",
                    "inspector-provided stick paths must be added");

                CollectionAssert.Contains(BindingPaths(type, component, "_probeLeftAction"), "<Keyboard>/leftArrow");
                CollectionAssert.Contains(BindingPaths(type, component, "_probeRightAction"), "<Keyboard>/rightArrow");

                // The experimenter types the number the participant says (Index buttons
                // may not arrive in recording mode), on the number row or the numpad.
                string[] digits = BindingPaths(type, component, "_probeDigitAction");
                for (int digit = 1; digit <= 9; digit++)
                {
                    CollectionAssert.Contains(digits, $"<Keyboard>/{digit}");
                    CollectionAssert.Contains(digits, $"<Keyboard>/numpad{digit}");
                }
            }
            finally
            {
                onDisable.Invoke(component, null);
            }
        }

        private static string[] BindingPaths(Type type, MonoBehaviour component, string fieldName)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"the input component must keep its {fieldName}");
            var action = (InputAction)field.GetValue(component);
            Assert.NotNull(action, $"OnEnable must create {fieldName}");
            return action.bindings.Select(b => b.path).ToArray();
        }
    }
}
