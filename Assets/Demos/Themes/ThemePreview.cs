using System.Linq;
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
        [SerializeField] private ThemeViewKey volumeKey;
        [SerializeField] private ThemeViewKey downloadStatusKey;
        [SerializeField] private ThemeViewKey downloadProgressKey;

        public void ShowLight() => Apply(light);
        public void ShowDark() => Apply(dark);
        public void ShowCustomButton() => Apply(customButton);
        public void ShowAlternateFont() => Apply(alternateFont);

        public void ResetSettings()
        {
            scope.GetComponentInChildren<Toggle>().isOn = true;
            FindView(volumeKey).GetComponent<Slider>().value = .65f;
        }

        public void StartDownload() => SetDownload("Downloading", .25f);
        public void PauseDownload() => FindView(downloadStatusKey).ContentLabel.text = "Paused";
        public void ClearDownload() => SetDownload("Idle", 0);
        public void CompleteDownload() => SetDownload("Complete", 1);

        private void SetDownload(string status, float progress)
        {
            FindView(downloadStatusKey).ContentLabel.text = status;
            FindView(downloadProgressKey).GetComponent<Slider>().value = progress;
        }

        private ThemeView FindView(ThemeViewKey key) =>
            scope.GetComponentsInChildren<ThemeView>().First(view => view.Key == key);

        private void Apply(ThemeDatabase theme)
        {
            if (!scope.TrySetTheme(theme, out string error))
                Debug.LogError(error, this);
        }
    }
}
