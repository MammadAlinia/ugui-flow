using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UguiFlow
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GridLayoutGroup))]
    [AddComponentMenu("uGUI Flow/Responsive Grid")]
    public sealed class ResponsiveGrid : UIBehaviour, ILayoutSelfController, ILayoutElement
    {
        [SerializeField, Min(0)] private float wideBreakpoint = 640;
        [SerializeField, Min(1)] private int wideColumns = 2;

        private GridLayoutGroup grid;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!TryValidateAndRefresh(out string error))
                Debug.LogError(error, this);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            ValidationQueue.Request(this);
        }
#endif

        internal bool TryValidateAndRefresh(out string error)
        {
            if (transform is not RectTransform rect || !TryGetComponent(out grid))
            {
                error = $"Responsive Grid '{name}' needs a RectTransform and Grid Layout Group.";
                return false;
            }

            if (isActiveAndEnabled)
                LayoutRebuilder.MarkLayoutForRebuild(rect);
            error = null;
            return true;
        }

        void ILayoutController.SetLayoutHorizontal()
        {
            if (grid == null || !grid.isActiveAndEnabled)
                return;

            float width = ((RectTransform)transform).rect.width;
            int columns = width >= wideBreakpoint ? Mathf.Max(1, wideColumns) : 1;
            float availableWidth = width - grid.padding.horizontal - grid.spacing.x * (columns - 1);

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = new Vector2(Mathf.Max(0, availableWidth / columns), grid.cellSize.y);
        }

        void ILayoutController.SetLayoutVertical() { }

        // The parent supplies the width. Keep the grid's current cell widths
        // from becoming a minimum width that prevents the grid from shrinking.
        float ILayoutElement.minWidth => 0;
        float ILayoutElement.preferredWidth => 0;
        float ILayoutElement.flexibleWidth => -1;
        float ILayoutElement.minHeight => -1;
        float ILayoutElement.preferredHeight => -1;
        float ILayoutElement.flexibleHeight => -1;
        int ILayoutElement.layoutPriority => 1;
        void ILayoutElement.CalculateLayoutInputHorizontal() { }
        void ILayoutElement.CalculateLayoutInputVertical() { }
    }
}
