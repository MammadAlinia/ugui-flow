using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace UguiFlow.Tests
{
    public sealed class ResponsiveLayoutTests
    {
        private RectTransform root;

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
            yield return null;
        }

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
