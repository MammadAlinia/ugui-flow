using UnityEngine;

namespace UguiFlow
{
    /// <summary>A controller on a stable child of a window prefab.</summary>
    public abstract class ScreenComponent : MonoBehaviour
    {
        protected Window Window { get; private set; }

        /// <summary>Initializes the controller while the window is still inactive.</summary>
        protected virtual void OnOpen() { }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void OnClose() { }

        internal void Open(Window window)
        {
            Window = window;
            OnOpen();
        }

        internal void Show() => OnShow();
        internal void Hide() => OnHide();

        internal void Close()
        {
            try
            {
                OnClose();
            }
            finally
            {
                Window = null;
            }
        }
    }
}
