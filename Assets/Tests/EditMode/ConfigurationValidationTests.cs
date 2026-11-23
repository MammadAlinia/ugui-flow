using System.Collections;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace UguiFlow.Tests
{
    public sealed class ConfigurationValidationTests
    {
        private const string LightTheme =
            "Packages/com.mammadalinia.ugui-flow/Runtime/Theming/Defaults/Resources/UguiFlow/Light.asset";

        [UnityTest]
        public IEnumerator EditorReportsMissingAndIncompatibleErrorViews()
        {
            ThemeDatabase light = AssetDatabase.LoadAssetAtPath<ThemeDatabase>(LightTheme);
            ThemeDatabase theme = Object.Instantiate(light);
            theme.name = "Validation theme";
            try
            {
                using var settings = new SerializedObject(theme);
                SerializedProperty prefab = settings.FindProperty("errorViewPrefab");
                LogAssert.Expect(LogType.Error, new Regex("Validation theme.*ErrorView slot"));
                prefab.objectReferenceValue = null;
                settings.ApplyModifiedPropertiesWithoutUndo();
                yield return null;
                yield return null;

                LogAssert.Expect(LogType.Error, new Regex("Validation theme.*ErrorView key.*ErrorView slot"));
                prefab.objectReferenceValue = settings.FindProperty("cardPrefab").objectReferenceValue;
                settings.ApplyModifiedPropertiesWithoutUndo();
                yield return null;
                yield return null;
            }
            finally
            {
                Object.DestroyImmediate(theme);
            }
        }

        [UnityTest]
        public IEnumerator EditorReportsAWindowWithNoScreenHost()
        {
            var canvas = new GameObject("Validation canvas", typeof(RectTransform), typeof(Canvas));
            LogAssert.Expect(LogType.Error, new Regex("Window 'Validation canvas' needs a Screen Host"));
            try
            {
                canvas.AddComponent<Window>();
                yield return null;
                yield return null;
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }
    }
}
