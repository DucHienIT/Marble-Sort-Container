using UnityEngine;

namespace MarbleSort.Systems
{
    /// <summary>Static PlayerPrefs wrapper with a game-specific key prefix.</summary>
    public static class SaveSystem
    {
        private const string Prefix = "MarbleSort.";
        private const string KeyMaxLevel = Prefix + "maxUnlockedLevel";
        private const string KeyMusic = Prefix + "musicOn";
        private const string KeySfx = Prefix + "sfxOn";

        /// <summary>Highest level number reached (1-based). Defaults to 1.</summary>
        public static int MaxUnlockedLevel
        {
            get { return Mathf.Max(1, PlayerPrefs.GetInt(KeyMaxLevel, 1)); }
            set { PlayerPrefs.SetInt(KeyMaxLevel, Mathf.Max(1, value)); PlayerPrefs.Save(); }
        }

        public static void UnlockLevel(int level)
        {
            if (level > MaxUnlockedLevel) MaxUnlockedLevel = level;
        }

        public static bool MusicOn
        {
            get { return PlayerPrefs.GetInt(KeyMusic, 1) == 1; }
            set { PlayerPrefs.SetInt(KeyMusic, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool SfxOn
        {
            get { return PlayerPrefs.GetInt(KeySfx, 1) == 1; }
            set { PlayerPrefs.SetInt(KeySfx, value ? 1 : 0); PlayerPrefs.Save(); }
        }
    }
}
