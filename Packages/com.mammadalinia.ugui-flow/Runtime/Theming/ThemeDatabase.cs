using System;
using TMPro;
using UnityEngine;

namespace UguiFlow
{
    [CreateAssetMenu(menuName = "uGUI Flow/Theme Database")]
    public sealed class ThemeDatabase : ScriptableObject
    {
        [SerializeField] private TMP_FontAsset defaultFont;
        [SerializeField] private ThemeView panelPrefab;
        [SerializeField] private ThemeView labelPrefab;
        [SerializeField] private ThemeView buttonPrefab;
        [SerializeField] private ThemeView resetButtonPrefab;
        [SerializeField] private ThemeView primaryButtonPrefab;
        [SerializeField] private ThemeView secondaryButtonPrefab;
        [SerializeField] private ThemeView tertiaryButtonPrefab;
        [SerializeField] private ThemeView iconButtonPrefab;
        [SerializeField] private ThemeView togglePrefab;
        [SerializeField] private ThemeView sliderPrefab;
        [SerializeField] private ThemeView badgePrefab;
        [SerializeField] private ThemeView dividerPrefab;
        [SerializeField] private ThemeView progressBarPrefab;
        [SerializeField] private ThemeView cardPrefab;
        [SerializeField] private ThemeView errorViewPrefab;

        private static readonly ThemeViewKind[] ViewKinds = (ThemeViewKind[])Enum.GetValues(typeof(ThemeViewKind));

        public TMP_FontAsset DefaultFont => defaultFont;
        public ThemeView ErrorViewPrefab => errorViewPrefab;

        private static ThemeDatabase defaultTheme;
        internal static ThemeDatabase Default => defaultTheme != null
            ? defaultTheme
            : (defaultTheme = Resources.Load<ThemeDatabase>("UguiFlow/Light"));

        public bool TryGetPrefab(ThemeView view, out ThemeView prefab)
        {
            ThemeViewKey key = view != null ? view.Key : null;
            ThemeView candidate = key != null ? GetPrefab(key.Kind) : null;
            if (candidate != null && candidate.Key == key)
            {
                prefab = candidate;
                return true;
            }

            prefab = null;
            return false;
        }

        internal bool Validate(out string error)
        {
            foreach (ThemeViewKind kind in ViewKinds)
            {
                if (kind == ThemeViewKind.Unassigned)
                    continue;

                ThemeView prefab = GetPrefab(kind);
                if (prefab == null)
                {
                    error = $"Theme '{name}' needs a prefab in its {kind} slot.";
                    return false;
                }

                if (prefab.gameObject.scene.IsValid() || prefab.transform.parent != null ||
                    prefab.Key == null || prefab.Key.Kind != kind)
                {
                    error = $"Theme '{name}' needs a root Theme View prefab with a {kind} key in its {kind} slot.";
                    return false;
                }

                if (!prefab.TryValidate(out string viewError))
                {
                    error = $"Theme '{name}', {kind} slot. {viewError}";
                    return false;
                }

                if (prefab.GetComponentsInChildren<ThemeView>(true).Length != 1 ||
                    prefab.GetComponentInChildren<UIComponent>(true) != null)
                {
                    error = $"Theme '{name}' needs a leaf Theme View without UI Components in its {kind} slot.";
                    return false;
                }
            }

            error = null;
            return true;
        }

#if UNITY_EDITOR
        private void OnValidate() => ValidationQueue.Request(this);
#endif

        private ThemeView GetPrefab(ThemeViewKind kind) => kind switch
        {
            ThemeViewKind.Panel => panelPrefab,
            ThemeViewKind.Label => labelPrefab,
            ThemeViewKind.Button => buttonPrefab,
            ThemeViewKind.ResetButton => resetButtonPrefab,
            ThemeViewKind.PrimaryButton => primaryButtonPrefab,
            ThemeViewKind.SecondaryButton => secondaryButtonPrefab,
            ThemeViewKind.TertiaryButton => tertiaryButtonPrefab,
            ThemeViewKind.IconButton => iconButtonPrefab,
            ThemeViewKind.Toggle => togglePrefab,
            ThemeViewKind.Slider => sliderPrefab,
            ThemeViewKind.Badge => badgePrefab,
            ThemeViewKind.Divider => dividerPrefab,
            ThemeViewKind.ProgressBar => progressBarPrefab,
            ThemeViewKind.Card => cardPrefab,
            ThemeViewKind.ErrorView => errorViewPrefab,
            _ => null
        };
    }
}
