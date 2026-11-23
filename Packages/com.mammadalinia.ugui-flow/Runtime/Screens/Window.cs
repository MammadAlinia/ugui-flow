using System;
using UnityEngine;

namespace UguiFlow
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [AddComponentMenu("uGUI Flow/Window")]
    public sealed class Window : MonoBehaviour
    {
        [SerializeField] private RectTransform screenHost;
        [SerializeField] private UIComponent startupScreen;

        public UIComponent CurrentScreen { get; private set; }

        private bool transitioning;
        private bool showScreen;

        private bool IsTransitioning => transitioning || (CurrentScreen != null && CurrentScreen.IsRefreshing);

        private void Start()
        {
            bool valid = startupScreen != null && CurrentScreen == null
                ? TryOpen(startupScreen, out _, out string error)
                : TryValidateHost(out error);
            if (!valid)
                Debug.LogError(error, this);
        }

#if UNITY_EDITOR
        private void OnValidate() => ValidationQueue.Request(this);
#endif

        internal bool TryValidate(out string error) =>
            TryValidateHost(out error) && (startupScreen == null || TryValidateScreen(startupScreen, out error));

        private bool TryValidateHost(out string error)
        {
            if (screenHost == null || !screenHost.IsChildOf(transform) ||
                screenHost.GetComponentInParent<Window>(true) != this ||
                screenHost.GetComponentInParent<UIComponent>(true) != null ||
                screenHost.GetComponentInParent<ThemeView>(true) != null)
            {
                error = $"Window '{name}' needs a Screen Host on or below its Canvas, outside UI Components and Theme Views.";
                return false;
            }

            error = null;
            return true;
        }

        private bool TryValidateScreen(UIComponent prefab, out string error)
        {
            if (prefab == null || prefab.gameObject.scene.IsValid() || prefab.transform.parent != null)
            {
                error = "Use a prefab asset with a UI Component at its root.";
                return false;
            }

            foreach (UIComponent component in prefab.GetComponentsInChildren<UIComponent>(true))
            {
                if (!component.TryValidate(out error))
                    return false;
                if (component.GetComponentInParent<ThemeView>(true) != null)
                {
                    error = $"UI Component '{component.name}' must be outside replaceable Theme Views.";
                    return false;
                }
            }

            foreach (ThemeScope nestedScope in prefab.GetComponentsInChildren<ThemeScope>(true))
            {
                if (!nestedScope.TryValidate(out error))
                    return false;
            }

            ThemeScope scope = screenHost.GetComponentInParent<ThemeScope>(true);
            if (scope != null)
                return scope.TryValidateScreen(prefab.transform, out error);

            ThemeDatabase light = ThemeDatabase.Default;
            if (light == null)
            {
                error = "The bundled Light database is required for unthemed error views.";
                return false;
            }

            if (!light.Validate(out error))
                return false;
            foreach (ThemeView view in prefab.GetComponentsInChildren<ThemeView>(true))
            {
                if (!view.TryValidate(out error))
                    return false;
            }

            return true;
        }

        public bool TryOpen(UIComponent prefab, out UIComponent screen, out string error)
        {
            screen = null;
            if (this == null || IsTransitioning || !isActiveAndEnabled || !Application.IsPlaying(gameObject))
            {
                error = "Open screens on an active Window during Play Mode, between transitions.";
                return false;
            }

            transitioning = true;
            GameObject staging = null;
            UIComponent candidate = null;
            try
            {
                if (!TryValidateHost(out error))
                    return false;
                if (!screenHost.gameObject.activeInHierarchy)
                {
                    error = $"Window '{name}' needs an active Screen Host before opening a screen.";
                    return false;
                }
                if (!TryValidateScreen(prefab, out error))
                    return false;

                staging = new GameObject("Screen staging", typeof(RectTransform));
                staging.SetActive(false);
                candidate = Instantiate(prefab, staging.transform, false);
                candidate.gameObject.SetActive(false);
                candidate.transform.SetParent(screenHost, false);

                candidate.Prepare();
                ThemeScope scope = candidate.GetComponentInParent<ThemeScope>(true);
                if (scope != null && !scope.TryApplyCurrentTheme((RectTransform)candidate.transform, out error))
                    return false;
                candidate.PrepareErrorViews();

                CloseCurrentScreen();
                CurrentScreen = candidate;
                showScreen = true;
                candidate.Mount(isActiveAndEnabled);
                screen = candidate;
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                if (CurrentScreen == candidate)
                {
                    CurrentScreen = null;
                    showScreen = false;
                }
                error = $"Could not open the screen. {exception.Message}";
                return false;
            }
            finally
            {
                if (screen == null && candidate != null)
                    Remove(candidate);
                if (staging != null)
                    Destroy(staging);
                transitioning = false;
            }
        }

        public bool TryShow() => TrySetShown(true);
        public bool TryHide() => TrySetShown(false);

        private bool TrySetShown(bool visible)
        {
            if (IsTransitioning || CurrentScreen == null || showScreen == visible)
                return false;

            showScreen = visible;
            UpdatePresentation();
            return true;
        }

        public bool TryClose()
        {
            if (IsTransitioning || CurrentScreen == null)
                return false;

            transitioning = true;
            try
            {
                CloseCurrentScreen();
                return true;
            }
            finally
            {
                transitioning = false;
            }
        }

        private void OnEnable() => UpdatePresentation();
        private void OnDisable() => UpdatePresentation();

        private void UpdatePresentation()
        {
            if (IsTransitioning || CurrentScreen == null)
                return;

            transitioning = true;
            try
            {
                CurrentScreen.SetPresented(showScreen && isActiveAndEnabled);
            }
            finally
            {
                transitioning = false;
            }
        }

        private void OnDestroy()
        {
            if (CurrentScreen == null)
                return;

            transitioning = true;
            CurrentScreen.DisposeScreen();
            Destroy(CurrentScreen.gameObject);
            CurrentScreen = null;
        }

        private void CloseCurrentScreen()
        {
            if (CurrentScreen != null)
                Remove(CurrentScreen);

            CurrentScreen = null;
            showScreen = false;
        }

        private static void Remove(UIComponent screen)
        {
            screen.DisposeScreen();
            screen.gameObject.SetActive(false);
            // Keep a later theme refresh from finding a screen queued for destruction.
            screen.transform.SetParent(null, false);
            Destroy(screen.gameObject);
        }
    }
}
