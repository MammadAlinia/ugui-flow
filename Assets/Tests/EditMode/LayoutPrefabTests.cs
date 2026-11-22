using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UguiFlow.Tests
{
    public sealed class LayoutPrefabTests
    {
        private const string PrefabDirectory = "Packages/com.mammadalinia.ugui-flow/Runtime/Layout/Prefabs";

        [TestCase("Column", typeof(VerticalLayoutGroup))]
        [TestCase("Row", typeof(HorizontalLayoutGroup))]
        [TestCase("Grid", typeof(GridLayoutGroup))]
        public void LayoutPrefabStretchesAndProvidesSizing(string name, Type groupType)
        {
            GameObject prefab = LoadPrefab(name);

            AssertFullStretch((RectTransform)prefab.transform);
            Assert.That(prefab.GetComponent(groupType), Is.Not.Null);
            Assert.That(prefab.GetComponents<LayoutElement>(), Has.Length.EqualTo(1));
            Assert.That(prefab.GetComponent<LayoutElement>().flexibleWidth, Is.EqualTo(1));
            Assert.That(prefab.GetComponent<LayoutElement>().flexibleHeight, Is.EqualTo(1));
        }

        [Test]
        public void ContainerStretchesWithoutControllingItsChildren()
        {
            GameObject prefab = LoadPrefab("Container");

            AssertFullStretch((RectTransform)prefab.transform);
            Assert.That(prefab.GetComponents<Component>(), Has.Length.EqualTo(1));
            Assert.That(prefab.transform.childCount, Is.Zero);

            var parent = new GameObject("Parent", typeof(RectTransform));
            try
            {
                var parentRect = (RectTransform)parent.transform;
                parentRect.sizeDelta = new Vector2(300, 200);
                var container = (RectTransform)UnityEngine.Object.Instantiate(prefab, parentRect, false).transform;
                var child = (RectTransform)new GameObject("Child", typeof(RectTransform)).transform;
                child.SetParent(container, false);
                child.anchoredPosition = new Vector2(30, -25);
                child.sizeDelta = new Vector2(60, 40);

                parentRect.sizeDelta = new Vector2(440, 280);
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);

                Assert.That(container.rect.size, Is.EqualTo(parentRect.rect.size));
                Assert.That(child.anchoredPosition, Is.EqualTo(new Vector2(30, -25)));
                Assert.That(child.rect.size, Is.EqualTo(new Vector2(60, 40)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        private static GameObject LoadPrefab(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDirectory}/{name}.prefab");
            Assert.That(prefab, Is.Not.Null, name);
            return prefab;
        }

        private static void AssertFullStretch(RectTransform rect)
        {
            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rect.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.offsetMax, Is.EqualTo(Vector2.zero));
        }
    }
}
