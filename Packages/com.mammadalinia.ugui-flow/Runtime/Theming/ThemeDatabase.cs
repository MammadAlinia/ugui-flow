using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UguiFlow
{
    [CreateAssetMenu(menuName = "uGUI Flow/Theme Database")]
    public sealed class ThemeDatabase : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            public string role;
            public ThemeView prefab;
        }

        [SerializeField] private TMP_FontAsset defaultFont;
        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        public TMP_FontAsset DefaultFont => defaultFont;

        public bool TryGetPrefab(string role, out ThemeView prefab)
        {
            foreach (Entry entry in entries)
            {
                if (entry.role == role)
                {
                    prefab = entry.prefab;
                    return prefab != null;
                }
            }

            prefab = null;
            return false;
        }

        internal bool Validate(out string error)
        {
            var roles = new HashSet<string>(StringComparer.Ordinal);
            foreach (Entry entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.role) || !roles.Add(entry.role))
                {
                    error = $"Theme '{name}' has an empty or duplicate role '{entry.role}'.";
                    return false;
                }

                if (entry.prefab == null || entry.prefab.Role != entry.role || entry.prefab.transform.parent != null)
                {
                    error = $"Theme '{name}' needs a root prefab with role '{entry.role}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}
