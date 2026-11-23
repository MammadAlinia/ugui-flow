using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UguiFlow.Tests
{
    public sealed class UIComponentHierarchyTests
    {
        [Test]
        public void ReparentAndUndoUpdateComponentOwnership()
        {
            var first = new GameObject("First", typeof(RectTransform)).AddComponent<UIComponent>();
            var second = new GameObject("Second", typeof(RectTransform)).AddComponent<UIComponent>();
            var wrapper = new GameObject("Layout wrapper", typeof(RectTransform));
            var child = new GameObject("Child", typeof(RectTransform)).AddComponent<UIComponent>();
            wrapper.transform.SetParent(first.transform, false);
            child.transform.SetParent(wrapper.transform, false);

            try
            {
                Assert.That(child.Parent, Is.SameAs(first));
                Assert.That(first.Children, Is.EqualTo(new[] { child }));
                Undo.IncrementCurrentGroup();
                Undo.SetTransformParent(wrapper.transform, second.transform, "Move wrapper");
                Assert.That(child.Parent, Is.SameAs(second));
                Assert.That(first.Children, Is.Empty);
                Assert.That(second.Children, Is.EqualTo(new[] { child }));

                Undo.PerformUndo();
                Assert.That(child.Parent, Is.SameAs(first));
                Assert.That(second.Children, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(first.gameObject);
                Object.DestroyImmediate(second.gameObject);
            }
        }
    }
}
