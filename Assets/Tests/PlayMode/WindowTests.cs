#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UguiFlow.Tests
{
    public sealed class WindowTests
    {
        private const string Themes = "Packages/com.mammadalinia.ugui-flow/Runtime/Theming/Defaults/";
        private const string LightTheme = Themes + "Resources/UguiFlow/Light.asset";
        private string assetFolder;
        private Window windowHost;
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
            windowHost = canvas.AddComponent<Window>();
            Configure(windowHost, "screenHost", host);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (windowHost != null)
                Object.Destroy(windowHost.gameObject);
            yield return null;
            AssetDatabase.DeleteAsset(assetFolder);
            LifecycleProbe.Calls.Clear();
        }

        [UnityTest]
        public IEnumerator StartupDispatchesEachCallbackInHierarchyOrder()
        {
            UIComponent prefab = MakeWindow("Startup", 2);
            Configure(windowHost, "startupScreen", prefab);
            windowHost.gameObject.SetActive(true);
            yield return null;
            yield return null;

            UIComponent window = windowHost.CurrentScreen;
            Assert.That(window, Is.Not.Null);
            Assert.That(window.Parent, Is.Null);
            Assert.That(window.IsVisible, Is.True);
            Assert.That(window.transform.parent, Is.SameAs(host));
            Assert.That(((RectTransform)window.transform).anchoredPosition, Is.EqualTo(new Vector2(17, -23)));
            Assert.That(windowHost.TryClose(), Is.True);
            Assert.That(windowHost.CurrentScreen, Is.Null);
            Assert.That(LifecycleProbe.Calls, Is.EqualTo(new[]
            {
                ("Startup 0", "Initialize", false, false), ("Startup 1", "Initialize", false, false),
                ("Startup 0", "Show", true, true), ("Startup 1", "Show", true, true),
                ("Startup 0", "Hide", true, false), ("Startup 1", "Hide", true, false),
                ("Startup 0", "Destroy", true, false), ("Startup 1", "Destroy", true, false)
            }));
            yield return null;
            Assert.That(window == null, Is.True);
        }

        [UnityTest]
        public IEnumerator HideKeepsControlAndChildStateWhileReopeningStartsFresh()
        {
            UIComponent prefab = MakeWindow("State");
            Assert.That(windowHost.TryHide(), Is.False);
            Assert.That(windowHost.TryShow(), Is.False);
            Assert.That(windowHost.TryClose(), Is.False);
            UIComponent window = Open(prefab);
            Toggle toggle = window.GetComponentInChildren<Toggle>();
            toggle.isOn = !toggle.isOn;
            bool editedValue = toggle.isOn;
            RectTransform view = AddChild(window.transform, "Added view");

            Assert.That(windowHost.TryShow(), Is.False);
            Assert.That(windowHost.TryHide(), Is.True);
            Assert.That(windowHost.TryHide(), Is.False);
            Assert.That(window.gameObject.activeInHierarchy, Is.True);
            Assert.That(window.GetComponent<CanvasGroup>().alpha, Is.Zero);
            Assert.That(windowHost.TryShow(), Is.True);
            Assert.That(windowHost.CurrentScreen, Is.SameAs(window));
            Assert.That(toggle.isOn, Is.EqualTo(editedValue));
            Assert.That(view.gameObject.activeInHierarchy, Is.True);
            Assert.That(windowHost.TryHide(), Is.True);
            Assert.That(windowHost.TryClose(), Is.True);
            Assert.That(windowHost.TryClose(), Is.False);
            Assert.That(LifecycleProbe.Calls.Select(call => call.callback),
                Is.EqualTo(new[] { "Initialize", "Show", "Hide", "Show", "Hide", "Destroy" }));

            UIComponent reopened = Open(prefab);
            Assert.That(reopened, Is.Not.SameAs(window));
            Assert.That(reopened.GetComponentInChildren<Toggle>().isOn, Is.Not.EqualTo(editedValue));
            Assert.That(reopened.transform.Find("Added view"), Is.Null);
            yield return null;
            Assert.That(window == null, Is.True);
        }

        [UnityTest]
        public IEnumerator ReplacementPreparesBeforeClosingCurrentScreen()
        {
            UIComponent original = Open(MakeWindow("First"));
            UIComponent replacement = Open(MakeWindow("Second"));

            Assert.That(windowHost.CurrentScreen, Is.SameAs(replacement));
            Assert.That(original.IsVisible, Is.False);
            Assert.That(LifecycleProbe.Calls.Select(call => (call.name, call.callback)), Is.EqualTo(new[]
            {
                ("First 0", "Initialize"), ("First 0", "Show"), ("Second 0", "Initialize"),
                ("First 0", "Hide"), ("First 0", "Destroy"), ("Second 0", "Show")
            }));
            yield return null;
            Assert.That(original == null, Is.True);
        }

        [UnityTest]
        public IEnumerator InitializationFailureUsesTheLightErrorViewAndLeavesSiblingsVisible()
        {
            UIComponent original = Open(MakeWindow("First"));
            Assert.That(windowHost.TryOpen(null, out _, out string invalidError), Is.False);
            Assert.That(invalidError, Does.Contain("prefab"));
            Assert.That(windowHost.TryOpen(original, out _, out _), Is.False);

            UIComponent broken = MakeWindow("Broken", 2, failOnOpen: true);
            UIComponent screen = Open(broken);
            UIComponent failed = screen.Children[0];
            UIComponent sibling = screen.Children[1];
            var errorSlot = failed.transform.parent.Find(failed.name + " (Error View)");
            Assert.That(windowHost.CurrentScreen, Is.SameAs(screen));
            Assert.That(failed.IsFaulted, Is.True);
            Assert.That(failed.gameObject.activeSelf, Is.False);
            Assert.That(failed.GetComponentInChildren<Button>(true).isActiveAndEnabled, Is.False);
            Assert.That(sibling.IsVisible, Is.True);
            Assert.That(errorSlot.GetSiblingIndex(), Is.Zero);
            Assert.That(errorSlot.GetComponent<LayoutElement>().preferredHeight, Is.EqualTo(36));
            Assert.That(((RectTransform)errorSlot).anchorMin, Is.EqualTo(((RectTransform)failed.transform).anchorMin));
            ThemeView lightError = AssetDatabase.LoadAssetAtPath<ThemeDatabase>(LightTheme).ErrorViewPrefab;
            Assert.That(errorSlot.GetComponentInChildren<ThemeView>().ContentLabel.text, Is.EqualTo("View unavailable"));
            Assert.That(errorSlot.GetComponentInChildren<Image>().color, Is.EqualTo(lightError.GetComponent<Image>().color));
            Assert.That(windowHost.TryClose(), Is.True);
            yield return null;
            Assert.That(host.childCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator AppliesInitialThemeThenRethemesHiddenWindowAndNewViews()
        {
            var scope = windowHost.gameObject.AddComponent<ThemeScope>();
            ThemeDatabase dark = AssetDatabase.LoadAssetAtPath<ThemeDatabase>(Themes + "Dark.asset");
            ThemeDatabase light = AssetDatabase.LoadAssetAtPath<ThemeDatabase>(LightTheme);
            Configure(scope, "initialTheme", dark);
            UIComponent window = Open(MakeWindow("Themed"));
            Toggle initial = window.GetComponentInChildren<Toggle>();
            Toggle darkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Themes + "Dark/toggle.prefab").GetComponent<Toggle>();
            Assert.That(initial.targetGraphic.color, Is.EqualTo(darkPrefab.targetGraphic.color));
            initial.SetIsOnWithoutNotify(false);
            Assert.That(windowHost.TryHide(), Is.True);

            Assert.That(scope.TrySetTheme(light, out string error), Is.True, error);
            Toggle themed = window.GetComponentInChildren<Toggle>(true);
            Assert.That(themed, Is.Not.SameAs(initial));
            Assert.That(themed.isOn, Is.False);
            Assert.That(window.IsVisible, Is.False);
            Assert.That(windowHost.TryShow(), Is.True);

            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Themes + "Dark/card.prefab");
            var sibling = Object.Instantiate(cardPrefab, windowHost.transform).transform;
            var newCard = Object.Instantiate(cardPrefab, window.transform);
            Assert.That(scope.TryApplyCurrentTheme((RectTransform)newCard.transform, out error), Is.True, error);
            Assert.That(window.transform.Find(newCard.name), Is.Not.SameAs(newCard.transform));
            Assert.That(scope.transform.Find(sibling.name), Is.SameAs(sibling));
            Assert.That(windowHost.CurrentScreen, Is.SameAs(window));
            Assert.That(LifecycleProbe.Calls.Select(call => call.callback), Is.EqualTo(new[] { "Initialize", "Show", "Hide", "Show" }));
            yield return null;
        }

        [UnityTest]
        public IEnumerator MissingThemeMappingLeavesThePreviousWindowIntact()
        {
            var scope = windowHost.gameObject.AddComponent<ThemeScope>();
            Configure(scope, "initialTheme", AssetDatabase.LoadAssetAtPath<ThemeDatabase>(LightTheme));
            UIComponent original = Open(MakeWindow("First"));
            UIComponent broken = MakeWindow("Unmapped", unmappedThemeView: true);
            Assert.That(windowHost.TryOpen(broken, out _, out string error), Is.False);
            Assert.That(error, Does.Contain("Theme View Key"));
            Assert.That(windowHost.CurrentScreen, Is.SameAs(original));
            Assert.That(original.IsVisible, Is.True);
            Assert.That(LifecycleProbe.Calls.Select(call => call.callback), Is.EqualTo(new[] { "Initialize", "Show" }));
            yield return null;
            Assert.That(host.childCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator EventFailureKeepsTheSlotWhenItsThemeChanges()
        {
            var scope = windowHost.gameObject.AddComponent<ThemeScope>();
            Configure(scope, "initialTheme", AssetDatabase.LoadAssetAtPath<ThemeDatabase>(LightTheme));
            UIComponent screen = Open(MakeWindow("Events", 2));
            UIComponent failed = screen.Children[0];
            UIComponent sibling = screen.Children[1];
            int received = 0;
            int skipped = 0;
            using var throwing = screen.Events.Subscribe<int>(failed, _ => throw new InvalidOperationException());
            using var ignored = screen.Events.Subscribe<int>(failed, _ => skipped++);
            using var healthy = screen.Events.Subscribe<int>(sibling, value => received += value);
            screen.Events.Publish(2);
            screen.Events.Publish(3);

            Assert.That(failed.IsFaulted && !failed.gameObject.activeSelf, Is.True);
            Assert.That(sibling.IsVisible, Is.True);
            Assert.That(received, Is.EqualTo(5));
            Assert.That(skipped, Is.Zero);
            Transform slot = screen.transform.Find(failed.name + " (Error View)");
            ThemeView firstVisual = slot.GetComponentInChildren<ThemeView>();
            int notified = 0;
            scope.ViewReplaced += (_, _) => throw new InvalidOperationException();
            scope.ViewReplaced += (_, _) => notified++;
            ThemeDatabase dark = AssetDatabase.LoadAssetAtPath<ThemeDatabase>(Themes + "Dark.asset");
            Assert.That(scope.TrySetTheme(dark, out string error), Is.True, error);
            ThemeView secondVisual = slot.GetComponentInChildren<ThemeView>();
            Assert.That(secondVisual, Is.Not.SameAs(firstVisual));
            Assert.That(secondVisual.GetComponent<Image>().color, Is.EqualTo(dark.ErrorViewPrefab.GetComponent<Image>().color));
            Assert.That(notified, Is.GreaterThan(0));
            var group = slot.GetComponent<CanvasGroup>();
            Assert.That(windowHost.TryHide(), Is.True);
            Assert.That(group.alpha, Is.Zero);
            Assert.That(group.blocksRaycasts, Is.False);
            Assert.That(windowHost.TryShow(), Is.True);
            Assert.That(group.alpha, Is.EqualTo(1));
            failed.Destroyed += () => throw new InvalidOperationException();
            Assert.That(windowHost.TryClose(), Is.True);
            yield return null;
            Assert.That(slot == null, Is.True);
        }

        [UnityTest]
        public IEnumerator FailedRootKeepsWindowVisibilityAndCanBeReplaced()
        {
            UIComponent screen = Open(MakeWindow("Broken root", failAtRoot: true));
            Transform slot = host.Find(screen.name + " (Error View)");
            Assert.That(screen.IsFaulted, Is.True);
            Assert.That(screen.gameObject.activeSelf, Is.False);
            Assert.That(slot, Is.Not.Null);
            Assert.That(LifecycleProbe.Calls.Select(call => call.callback), Is.EqualTo(new[] { "Initialize" }));
            Assert.That(windowHost.TryHide(), Is.True);
            Assert.That(slot.GetComponent<CanvasGroup>().alpha, Is.Zero);
            Assert.That(windowHost.TryShow(), Is.True);
            Assert.That(slot.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1));

            UIComponent replacement = Open(MakeWindow("Healthy root"));
            Assert.That(replacement.IsVisible, Is.True);
            yield return null;
            Assert.That(slot == null && screen == null, Is.True);
        }

        [UnityTest]
        public IEnumerator InvalidStartupLogsOnceAndAValidOpenStillWorks()
        {
            UIComponent prefab = MakeWindow("Startup");
            Configure(windowHost, "startupScreen", prefab);
            Configure(windowHost, "screenHost", null);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Window 'Test canvas' needs a Screen Host"));
            windowHost.gameObject.SetActive(true);
            yield return null;
            yield return null;
            Assert.That(windowHost.CurrentScreen, Is.Null);

            Configure(windowHost, "screenHost", host);
            Assert.That(Open(prefab).IsVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator NestedVisibilityPreservesRequestsAndClearsInputFocus()
        {
            UIComponent screen = Open(MakeWindow("Nested", 2, nested: true));
            UIComponent parent = screen.Children.Single();
            UIComponent child = parent.Children.Single();
            Assert.That(child.Parent, Is.SameAs(parent));
            Assert.That(child.Events, Is.SameAs(screen.Events));

            var eventSystem = new GameObject("Test EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            try
            {
                Toggle toggle = child.GetComponentInChildren<Toggle>();
                eventSystem.SetSelectedGameObject(toggle.gameObject);
                Assert.That(eventSystem.currentSelectedGameObject, Is.SameAs(toggle.gameObject));

                var events = new System.Collections.Generic.List<string>();
                child.Hidden += () =>
                {
                    Assert.That(LifecycleProbe.Calls.Last().callback, Is.EqualTo("Hide"));
                    events.Add("Child hidden");
                };
                parent.Hidden += () => events.Add("Parent hidden");

                parent.SetVisible(false);
                Assert.That(child.IsVisible, Is.False);
                Assert.That(child.RequestedVisible, Is.True);
                Assert.That(eventSystem.currentSelectedGameObject, Is.Null);
                Assert.That(toggle.IsInteractable(), Is.False);
                Assert.That(child.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
                Assert.That(events, Is.EqualTo(new[] { "Child hidden", "Parent hidden" }));

                child.SetVisible(false);
                parent.SetVisible(true);
                Assert.That(parent.IsVisible, Is.True);
                Assert.That(child.IsVisible, Is.False);
                child.SetVisible(true);
                Assert.That(toggle.IsInteractable(), Is.True);

                LifecycleProbe.Calls.Clear();
                Assert.That(windowHost.TryClose(), Is.True);
                Assert.That(LifecycleProbe.Calls.Select(call => (call.name, call.callback)), Is.EqualTo(new[]
                {
                    ("Nested 1", "Hide"), ("Nested 0", "Hide"), ("Nested 1", "Destroy"), ("Nested 0", "Destroy")
                }));
            }
            finally
            {
                Object.Destroy(eventSystem.gameObject);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator MessagesAreIsolatedByMountAndDisposedOnClose()
        {
            UIComponent prefab = MakeWindow("Messages");
            UIComponent first = Open(prefab);
            var otherWindow = new GameObject("Other window", typeof(RectTransform), typeof(Canvas)).AddComponent<Window>();

            try
            {
                Configure(otherWindow, "screenHost", otherWindow.transform);
                Assert.That(otherWindow.TryOpen(prefab, out var second, out string error), Is.True, error);
                int count = 0;
                ScreenEvents firstEvents = first.Events;
                using var subscription = firstEvents.Subscribe<int>(first, value => count += value);
                using var otherSubscription = second.Events.Subscribe<int>(second, value => count += value * 100);

                firstEvents.Publish(1);
                second.Events.Publish(2);
                Assert.That(count, Is.EqualTo(201));

                subscription.Dispose();
                firstEvents.Publish(3);
                Assert.That(count, Is.EqualTo(201));

                using var finalSubscription = firstEvents.Subscribe<int>(first, value => count += value);
                Assert.That(windowHost.TryClose(), Is.True);
                firstEvents.Publish(4);
                Assert.That(count, Is.EqualTo(201));
            }
            finally
            {
                Object.Destroy(otherWindow.gameObject);
            }
            yield return null;
        }

        private UIComponent Open(UIComponent prefab)
        {
            windowHost.gameObject.SetActive(true);
            Assert.That(windowHost.TryOpen(prefab, out var window, out string error), Is.True, error);
            return window;
        }

        private UIComponent MakeWindow(string name, int controllerCount = 1, bool failOnOpen = false,
            bool unmappedThemeView = false, bool nested = false, bool failAtRoot = false)
        {
            RectTransform rect = AddChild(null, name);
            rect.anchoredPosition = new Vector2(17, -23);
            if (failAtRoot)
                rect.gameObject.AddComponent<LifecycleProbe>().failOnOpen = true;
            else
                rect.gameObject.AddComponent<UIComponent>();
            RectTransform owner = rect;
            for (int i = 0; i < controllerCount; i++)
            {
                var controller = AddChild(owner, $"{name} {i}").gameObject.AddComponent<LifecycleProbe>();
                controller.failOnOpen = failOnOpen && i == 0;
                controller.checkReentrancy = true;
                if (controller.failOnOpen)
                {
                    controller.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;
                    AddChild(controller.transform, "Control").gameObject.AddComponent<Button>();
                }
                if (nested)
                    owner = AddChild(controller.transform, "Layout wrapper");
            }
            var toggle = AssetDatabase.LoadAssetAtPath<GameObject>(Themes + "Light/toggle.prefab");
            PrefabUtility.InstantiatePrefab(toggle, owner);
            if (unmappedThemeView)
                AddChild(rect, "Unmapped view").gameObject.AddComponent<ThemeView>();
            try
            {
                return PrefabUtility.SaveAsPrefabAsset(rect.gameObject, $"{assetFolder}/{name}.prefab").GetComponent<UIComponent>();
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
