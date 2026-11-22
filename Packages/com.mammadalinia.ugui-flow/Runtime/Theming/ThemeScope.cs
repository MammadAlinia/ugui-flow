using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UguiFlow
{
    [DisallowMultipleComponent]
    [AddComponentMenu("uGUI Flow/Theme Scope")]
    public sealed class ThemeScope : MonoBehaviour
    {
        [SerializeField] private ThemeDatabase initialTheme;

        public ThemeDatabase CurrentTheme { get; private set; }
        public event Action<ThemeView, ThemeView> ViewReplaced;

        private bool applying;

        private void Start()
        {
            if (CurrentTheme == null && initialTheme != null && !TrySetTheme(initialTheme, out string error))
                Debug.LogError(error, this);
        }

        public bool TrySetTheme(ThemeDatabase theme, out string error)
        {
            if (theme == null)
            {
                error = "Select a theme database.";
                return false;
            }

            return TryApply(theme, transform, true, out error);
        }

        /// <summary>Applies this scope's theme to newly mounted UI without refreshing its siblings.</summary>
        public bool TryApplyCurrentTheme(RectTransform root, out string error)
        {
            if (root == null || root.GetComponentInParent<ThemeScope>(true) != this)
            {
                error = "The UI root must belong to this Theme Scope.";
                return false;
            }

            return TryApply(CurrentTheme != null ? CurrentTheme : initialTheme, root, false, out error);
        }

        private bool TryApply(ThemeDatabase theme, Transform root, bool setCurrentTheme, out string error)
        {
            if (applying || !Application.IsPlaying(gameObject))
            {
                error = "Apply themes during Play Mode and wait for the current theme change to finish.";
                return false;
            }

            error = null;
            if (theme == null)
                return true;

            if (!theme.Validate(out error) || !TryGetReplacements(theme, root, out var replacements, out error))
                return false;

            applying = true;
            try
            {
                Apply(theme, root, setCurrentTheme, replacements);
                return true;
            }
            finally
            {
                applying = false;
            }
        }

        private bool TryGetReplacements(ThemeDatabase theme, Transform root,
            out List<(ThemeView view, ThemeView prefab)> replacements, out string error)
        {
            replacements = new List<(ThemeView, ThemeView)>();
            foreach (ThemeView view in root.GetComponentsInChildren<ThemeView>(true))
            {
                if (view.GetComponentInParent<ThemeScope>(true) != this)
                    continue;

                if (view.transform == transform || view.transform.parent.GetComponentInParent<ThemeView>(true) != null ||
                    !theme.TryGetPrefab(view, out ThemeView prefab) || !view.CanReplaceWith(prefab) ||
                    prefab.GetComponentsInChildren<ThemeView>(true).Length != 1)
                {
                    string keyName = view.Key != null ? view.Key.name : "unassigned";
                    error = $"Theme '{theme.name}' needs a compatible prefab for leaf view '{view.name}' (key '{keyName}').";
                    return false;
                }

                replacements.Add((view, prefab));
            }

            error = null;
            return true;
        }

        private void Apply(ThemeDatabase theme, Transform root, bool setCurrentTheme,
            List<(ThemeView view, ThemeView prefab)> replacements)
        {
            Canvas.ForceUpdateCanvases();
            var scrollPositions = new List<(ScrollRect scroll, Vector2 position)>();
            foreach (ScrollRect scroll in root.GetComponentsInChildren<ScrollRect>(true))
            {
                if (scroll.isActiveAndEnabled && scroll.content != null && scroll.GetComponentInParent<ThemeScope>(true) == this)
                    scrollPositions.Add((scroll, scroll.normalizedPosition));
            }

            EventSystem events = EventSystem.current;
            GameObject selected = events != null ? events.currentSelectedGameObject : null;
            GameObject nextSelection = null;
            var swapped = new List<(ThemeView before, ThemeView after)>();
            foreach ((ThemeView view, ThemeView prefab) in replacements)
            {
                if (view.SourcePrefab == prefab)
                {
                    view.ApplyFont(theme.DefaultFont);
                    continue;
                }

                bool wasSelected = selected == view.gameObject;
                if (wasSelected)
                    events.SetSelectedGameObject(null);

                ThemeView next = view.ReplaceWith(prefab, theme.DefaultFont);
                if (wasSelected && next.TryGetComponent(out Selectable selectable) &&
                    selectable.isActiveAndEnabled && selectable.IsInteractable())
                    nextSelection = next.gameObject;

                swapped.Add((view, next));
            }

            if (setCurrentTheme)
                CurrentTheme = theme;
            Canvas.ForceUpdateCanvases();
            foreach ((ScrollRect scroll, Vector2 position) in scrollPositions)
            {
                scroll.StopMovement();
                scroll.normalizedPosition = position;
            }

            if (nextSelection != null)
                events.SetSelectedGameObject(nextSelection);

            foreach ((ThemeView before, ThemeView after) in swapped)
                ViewReplaced?.Invoke(before, after);
        }
    }
}
