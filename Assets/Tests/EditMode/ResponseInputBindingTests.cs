using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
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
    }
}
