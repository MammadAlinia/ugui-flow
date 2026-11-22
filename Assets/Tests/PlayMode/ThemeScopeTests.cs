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

        [TestCase(368)]
        [TestCase(720)]
        public void FontOnlySwitchKeepsLayoutHeightAndControlPositions(int width)
        {
            var frame = (RectTransform)scope.transform.Find("Frame");
            frame.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            var page = (RectTransform)frame.Find("Page");
            Canvas.ForceUpdateCanvases();
            float initialHeight = page.rect.height;
            var controls = new[] { View("Light"), View("Music"), View("Volume"), View("Reset settings") };
            Vector3[] initialPositions = controls.Select(view => view.transform.localPosition).ToArray();

            Click("Alternate font");
            Canvas.ForceUpdateCanvases();

            Assert.That(page.rect.height, Is.EqualTo(initialHeight).Within(.01f));
            for (int i = 0; i < controls.Length; i++)
                Assert.That(Vector3.Distance(controls[i].transform.localPosition, initialPositions[i]), Is.LessThan(.01f));

            foreach (string name in new[] { "Settings themes", "Switch themes and check that your settings stay in place.", "Audio settings" })
            {
                var label = View(name).ContentLabel;
                Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(label.rectTransform.rect.height + .01f), name);
            }
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

        [Test]
        public void AdditionalViewsSwitchThemesAndKeepProgressReadOnly()
        {
            ThemeView badge = View("Download status");
            badge.ContentLabel.text = "Paused";
            var progress = View("Asset download").GetComponent<Slider>();
            progress.SetValueWithoutNotify(.37f);
            View("Volume").GetComponent<Slider>().SetValueWithoutNotify(.2f);
            var separatorColor = View("Downloads divider").GetComponent<Image>().color;

            Click("Dark");

            Assert.That(View("Downloads").ContentLabel.text, Is.EqualTo("Downloads"));
            Assert.That(View("Download status").ContentLabel.text, Is.EqualTo("Paused"));
            Assert.That(View("Downloads divider").GetComponent<Image>().color, Is.Not.EqualTo(separatorColor));
            var themedProgress = View("Asset download").GetComponent<Slider>();
            Assert.That(themedProgress.interactable, Is.False);
            Assert.That(themedProgress.value, Is.EqualTo(.37f).Within(.001f));

            Click("Reset settings");
            Assert.That(View("Volume").GetComponent<Slider>().value, Is.EqualTo(.65f).Within(.001f));
            Assert.That(themedProgress.value, Is.EqualTo(.37f).Within(.001f));
        }

        [Test]
        public void CardsAndActionButtonsKeepTheirContentAndActionsAcrossThemes()
        {
            ThemeView card = View("Offline content");
            card.ContentLabel.text = "Cached content";
            card.SupportingLabel.text = "Ready for offline use.";
            var icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f));
            try
            {
                View("Complete download").ContentIcon.sprite = icon;
                Click("Start download");
                Assert.That(View("Asset download").GetComponent<Slider>().value, Is.EqualTo(.25f));

                Click("Dark");
                Assert.That(View("Offline content").ContentLabel.text, Is.EqualTo("Cached content"));
                Assert.That(View("Offline content").SupportingLabel.text, Is.EqualTo("Ready for offline use."));
                Assert.That(View("Complete download").ContentIcon.sprite, Is.SameAs(icon));

                Click("Pause download");
                Assert.That(View("Download status").ContentLabel.text, Is.EqualTo("Paused"));
                Click("Clear download");
                Assert.That(View("Asset download").GetComponent<Slider>().value, Is.Zero);
                Click("Complete download");
                Assert.That(View("Download status").ContentLabel.text, Is.EqualTo("Complete"));
                Assert.That(View("Asset download").GetComponent<Slider>().value, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(icon);
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
