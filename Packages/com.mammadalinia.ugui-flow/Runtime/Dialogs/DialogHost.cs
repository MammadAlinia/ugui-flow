using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[assembly: InternalsVisibleTo("UguiFlow.InputSystem")]

namespace UguiFlow
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
    [AddComponentMenu("uGUI Flow/Dialog Host")]
    public sealed class DialogHost : MonoBehaviour
    {
        [SerializeField] private DialogScreen infoPrefab;
        [SerializeField] private DialogScreen errorPrefab;

        private readonly Queue<(bool isError, string title, string description, Dialog.Choice[] buttons)> pending = new();
        private DialogScreen current;
        private EventSystem focusSystem;
        private GameObject previousSelection;
        private GameObject dialogSelection;
        private bool automatic;
        private bool advancing;

        internal static Action<GameObject> InstallInputSystemModule { get; set; }

        internal static DialogHost ForActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            DialogHost fallback = null;
            DialogHost custom = null;
            foreach (DialogHost host in FindObjectsByType<DialogHost>(FindObjectsSortMode.None))
            {
                if (host.gameObject.scene != scene || !host.isActiveAndEnabled)
                    continue;
                if (host.automatic)
                    fallback = host;
                else if (custom == null)
                    custom = host;
                else
                    throw new InvalidOperationException("Use one active Dialog Host per scene.");
            }

            // Keep requests together if a custom host is enabled during a modal sequence.
            if (fallback != null && (fallback.current != null || fallback.advancing))
                return fallback;
            if (custom != null)
                return custom;
            if (fallback != null)
                return fallback;

            var root = new GameObject("uGUI Flow Dialog Host", typeof(RectTransform), typeof(Canvas));
            root.SetActive(false);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 640);
            scaler.matchWidthOrHeight = 1;
            var created = root.AddComponent<DialogHost>();
            created.automatic = true;
            root.SetActive(true);
            return created;
        }

        internal void Enqueue(bool isError, string title, string description, Dialog.Choice[] buttons)
        {
            if (transform.parent != null)
                throw new InvalidOperationException("Put the Dialog Host on a dedicated root Canvas.");

            pending.Enqueue((isError, title, description, buttons));
            Advance();
        }

        private void Advance()
        {
            if (advancing || !isActiveAndEnabled)
                return;

            advancing = true;
            try
            {
                while (isActiveAndEnabled && current == null && pending.Count > 0)
                {
                    var request = pending.Dequeue();
                    Open(request.isError, request.title, request.description, request.buttons);
                }
            }
            finally
            {
                advancing = false;
            }
            if (current == null)
                RestoreFocus();
        }

        private void Open(bool isError, string title, string description, Dialog.Choice[] buttons)
        {
            Canvas canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            DialogScreen prefab = isError ? errorPrefab : infoPrefab;
            if (prefab == null)
                prefab = Resources.Load<DialogScreen>(isError ? "UguiFlow/Dialogs/Error" : "UguiFlow/Dialogs/Info");
            if (prefab == null || prefab.gameObject.scene.IsValid() || prefab.transform.parent != null)
            {
                Debug.LogError("Assign a root Dialog Screen prefab to the Dialog Host.", this);
                return;
            }
            if (!prefab.TryValidate(out string error))
            {
                Debug.LogError(error, prefab);
                return;
            }

            var staging = new GameObject("Dialog staging", typeof(RectTransform));
            staging.SetActive(false);
            DialogScreen candidate = null;
            bool adopted = false;
            try
            {
                candidate = Instantiate(prefab, staging.transform, false);
                candidate.gameObject.SetActive(false);
                candidate.transform.SetParent(transform, false);
                candidate.Configure(title, description, buttons, Complete);
                candidate.Prepare();

                ThemeScope scope = GetComponent<ThemeScope>();
                if (scope != null && !scope.TryApplyCurrentTheme((RectTransform)candidate.transform, out error))
                    throw new InvalidOperationException(error);
                if (!isActiveAndEnabled)
                    return;

                EnsureEventSystem();
                current = candidate;
                adopted = true;
                dialogSelection = null;
                candidate.Mount(true);
                candidate.UpdateLayout();
                KeepFocus();
            }
            catch (Exception exception)
            {
                if (current == candidate && candidate != null)
                {
                    current = null;
                    Remove(candidate);
                }
                Debug.LogException(exception, this);
            }
            finally
            {
                if (!adopted && candidate != null)
                    Remove(candidate);
                Destroy(staging);
            }
        }

        private void Complete(DialogScreen screen, Action callback)
        {
            if (advancing || current != screen || !isActiveAndEnabled)
                return;

            advancing = true;
            try
            {
                current = null;
                Remove(screen);
                callback();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                advancing = false;
                if (this != null)
                    Advance();
            }
        }

        private void LateUpdate()
        {
            if (current == null)
            {
                if (pending.Count > 0 || focusSystem != null)
                    Advance();
                return;
            }
            current.UpdateLayout();
            KeepFocus();
        }

        private void KeepFocus()
        {
            EventSystem events = EventSystem.current;
            if (events == null || events.alreadySelecting)
                return;
            if (focusSystem != events)
            {
                RestoreFocus();
                focusSystem = events;
                previousSelection = events.currentSelectedGameObject;
            }

            GameObject selected = events.currentSelectedGameObject;
            if (!current.ContainsAction(selected))
            {
                selected = current.ContainsAction(dialogSelection) ? dialogSelection : current.FirstAction;
                events.SetSelectedGameObject(selected);
            }
            if (current != null && dialogSelection != selected)
            {
                dialogSelection = selected;
                current.RevealAction(selected);
            }
        }

        private void RestoreFocus()
        {
            EventSystem events = focusSystem;
            GameObject target = previousSelection;
            focusSystem = null;
            previousSelection = null;
            dialogSelection = null;
            if (events != null && events.isActiveAndEnabled && !events.alreadySelecting)
            {
                if (target != null && (!target.activeInHierarchy ||
                    (target.TryGetComponent(out Selectable selectable) &&
                    (!selectable.isActiveAndEnabled || !selectable.IsInteractable()))))
                    target = null;
                events.SetSelectedGameObject(target);
            }
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null)
                return;

            var input = new GameObject("Dialog Event System");
            input.SetActive(false);
            input.transform.SetParent(transform, false);
            input.AddComponent<EventSystem>();
            if (InstallInputSystemModule != null)
                InstallInputSystemModule(input);
#if ENABLE_LEGACY_INPUT_MANAGER
            else
                input.AddComponent<StandaloneInputModule>();
#endif
            input.SetActive(true);
        }

        private void OnDisable()
        {
            pending.Clear();
            DialogScreen screen = current;
            current = null;
            if (screen != null)
                Remove(screen);
            RestoreFocus();
        }

        private static void Remove(DialogScreen screen)
        {
            screen.DisposeScreen();
            screen.gameObject.SetActive(false);
            if (screen.transform.parent != null && screen.transform.parent.gameObject.activeInHierarchy)
                screen.transform.SetParent(null, false);
            Destroy(screen.gameObject);
        }
    }
}
