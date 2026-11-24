using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UguiFlow.Tests
{
    public sealed class DialogPrefabTests
    {
        [TestCase("Info")]
        [TestCase("Error")]
        public void BundledPresetInheritsDialogScreenAndHasItsOwnHeader(string preset)
        {
            DialogScreen screen = Resources.Load<DialogScreen>($"UguiFlow/Dialogs/{preset}");
            Assert.That(screen, Is.Not.Null);
            Assert.That(PrefabUtility.GetPrefabAssetType(screen), Is.EqualTo(PrefabAssetType.Variant));
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(screen).name, Is.EqualTo("Dialog Screen"));

            var fields = new SerializedObject(screen);
            var backdrop = (Image)fields.FindProperty("backdrop").objectReferenceValue;
            Assert.That(backdrop.raycastTarget && backdrop.color.a > 0, Is.True);
            Assert.That(screen.GetComponentInChildren<RectMask2D>(), Is.Not.Null);
            Assert.That(screen.GetComponentInChildren<Image>().gameObject, Is.SameAs(screen.gameObject));
            Assert.That(System.Array.Exists(screen.GetComponentsInChildren<Image>(),
                image => image.sprite != null && image.sprite.name == preset), Is.True);

            foreach (string part in new[] { "panel", "scroll", "titleView", "descriptionView", "actions",
                         "labelButtonPrefab", "iconButtonPrefab", "acceptIcon", "cancelIcon", "retryIcon" })
                Assert.That(fields.FindProperty(part).objectReferenceValue, Is.Not.Null, part);
        }
    }
}
