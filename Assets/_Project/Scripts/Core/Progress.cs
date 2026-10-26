using UnityEngine;

namespace BlindRunway
{
    // Local campaign progress (PlayerPrefs). Each player keeps their own; the host's decides what can be flown.
    public static class Progress
    {
        const string UnlockKey = "br.unlocked", ClearKey = "br.cleared", StarsKey = "br.stars.";

        public static int Unlocked => Mathf.Clamp(PlayerPrefs.GetInt(UnlockKey, 0), 0, Levels.Count - 1);
        public static bool IsUnlocked(int level) => level <= Unlocked;
        public static bool Cleared(int level) => (PlayerPrefs.GetInt(ClearKey, 0) & (1 << level)) != 0;
        public static int Stars(int level) => PlayerPrefs.GetInt(StarsKey + level, 0);

        public static bool AllCleared
        {
            get
            {
                for (int i = 0; i < Levels.Count; i++) if (!Cleared(i)) return false;
                return true;
            }
        }

        public static void Record(ResultMsg r)
        {
            if (!r.win) return;
            PlayerPrefs.SetInt(ClearKey, PlayerPrefs.GetInt(ClearKey, 0) | (1 << r.level));
            PlayerPrefs.SetInt(StarsKey + r.level, Mathf.Max(Stars(r.level), r.stars));
            PlayerPrefs.SetInt(UnlockKey, Mathf.Max(Unlocked, Mathf.Min(r.level + 1, Levels.Count - 1)));
            PlayerPrefs.Save();
        }
    }
}
