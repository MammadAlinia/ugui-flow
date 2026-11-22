using UnityEngine;

namespace UguiFlow.Samples
{
    public sealed class LifecycleWindowDemo : ScreenComponent
    {
        [SerializeField] private string title;
        [SerializeField] private RectTransform cards;
        [SerializeField] private ThemeView cardPrefab;

        private LifecycleDemo demo;
        private int nextCardNumber = 1;

        public string Title => title;

        protected override void OnOpen()
        {
            demo = Window.Screen.GetComponent<LifecycleDemo>();
            demo?.Record(title, "Open");
            CreateCard();
        }

        protected override void OnShow()
        {
            demo?.Record(title, "Show");
            demo?.RefreshStatus();
        }

        protected override void OnHide() => demo?.Record(title, "Hide");
        protected override void OnClose() => demo?.Record(title, "Close");

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
            if (cards.childCount > 0)
                Discard(cards.GetChild(cards.childCount - 1));
        }

        private ThemeView CreateCard()
        {
            int number = nextCardNumber++;
            ThemeView card = Instantiate(cardPrefab, cards, false);
            card.name = $"Note {number}";
            card.ContentLabel.text = card.name;
            card.SupportingLabel.text = number == 1 ? "Created when this window opened." : "Added by the child controller.";
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
