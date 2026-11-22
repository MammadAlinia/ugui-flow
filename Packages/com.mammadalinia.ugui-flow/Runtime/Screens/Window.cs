using System;
using System.Collections.Generic;
using UnityEngine;

namespace UguiFlow
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("uGUI Flow/Window")]
    public sealed class Window : MonoBehaviour
    {
        public FlowScreen Screen { get; private set; }
        public bool IsVisible { get; private set; }

        private readonly List<ScreenComponent> controllers = new();

        internal void Open(FlowScreen screen)
        {
            Screen = screen;
            var authoredControllers = GetComponentsInChildren<ScreenComponent>(true);
            foreach (ScreenComponent controller in authoredControllers)
            {
                if (controller.transform != transform && controller.enabled)
                    controllers.Add(controller);
            }

            for (int i = 0; i < controllers.Count; i++)
            {
                try
                {
                    controllers[i].Open(this);
                }
                catch
                {
                    // Close only controllers that entered OnOpen, including the one that failed.
                    controllers.RemoveRange(i + 1, controllers.Count - i - 1);
                    throw;
                }
            }
        }

        internal void Show()
        {
            IsVisible = true;
            gameObject.SetActive(true);
            Notify(controller => controller.Show());
        }

        internal void Hide()
        {
            Notify(controller => controller.Hide());
            gameObject.SetActive(false);
            IsVisible = false;
        }

        internal void Close()
        {
            if (IsVisible)
                Hide();

            Notify(controller => controller.Close());
            controllers.Clear();
            Screen = null;
            // A theme refresh can scan inactive children before Unity processes Destroy.
            transform.SetParent(null, false);
            Destroy(gameObject);
        }

        private void Notify(Action<ScreenComponent> callback)
        {
            foreach (ScreenComponent controller in controllers)
            {
                if (controller == null)
                    continue;

                try
                {
                    callback(controller);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, controller);
                }
            }
        }
    }
}
