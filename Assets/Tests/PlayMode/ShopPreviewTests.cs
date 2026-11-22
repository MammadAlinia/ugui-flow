#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace UguiFlow.Tests
{
    public sealed class ShopPreviewTests
    {
        private Scene scene;

        [UnityTearDown]
        public IEnumerator ClosePreview()
        {
            if (scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator ShopReflowsScrollsAndKeepsItsPositionAcrossThemes()
        {
            scene = EditorSceneManager.LoadSceneInPlayMode("Assets/Demos/Shop/ShopPreview.unity",
                new LoadSceneParameters(LoadSceneMode.Additive));
            yield return null;
            yield return null;

            ThemeScope scope = scene.GetRootGameObjects().Select(root => root.GetComponent<ThemeScope>()).First(x => x != null);
            ScrollRect scroll = scope.GetComponentInChildren<ScrollRect>();
            RectTransform layout = (RectTransform)scroll.transform.parent;
            var grid = scroll.content.GetComponent<GridLayoutGroup>();
            Assert.That(scroll.content.childCount, Is.EqualTo(30));
            Assert.That(scroll.content.GetChild(0).name, Is.EqualTo("Trail rations"));

            layout.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 900);
            Canvas.ForceUpdateCanvases();
            Assert.That(grid.constraintCount, Is.EqualTo(3));
            Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));

            layout.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 368);
            Canvas.ForceUpdateCanvases();
            Assert.That(grid.constraintCount, Is.EqualTo(1));
            var title = scope.GetComponentsInChildren<ThemeView>().First(view => view.name == "Outpost shop").ContentLabel;
            Assert.That(title.preferredHeight, Is.LessThanOrEqualTo(title.rectTransform.rect.height));
            scroll.verticalNormalizedPosition = .42f;
            ThemeView darkButton = scope.GetComponentsInChildren<ThemeView>().First(view => view.name == "Dark");
            darkButton.GetComponent<Button>().onClick.Invoke();
            Assert.That(scope.CurrentTheme.name, Is.EqualTo("Dark"));
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(.42f).Within(.01f));

            scroll.verticalNormalizedPosition = 0;
            Canvas.ForceUpdateCanvases();
            var last = (RectTransform)scroll.content.GetChild(scroll.content.childCount - 1);
            ThemeView card = last.GetComponent<ThemeView>();
            Assert.That(card.ContentLabel.text, Is.EqualTo("Weather radio"));
            Assert.That(card.SupportingLabel.text, Does.Contain("credits"));
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, last);
            Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(scroll.viewport.rect.yMin - 1));
            Assert.That(bounds.max.y, Is.LessThanOrEqualTo(scroll.viewport.rect.yMax + 1));
        }
    }
}
#endif
