using UnityEngine;

namespace UguiFlow.Samples
{
    public sealed class LifecycleWindowDemo : UIComponent
    {
        [SerializeField] private string title;
        [SerializeField] private RectTransform cards;
        [SerializeField] private ThemeView cardPrefab;

        private LifecycleDemo demo;
        private int nextCardNumber = 1;

        protected override void OnInitialize()
        {
            demo = GetComponentInParent<LifecycleDemo>(true);
            demo?.Record(title, "Initialize");
            CreateCard();
        }

        protected override void OnShow() => demo?.Record(title, "Show");

        protected override void OnHide() => demo?.Record(title, "Hide");
        protected override void OnDispose() => demo?.Record(title, "Destroy");

        public void AddCard()
        {
            ThemeView card = CreateCard();
            ThemeScope scope = GetComponentInParent<ThemeScope>();
            if (scope != null && !scope.TryApplyCurrentTheme((RectTransform)card.transform, out string error))
            {
                Discard(card.transform);
                Debug.LogError(error, this);
            }
        }

        public void RemoveCard()
        {
            for (int i = cards.childCount - 1; i >= 0; i--)
            {
                Transform child = cards.GetChild(i);
                if (!child.TryGetComponent<ThemeView>(out _))
                    continue;

                Discard(child);
                return;
            }
        }

        private ThemeView CreateCard()
        {
            int number = nextCardNumber++;
            ThemeView card = Instantiate(cardPrefab, cards, false);
            card.name = $"Note {number}";
            card.ContentLabel.text = card.name;
            card.SupportingLabel.text = number == 1 ? "Created when this page initialized." : "Added by the child controller.";
            return card;
        }

        private void Discard(Transform child)
        {
            child.gameObject.SetActive(false);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }
}
