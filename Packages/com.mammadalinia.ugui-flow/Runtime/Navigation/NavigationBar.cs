using System;
using UnityEngine;
using UnityEngine.UI;

namespace UguiFlow
{
    [AddComponentMenu("uGUI Flow/Navigation Bar")]
    public sealed class NavigationBar : UIComponent
    {
        [SerializeField] private PageNavigator navigator;
        [SerializeField] private RectTransform indicator;

        private IDisposable selection;
        private NavigationTab selectedTab;
        private RectTransform indicatorParent;
        private bool snapIndicator;

        protected override void OnInitialize()
        {
            if (indicator != null && indicator != transform && indicator.IsChildOf(transform))
            {
                indicatorParent = indicator.parent as RectTransform;
                indicator.gameObject.SetActive(false);
            }

            selection = Events.Subscribe<PageChanged>(this, OnPageChanged);
        }

        protected override void OnShow()
        {
            snapIndicator = true;
            UpdateSelectedTab(navigator != null && navigator.Events == Events ? navigator.SelectedPage : null);
        }

        protected override void OnDispose() => selection?.Dispose();

        internal void SelectPage(UIComponent page)
        {
            if (IsVisible && navigator != null && navigator.Events == Events)
                navigator.TrySelectPage(page);
        }

        private void OnPageChanged(PageChanged change)
        {
            if (change.Navigator == navigator)
                UpdateSelectedTab(change.Current);
        }

        private void UpdateSelectedTab(UIComponent page)
        {
            selectedTab = null;
            if (page == null)
                return;

            foreach (UIComponent child in Children)
            {
                if (child is NavigationTab tab && tab.Page == page)
                {
                    selectedTab = tab;
                    break;
                }
            }
        }

        private void LateUpdate()
        {
            if (!IsVisible || indicatorParent == null || indicator == null)
                return;

            bool hasVisibleTab = selectedTab != null && selectedTab.IsVisible;
            if (!hasVisibleTab)
            {
                indicator.gameObject.SetActive(false);
                return;
            }

            var tabRect = (RectTransform)selectedTab.transform;
            bool snap = snapIndicator || !indicator.gameObject.activeSelf;
            if (snap && tabRect.parent is RectTransform tabRow)
                LayoutRebuilder.ForceRebuildLayoutImmediate(tabRow);

            Rect bounds = indicatorParent.rect;
            if (bounds.width <= 0 || tabRect.rect.width <= 0)
                return;

            float left = GetAnchorX(tabRect, tabRect.rect.xMin, bounds);
            float right = GetAnchorX(tabRect, tabRect.rect.xMax, bounds);
            float step = 3.5f * Time.unscaledDeltaTime;
            Vector2 min = indicator.anchorMin;
            Vector2 max = indicator.anchorMax;
            min.x = snap ? left : Mathf.MoveTowards(min.x, left, step);
            max.x = snap ? right : Mathf.MoveTowards(max.x, right, step);
            indicator.anchorMin = min;
            indicator.anchorMax = max;
            indicator.gameObject.SetActive(true);
            snapIndicator = false;
        }

        private float GetAnchorX(RectTransform tab, float edge, Rect bounds)
        {
            Vector3 world = tab.TransformPoint(new Vector3(edge, tab.rect.center.y));
            float localX = indicatorParent.InverseTransformPoint(world).x;
            return (localX - bounds.xMin) / bounds.width;
        }
    }
}
