#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace UguiFlow.Tests
{
    public sealed class ThemeScopeTests
    {
        private const string PreviewScene = "Assets/Demos/Themes/ThemePreview.unity";
        private Scene scene;
        private ThemeScope scope;

        [UnitySetUp]
        public IEnumerator OpenPreview()
        {
            scene = EditorSceneManager.LoadSceneInPlayMode(PreviewScene, new LoadSceneParameters(LoadSceneMode.Additive));
            yield return null;
            scope = scene.GetRootGameObjects().Select(root => root.GetComponent<ThemeScope>()).First(x => x != null);
            yield return null;
            Assert.That(scope.CurrentTheme.name, Is.EqualTo("Light"));
        }

        [UnityTearDown]
        public IEnumerator ClosePreview()
        {
            if (scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator ThemeSwitchPreservesControlValuesFocusAndHandlers()
        {
            ThemeView music = View("Music");
            var toggle = music.GetComponent<Toggle>();
            var slider = View("Volume").GetComponent<Slider>();
            int toggleCalls = 0;
            toggle.SetIsOnWithoutNotify(false);
            toggle.onValueChanged.AddListener(_ => toggleCalls++);
            slider.SetValueWithoutNotify(.28f);
            EventSystem.current.SetSelectedGameObject(music.gameObject);
            ScrollRect scroll = AddScrollableProbe();
            scroll.verticalNormalizedPosition = .37f;

            Click("Dark");

            Assert.That(scope.CurrentTheme.name, Is.EqualTo("Dark"));
            ThemeView themedMusic = View("Music");
            Assert.That(themedMusic, Is.Not.SameAs(music));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(themedMusic.gameObject));
            Assert.That(themedMusic.GetComponent<Toggle>().isOn, Is.False);
            Assert.That(View("Volume").GetComponent<Slider>().value, Is.EqualTo(.28f).Within(.001f));
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(.37f).Within(.01f));
            Assert.That(toggleCalls, Is.Zero);
            themedMusic.GetComponent<Toggle>().isOn = true;
            Assert.That(toggleCalls, Is.EqualTo(1));

            Click("Light");
            Assert.That(scope.CurrentTheme.name, Is.EqualTo("Light"));
            yield return null;
            Assert.That(music == null, Is.True);
        }

        [Test]
        public void ButtonOverrideAndFontOnlyOverrideReplaceOnlyWhatChanges()
        {
            ThemeView title = View("Settings themes");
            ThemeView reset = View("Reset settings");
            ThemeView selector = View("Light");
            var originalFont = title.ContentLabel.font;
            var originalColor = reset.GetComponent<Image>().color;

            Click("Custom button");
            Assert.That(View("Reset settings"), Is.Not.SameAs(reset));
            Assert.That(View("Reset settings").GetComponent<Image>().color, Is.Not.EqualTo(originalColor));
            Assert.That(View("Light"), Is.SameAs(selector));
            Assert.That(View("Settings themes"), Is.SameAs(title));

            Click("Light");
            ThemeView restoredButton = View("Reset settings");
            Click("Alternate font");
            Assert.That(View("Reset settings"), Is.SameAs(restoredButton));
            Assert.That(View("Settings themes"), Is.SameAs(title));
            Assert.That(title.ContentLabel.font, Is.EqualTo(scope.CurrentTheme.DefaultFont));
            Assert.That(title.ContentLabel.font, Is.Not.EqualTo(originalFont));

            View("Music").GetComponent<Toggle>().SetIsOnWithoutNotify(false);
            Click("Reset settings");
            Assert.That(View("Music").GetComponent<Toggle>().isOn, Is.True);
        }

        [Test]
        public void DatabaseLooksUpTheViewKeyAcrossButtonVariants()
        {
            ThemeDatabase light = scope.CurrentTheme;
            ThemeView selector = View("Light");
            ThemeView reset = View("Reset settings");
            Assert.That(light.TryGetPrefab(selector, out ThemeView lightSelector), Is.True);
            Assert.That(light.TryGetPrefab(reset, out ThemeView lightReset), Is.True);
            Assert.That(lightSelector.Key, Is.Not.EqualTo(lightReset.Key));

            Click("Dark");
            ThemeDatabase dark = scope.CurrentTheme;
            Assert.That(dark.TryGetPrefab(lightSelector, out ThemeView darkSelector), Is.True);
            Assert.That(dark.TryGetPrefab(lightReset, out ThemeView darkReset), Is.True);
            Assert.That(darkSelector.Key, Is.EqualTo(lightSelector.Key));
            Assert.That(darkReset.Key, Is.EqualTo(lightReset.Key));
            Assert.That(darkReset, Is.Not.EqualTo(lightReset));
        }

        [Test]
        public void IncompleteThemeLeavesCurrentViewsInPlace()
        {
            ThemeDatabase initial = scope.CurrentTheme;
            ThemeView music = View("Music");
            var empty = ScriptableObject.CreateInstance<ThemeDatabase>();
            try
            {
                Assert.That(scope.TrySetTheme(empty, out string error), Is.False);
                Assert.That(error, Does.Contain("compatible prefab"));
                Assert.That(scope.CurrentTheme, Is.SameAs(initial));
                Assert.That(View("Music"), Is.SameAs(music));
            }
            finally
            {
                Object.DestroyImmediate(empty);
            }
        }

        private ScrollRect AddScrollableProbe()
        {
            var host = new GameObject("Scroll position probe", typeof(RectTransform), typeof(ScrollRect));
            var viewport = (RectTransform)host.transform;
            viewport.SetParent(scope.transform, false);
            viewport.sizeDelta = new Vector2(180, 100);
            var content = (RectTransform)new GameObject("Content", typeof(RectTransform)).transform;
            content.SetParent(viewport, false);
            content.sizeDelta = new Vector2(180, 400);
            var scroll = host.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.viewport = viewport;
            scroll.content = content;
            Canvas.ForceUpdateCanvases();
            return scroll;
        }

        private ThemeView View(string name) => scope.GetComponentsInChildren<ThemeView>().First(view => view.name == name);

        private void Click(string name) => View(name).GetComponent<Button>().onClick.Invoke();
    }
}
#endif
