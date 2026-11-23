using UnityEngine;

namespace UguiFlow
{
    internal enum ThemeViewKind
    {
        Unassigned = 0,
        Panel = 1,
        Label = 2,
        Button = 3,
        ResetButton = 4,
        PrimaryButton = 5,
        SecondaryButton = 6,
        TertiaryButton = 7,
        IconButton = 8,
        Toggle = 9,
        Slider = 10,
        Badge = 11,
        Divider = 12,
        ProgressBar = 13,
        Card = 14,
        ErrorView = 15
    }

    [CreateAssetMenu(menuName = "uGUI Flow/Theme View Key")]
    public sealed class ThemeViewKey : ScriptableObject
    {
        [SerializeField] private ThemeViewKind kind;

        internal ThemeViewKind Kind => kind;
    }
}
