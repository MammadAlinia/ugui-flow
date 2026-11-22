using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UguiFlow
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GridLayoutGroup))]
    [AddComponentMenu("uGUI Flow/Responsive Grid")]
    public sealed class ResponsiveGrid : UIBehaviour, ILayoutSelfController
    {
        [SerializeField, Min(0)] private float wideBreakpoint = 640;
        [SerializeField, Min(1)] private int wideColumns = 2;

        private GridLayoutGroup grid;

        protected override void OnEnable()
        {
            base.OnEnable();
            grid = GetComponent<GridLayoutGroup>();
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (isActiveAndEnabled)
                LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
        }
#endif

        void ILayoutController.SetLayoutHorizontal()
        {
            if (!grid.isActiveAndEnabled)
                return;

            float width = ((RectTransform)transform).rect.width;
            int columns = width >= wideBreakpoint ? Mathf.Max(1, wideColumns) : 1;
            float availableWidth = width - grid.padding.horizontal - grid.spacing.x * (columns - 1);

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = new Vector2(Mathf.Max(0, availableWidth / columns), grid.cellSize.y);
        }

        void ILayoutController.SetLayoutVertical() { }
    }
}
