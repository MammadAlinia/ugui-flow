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

        [UnityTest]
        public IEnumerator SlotMountsMeasuresAndReplacesAView()
        {
            RectTransform group = CreateRect("List", root);
            group.sizeDelta = new Vector2(300, 200);
            var layout = group.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            var slot = CreateRect("Slot", group).gameObject.AddComponent<LayoutSlot>();

            RectTransform firstPrefab = CreateRect("First Visual", root);
            firstPrefab.gameObject.AddComponent<LayoutElement>().preferredHeight = 42;
            slot.SetViewPrefab(firstPrefab);
            RectTransform firstView = slot.View;
            Canvas.ForceUpdateCanvases();

            Assert.That(firstView.parent, Is.EqualTo(slot.transform));
            Assert.That(firstView.rect.size, Is.EqualTo(((RectTransform)slot.transform).rect.size));
            Assert.That(firstView.rect.height, Is.EqualTo(42).Within(0.01));

            firstView.GetComponent<LayoutElement>().preferredHeight = 60;
            Canvas.ForceUpdateCanvases();
            Assert.That(firstView.rect.height, Is.EqualTo(60).Within(0.01));

            slot.SetViewPrefab(firstPrefab);
            Assert.That(slot.View, Is.SameAs(firstView));

            RectTransform secondPrefab = CreateRect("Second Visual", root);
            secondPrefab.gameObject.AddComponent<LayoutElement>().preferredHeight = 90;
            slot.SetViewPrefab(secondPrefab);
            Canvas.ForceUpdateCanvases();
            Assert.That(firstView.gameObject.activeSelf, Is.False);
            Assert.That(slot.View.rect.height, Is.EqualTo(90).Within(0.01));

            yield return null;
            Assert.That(slot.transform.childCount, Is.EqualTo(1));
            slot.SetViewPrefab(null);
            Canvas.ForceUpdateCanvases();
            Assert.That(slot.View, Is.Null);
            Assert.That(((RectTransform)slot.transform).rect.height, Is.Zero.Within(0.01));
        }

        [Test]
        public void GridReflowsAtTheBreakpointWithoutReplacingViews()
        {
            RectTransform gridRect = CreateRect("Grid", root);
            var grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(12, 12, 8, 8);
            grid.spacing = new Vector2(16, 10);
            grid.cellSize = new Vector2(100, 60);
            gridRect.gameObject.AddComponent<ResponsiveGrid>();
            RectTransform prefab = CreateRect("Visual", root);

            var firstSlot = CreateRect("First", gridRect).gameObject.AddComponent<LayoutSlot>();
            var secondSlot = CreateRect("Second", gridRect).gameObject.AddComponent<LayoutSlot>();
            firstSlot.SetViewPrefab(prefab);
            secondSlot.SetViewPrefab(prefab);
            RectTransform firstView = firstSlot.View;
            RectTransform secondView = secondSlot.View;

            gridRect.sizeDelta = new Vector2(640, 250);
            Canvas.ForceUpdateCanvases();
            Assert.That(grid.constraintCount, Is.EqualTo(2));
            Assert.That(firstView.rect.width, Is.EqualTo(300).Within(0.01));
            Assert.That(firstView.position.y, Is.EqualTo(secondView.position.y).Within(0.01));

            gridRect.sizeDelta = new Vector2(639, 250);
            Canvas.ForceUpdateCanvases();
            Assert.That(grid.constraintCount, Is.EqualTo(1));
            Assert.That(firstView.rect.width, Is.EqualTo(615).Within(0.01));
            Assert.That(firstView.position.x, Is.EqualTo(secondView.position.x).Within(0.01));
            Assert.That(firstView.position.y, Is.GreaterThan(secondView.position.y));

            gridRect.sizeDelta = new Vector2(760, 250);
            Canvas.ForceUpdateCanvases();
            Assert.That(grid.constraintCount, Is.EqualTo(2));
            Assert.That(firstSlot.View, Is.SameAs(firstView));
            Assert.That(secondSlot.View, Is.SameAs(secondView));
        }

        private static RectTransform CreateRect(string name, Transform parent = null)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            return rect;
        }
    }
}
