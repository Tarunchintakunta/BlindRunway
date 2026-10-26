using System;
using UnityEngine;

namespace BlindRunway
{
    // LevelDefinition assets under Resources/Levels, ordered by number.
    public static class Levels
    {
        static LevelDefinition[] all;
        static LevelDefinition fallback;

        public static LevelDefinition[] All
        {
            get
            {
                if (all == null || all.Length == 0)
                {
                    all = Resources.LoadAll<LevelDefinition>("Levels");
                    Array.Sort(all, (a, b) => a.number.CompareTo(b.number));
                    if (all.Length == 0) Debug.LogError("No LevelDefinition assets in Resources/Levels.");
                }
                return all;
            }
        }

        public static int Count => Mathf.Max(1, All.Length);

        public static LevelDefinition Get(int index)
        {
            if (All.Length == 0) return fallback ? fallback : (fallback = ScriptableObject.CreateInstance<LevelDefinition>());
            return All[Mathf.Clamp(index, 0, All.Length - 1)];
        }
    }
}
