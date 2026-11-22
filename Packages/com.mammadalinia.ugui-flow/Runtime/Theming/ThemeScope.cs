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
            if (theme == null || applying || !Application.IsPlaying(gameObject))
            {
                error = "Set a valid theme during Play Mode and wait for the current theme change to finish.";
                return false;
            }

            if (!theme.Validate(out error) || !TryGetReplacements(theme, out var replacements, out error))
                return false;

            applying = true;
            try
            {
                Apply(theme, replacements);
                return true;
            }
            finally
            {
                applying = false;
            }
        }

        private bool TryGetReplacements(ThemeDatabase theme, out List<(ThemeView view, ThemeView prefab)> replacements, out string error)
        {
            replacements = new List<(ThemeView, ThemeView)>();
            foreach (ThemeView view in GetComponentsInChildren<ThemeView>(true))
            {
                if (view.GetComponentInParent<ThemeScope>(true) != this)
                    continue;

                if (view.transform == transform || view.transform.parent.GetComponentInParent<ThemeView>(true) != null ||
                    !theme.TryGetPrefab(view.Role, out ThemeView prefab) || !view.CanReplaceWith(prefab) ||
                    prefab.GetComponentsInChildren<ThemeView>(true).Length != 1)
                {
                    error = $"Theme '{theme.name}' needs a compatible prefab for leaf view '{view.name}' (role '{view.Role}').";
                    return false;
                }

                replacements.Add((view, prefab));
            }

            error = null;
            return true;
        }

        private void Apply(ThemeDatabase theme, List<(ThemeView view, ThemeView prefab)> replacements)
        {
            Canvas.ForceUpdateCanvases();
            var scrollPositions = new List<(ScrollRect scroll, Vector2 position)>();
            foreach (ScrollRect scroll in GetComponentsInChildren<ScrollRect>(true))
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
