using System.Collections;
#if UNITY_EDITOR
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace UguiFlow.Tests
{
    public sealed class ResponsiveLayoutTests
    {
        private RectTransform root;
#if UNITY_EDITOR
        private Scene demoScene;
#endif

        [SetUp]
        public void CreateRoot()
        {
            root = CreateRect("Test Root");
            root.gameObject.AddComponent<Canvas>();
        }

        [UnityTearDown]
        public IEnumerator DestroyRoot()
        {
            Object.Destroy(root.gameObject);
#if UNITY_EDITOR
            if (demoScene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(demoScene);
#endif
            yield return null;
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator DemoMountsSettingsAndReflowsTheQuickSettingsGrid()
        {
            demoScene = EditorSceneManager.LoadSceneInPlayMode("Assets/Demos/ResponsiveLayout/ResponsiveLayout.unity",
                new LoadSceneParameters(LoadSceneMode.Additive));
            yield return null;
            yield return null;

            Window window = demoScene.GetRootGameObjects().Select(root => root.GetComponent<Window>())
                .Single(component => component != null);
            UIComponent screen = window.CurrentScreen;
            Assert.That(screen, Is.Not.Null);
            Assert.That(screen.IsVisible, Is.True);
            Assert.That(screen.GetComponentsInChildren<Canvas>(true), Is.Empty);

            UIComponent quickSettings = screen.Children.Single(component => component.name == "Quick Settings Grid");
            UIComponent advancedSettings = screen.Children.Single(component => component.name == "Advanced Settings Scroll View");
            Assert.That(quickSettings.IsVisible && advancedSettings.IsVisible, Is.True);

            GridLayoutGroup grid = quickSettings.GetComponent<GridLayoutGroup>();
            RectTransform screenRect = (RectTransform)screen.transform;
            screenRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1040);
            Canvas.ForceUpdateCanvases();
            Assert.That(grid.constraintCount, Is.EqualTo(2));

            screenRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 440);
            Canvas.ForceUpdateCanvases();
            Assert.That(grid.constraintCount, Is.EqualTo(1));
        }
#endif

        [Test]
        public void GridReflowsDirectChildrenInSiblingOrder()
        {
            RectTransform container = CreateRect("Container", root);
            var containerLayout = container.gameObject.AddComponent<VerticalLayoutGroup>();
            containerLayout.childControlWidth = true;
            containerLayout.childForceExpandWidth = true;
            RectTransform gridRect = CreateRect("Grid", container);
            var grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(12, 12, 8, 8);
            grid.spacing = new Vector2(16, 10);
            grid.cellSize = new Vector2(100, 60);
            gridRect.gameObject.AddComponent<ResponsiveGrid>();
            RectTransform firstView = CreateRect("First Visual", gridRect);
            RectTransform secondView = CreateRect("Second Visual", gridRect);

            container.sizeDelta = new Vector2(760, 250);
            Canvas.ForceUpdateCanvases();
            Assert.That(grid.constraintCount, Is.EqualTo(2));
            Assert.That(firstView.rect.width, Is.EqualTo(360).Within(0.01));

            container.sizeDelta = new Vector2(640, 250);
            Canvas.ForceUpdateCanvases();
            Assert.That(grid.constraintCount, Is.EqualTo(2));
            Assert.That(firstView.rect.width, Is.EqualTo(300).Within(0.01));
            Assert.That(firstView.position.y, Is.EqualTo(secondView.position.y).Within(0.01));
            Assert.That(firstView.position.x, Is.LessThan(secondView.position.x));

            container.sizeDelta = new Vector2(639, 250);
            Canvas.ForceUpdateCanvases();
            Assert.That(grid.constraintCount, Is.EqualTo(1));
            Assert.That(firstView.rect.width, Is.EqualTo(615).Within(0.01));
            Assert.That(firstView.position.x, Is.EqualTo(secondView.position.x).Within(0.01));
            Assert.That(firstView.position.y, Is.GreaterThan(secondView.position.y));

            container.sizeDelta = new Vector2(760, 250);
            Canvas.ForceUpdateCanvases();
            Assert.That(grid.constraintCount, Is.EqualTo(2));
            Assert.That(gridRect.GetChild(0), Is.SameAs(firstView));
            Assert.That(gridRect.GetChild(1), Is.SameAs(secondView));
        }

        private static RectTransform CreateRect(string name, Transform parent = null)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            return rect;
        }
    }
}
