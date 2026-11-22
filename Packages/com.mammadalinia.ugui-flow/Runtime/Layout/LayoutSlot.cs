using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UguiFlow
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("uGUI Flow/Layout Slot")]
    public sealed class LayoutSlot : UIBehaviour, ILayoutElement, ILayoutGroup
    {
        [SerializeField] private string slotId;
        [SerializeField] private RectTransform viewPrefab;

        public string SlotId => slotId;
        public RectTransform View => view;

        private RectTransform view;

        public void SetViewPrefab(RectTransform prefab)
        {
            if (viewPrefab == prefab && (prefab == null || view != null))
                return;

            viewPrefab = prefab;
            if (Application.IsPlaying(gameObject))
                ReplaceView();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (Application.IsPlaying(gameObject) && view == null && viewPrefab != null)
                ReplaceView();

            MarkLayoutDirty();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            MarkLayoutDirty();
        }

        private void OnTransformChildrenChanged() => MarkLayoutDirty();

        private void ReplaceView()
        {
            if (view != null)
            {
                view.gameObject.SetActive(false);
                Destroy(view.gameObject);
            }

            view = viewPrefab != null ? Instantiate(viewPrefab, transform, false) : null;
            if (view != null)
            {
                view.anchorMin = Vector2.zero;
                view.anchorMax = Vector2.one;
                view.offsetMin = Vector2.zero;
                view.offsetMax = Vector2.zero;
            }

            MarkLayoutDirty();
        }

        private void MarkLayoutDirty() => LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);

        private RectTransform VisibleView => view != null && view.gameObject.activeInHierarchy ? view : null;

        public float minWidth => LayoutUtility.GetMinWidth(VisibleView);
        public float preferredWidth => LayoutUtility.GetPreferredWidth(VisibleView);
        public float flexibleWidth => LayoutUtility.GetFlexibleWidth(VisibleView);
        public float minHeight => LayoutUtility.GetMinHeight(VisibleView);
        public float preferredHeight => LayoutUtility.GetPreferredHeight(VisibleView);
        public float flexibleHeight => LayoutUtility.GetFlexibleHeight(VisibleView);
        public int layoutPriority => 0;

        // The view fills the slot through its anchors. Implementing ILayoutGroup
        // lets uGUI traverse the slot and rebuild its parent when the view resizes.
        void ILayoutController.SetLayoutHorizontal() { }
        void ILayoutController.SetLayoutVertical() { }
        void ILayoutElement.CalculateLayoutInputHorizontal() { }
        void ILayoutElement.CalculateLayoutInputVertical() { }
    }
}
