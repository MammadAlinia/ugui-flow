#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UguiFlow.Tests
{
    public sealed class DialogTests
    {
        private const string Package = "Packages/com.mammadalinia.ugui-flow/Runtime/";
        private Scene previousScene;
        private Scene scene;
        private string screenAsset;

        [SetUp]
        public void SetUp()
        {
            previousScene = SceneManager.GetActiveScene();
            scene = SceneManager.CreateScene("Dialog tests");
            SceneManager.SetActiveScene(scene);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.SetActiveScene(previousScene);
            if (scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(scene);
            if (screenAsset != null)
            {
                AssetDatabase.DeleteAsset(screenAsset);
                screenAsset = null;
            }
        }

        [UnityTest]
        public IEnumerator ShowsAllOverloadsAndPreservesQueueOrderAcrossCallbacks()
        {
            var calls = new List<int>();
            Dialog.Info("Choose an action", "Three icon choices",
                (Icons.Accept, () => calls.Add(-1)),
                (Icons.Cancel, () => calls.Add(-2)),
                (Icons.Retry, () =>
                {
                    Assert.That(Current(), Is.Null);
                    calls.Add(1);
                    Dialog.Info("Added from callback", "", () => calls.Add(4));
                }));
            Dialog.Error("Download failed", "Choose what happens next",
                ("Retry", () => throw new InvalidOperationException("Retry failed")),
                ("Cancel", () => calls.Add(-3)));
            Dialog.Error("Connection lost", "", () => calls.Add(3));
            yield return null;

            Assert.That(Host().gameObject.scene, Is.EqualTo(scene));
            BaseInputModule input = EventSystem.current.GetComponent<BaseInputModule>();
            Assert.That(input, Is.Not.Null);
#if ENABLE_INPUT_SYSTEM
            Assert.That(input.GetType().Name, Is.EqualTo("InputSystemUIInputModule"));
#endif
            Button[] icons = Actions();
            Assert.That(icons.Length, Is.EqualTo(3));
            Assert.That(icons.Select(button => button.GetComponent<ThemeView>().ContentIcon.sprite.name),
                Is.EqualTo(new[] { "Accept", "Cancel", "Retry" }));
            Assert.That(icons.All(button => button.GetComponent<ThemeView>().ContentLabel == null), Is.True);
            icons[2].onClick.Invoke();
            icons[2].onClick.Invoke();
            Assert.That(calls, Is.EqualTo(new[] { 1 }));

            Assert.That(Labels(), Is.EqualTo(new[] { "Retry", "Cancel" }));
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("InvalidOperationException: Retry failed"));
            Actions()[0].onClick.Invoke();
            Assert.That(Labels(), Is.EqualTo(new[] { "OK" }));
            Assert.That(Current().GetComponentsInChildren<ThemeView>(true)
                .Single(view => view.name == "Description").gameObject.activeSelf, Is.False);
            Actions()[0].onClick.Invoke();

            Assert.That(Current().GetComponentsInChildren<ThemeView>()
                .Single(view => view.name == "Title").ContentLabel.text, Is.EqualTo("Added from callback"));
            Actions()[0].onClick.Invoke();
            Assert.That(Current(), Is.Null);
            Assert.That(calls, Is.EqualTo(new[] { 1, 3, 4 }));
        }

        [UnityTest]
        public IEnumerator KeepsWindowVisibleAndFocusInsideTheModalUntilQueueEmpties()
        {
            var input = new GameObject("Existing Event System", typeof(EventSystem)).GetComponent<EventSystem>();
            Window window = CreateWindow(out UIComponent screen, out Toggle toggle);
            toggle.isOn = true;
            input.SetSelectedGameObject(toggle.gameObject);

            Dialog.Info("Overlay", "Choose an action", ("Continue", () => { }), ("Cancel", () => { }));
            Dialog.Info("Second", "", () => { });
            yield return null;

            Assert.That(EventSystem.current, Is.SameAs(input));
            Assert.That(window.CurrentScreen, Is.SameAs(screen));
            Assert.That(screen.IsVisible && toggle.isOn, Is.True);
            var pointer = new PointerEventData(input) { position = new Vector2(6, 6) };
            var hits = new List<RaycastResult>();
            input.RaycastAll(pointer, hits);
            Assert.That(hits.Any(hit => hit.gameObject == toggle.gameObject), Is.True);
            Assert.That(hits[0].gameObject, Is.SameAs(Current().gameObject));
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(Current(), Is.Not.Null);

            Button[] actions = Actions();
            Assert.That(input.currentSelectedGameObject, Is.SameAs(actions[0].gameObject));
            Move(input, MoveDirection.Up);
            Assert.That(input.currentSelectedGameObject, Is.SameAs(actions[0].gameObject));
            Move(input, MoveDirection.Down);
            Move(input, MoveDirection.Down);
            Assert.That(input.currentSelectedGameObject, Is.SameAs(actions[1].gameObject));
            yield return null;
            input.SetSelectedGameObject(null);
            yield return null;
            Assert.That(input.currentSelectedGameObject, Is.SameAs(actions[1].gameObject));

            ExecuteEvents.Execute(input.currentSelectedGameObject, new BaseEventData(input), ExecuteEvents.submitHandler);
            Assert.That(input.currentSelectedGameObject, Is.SameAs(Actions()[0].gameObject));
            Actions()[0].onClick.Invoke();
            Assert.That(input.currentSelectedGameObject, Is.SameAs(toggle.gameObject));
            Assert.That(window.CurrentScreen, Is.SameAs(screen));
            Assert.That(toggle.isOn, Is.True);
        }

        [UnityTest]
        public IEnumerator UsesSceneHostAndKeepsActionsAfterThemeReplacement()
        {
            var root = new GameObject("Custom overlay", typeof(RectTransform), typeof(DialogHost));
            var host = root.GetComponent<DialogHost>();
            var scope = root.AddComponent<ThemeScope>();
            Configure(scope, "initialTheme", AssetDatabase.LoadAssetAtPath<ThemeDatabase>(
                Package + "Theming/Defaults/Resources/UguiFlow/Light.asset"));
            Configure(host, "infoPrefab", Resources.Load<DialogScreen>("UguiFlow/Dialogs/Error"));

            int chosen = 0;
            Dialog.Info("Save failed", "Try again", (Icons.Retry, () => chosen = 1), (Icons.Cancel, () => chosen = 2));
            yield return null;
            Assert.That(Host(), Is.SameAs(host));
            Assert.That(Current().name, Is.EqualTo("Error(Clone)"));

            Button[] old = Actions();
            ThemeDatabase dark = AssetDatabase.LoadAssetAtPath<ThemeDatabase>(Package + "Theming/Defaults/Dark.asset");
            Assert.That(scope.TrySetTheme(dark, out string error), Is.True, error);
            yield return null;

            Button[] updated = Actions();
            Assert.That(updated[0], Is.Not.SameAs(old[0]));
            Assert.That(updated[0].GetComponent<ThemeView>().ContentIcon.sprite.name, Is.EqualTo("Retry"));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(updated[0].gameObject));
            Move(EventSystem.current, MoveDirection.Down);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(updated[1].gameObject));
            updated[1].onClick.Invoke();
            Assert.That(chosen, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator RejectsInvalidRequestsAndClearsPendingCallbacksOnSceneUnload()
        {
            Assert.Throws<ArgumentException>(() => Dialog.Info("Title", "", Array.Empty<(Icons, Action)>()));
            Assert.Throws<ArgumentException>(() => Dialog.Info("Title", "", (" ", () => { })));
            Assert.Throws<ArgumentException>(() => Dialog.Error("Title", "", ((Icons)42, () => { })));
            Assert.Throws<ArgumentException>(() => Dialog.Error("Title", "", (Action)null));
            Assert.That(Host(), Is.Null);

            int callbacks = 0;
            Dialog.Info("Opened", "", () => callbacks++);
            Dialog.Error("Queued", "", () => callbacks++);
            SceneManager.SetActiveScene(previousScene);
            yield return SceneManager.UnloadSceneAsync(scene);
            Assert.That(callbacks, Is.Zero);
            Assert.That(Host(), Is.Null);
        }

        [UnityTest]
        public IEnumerator WideAndNarrowOverlaysFitLongContentAndScrollToTheLastAction()
        {
            string description = string.Join(" ", Enumerable.Repeat(
                "Verify that the download is available and your connection is working before retrying.", 6));
            var choices = Enumerable.Range(1, 8)
                .Select(number => ($"Option {number}", (Action)(() => { }))).ToArray();

            foreach (Vector2 size in new[] { new Vector2(1040, 780), new Vector2(320, 480) })
            {
                if (size.x > 500)
                    Dialog.Info("Additional confirmation required", description, choices);
                else
                    Dialog.Error("Additional confirmation required", description, choices);

                // Give the canvas a deterministic rect for layout checks in batch mode.
                Canvas canvas = Host().GetComponent<Canvas>();
                Host().GetComponent<CanvasScaler>().enabled = false;
                canvas.renderMode = RenderMode.WorldSpace;
                ((RectTransform)canvas.transform).sizeDelta = size;
                yield return null;

                ScrollRect scroll = Current().GetComponentInChildren<ScrollRect>();
                Assert.That(scroll.viewport.rect.width, Is.LessThanOrEqualTo(size.x - 40));
                Assert.That(scroll.viewport.rect.height, Is.LessThanOrEqualTo(size.y - 40));
                Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
                Button[] actions = Actions();
                EventSystem input = EventSystem.current;
                for (int i = 1; i < actions.Length; i++)
                {
                    Move(input, MoveDirection.Down);
                    yield return null;
                }
                Assert.That(input.currentSelectedGameObject, Is.SameAs(actions[7].gameObject));
                Bounds last = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, actions[7].transform);
                Assert.That(last.min.y, Is.GreaterThanOrEqualTo(scroll.viewport.rect.yMin - 1));
                actions[7].onClick.Invoke();
            }
        }

        private Window CreateWindow(out UIComponent screen, out Toggle toggle)
        {
            var root = new GameObject("Window canvas", typeof(RectTransform), typeof(GraphicRaycaster), typeof(Window));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var window = root.GetComponent<Window>();
            Configure(window, "screenHost", root.GetComponent<RectTransform>());

            var source = new GameObject("Background screen", typeof(RectTransform), typeof(UIComponent));
            Stretch(source.GetComponent<RectTransform>());
            var control = new GameObject("Background toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
            control.transform.SetParent(source.transform, false);
            Stretch(control.GetComponent<RectTransform>());
            screenAsset = $"Assets/DialogTest-{Guid.NewGuid():N}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, screenAsset);
            Object.DestroyImmediate(source);
            Assert.That(window.TryOpen(prefab.GetComponent<UIComponent>(), out screen, out string error), Is.True, error);
            toggle = screen.GetComponentInChildren<Toggle>();
            return window;
        }

        private DialogHost Host() => Object.FindObjectsByType<DialogHost>(FindObjectsSortMode.None)
            .SingleOrDefault(host => host.gameObject.scene == scene);

        private DialogScreen Current() => Host() != null ? Host().GetComponentInChildren<DialogScreen>() : null;

        private Button[] Actions() => Current().GetComponentsInChildren<Button>();

        private string[] Labels() => Actions().Select(button => button.GetComponent<ThemeView>().ContentLabel.text).ToArray();

        private static void Move(EventSystem input, MoveDirection direction) =>
            ExecuteEvents.Execute(input.currentSelectedGameObject, new AxisEventData(input) { moveDir = direction },
                ExecuteEvents.moveHandler);

        private static void Configure(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
#endif
