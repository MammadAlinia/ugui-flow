#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UguiFlow.Tests
{
    public sealed class FlowScreenTests
    {
        private const string Themes = "Packages/com.mammadalinia.ugui-flow/Runtime/Theming/Defaults/";
        private string assetFolder;
        private FlowScreen screen;
        private RectTransform host;

        [SetUp]
        public void SetUp()
        {
            string folderName = "UguiFlowTests-" + Guid.NewGuid().ToString("N");
            assetFolder = AssetDatabase.GUIDToAssetPath(AssetDatabase.CreateFolder("Assets", folderName));
            LifecycleProbe.Calls.Clear();
            var canvas = new GameObject("Test canvas", typeof(RectTransform), typeof(Canvas));
            canvas.SetActive(false);
            host = AddChild(canvas.transform, "Window host");
            screen = canvas.AddComponent<FlowScreen>();
            Configure(screen, "windowHost", host);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (screen != null)
                Object.Destroy(screen.gameObject);
            yield return null;
            AssetDatabase.DeleteAsset(assetFolder);
            LifecycleProbe.Calls.Clear();
        }

        [UnityTest]
        public IEnumerator StartupDispatchesEachCallbackInHierarchyOrder()
        {
            Window prefab = MakeWindow("Startup", 2);
            Configure(screen, "startupWindow", prefab);
            screen.gameObject.SetActive(true);
            yield return null;
            yield return null;

            Window window = screen.CurrentWindow;
            Assert.That(window, Is.Not.Null);
            Assert.That(window.Screen, Is.SameAs(screen));
            Assert.That(window.IsVisible, Is.True);
            Assert.That(window.transform.parent, Is.SameAs(host));
            Assert.That(((RectTransform)window.transform).anchoredPosition, Is.EqualTo(new Vector2(17, -23)));
            Assert.That(screen.TryClose(), Is.True);
            Assert.That(screen.CurrentWindow, Is.Null);
            Assert.That(LifecycleProbe.Calls, Is.EqualTo(new[]
            {
                ("Startup 0", "Open", false, false), ("Startup 1", "Open", false, false),
                ("Startup 0", "Show", true, true), ("Startup 1", "Show", true, true),
                ("Startup 0", "Hide", true, true), ("Startup 1", "Hide", true, true),
                ("Startup 0", "Close", false, false), ("Startup 1", "Close", false, false)
            }));
            yield return null;
            Assert.That(window == null, Is.True);
        }

        [UnityTest]
        public IEnumerator HideKeepsControlAndChildStateWhileReopeningStartsFresh()
        {
            Window prefab = MakeWindow("State");
            Assert.That(screen.TryHide(), Is.False);
            Assert.That(screen.TryShow(), Is.False);
            Assert.That(screen.TryClose(), Is.False);
            Window window = Open(prefab);
            Toggle toggle = window.GetComponentInChildren<Toggle>();
            toggle.isOn = !toggle.isOn;
            bool editedValue = toggle.isOn;
            RectTransform view = AddChild(window.transform, "Added view");

            Assert.That(screen.TryShow(), Is.False);
            Assert.That(screen.TryHide(), Is.True);
            Assert.That(screen.TryHide(), Is.False);
            Assert.That(window.gameObject.activeInHierarchy, Is.False);
            Assert.That(screen.TryShow(), Is.True);
            Assert.That(screen.CurrentWindow, Is.SameAs(window));
            Assert.That(toggle.isOn, Is.EqualTo(editedValue));
            Assert.That(view.gameObject.activeInHierarchy, Is.True);
            Assert.That(screen.TryHide(), Is.True);
            Assert.That(screen.TryClose(), Is.True);
            Assert.That(screen.TryClose(), Is.False);
            Assert.That(LifecycleProbe.Calls.Select(call => call.callback),
                Is.EqualTo(new[] { "Open", "Show", "Hide", "Show", "Hide", "Close" }));

            Window reopened = Open(prefab);
            Assert.That(reopened, Is.Not.SameAs(window));
            Assert.That(reopened.GetComponentInChildren<Toggle>().isOn, Is.Not.EqualTo(editedValue));
            Assert.That(reopened.transform.Find("Added view"), Is.Null);
            yield return null;
            Assert.That(window == null, Is.True);
        }

        [UnityTest]
        public IEnumerator ReplacementPreparesBeforeClosingCurrentWindow()
        {
            Window original = Open(MakeWindow("First"));
            Window replacement = Open(MakeWindow("Second"));

            Assert.That(screen.CurrentWindow, Is.SameAs(replacement));
            Assert.That(original.IsVisible, Is.False);
            Assert.That(LifecycleProbe.Calls.Select(call => (call.name, call.callback)), Is.EqualTo(new[]
            {
                ("First 0", "Open"), ("First 0", "Show"), ("Second 0", "Open"),
                ("First 0", "Hide"), ("First 0", "Close"), ("Second 0", "Show")
            }));
            yield return null;
            Assert.That(original == null, Is.True);
        }

        [UnityTest]
        public IEnumerator FailedOpensCleanUpTheCandidateAndLeaveTheCurrentWindowVisible()
        {
            Window original = Open(MakeWindow("First"));
            Assert.That(screen.TryOpen(null, out _, out string invalidError), Is.False);
            Assert.That(invalidError, Does.Contain("prefab"));
            Assert.That(screen.TryOpen(original, out _, out _), Is.False);

            Window broken = MakeWindow("Broken", 2, failOnOpen: true);
            Assert.That(screen.TryOpen(broken, out var failed, out string error), Is.False);
            Assert.That(error, Does.Contain("Probe failed"));
            Assert.That(failed, Is.Null);
            Assert.That(screen.CurrentWindow, Is.SameAs(original));
            Assert.That(original.IsVisible, Is.True);
            Assert.That(LifecycleProbe.Calls.Select(call => (call.name, call.callback)), Is.EqualTo(new[]
            {
                ("First 0", "Open"), ("First 0", "Show"), ("Broken 0", "Open"), ("Broken 0", "Close")
            }));
            yield return null;
            Assert.That(host.childCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator AppliesInitialThemeThenRethemesHiddenWindowAndNewViews()
        {
            var scope = screen.gameObject.AddComponent<ThemeScope>();
            ThemeDatabase dark = AssetDatabase.LoadAssetAtPath<ThemeDatabase>(Themes + "Dark.asset");
            ThemeDatabase light = AssetDatabase.LoadAssetAtPath<ThemeDatabase>(Themes + "Light.asset");
            Configure(scope, "initialTheme", dark);
            Window window = Open(MakeWindow("Themed"));
            Toggle initial = window.GetComponentInChildren<Toggle>();
            Toggle darkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Themes + "Dark/toggle.prefab").GetComponent<Toggle>();
            Assert.That(initial.targetGraphic.color, Is.EqualTo(darkPrefab.targetGraphic.color));
            initial.SetIsOnWithoutNotify(false);
            Assert.That(screen.TryHide(), Is.True);

            Assert.That(scope.TrySetTheme(light, out string error), Is.True, error);
            Toggle themed = window.GetComponentInChildren<Toggle>(true);
            Assert.That(themed, Is.Not.SameAs(initial));
            Assert.That(themed.isOn, Is.False);
            Assert.That(window.IsVisible, Is.False);
            Assert.That(screen.TryShow(), Is.True);

            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Themes + "Dark/card.prefab");
            var sibling = Object.Instantiate(cardPrefab, screen.transform).transform;
            var newCard = Object.Instantiate(cardPrefab, window.transform);
            Assert.That(scope.TryApplyCurrentTheme((RectTransform)newCard.transform, out error), Is.True, error);
            Assert.That(window.transform.Find(newCard.name), Is.Not.SameAs(newCard.transform));
            Assert.That(scope.transform.Find(sibling.name), Is.SameAs(sibling));
            Assert.That(screen.CurrentWindow, Is.SameAs(window));
            Assert.That(LifecycleProbe.Calls.Select(call => call.callback), Is.EqualTo(new[] { "Open", "Show", "Hide", "Show" }));
            yield return null;
        }

        [UnityTest]
        public IEnumerator MissingThemeMappingLeavesThePreviousWindowIntact()
        {
            var scope = screen.gameObject.AddComponent<ThemeScope>();
            Configure(scope, "initialTheme", AssetDatabase.LoadAssetAtPath<ThemeDatabase>(Themes + "Light.asset"));
            Window original = Open(MakeWindow("First"));
            Window broken = MakeWindow("Unmapped", unmappedThemeView: true);
            Assert.That(screen.TryOpen(broken, out _, out string error), Is.False);
            Assert.That(error, Does.Contain("compatible prefab"));
            Assert.That(screen.CurrentWindow, Is.SameAs(original));
            Assert.That(original.IsVisible, Is.True);
            Assert.That(LifecycleProbe.Calls.Select(call => call.callback), Is.EqualTo(new[] { "Open", "Show", "Open", "Close" }));
            yield return null;
            Assert.That(host.childCount, Is.EqualTo(1));
        }

        private Window Open(Window prefab)
        {
            screen.gameObject.SetActive(true);
            Assert.That(screen.TryOpen(prefab, out var window, out string error), Is.True, error);
            return window;
        }

        private Window MakeWindow(string name, int controllerCount = 1, bool failOnOpen = false, bool unmappedThemeView = false)
        {
            RectTransform rect = AddChild(null, name);
            rect.anchoredPosition = new Vector2(17, -23);
            rect.gameObject.AddComponent<Window>();
            for (int i = 0; i < controllerCount; i++)
            {
                var controller = AddChild(rect, $"{name} {i}").gameObject.AddComponent<LifecycleProbe>();
                controller.failOnOpen = failOnOpen && i == 0;
                controller.checkReentrancy = true;
            }
            AddChild(rect, "Disabled controller").gameObject.AddComponent<LifecycleProbe>().enabled = false;
            var toggle = AssetDatabase.LoadAssetAtPath<GameObject>(Themes + "Light/toggle.prefab");
            PrefabUtility.InstantiatePrefab(toggle, rect);
            if (unmappedThemeView)
                AddChild(rect, "Unmapped view").gameObject.AddComponent<ThemeView>();
            try
            {
                return PrefabUtility.SaveAsPrefabAsset(rect.gameObject, $"{assetFolder}/{name}.prefab").GetComponent<Window>();
            }
            finally
            {
                Object.DestroyImmediate(rect.gameObject);
            }
        }

        private static RectTransform AddChild(Transform parent, string name)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Configure(Object target, string field, Object value)
        {
            using var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
