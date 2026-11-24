#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace UguiFlow.Tests
{
    public sealed class NotesNavigationTests
    {
        private Scene scene;
        private Window window;

        [UnitySetUp]
        public IEnumerator OpenDemo()
        {
            scene = EditorSceneManager.LoadSceneInPlayMode(
                "Assets/Demos/ScreenLifecycle/ScreenLifecycle.unity",
                new LoadSceneParameters(LoadSceneMode.Additive));
            yield return null;

            window = scene.GetRootGameObjects().Select(root => root.GetComponentInChildren<Window>())
                .Single(component => component != null);
            Assert.That(window.CurrentScreen, Is.Not.Null);
        }

        [UnityTearDown]
        public IEnumerator UnloadDemo()
        {
            if (scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator ThreeTabsShareSelectionWithCode()
        {
            const string notesPath = "Assets/Demos/ScreenLifecycle/NotesScreen.prefab";
            const string tabPath = "Packages/com.mammadalinia.ugui-flow/Runtime/Navigation/Prefabs/Tab.prefab";
            string folder = AssetDatabase.GUIDToAssetPath(
                AssetDatabase.CreateFolder("Assets", "UguiFlowTests-" + System.Guid.NewGuid().ToString("N")));
            string prefabPath = folder + "/ThreeTabs.prefab";
            try
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(notesPath);
                try
                {
                    PageNavigator navigator = contents.GetComponentInChildren<PageNavigator>();
                    var thirdPage = new GameObject("Third page", typeof(RectTransform), typeof(UIComponent));
                    thirdPage.transform.SetParent(navigator.transform, false);

                    var navigatorSettings = new SerializedObject(navigator);
                    SerializedProperty pages = navigatorSettings.FindProperty("pages");
                    pages.arraySize = 3;
                    pages.GetArrayElementAtIndex(2).objectReferenceValue = thirdPage.GetComponent<UIComponent>();
                    navigatorSettings.ApplyModifiedPropertiesWithoutUndo();

                    Transform row = contents.GetComponentInChildren<NavigationBar>().transform.Find("Tabs");
                    GameObject tabPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(tabPath);
                    var thirdTab = (GameObject)PrefabUtility.InstantiatePrefab(tabPrefab, row);
                    thirdTab.name = "Third tab";
                    var tabSettings = new SerializedObject(thirdTab.GetComponent<NavigationTab>());
                    tabSettings.FindProperty("page").objectReferenceValue = thirdPage.GetComponent<UIComponent>();
                    tabSettings.ApplyModifiedPropertiesWithoutUndo();

                    Assert.That(PrefabUtility.SaveAsPrefabAsset(contents, prefabPath), Is.Not.Null);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath).GetComponent<UIComponent>();
                Assert.That(window.TryOpen(prefab, out UIComponent screen, out string error), Is.True, error);
                var mountedNavigator = screen.GetComponentInChildren<PageNavigator>();
                var bar = screen.GetComponentInChildren<NavigationBar>();
                UIComponent first = mountedNavigator.Children.Single(page => page.name == "FieldNotes");
                UIComponent second = mountedNavigator.Children.Single(page => page.name == "ReviewNotes");
                UIComponent third = mountedNavigator.Children.Single(page => page.name == "Third page");
                var indicator = (RectTransform)bar.transform.Find("Selected tab indicator");
                var changes = new List<PageChanged>();
                using var subscription = screen.Events.Subscribe<PageChanged>(screen, changes.Add);
                Assert.That(mountedNavigator.SelectedPage, Is.SameAs(first));
                Assert.That(first.IsVisible && !second.IsVisible && !third.IsVisible, Is.True);

                yield return null;
                Click(ButtonNamed(bar, "Review notes tab"));
                Assert.That(mountedNavigator.SelectedPage, Is.SameAs(second));
                Assert.That(mountedNavigator.TrySelectPage(third), Is.True);
                Assert.That(mountedNavigator.TrySelectPage(third), Is.True);
                Assert.That(mountedNavigator.TrySelectPage(screen), Is.False);
                Assert.That(mountedNavigator.TrySelectPage(null), Is.False);
                Assert.That(mountedNavigator.SelectedPage, Is.SameAs(third));
                Assert.That(!first.IsVisible && !second.IsVisible && third.IsVisible, Is.True);
                Assert.That(changes.Select(change => (change.Navigator, change.Previous, change.Current)),
                    Is.EqualTo(new[] { (mountedNavigator, first, second), (mountedNavigator, second, third) }));

                yield return new WaitForSecondsRealtime(0.35f);
                Assert.That(indicator.anchorMin.x, Is.EqualTo(2f / 3f).Within(0.001f));
                Assert.That(indicator.anchorMax.x, Is.EqualTo(1f).Within(0.001f));

                Assert.That(window.TryHide(), Is.True);
                Assert.That(window.TryShow(), Is.True);
                Assert.That(mountedNavigator.SelectedPage, Is.SameAs(third));
                Assert.That(third.IsVisible, Is.True);
                Assert.That(changes, Has.Count.EqualTo(2));
            }
            finally
            {
                window.TryClose();
                AssetDatabase.DeleteAsset(folder);
            }
        }

        [UnityTest]
        public IEnumerator SwitchingTabsAndThemeKeepsPageEditsAndUpdatesInputAndIndicator()
        {
            UIComponent screen = window.CurrentScreen;
            UIComponent navbar = screen.Children.Single(component => component.name == "Navbar");
            UIComponent body = screen.Children.Single(component => component.name == "Body");
            UIComponent field = body.Children.Single(component => component.name == "FieldNotes");
            UIComponent review = body.Children.Single(component => component.name == "ReviewNotes");
            RectTransform indicator = (RectTransform)navbar.transform.Find("Selected tab indicator");
            Assert.That(field.IsVisible, Is.True);
            Assert.That(review.IsVisible, Is.False);
            Assert.That(indicator.anchorMin.x, Is.EqualTo(0f).Within(0.001f));

            Toggle toggle = field.GetComponentInChildren<Toggle>();
            bool editedValue = !toggle.isOn;
            Click(toggle);
            Assert.That(toggle.isOn, Is.EqualTo(editedValue));
            Button addCard = ButtonNamed(field, "Add card");
            ScrollRect scroll = field.GetComponentInChildren<ScrollRect>();
            int initialCardCount = scroll.content.childCount;
            Click(addCard);
            Assert.That(scroll.content.childCount, Is.EqualTo(initialCardCount + 1));
            Click(ButtonNamed(field, "Remove card"));
            Assert.That(scroll.content.childCount, Is.EqualTo(initialCardCount));
            for (int i = 0; i < 12; i++)
                Click(addCard);
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0.35f;
            yield return null;
            float editedScrollPosition = scroll.verticalNormalizedPosition;
            int editedCardCount = scroll.content.childCount;

            EventSystem.current.SetSelectedGameObject(toggle.gameObject);
            ButtonNamed(navbar, "Review notes tab").onClick.Invoke();
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null);
            Assert.That(toggle.IsInteractable(), Is.False);
            Assert.That(toggle.targetGraphic.Raycast(CenterOf(toggle.targetGraphic, null), null), Is.False);
            Assert.That(field.IsVisible, Is.False);
            Assert.That(review.IsVisible, Is.True);
            ScrollRect reviewScroll = review.GetComponentInChildren<ScrollRect>();
            int reviewCardCount = reviewScroll.content.childCount;
            Click(ButtonNamed(review, "Add card"));
            Assert.That(reviewScroll.content.childCount, Is.EqualTo(reviewCardCount + 1));
            Click(ButtonNamed(review, "Remove card"));
            Assert.That(reviewScroll.content.childCount, Is.EqualTo(reviewCardCount));
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(indicator.anchorMin.x, Is.EqualTo(0.5f).Within(0.001f));

            ButtonNamed(window, "Dark").onClick.Invoke();
            ButtonNamed(window, "Light").onClick.Invoke();
            ButtonNamed(window, "Dark").onClick.Invoke();
            yield return null;
            Click(ButtonNamed(navbar, "Field notes tab"));
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(field.IsVisible, Is.True);
            Assert.That(review.IsVisible, Is.False);
            Assert.That(indicator.anchorMin.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(field.GetComponentInChildren<Toggle>().isOn, Is.EqualTo(editedValue));
            Assert.That(scroll.content.childCount, Is.EqualTo(editedCardCount));
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(editedScrollPosition).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator DemoDialogsUseTheSelectedThemeAndRecordEachChoice()
        {
            Scene previousScene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);
            try
            {
                UIComponent screen = window.CurrentScreen;
                PageNavigator navigator = screen.GetComponentInChildren<PageNavigator>();
                Click(ButtonNamed(window, "Dark"));
                Click(ButtonNamed(screen, "Review notes tab"));
                UIComponent selected = navigator.SelectedPage;
                var host = scene.GetRootGameObjects().Select(root => root.GetComponent<DialogHost>())
                    .Single(component => component != null);
                Assert.That(host.GetComponent<ThemeScope>().CurrentTheme,
                    Is.SameAs(window.GetComponent<ThemeScope>().CurrentTheme));

                foreach (var (trigger, action) in new[] { ("Info dialog", "OK"), ("Error dialog", "Retry"), ("Error dialog", "Cancel") })
                {
                    Click(ButtonNamed(window, trigger));
                    yield return null;
                    DialogScreen dialog = host.GetComponentInChildren<DialogScreen>();
                    Assert.That(dialog, Is.Not.Null);
                    Assert.That(window.CurrentScreen, Is.SameAs(screen));
                    Assert.That(screen.IsVisible && selected.IsVisible, Is.True);
                    Assert.That(navigator.SelectedPage, Is.SameAs(selected));

                    Button button = dialog.GetComponentsInChildren<Button>()
                        .Single(candidate => candidate.GetComponent<ThemeView>().ContentLabel.text == action);
                    Click(button);
                    Assert.That(host.GetComponentInChildren<DialogScreen>(), Is.Null);
                    ThemeView callbackLog = window.GetComponentsInChildren<ThemeView>()
                        .Single(view => view.name == "Callback log");
                    Assert.That(callbackLog.ContentLabel.text, Does.Contain($"{action}   /   {trigger}"));
                }
            }
            finally
            {
                SceneManager.SetActiveScene(previousScene);
            }
        }

        [UnityTest]
        public IEnumerator TestErrorReplacesEitherPageAndResetRestoresTheScreen()
        {
            UIComponent screen = window.CurrentScreen;
            UIComponent navbar = screen.Children.Single(component => component.name == "Navbar");
            UIComponent body = screen.Children.Single(component => component.name == "Body");
            UIComponent field = body.Children.Single(component => component.name == "FieldNotes");
            UIComponent review = body.Children.Single(component => component.name == "ReviewNotes");

            Click(ButtonNamed(field, "Test error"));
            Assert.That(field.IsFaulted, Is.True);
            Assert.That(field.gameObject.activeSelf, Is.False);
            Assert.That(navbar.IsVisible, Is.True);
            Transform fieldError = body.transform.Find("FieldNotes (Error View)");
            Assert.That(fieldError.GetComponentInChildren<ThemeView>().ContentLabel.text, Is.EqualTo("View unavailable"));
            Assert.That(fieldError.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1));

            Click(ButtonNamed(navbar, "Review notes tab"));
            Assert.That(review.IsVisible, Is.True);
            Assert.That(fieldError.GetComponent<CanvasGroup>().alpha, Is.Zero);
            Click(ButtonNamed(window, "Dark"));
            ThemeView darkError = window.GetComponent<ThemeScope>().CurrentTheme.ErrorViewPrefab;
            Assert.That(fieldError.GetComponentInChildren<Image>().color, Is.EqualTo(darkError.GetComponent<Image>().color));

            Click(ButtonNamed(review, "Test error"));
            Assert.That(review.IsFaulted, Is.True);
            Assert.That(review.gameObject.activeSelf, Is.False);
            Assert.That(body.transform.Find("ReviewNotes (Error View)").GetComponentInChildren<Image>().color,
                Is.EqualTo(darkError.GetComponent<Image>().color));

            Click(ButtonNamed(window, "Reset"));
            Assert.That(window.CurrentScreen, Is.Not.SameAs(screen));
            int availableActions = window.CurrentScreen.GetComponentsInChildren<Button>()
                .Count(button => button.name == "Test error" && button.IsInteractable());
            Assert.That(availableActions, Is.EqualTo(1));
            yield return null;
            Assert.That(fieldError == null, Is.True);
        }

        [UnityTest]
        public IEnumerator TestTaskErrorReplacesTheTaskAndLeavesNotesUsable()
        {
            UIComponent field = window.CurrentScreen.GetComponentsInChildren<UIComponent>()
                .Single(component => component.name == "FieldNotes");
            UIComponent task = field.GetComponentsInChildren<UIComponent>().Single(component => component.name == "Sample task");
            RectTransform cards = field.GetComponentInChildren<ScrollRect>().content;
            Transform note = cards.Find("Note 1");
            Canvas.ForceUpdateCanvases();
            Vector2 taskSize = ((RectTransform)task.transform).rect.size;

            Click(ButtonNamed(task, "Test task error"));
            Assert.That(task.IsFaulted, Is.True);
            Assert.That(task.gameObject.activeSelf, Is.False);
            Assert.That(field.IsVisible, Is.True);
            Assert.That(note.gameObject.activeInHierarchy, Is.True);

            var error = (RectTransform)cards.Find("Sample task (Error View)");
            Assert.That(error.GetSiblingIndex(), Is.Zero);
            Assert.That(error.GetComponentInChildren<ThemeView>().ContentLabel.text, Is.EqualTo("View unavailable"));
            Canvas.ForceUpdateCanvases();
            Assert.That(Vector2.Distance(error.rect.size, taskSize), Is.LessThan(0.01f));

            Click(ButtonNamed(field, "Add card"));
            Assert.That(cards.Find("Note 2"), Is.Not.Null);
            Click(ButtonNamed(field, "Remove card"));
            Assert.That(cards.Find("Note 2"), Is.Null);
            Click(ButtonNamed(field, "Remove card"));
            Click(ButtonNamed(field, "Remove card"));
            Assert.That(cards.Find("Note 1"), Is.Null);
            Assert.That(error.parent, Is.SameAs(cards));

            Click(ButtonNamed(window, "Dark"));
            ThemeView darkError = window.GetComponent<ThemeScope>().CurrentTheme.ErrorViewPrefab;
            Assert.That(error.GetComponentInChildren<Image>().color, Is.EqualTo(darkError.GetComponent<Image>().color));
            yield return null;
        }

        private static void Click(Selectable control)
        {
            Canvas.ForceUpdateCanvases();
            Graphic graphic = control.targetGraphic;
            GraphicRaycaster raycaster = control.GetComponentInParent<GraphicRaycaster>();
            Assert.That(raycaster, Is.Not.Null);
            var raycastableGraphics = GraphicRegistry.GetRaycastableGraphicsForCanvas(raycaster.GetComponent<Canvas>());
            Assert.That(raycastableGraphics.Contains(graphic), Is.True, $"{control.name} must be on the raycaster's Canvas.");

            var pointer = new PointerEventData(EventSystem.current)
            {
                position = CenterOf(graphic, raycaster.eventCamera),
                button = PointerEventData.InputButton.Left
            };
            Assert.That(graphic.Raycast(pointer.position, raycaster.eventCamera), Is.True);
            GameObject target = control.gameObject;
            // Batch Mode leaves overlay graphics without a render depth.
            if (!Application.isBatchMode || graphic.depth >= 0)
            {
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, hits);
                Assert.That(hits, Is.Not.Empty, $"No raycast hit over {control.name}.");
                target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
                Assert.That(target, Is.SameAs(control.gameObject),
                    $"The raycast over {control.name} hit {hits[0].gameObject.name}.");
            }
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
        }

        private static Vector2 CenterOf(Graphic graphic, Camera eventCamera)
        {
            RectTransform rect = graphic.rectTransform;
            return RectTransformUtility.WorldToScreenPoint(eventCamera, rect.TransformPoint(rect.rect.center));
        }

        private static Button ButtonNamed(Component owner, string name) =>
            owner.GetComponentsInChildren<Button>().Single(button => button.name == name);
    }
}
#endif
