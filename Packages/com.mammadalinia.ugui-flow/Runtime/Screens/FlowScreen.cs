using System;
using UnityEngine;

namespace UguiFlow
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [AddComponentMenu("uGUI Flow/Flow Screen")]
    public sealed class FlowScreen : MonoBehaviour
    {
        [SerializeField] private RectTransform windowHost;
        [SerializeField] private Window startupWindow;

        public Window CurrentWindow { get; private set; }

        private bool transitioning;

        private void Start()
        {
            if (startupWindow != null && CurrentWindow == null && !TryOpen(startupWindow, out _, out string error))
                Debug.LogError(error, this);
        }

        public bool TryOpen(Window prefab, out Window window, out string error)
        {
            window = null;
            if (transitioning || !isActiveAndEnabled || !Application.IsPlaying(gameObject))
            {
                error = "Open windows on an active Flow Screen during Play Mode, between transitions.";
                return false;
            }

            if (windowHost == null || windowHost == transform || !windowHost.IsChildOf(transform) ||
                !windowHost.gameObject.activeInHierarchy ||
                windowHost.GetComponentInParent<FlowScreen>() != this ||
                windowHost.GetComponentInParent<Window>() != null)
            {
                error = "Assign an active window host below this Flow Screen, outside any window.";
                return false;
            }

            if (prefab == null || prefab.gameObject.scene.IsValid() ||
                prefab.GetComponentsInChildren<Window>(true).Length != 1 ||
                prefab.TryGetComponent<ThemeView>(out _))
            {
                error = "Use a window prefab asset with a single Window and a stable root.";
                return false;
            }

            transitioning = true;
            GameObject staging = null;
            Window candidate = null;
            try
            {
                try
                {
                    staging = new GameObject("Window staging", typeof(RectTransform));
                    staging.SetActive(false);
                    candidate = Instantiate(prefab, staging.transform, false);
                    candidate.gameObject.SetActive(false);
                    candidate.transform.SetParent(windowHost, false);

                    candidate.Open(this);
                    ThemeScope scope = candidate.GetComponentInParent<ThemeScope>(true);
                    if (scope != null && !scope.TryApplyCurrentTheme((RectTransform)candidate.transform, out error))
                        return false;
                }
                catch (Exception exception)
                {
                    error = $"Could not prepare window '{prefab.name}'. {exception.Message}";
                    return false;
                }

                if (CurrentWindow != null)
                    CurrentWindow.Close();

                CurrentWindow = candidate;
                window = candidate;
                error = null;
                window.Show();
                return true;
            }
            finally
            {
                if (window == null && candidate != null)
                    candidate.Close();
                if (staging != null)
                    Destroy(staging);
                transitioning = false;
            }
        }

        public bool TryShow()
        {
            if (transitioning || CurrentWindow == null || CurrentWindow.IsVisible)
                return false;

            return Transition(window => window.Show());
        }

        public bool TryHide()
        {
            if (transitioning || CurrentWindow == null || !CurrentWindow.IsVisible)
                return false;

            return Transition(window => window.Hide());
        }

        public bool TryClose()
        {
            if (transitioning || CurrentWindow == null)
                return false;

            return Transition(window =>
            {
                window.Close();
                CurrentWindow = null;
            });
        }

        private bool Transition(Action<Window> action)
        {
            transitioning = true;
            try
            {
                action(CurrentWindow);
                return true;
            }
            finally
            {
                transitioning = false;
            }
        }
    }
}
