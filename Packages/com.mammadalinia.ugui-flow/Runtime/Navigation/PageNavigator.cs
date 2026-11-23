using System;
using System.Collections.Generic;
using UnityEngine;

namespace UguiFlow
{
    /// <summary>A page selection in one mounted screen.</summary>
    public readonly struct PageChanged
    {
        public PageNavigator Navigator { get; }
        public UIComponent Previous { get; }
        public UIComponent Current { get; }

        internal PageChanged(PageNavigator navigator, UIComponent previous, UIComponent current)
        {
            Navigator = navigator;
            Previous = previous;
            Current = current;
        }
    }

    [AddComponentMenu("uGUI Flow/Page Navigator")]
    public sealed class PageNavigator : UIComponent
    {
        [SerializeField] private UIComponent[] pages = Array.Empty<UIComponent>();

        public UIComponent SelectedPage { get; private set; }

        private bool changing;
        private bool publishedSelection;

        protected override void OnInitialize()
        {
            SelectedPage = pages[0];
            foreach (UIComponent page in pages)
                page.SetVisible(page == SelectedPage);
        }

        protected override void OnShow()
        {
            if (!publishedSelection)
                PublishSelection(null);
        }

        protected override void OnDispose() => SelectedPage = null;

        /// <summary>Selects a registered page on a mounted screen between lifecycle transitions.</summary>
        public bool TrySelectPage(UIComponent page)
        {
            if (changing || !CanChangeVisibility || page == null || Array.IndexOf(pages, page) < 0)
                return false;
            if (SelectedPage == page)
                return true;

            changing = true;
            try
            {
                UIComponent previous = SelectedPage;
                SelectedPage = page;
                if (previous != null)
                    previous.SetVisible(false);
                page.SetVisible(true);
                PublishSelection(previous);
                return true;
            }
            finally
            {
                changing = false;
            }
        }

        private void PublishSelection(UIComponent previous)
        {
            publishedSelection = true;
            Events.Publish(new PageChanged(this, previous, SelectedPage));
        }

        internal override bool TryValidate(out string error)
        {
            if (!base.TryValidate(out error))
                return false;

            var seen = new HashSet<UIComponent>();
            if (pages.Length > 0)
            {
                foreach (UIComponent page in pages)
                {
                    if (page == null || page.FindParent() != this || !seen.Add(page))
                    {
                        error = $"Page Navigator '{name}' needs unique pages whose closest UI Component parent is the navigator.";
                        return false;
                    }
                }

                return true;
            }

            error = $"Page Navigator '{name}' needs at least one page.";
            return false;
        }
    }
}
