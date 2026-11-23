using UnityEngine;

namespace UguiFlow
{
    [AddComponentMenu("uGUI Flow/Navigation Tab")]
    public sealed class NavigationTab : UIComponent
    {
        [SerializeField] private UnityEngine.UI.Button button;
        [SerializeField] private UIComponent page;

        public UIComponent Page => page;

        private NavigationBar bar;
        private UnityEngine.UI.Button.ButtonClickedEvent click;

        protected override void OnInitialize()
        {
            bar = Parent as NavigationBar;
            // ThemeView transfers the event to each replacement button.
            click = button != null ? button.onClick : null;
        }

        protected override void OnShow() => click?.AddListener(SelectPage);

        protected override void OnHide() => click?.RemoveListener(SelectPage);

        private void SelectPage()
        {
            if (IsVisible && bar != null)
                bar.SelectPage(page);
        }
    }
}
