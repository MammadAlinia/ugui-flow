using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UguiFlow
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("uGUI Flow/Theme View")]
    public sealed class ThemeView : MonoBehaviour
    {
        [SerializeField] private ThemeViewKey key;
        [SerializeField] private TMP_Text contentLabel;
        [SerializeField] private TMP_Text supportingLabel;
        [SerializeField] private Image contentIcon;
        [SerializeField] private bool preserveTextFormatting;

        public ThemeViewKey Key => key;
        public TMP_Text ContentLabel => contentLabel;
        public TMP_Text SupportingLabel => supportingLabel;
        public Image ContentIcon => contentIcon;

        internal ThemeView SourcePrefab { get; private set; }
        internal RectTransform Rect => (RectTransform)transform;

        internal bool CanReplaceWith(ThemeView prefab)
        {
            Selectable control = GetComponent<Selectable>();
            Selectable replacement = prefab.GetComponent<Selectable>();
            return control?.GetType() == replacement?.GetType() &&
                   (contentLabel == null) == (prefab.contentLabel == null) &&
                   (supportingLabel == null) == (prefab.supportingLabel == null) &&
                   (contentIcon == null) == (prefab.contentIcon == null);
        }

        internal ThemeView ReplaceWith(ThemeView prefab, TMP_FontAsset font)
        {
            int index = transform.GetSiblingIndex();
            ThemeView next = Instantiate(prefab, transform.parent, false);
            next.SourcePrefab = prefab;
            next.name = name;
            next.Rect.anchorMin = Rect.anchorMin;
            next.Rect.anchorMax = Rect.anchorMax;
            next.Rect.pivot = Rect.pivot;
            next.Rect.sizeDelta = Rect.sizeDelta;
            next.Rect.anchoredPosition3D = Rect.anchoredPosition3D;
            next.Rect.localRotation = Rect.localRotation;
            next.Rect.localScale = Rect.localScale;
            next.transform.SetSiblingIndex(index);
            CopyLayout(next);
            CopyState(next);
            next.ApplyFont(font);
            next.gameObject.SetActive(gameObject.activeSelf);

            gameObject.SetActive(false);
            transform.SetParent(null, false);
            Destroy(gameObject);
            return next;
        }

        internal void ApplyFont(TMP_FontAsset font)
        {
            TMP_FontAsset target = font != null ? font : TMP_Settings.defaultFontAsset;
            if (target == null)
                return;

            foreach (TMP_Text label in GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.font != target)
                    label.font = target;
            }
        }

        private void CopyLayout(ThemeView next)
        {
            if (!TryGetComponent(out LayoutElement before))
                return;

            if (!next.TryGetComponent(out LayoutElement after))
                after = next.gameObject.AddComponent<LayoutElement>();

            after.ignoreLayout = before.ignoreLayout;
            after.minWidth = before.minWidth;
            after.minHeight = before.minHeight;
            after.preferredWidth = before.preferredWidth;
            after.preferredHeight = before.preferredHeight;
            after.flexibleWidth = before.flexibleWidth;
            after.flexibleHeight = before.flexibleHeight;
            after.layoutPriority = before.layoutPriority;
        }

        private void CopyState(ThemeView next)
        {
            if (contentLabel != null)
            {
                next.contentLabel.text = contentLabel.text;
                next.preserveTextFormatting = preserveTextFormatting;
                if (preserveTextFormatting)
                {
                    next.contentLabel.fontSize = contentLabel.fontSize;
                    next.contentLabel.fontStyle = contentLabel.fontStyle;
                    next.contentLabel.alignment = contentLabel.alignment;
                }
            }

            if (supportingLabel != null)
                next.supportingLabel.text = supportingLabel.text;

            if (contentIcon != null)
                next.contentIcon.sprite = contentIcon.sprite;

            if (TryGetComponent(out Selectable oldControl))
                next.GetComponent<Selectable>().interactable = oldControl.interactable;

            if (TryGetComponent(out Button button))
                next.GetComponent<Button>().onClick = button.onClick;

            if (TryGetComponent(out Toggle toggle))
            {
                var replacement = next.GetComponent<Toggle>();
                bool isOn = toggle.isOn;
                replacement.SetIsOnWithoutNotify(false);
                replacement.group = toggle.group;
                replacement.SetIsOnWithoutNotify(isOn);
                replacement.onValueChanged = toggle.onValueChanged;
            }

            if (TryGetComponent(out Slider slider))
            {
                var replacement = next.GetComponent<Slider>();
                replacement.minValue = slider.minValue;
                replacement.maxValue = slider.maxValue;
                replacement.wholeNumbers = slider.wholeNumbers;
                replacement.SetValueWithoutNotify(slider.value);
                replacement.onValueChanged = slider.onValueChanged;
            }
        }
    }
}
