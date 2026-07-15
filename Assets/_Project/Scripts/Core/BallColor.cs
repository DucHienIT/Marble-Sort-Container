namespace MarbleSort.Core
{
    /// <summary>
    /// Logical ball colours. Index maps into PaletteConfig.ballColors so designers can
    /// recolour without touching code. Keep "None" last-ish only if needed; here every
    /// value is a real colour used by gameplay.
    /// </summary>
    public enum BallColor
    {
        Red = 0,
        Blue = 1,
        Green = 2,
        Yellow = 3,
        Purple = 4,
        Orange = 5,
    }

    public static class BallColorUtil
    {
        public const int Count = 6;

        /// <summary>Parse the lowercase colour names used in level JSON ("red", "blue", ...).</summary>
        public static BallColor Parse(string s)
        {
            if (string.IsNullOrEmpty(s)) return BallColor.Red;
            switch (s.Trim().ToLowerInvariant())
            {
                case "red": return BallColor.Red;
                case "blue": return BallColor.Blue;
                case "green": return BallColor.Green;
                case "yellow": return BallColor.Yellow;
                case "purple": return BallColor.Purple;
                case "orange": return BallColor.Orange;
                default: return BallColor.Red;
            }
        }

        public static string ToName(BallColor c)
        {
            return c.ToString().ToLowerInvariant();
        }
    }
}
