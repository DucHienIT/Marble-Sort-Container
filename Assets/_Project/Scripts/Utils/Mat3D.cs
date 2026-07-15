using System.Collections.Generic;
using UnityEngine;

namespace MarbleSort.Utils
{
    /// <summary>
    /// Runtime URP/Lit material factory. Candy look = coloured base + high smoothness (glossy).
    /// Materials are cached by (colour, finish) so thousands of marbles/tiles share instances.
    /// Falls back to the built-in Standard/Sprites shader if URP Lit isn't found.
    /// </summary>
    public static class Mat3D
    {
        private static Shader _lit;
        private static Shader Lit
        {
            get
            {
                if (_lit == null) _lit = Shader.Find("Universal Render Pipeline/Lit");
                if (_lit == null) _lit = Shader.Find("Standard");
                if (_lit == null) _lit = Shader.Find("Sprites/Default");
                return _lit;
            }
        }

        private static readonly Dictionary<string, Material> _cache = new Dictionary<string, Material>();

        /// <summary>A glossy candy material — high gloss so the bump-domes catch crisp highlights.</summary>
        public static Material Candy(Color c) { return Get(c, 0.72f, 0f, 0.07f); }
        /// <summary>A very glossy marble material.</summary>
        public static Material Marble(Color c) { return Get(c, 0.92f, 0.05f, 0.1f); }
        /// <summary>A soft matte material (trays, frames).</summary>
        public static Material Matte(Color c) { return Get(c, 0.25f, 0f, 0f); }
        /// <summary>A semi-glossy plastic (belt, bins).</summary>
        public static Material Plastic(Color c) { return Get(c, 0.45f, 0f, 0f); }
        /// <summary>A gently glowing material (star / completed bin).</summary>
        public static Material Glow(Color c) { return Get(c, 0.8f, 0.1f, 0.55f); }

        public static Material Get(Color c, float smoothness, float metallic, float emission)
        {
            string key = ColorKey(c) + "|" + smoothness.ToString("0.00") + "|" +
                         metallic.ToString("0.00") + "|" + emission.ToString("0.00");
            Material m;
            if (_cache.TryGetValue(key, out m) && m != null) return m;

            m = new Material(Lit);
            SetColor(m, c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (emission > 0f && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            _cache[key] = m;
            return m;
        }

        private static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        private static string ColorKey(Color c)
        {
            return Mathf.RoundToInt(c.r * 255) + "_" + Mathf.RoundToInt(c.g * 255) + "_" +
                   Mathf.RoundToInt(c.b * 255) + "_" + Mathf.RoundToInt(c.a * 255);
        }
    }
}
