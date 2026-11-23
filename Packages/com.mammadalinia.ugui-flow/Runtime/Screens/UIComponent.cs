using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UguiFlow
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    [AddComponentMenu("uGUI Flow/UI Component")]
    public class UIComponent : MonoBehaviour
    {
        [SerializeField] private bool initiallyVisible = true;

        private readonly List<UIComponent> children = new();
        private UIComponent parent;
        private UIComponent root;
        private CanvasGroup group;
        private CanvasGroup errorView;
        private bool initialized;
        private bool mounted;
        private bool disposing;
        private bool presented;
        private bool refreshing;
        private bool visibilityDirty;

        public UIComponent Parent => root != null || Application.IsPlaying(gameObject) ? parent : FindParent();

        public IReadOnlyList<UIComponent> Children => root != null || Application.IsPlaying(gameObject)
            ? children.AsReadOnly()
            : Array.AsReadOnly(Array.FindAll(GetComponentsInChildren<UIComponent>(true),
                component => component.FindParent() == this));

        public ScreenEvents Events { get; private set; } = ScreenEvents.Closed;
        public bool RequestedVisible { get; private set; }
        public bool IsVisible { get; private set; }
        public bool IsFaulted { get; private set; }

        internal bool IsRefreshing => refreshing;
        internal bool CanReceiveEvents => root != null && !root.disposing && initialized && !HasFaultedAncestor();
        internal bool CanChangeVisibility => CanReceiveEvents && root.mounted && !root.refreshing;

        public event Action Initialized;
        public event Action Shown;
        public event Action Hidden;
        public event Action Destroyed;

        protected virtual void OnInitialize() { }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void OnDispose() { }

        /// <summary>Changes local visibility while preserving the state of descendant components.</summary>
        public void SetVisible(bool visible)
        {
            if (root == null || root.disposing || RequestedVisible == visible)
                return;

            RequestedVisible = visible;
            root.RefreshVisibility();
        }

        /// <summary>Replaces the mounted component and its children with the error view for this instance.</summary>
        public void ShowError()
        {
            if (root == null || root.disposing || HasFaultedAncestor())
                return;

            IsFaulted = true;
            if (root.mounted)
                root.RefreshVisibility();
        }

        private bool HasFaultedAncestor()
        {
            for (UIComponent component = this; component != null; component = component.parent)
            {
                if (component.IsFaulted)
                    return true;
            }

            return false;
        }

        internal UIComponent FindParent() => transform.parent != null
            ? transform.parent.GetComponentInParent<UIComponent>(true)
            : null;

        internal void Prepare()
        {
            Events = new ScreenEvents();
            // Bind every node before invoking user code, so initialization can access siblings.
            foreach (UIComponent component in GetComponentsInChildren<UIComponent>(true))
                component.Bind(this, component == this ? null : component.FindParent());

            InitializeTree();
        }

        private void Bind(UIComponent screen, UIComponent owner)
        {
            root = screen;
            parent = owner;
            parent?.children.Add(this);
            Events = screen.Events;
            RequestedVisible = initiallyVisible;
            group = GetComponent<CanvasGroup>();
            group.ignoreParentGroups = false;
            ApplyCanvasGroup(false);
        }

        private void InitializeTree()
        {
            if (this == null || HasFaultedAncestor())
                return;

            initialized = true;
            Notify(OnInitialize);
            if (!HasFaultedAncestor())
                Notify(Initialized);
            foreach (UIComponent child in children)
                child.InitializeTree();
        }

        internal void PrepareErrorViews()
        {
            if (this == null)
                return;

            if (IsFaulted)
                ReplaceWithErrorView();
            else
            {
                foreach (UIComponent child in children)
                    child.PrepareErrorViews();
            }
        }

        internal void Mount(bool visible)
        {
            mounted = true;
            if (!IsFaulted)
                gameObject.SetActive(true);
            SetPresented(visible);
        }

        internal void SetPresented(bool visible)
        {
            presented = visible;
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            visibilityDirty = true;
            if (refreshing)
                return;

            refreshing = true;
            try
            {
                while (visibilityDirty)
                {
                    visibilityDirty = false;
                    UpdateVisibility(presented);
                }
            }
            finally
            {
                refreshing = false;
            }
        }

        private void UpdateVisibility(bool parentVisible)
        {
            if (this == null)
                return;

            bool requested = parentVisible && RequestedVisible;
            bool visible = requested && !IsFaulted && isActiveAndEnabled;
            bool changed = IsVisible != visible;
            if (changed)
            {
                IsVisible = visible;
                ApplyCanvasGroup(visible);
                if (visible)
                {
                    Notify(OnShow);
                    if (!HasFaultedAncestor())
                        Notify(Shown);
                }
                else
                {
                    ClearInputFocus();
                }
            }

            foreach (UIComponent child in children)
                child.UpdateVisibility(visible && RequestedVisible && !IsFaulted);

            if (changed && !visible)
            {
                Notify(OnHide);
                Notify(Hidden);
            }

            if (IsFaulted && root.mounted && !root.disposing && errorView == null &&
                (parent == null || !parent.HasFaultedAncestor()))
                ReplaceWithErrorView();

            if (errorView != null)
            {
                errorView.alpha = requested && enabled ? 1 : 0;
                errorView.blocksRaycasts = requested && enabled;
            }
        }

        private void ReplaceWithErrorView()
        {
            var source = (RectTransform)transform;
            ThemeDatabase theme = ThemeScope.ResolveForSlot(source.parent);
            ThemeView prefab = theme != null ? theme.ErrorViewPrefab : null;
            if (prefab == null)
            {
                gameObject.SetActive(false);
                return;
            }

            var replacement = new GameObject($"{name} (Error View)", typeof(RectTransform),
                typeof(CanvasGroup), typeof(LayoutElement));
            replacement.SetActive(false);
            errorView = replacement.GetComponent<CanvasGroup>();
            errorView.alpha = 0;
            errorView.interactable = false;
            errorView.blocksRaycasts = false;
            var rect = (RectTransform)replacement.transform;
            rect.SetParent(source.parent, false);
            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.pivot = source.pivot;
            rect.sizeDelta = source.sizeDelta;
            rect.anchoredPosition3D = source.anchoredPosition3D;
            rect.localRotation = source.localRotation;
            rect.localScale = source.localScale;
            rect.SetSiblingIndex(source.GetSiblingIndex());

            ThemeView visual = Instantiate(prefab, rect, false);
            visual.SourcePrefab = prefab;
            visual.Rect.anchorMin = Vector2.zero;
            visual.Rect.anchorMax = Vector2.one;
            visual.Rect.offsetMin = Vector2.zero;
            visual.Rect.offsetMax = Vector2.zero;
            visual.ApplyFont(theme.DefaultFont);

            var layout = replacement.GetComponent<LayoutElement>();
            if (TryGetComponent(out LayoutElement authored) && authored.enabled)
            {
                layout.ignoreLayout = authored.ignoreLayout;
                layout.minWidth = authored.minWidth;
                layout.minHeight = authored.minHeight;
                layout.preferredWidth = authored.preferredWidth;
                layout.preferredHeight = authored.preferredHeight;
                layout.flexibleWidth = authored.flexibleWidth;
                layout.flexibleHeight = authored.flexibleHeight;
                layout.layoutPriority = authored.layoutPriority;
            }
            else
            {
                layout.minWidth = LayoutUtility.GetMinWidth(source);
                layout.minHeight = LayoutUtility.GetMinHeight(source);
                layout.preferredWidth = Mathf.Max(source.rect.width, LayoutUtility.GetPreferredWidth(source));
                layout.preferredHeight = Mathf.Max(source.rect.height, LayoutUtility.GetPreferredHeight(source));
                layout.flexibleWidth = LayoutUtility.GetFlexibleWidth(source);
                layout.flexibleHeight = LayoutUtility.GetFlexibleHeight(source);
            }

            ClearInputFocus();
            replacement.SetActive(this == root || gameObject.activeSelf);
            gameObject.SetActive(false);
        }

        private void ApplyCanvasGroup(bool visible)
        {
            group.alpha = visible ? 1 : 0;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        private void ClearInputFocus()
        {
            EventSystem events = EventSystem.current;
            GameObject selected = events != null ? events.currentSelectedGameObject : null;
            if (selected != null && selected.transform.IsChildOf(transform))
                events.SetSelectedGameObject(null);
        }

        internal void DisposeScreen()
        {
            disposing = true;
            SetPresented(false);
            ScreenEvents events = Events;
            DisposeTree();
            events.Clear();
        }

        private void DisposeTree()
        {
            foreach (UIComponent child in children)
                child.DisposeTree();

            if (initialized)
            {
                initialized = false;
                Notify(OnDispose);
                Notify(Destroyed);
            }

            if (errorView != null)
            {
                errorView.gameObject.SetActive(false);
                Destroy(errorView.gameObject);
                errorView = null;
            }

            children.Clear();
            Events = ScreenEvents.Closed;
            parent = null;
            root = null;
        }

        internal virtual bool TryValidate(out string error)
        {
            error = transform is not RectTransform || !TryGetComponent<CanvasGroup>(out _)
                ? $"UI Component '{name}' needs a RectTransform and CanvasGroup."
                : null;
            return error == null;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate() => ValidationQueue.Request(this);
#endif

        private void Notify(Action callbacks)
        {
            if (callbacks == null)
                return;

            foreach (Action callback in callbacks.GetInvocationList())
            {
                try
                {
                    callback();
                }
                catch (Exception)
                {
                    ShowError();
                }
            }
        }
    }
}
