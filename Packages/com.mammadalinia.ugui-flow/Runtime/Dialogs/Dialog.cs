using System;
using UnityEngine;

namespace UguiFlow
{
    public enum Icons
    {
        Accept,
        Cancel,
        Retry
    }

    /// <summary>Shows a scene-local modal dialog during Play Mode.</summary>
    public static class Dialog
    {
        public static void Info(string title, string description, params (Icons icon, Action onClick)[] buttons) =>
            Show(false, title, description, Convert(buttons));

        public static void Info(string title, string description, params (string label, Action onClick)[] buttons) =>
            Show(false, title, description, Convert(buttons));

        public static void Info(string title, string description, Action onClick) =>
            Show(false, title, description, new[] { new Choice("OK", null, onClick) });

        public static void Error(string title, string description, params (Icons icon, Action onClick)[] buttons) =>
            Show(true, title, description, Convert(buttons));

        public static void Error(string title, string description, params (string label, Action onClick)[] buttons) =>
            Show(true, title, description, Convert(buttons));

        public static void Error(string title, string description, Action onClick) =>
            Show(true, title, description, new[] { new Choice("OK", null, onClick) });

        private static Choice[] Convert((Icons icon, Action onClick)[] buttons)
        {
            RequireButtons(buttons);
            var choices = new Choice[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                var (icon, onClick) = buttons[i];
                if (icon is not (Icons.Accept or Icons.Cancel or Icons.Retry))
                    throw new ArgumentException($"Button {i + 1} has an unknown icon.", nameof(buttons));
                choices[i] = new Choice(null, icon, onClick);
            }
            return choices;
        }

        private static Choice[] Convert((string label, Action onClick)[] buttons)
        {
            RequireButtons(buttons);
            var choices = new Choice[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                var (label, onClick) = buttons[i];
                if (string.IsNullOrWhiteSpace(label))
                    throw new ArgumentException($"Button {i + 1} needs a label.", nameof(buttons));
                choices[i] = new Choice(label, null, onClick);
            }
            return choices;
        }

        private static void RequireButtons(Array buttons)
        {
            if (buttons == null || buttons.Length == 0)
                throw new ArgumentException("Supply at least one dialog button.", nameof(buttons));
        }

        private static void Show(bool isError, string title, string description, Choice[] buttons)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Supply a dialog title.", nameof(title));
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].OnClick == null)
                    throw new ArgumentException($"Button {i + 1} needs a callback.", nameof(buttons));
            }
            if (!Application.isPlaying)
                throw new InvalidOperationException("Show dialogs during Play Mode.");

            DialogHost.ForActiveScene().Enqueue(isError, title, description ?? string.Empty, buttons);
        }

        internal readonly struct Choice
        {
            internal Choice(string label, Icons? icon, Action onClick)
            {
                Label = label;
                Icon = icon;
                OnClick = onClick;
            }

            internal string Label { get; }
            internal Icons? Icon { get; }
            internal Action OnClick { get; }
        }
    }
}
