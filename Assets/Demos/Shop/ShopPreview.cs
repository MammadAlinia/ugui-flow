using System;
using UnityEngine;

namespace UguiFlow.Samples
{
    public sealed class ShopPreview : UIComponent
    {
        [Serializable]
        private struct Item
        {
            public string title;
            public string category;
            public int price;
        }

        [SerializeField] private RectTransform grid;
        [SerializeField] private ThemeView cardPrefab;
        [SerializeField] private ThemeView itemCount;
        [SerializeField] private ThemeDatabase light;
        [SerializeField] private ThemeDatabase dark;
        [SerializeField] private Item[] items = Array.Empty<Item>();

        private ThemeScope scope;

        protected override void OnInitialize()
        {
            scope = GetComponentInParent<ThemeScope>(true);
            itemCount.ContentLabel.text = $"{items.Length} items";
            foreach (Item item in items)
            {
                ThemeView card = Instantiate(cardPrefab, grid, false);
                card.name = item.title;
                card.ContentLabel.text = item.title;
                card.SupportingLabel.text = $"{item.category} / {item.price} credits";
            }
        }

        public void ShowLight() => Apply(light);
        public void ShowDark() => Apply(dark);

        private void Apply(ThemeDatabase theme)
        {
            if (!scope.TrySetTheme(theme, out string error))
                Debug.LogError(error, this);
        }
    }
}
