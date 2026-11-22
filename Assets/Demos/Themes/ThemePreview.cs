using UnityEngine;
using UnityEngine.UI;

namespace UguiFlow.Samples
{
    public sealed class ThemePreview : MonoBehaviour
    {
        [SerializeField] private ThemeScope scope;
        [SerializeField] private ThemeDatabase light;
        [SerializeField] private ThemeDatabase dark;
        [SerializeField] private ThemeDatabase customButton;
        [SerializeField] private ThemeDatabase alternateFont;

        public void ShowLight() => Apply(light);
        public void ShowDark() => Apply(dark);
        public void ShowCustomButton() => Apply(customButton);
        public void ShowAlternateFont() => Apply(alternateFont);

        public void ResetSettings()
        {
            scope.GetComponentInChildren<Toggle>().isOn = true;
            scope.GetComponentInChildren<Slider>().value = .65f;
        }

        private void Apply(ThemeDatabase theme)
        {
            if (!scope.TrySetTheme(theme, out string error))
                Debug.LogError(error, this);
        }
    }
}
