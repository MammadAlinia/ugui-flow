using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UguiFlow
{
    [CreateAssetMenu(menuName = "uGUI Flow/Theme Database")]
    public sealed class ThemeDatabase : ScriptableObject
    {
        [SerializeField] private TMP_FontAsset defaultFont;
        [SerializeField] private ThemeView[] prefabs = Array.Empty<ThemeView>();

        public TMP_FontAsset DefaultFont => defaultFont;

        public bool TryGetPrefab(ThemeView view, out ThemeView prefab)
        {
            ThemeViewKey key = view != null ? view.Key : null;
            if (key != null)
            {
                foreach (ThemeView candidate in prefabs)
                {
                    if (candidate != null && candidate.Key == key)
                    {
                        prefab = candidate;
                        return true;
                    }
                }
            }

            prefab = null;
            return false;
        }

        internal bool Validate(out string error)
        {
            var keys = new HashSet<ThemeViewKey>();
            foreach (ThemeView prefab in prefabs)
            {
                if (prefab == null || prefab.transform.parent != null || prefab.Key == null)
                {
                    error = $"Theme '{name}' needs a root Theme View prefab with a key in each prefab entry.";
                    return false;
                }

                if (!keys.Add(prefab.Key))
                {
                    error = $"Theme '{name}' has multiple prefabs for key '{prefab.Key.name}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
