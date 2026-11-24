using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UguiFlow
{
    [AddComponentMenu("uGUI Flow/Dialog Screen")]
    public sealed class DialogScreen : UIComponent
    {
        [SerializeField] private Image backdrop;
        [SerializeField] private RectTransform panel;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private ThemeView titleView;
        [SerializeField] private ThemeView descriptionView;
        [SerializeField] private RectTransform actions;
        [SerializeField] private ThemeView labelButtonPrefab;
        [SerializeField] private ThemeView iconButtonPrefab;
        [SerializeField] private Sprite acceptIcon;
        [SerializeField] private Sprite cancelIcon;
        [SerializeField] private Sprite retryIcon;
        [SerializeField, Min(1f)] private float maximumWidth = 480f;
        [SerializeField, Min(0f)] private float edgeMargin = 20f;

        private readonly List<Button> buttons = new();
        private ThemeScope themeScope;
        private ThemeDatabase layoutTheme;
        private Vector2 layoutSize;
        private bool layoutDirty = true;

        internal GameObject FirstAction => buttons[0].gameObject;

        internal void Configure(string title, string description, Dialog.Choice[] choices, Action<DialogScreen, Action> select)
        {
            titleView.ContentLabel.text = title;
            descriptionView.ContentLabel.text = description;
            descriptionView.gameObject.SetActive(description.Length > 0);

            foreach (Dialog.Choice choice in choices)
            {
                ThemeView view = Instantiate(choice.Icon.HasValue ? iconButtonPrefab : labelButtonPrefab, actions, false);
                var button = view.GetComponent<Button>();
                if (choice.Icon.HasValue)
                {
                    view.ContentIcon.sprite = choice.Icon.Value switch
                    {
                        Icons.Accept => acceptIcon,
                        Icons.Cancel => cancelIcon,
                        Icons.Retry => retryIcon,
                        _ => throw new ArgumentOutOfRangeException(nameof(choices))
                    };
                }
                else
                {
                    view.ContentLabel.text = choice.Label;
                }
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(() => select(this, choice.OnClick));
                buttons.Add(button);
            }

            LinkActions();
        }

        protected override void OnInitialize()
        {
            themeScope = GetComponentInParent<ThemeScope>(true);
            if (themeScope != null)
                themeScope.ViewReplaced += OnViewReplaced;
        }

        protected override void OnDispose()
        {
            if (themeScope != null)
                themeScope.ViewReplaced -= OnViewReplaced;
        }

        internal bool ContainsAction(GameObject selection)
        {
            return selection != null && selection.TryGetComponent(out Button button) && buttons.Contains(button);
        }

        internal void UpdateLayout()
        {
            Vector2 size = ((RectTransform)transform).rect.size;
            ThemeDatabase theme = themeScope != null ? themeScope.CurrentTheme : null;
            if (!layoutDirty && size == layoutSize && theme == layoutTheme)
                return;

            bool firstLayout = layoutSize == Vector2.zero;
            layoutDirty = false;
            layoutSize = size;
            layoutTheme = theme;
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                Mathf.Clamp(size.x - 2f * edgeMargin, 1f, maximumWidth));
            panel.ForceUpdateRectTransforms();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            if (FitActionLabels())
                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Clamp(scroll.content.rect.height, 1f, Mathf.Max(1f, size.y - 2f * edgeMargin)));
            if (firstLayout)
                scroll.verticalNormalizedPosition = 1f;
        }

        private bool FitActionLabels()
        {
            if (!labelButtonPrefab.TryGetComponent(out LayoutElement authored))
                return false;

            bool changed = false;
            foreach (Button button in buttons)
            {
                var label = button.GetComponent<ThemeView>().ContentLabel;
                if (label == null || !button.TryGetComponent(out LayoutElement layout))
                    continue;

                Rect labelRect = label.rectTransform.rect;
                float textHeight = label.GetPreferredValues(label.text, Mathf.Max(1f, labelRect.width), 0f).y;
                float padding = ((RectTransform)button.transform).rect.height - labelRect.height;
                float preferred = Mathf.Max(authored.preferredHeight, Mathf.Ceil(textHeight + padding + 8f));
                if (!Mathf.Approximately(layout.preferredHeight, preferred))
                {
                    layout.preferredHeight = preferred;
                    changed = true;
                }
            }
            return changed;
        }

        internal void RevealAction(GameObject selection)
        {
            if (selection == null)
                return;

            RectTransform viewport = scroll.viewport;
            Rect bounds = viewport.rect;
            Bounds action = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, selection.transform);
            float offset = action.max.y > bounds.yMax ? bounds.yMax - action.max.y
                : Mathf.Max(0f, bounds.yMin - action.min.y);
            float overflow = scroll.content.rect.height - bounds.height;
            if (!Mathf.Approximately(offset, 0f) && overflow > 0f)
            {
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition - offset / overflow);
            }
        }

        private void LinkActions()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                buttons[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = i > 0 ? buttons[i - 1] : null,
                    selectOnDown = i + 1 < buttons.Count ? buttons[i + 1] : null
                };
            }
        }

        private void OnViewReplaced(ThemeView before, ThemeView after)
        {
            if (!after.transform.IsChildOf(transform))
                return;

            layoutDirty = true;
            if (before.TryGetComponent(out Button oldButton))
            {
                int index = buttons.IndexOf(oldButton);
                if (index >= 0)
                {
                    buttons[index] = after.GetComponent<Button>();
                    LinkActions();
                }
            }
        }

        internal override bool TryValidate(out string error)
        {
            if (!base.TryValidate(out error))
                return false;
            if (backdrop == null || backdrop.transform != transform || !backdrop.raycastTarget ||
                !IsChild(panel) || !IsChild(scroll) || !IsChild(actions) ||
                scroll.viewport == null || scroll.content == null ||
                !scroll.transform.IsChildOf(panel) || !scroll.viewport.IsChildOf(scroll.transform) ||
                !scroll.content.IsChildOf(scroll.viewport) || !actions.IsChildOf(scroll.content) ||
                titleView == null || titleView.ContentLabel == null || !titleView.transform.IsChildOf(scroll.content) ||
                descriptionView == null || descriptionView.ContentLabel == null ||
                !descriptionView.transform.IsChildOf(scroll.content))
            {
                error = $"Dialog Screen '{name}' needs a raycast backdrop, panel, scroll content, title, description, and action container.";
                return false;
            }
            if (!IsButtonPrefab(labelButtonPrefab) || labelButtonPrefab.ContentLabel == null ||
                !IsButtonPrefab(iconButtonPrefab) || iconButtonPrefab.ContentIcon == null ||
                acceptIcon == null || cancelIcon == null || retryIcon == null)
            {
                error = $"Dialog Screen '{name}' needs button prefabs with the corresponding label or icon and a sprite for every action icon.";
                return false;
            }

            return true;
        }

        private bool IsChild(Component component) => component != null && component.transform.IsChildOf(transform);

        private static bool IsButtonPrefab(ThemeView view) =>
            view != null && !view.gameObject.scene.IsValid() && view.transform.parent == null &&
            view.gameObject.activeSelf && view.TryGetComponent<Button>(out _) && view.TryValidate(out _);
    }
}
