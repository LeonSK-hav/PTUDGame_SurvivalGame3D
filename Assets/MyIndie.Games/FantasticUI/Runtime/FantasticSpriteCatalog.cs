using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyIndieGames.FantasticUI
{
    [CreateAssetMenu(menuName = "MyIndie.Games/Fantastic UI/Sprite Catalog", fileName = "Sprite Catalog", order = 1)]
    public class FantasticSpriteCatalog : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string category;
            public string name;
            public Sprite sprite;
        }

        [SerializeField] List<Entry> entries = new List<Entry>();

        public List<Entry> Entries => entries;

        public Sprite Find(string spriteName)
        {
            foreach (var entry in entries)
            {
                if (entry.sprite != null && string.Equals(entry.name, spriteName, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.sprite;
                }
            }

            return null;
        }

        public IEnumerable<Entry> InCategory(string category)
        {
            foreach (var entry in entries)
            {
                if (string.Equals(entry.category, category, StringComparison.OrdinalIgnoreCase))
                {
                    yield return entry;
                }
            }
        }

        public List<string> Categories()
        {
            var result = new List<string>();
            foreach (var entry in entries)
            {
                if (!result.Contains(entry.category))
                {
                    result.Add(entry.category);
                }
            }

            return result;
        }
    }
}
