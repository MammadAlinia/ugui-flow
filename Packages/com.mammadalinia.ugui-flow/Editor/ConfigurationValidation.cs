using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UguiFlow.Editor
{
    [InitializeOnLoad]
    internal static class ConfigurationValidation
    {
        private static readonly Dictionary<Object, string> Reported = new();
        private static bool recheck;

        static ConfigurationValidation()
        {
            EditorApplication.update += ValidatePending;
            EditorApplication.hierarchyChanged += Recheck;
            EditorApplication.projectChanged += Recheck;
            Undo.undoRedoPerformed += Recheck;
        }

        private static void Recheck() => recheck = true;

        private static void ValidatePending()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                while (ValidationQueue.TryDequeue(out _)) { }
                return;
            }

            var pending = new HashSet<Object>();
            while (ValidationQueue.TryDequeue(out Object target))
            {
                if (target != null)
                    pending.Add(target);
            }

            if (recheck)
            {
                recheck = false;
                pending.UnionWith(Resources.FindObjectsOfTypeAll<Window>());
                pending.UnionWith(Resources.FindObjectsOfTypeAll<ThemeScope>());
                pending.UnionWith(Resources.FindObjectsOfTypeAll<ThemeDatabase>());
                pending.UnionWith(Resources.FindObjectsOfTypeAll<ThemeView>());
                foreach (Object destroyed in Reported.Keys.Where(target => target == null).ToArray())
                    Reported.Remove(destroyed);
            }

            foreach (Object target in pending)
                ValidateAndReport(target);
        }

        private static void ValidateAndReport(Object target)
        {
            string error = null;
            switch (target)
            {
                case Window window:
                    window.TryValidate(out error);
                    break;
                case ThemeScope scope:
                    scope.TryValidate(out error);
                    break;
                case ThemeDatabase database:
                    database.Validate(out error);
                    break;
                case ThemeView view:
                    view.TryValidate(out error);
                    break;
                case UIComponent component:
                    component.TryValidate(out error);
                    break;
                case ResponsiveGrid grid:
                    grid.TryValidateAndRefresh(out error);
                    break;
            }

            if (error == null)
            {
                Reported.Remove(target);
                return;
            }

            if (!Reported.TryGetValue(target, out string previous) || previous != error)
                Debug.LogError(error, target);
            Reported[target] = error;
        }
    }
}
