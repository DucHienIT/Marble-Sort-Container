using UnityEngine;

namespace MarbleSort.Core
{
    /// <summary>
    /// Loads level JSON from Resources/Levels/level_NNN.json. Pure loader (no MonoBehaviour);
    /// callers pass 1-based level numbers. Also discovers how many levels exist.
    /// </summary>
    public static class LevelLoader
    {
        public const string LevelsPath = "Levels/";

        public static string FileName(int levelNumber)
        {
            return LevelsPath + "level_" + levelNumber.ToString("000");
        }

        public static LevelDefinition Load(int levelNumber)
        {
            var ta = Resources.Load<TextAsset>(FileName(levelNumber));
            if (ta == null)
            {
                Debug.LogWarning("[MarbleSort] Level not found: " + FileName(levelNumber));
                return null;
            }
            var json = JsonUtility.FromJson<LevelJson>(ta.text);
            var def = LevelDefinition.FromJson(json);
            var err = def.ValidateTotals();
            if (err != null)
                Debug.LogWarning("[MarbleSort] Level " + levelNumber + " totals mismatch: " + err);
            return def;
        }

        /// <summary>Count contiguous level_NNN assets starting at 1.</summary>
        public static int CountLevels()
        {
            int n = 0;
            while (Resources.Load<TextAsset>(FileName(n + 1)) != null) n++;
            return n;
        }
    }
}
