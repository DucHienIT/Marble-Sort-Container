using System.Collections.Generic;
using UnityEngine;

namespace MarbleSort.Utils
{
    /// <summary>
    /// Procedurally generates white sprites (tint at use-site via SpriteRenderer/Image colour).
    /// SDF-based alpha for crisp rounded shapes at any size. Results are cached by key so
    /// repeated requests (every ball, every button) share one texture.
    /// </summary>
    public static class SpriteFactory
    {
        private const float PPU = 100f;
        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        private static Sprite Cached(string key, System.Func<Sprite> make)
        {
            Sprite s;
            if (_cache.TryGetValue(key, out s) && s != null) return s;
            s = make();
            _cache[key] = s;
            return s;
        }

        public static Sprite Circle(int size = 128)
        {
            return Cached("circle" + size, () =>
            {
                var tex = NewTex(size, size);
                float r = size * 0.5f;
                float c = r;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float d = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c));
                        float a = Mathf.Clamp01(r - d);      // 1px AA edge
                        tex.SetPixel(x, y, new Color(1, 1, 1, a));
                    }
                tex.Apply();
                return ToSprite(tex);
            });
        }

        /// <summary>Glossy ball: radial shading baked into RGB so a flat-tinted SpriteRenderer still reads as a sphere.</summary>
        public static Sprite Ball(int size = 128)
        {
            return Cached("ball" + size, () =>
            {
                var tex = NewTex(size, size);
                float r = size * 0.5f;
                float c = r;
                // light from upper-left
                Vector2 lightDir = new Vector2(-0.45f, 0.55f).normalized;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x + 0.5f - c) / r;
                        float dy = (y + 0.5f - c) / r;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01((1f - dist) * r);
                        if (a <= 0f) { tex.SetPixel(x, y, new Color(1, 1, 1, 0)); continue; }
                        // fake sphere normal for shading
                        float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - dist * dist));
                        float lit = Mathf.Clamp01(Vector2.Dot(new Vector2(dx, dy), lightDir) * -1f * 0.5f + nz * 0.6f + 0.35f);
                        // subtle specular
                        Vector2 hl = new Vector2(-0.35f, 0.42f);
                        float spec = Mathf.Clamp01(1f - new Vector2(dx - hl.x, dy - hl.y).magnitude * 2.4f);
                        float shade = Mathf.Lerp(0.55f, 1.08f, lit);
                        Color rgb = new Color(shade, shade, shade, a);
                        rgb.r = Mathf.Clamp01(rgb.r + spec * 0.9f);
                        rgb.g = Mathf.Clamp01(rgb.g + spec * 0.9f);
                        rgb.b = Mathf.Clamp01(rgb.b + spec * 0.9f);
                        tex.SetPixel(x, y, rgb);
                    }
                tex.Apply();
                return ToSprite(tex);
            });
        }

        public static Sprite RoundedRect(int w = 128, int h = 128, int radius = 28)
        {
            string key = "rr" + w + "_" + h + "_" + radius;
            return Cached(key, () =>
            {
                var tex = NewTex(w, h);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        tex.SetPixel(x, y, new Color(1, 1, 1, RoundedAlpha(x, y, w, h, radius)));
                tex.Apply();
                return ToSprite(tex);
            });
        }

        /// <summary>9-slice rounded rect for UI panels/buttons. Border in pixels.</summary>
        public static Sprite RoundedRectUI(int size = 96, int radius = 28)
        {
            string key = "rrui" + size + "_" + radius;
            return Cached(key, () =>
            {
                var tex = NewTex(size, size);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        tex.SetPixel(x, y, new Color(1, 1, 1, RoundedAlpha(x, y, size, size, radius)));
                tex.Apply();
                float b = radius + 2;
                return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PPU, 0,
                    SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            });
        }

        public static Sprite Square(int size = 8)
        {
            return Cached("sq" + size, () =>
            {
                var tex = NewTex(size, size);
                var px = new Color[size * size];
                for (int i = 0; i < px.Length; i++) px[i] = Color.white;
                tex.SetPixels(px);
                tex.Apply();
                return ToSprite(tex);
            });
        }

        public static Sprite Ring(int size = 128, float thickness = 0.16f)
        {
            string key = "ring" + size + "_" + thickness.ToString("0.00");
            return Cached(key, () =>
            {
                var tex = NewTex(size, size);
                float r = size * 0.5f;
                float inner = r * (1f - thickness);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float d = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r));
                        float aOuter = Mathf.Clamp01(r - d);
                        float aInner = Mathf.Clamp01(d - inner);
                        tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Min(aOuter, aInner)));
                    }
                tex.Apply();
                return ToSprite(tex);
            });
        }

        /// <summary>
        /// A "candy" tile: rounded square covered in a grid of puffy bumps (push-pop / bubble-wrap
        /// look), with shading baked into RGB so a flat-tinted SpriteRenderer still reads as glossy.
        /// </summary>
        public static Sprite BubbleTile(int size = 128, int bumps = 3)
        {
            string key = "bubble" + size + "_" + bumps;
            return Cached(key, () =>
            {
                var tex = NewTex(size, size);
                int radius = Mathf.RoundToInt(size * 0.2f);
                float cell = size / (float)bumps;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float a = RoundedAlpha(x, y, size, size, radius);
                        if (a <= 0f) { tex.SetPixel(x, y, new Color(1, 1, 1, 0)); continue; }
                        float u = ((x % cell) / cell);
                        float v = ((y % cell) / cell);
                        float dx = (u - 0.5f) * 2f;
                        float dy = (v - 0.5f) * 2f;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float domed = Mathf.Clamp01(1f - dist);
                        float lit = (-dx + dy) * 0.5f;           // light from top-left
                        float shade = 0.86f + domed * 0.16f + lit * domed * 0.30f;
                        if (dist > 0.86f) shade -= (dist - 0.86f) * 1.4f; // grooves between bumps
                        shade = Mathf.Clamp(shade, 0.42f, 1.18f);
                        tex.SetPixel(x, y, new Color(shade, shade, shade, a));
                    }
                tex.Apply();
                return ToSprite(tex);
            });
        }

        /// <summary>Belt end-roller / recycle disc: a shaded circle (like a metal roller).</summary>
        public static Sprite Roller(int size = 96)
        {
            return Cached("roller" + size, () =>
            {
                var tex = NewTex(size, size);
                float r = size * 0.5f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x + 0.5f - r) / r;
                        float dy = (y + 0.5f - r) / r;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01((1f - dist) * r);
                        float shade = Mathf.Lerp(1.05f, 0.55f, Mathf.Clamp01(dist));
                        tex.SetPixel(x, y, new Color(shade, shade, shade, a));
                    }
                tex.Apply();
                return ToSprite(tex);
            });
        }

        /// <summary>A small stylised globe (recycle marker) — blue disc with lat/long lines.</summary>
        public static Sprite Globe(int size = 96)
        {
            return Cached("globe" + size, () =>
            {
                var tex = NewTex(size, size);
                float r = size * 0.5f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x + 0.5f - r) / r;
                        float dy = (y + 0.5f - r) / r;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01((1f - dist) * r);
                        if (a <= 0f) { tex.SetPixel(x, y, new Color(1, 1, 1, 0)); continue; }
                        float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - dist * dist));
                        float shade = 0.7f + nz * 0.4f;
                        // lat/long grid (lighter lines)
                        float lat = Mathf.Abs(Mathf.Sin(dy * 6f));
                        float lon = Mathf.Abs(Mathf.Sin(dx * 6f * (0.4f + nz)));
                        if (lat > 0.93f || lon > 0.93f) shade += 0.25f;
                        shade = Mathf.Clamp(shade, 0.4f, 1.2f);
                        tex.SetPixel(x, y, new Color(shade, shade, shade, a));
                    }
                tex.Apply();
                return ToSprite(tex);
            });
        }

        public static Sprite Star(int size = 128, int points = 5)
        {
            string key = "star" + size + "_" + points;
            return Cached(key, () =>
            {
                var tex = NewTex(size, size);
                float cx = size * 0.5f, cy = size * 0.5f;
                float outer = size * 0.48f, inner = outer * 0.46f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                        float ang = Mathf.Atan2(dy, dx);
                        float seg = Mathf.PI / points;
                        float a = Mathf.Repeat(ang + Mathf.PI / 2f, seg * 2f);
                        float t = Mathf.Abs(a - seg) / seg;         // 0 at spike, 1 at valley
                        float radius = Mathf.Lerp(outer, inner, t);
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(radius - dist);
                        tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                tex.Apply();
                return ToSprite(tex);
            });
        }

        /// <summary>A cog / gear (tintable, alpha only).</summary>
        public static Sprite Gear(int size = 96, int teeth = 8)
        {
            string key = "gear" + size + "_" + teeth;
            return Cached(key, () =>
            {
                var tex = NewTex(size, size);
                float c = size * 0.5f;
                float outer = size * 0.46f, valley = size * 0.36f, hole = size * 0.16f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - c, dy = y + 0.5f - c;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Atan2(dy, dx);
                        float toothR = (Mathf.Cos(a * teeth) > 0f) ? outer : valley;
                        float alpha = Mathf.Clamp01(toothR - r) * Mathf.Clamp01(r - hole);
                        tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                tex.Apply();
                return ToSprite(tex);
            });
        }

        /// <summary>Horseshoe magnet with red body + blue pole tips (colours baked; use white tint).</summary>
        public static Sprite Magnet(int size = 96)
        {
            return Cached("magnet" + size, () =>
            {
                var tex = NewTex(size, size);
                float c = size * 0.5f;
                float outer = size * 0.44f, inner = size * 0.24f;
                Color red = new Color(0.92f, 0.28f, 0.30f, 1f);
                Color blue = new Color(0.30f, 0.55f, 0.95f, 1f);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - c, dy = y + 0.5f - c;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg; // -180..180, up = +90
                        bool inRing = r < outer && r > inner;
                        bool inGap = a > 50f && a < 130f;             // opening faces up
                        float alpha = (inRing && !inGap) ? Mathf.Clamp01(Mathf.Min(outer - r, r - inner)) : 0f;
                        if (alpha <= 0f) { tex.SetPixel(x, y, new Color(0, 0, 0, 0)); continue; }
                        float yn = dy / outer;
                        Color col = (yn > 0.32f) ? blue : red;       // top ends are the poles
                        col.a = alpha;
                        tex.SetPixel(x, y, col);
                    }
                tex.Apply();
                return ToSprite(tex);
            });
        }

        /// <summary>Two interlocking orange rings (stands in for the tangled tool icon).</summary>
        public static Sprite KnotTool(int size = 96)
        {
            return Cached("knot" + size, () =>
            {
                var tex = NewTex(size, size);
                Color orange = new Color(0.98f, 0.62f, 0.22f, 1f);
                Vector2[] centers = { new Vector2(size * 0.38f, size * 0.5f), new Vector2(size * 0.62f, size * 0.5f) };
                float R = size * 0.26f, thick = size * 0.10f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float best = 0f;
                        foreach (var ctr in centers)
                        {
                            float d = Mathf.Sqrt((x + 0.5f - ctr.x) * (x + 0.5f - ctr.x) + (y + 0.5f - ctr.y) * (y + 0.5f - ctr.y));
                            float ring = Mathf.Clamp01(thick - Mathf.Abs(d - R));
                            best = Mathf.Max(best, ring);
                        }
                        tex.SetPixel(x, y, new Color(orange.r, orange.g, orange.b, best));
                    }
                tex.Apply();
                return ToSprite(tex);
            });
        }

        /// <summary>A gumball machine: glass dome with colourful gumballs + a base (colours baked).</summary>
        public static Sprite Gumball(int size = 96)
        {
            return Cached("gumball" + size, () =>
            {
                var tex = NewTex(size, size);
                float c = size * 0.5f;
                float domeCy = size * 0.6f, domeR = size * 0.34f;
                Color glass = new Color(0.72f, 0.88f, 0.95f, 0.55f);
                Color baseCol = new Color(0.90f, 0.30f, 0.32f, 1f);
                Color[] balls = {
                    new Color(0.95f,0.34f,0.38f,1f), new Color(0.30f,0.58f,0.98f,1f),
                    new Color(0.44f,0.83f,0.35f,1f), new Color(0.99f,0.82f,0.26f,1f),
                    new Color(0.74f,0.46f,0.94f,1f) };
                Vector2[] bpos = {
                    new Vector2(c-domeR*0.4f, domeCy+domeR*0.2f), new Vector2(c+domeR*0.35f, domeCy+domeR*0.25f),
                    new Vector2(c, domeCy-domeR*0.1f), new Vector2(c-domeR*0.35f, domeCy-domeR*0.3f),
                    new Vector2(c+domeR*0.4f, domeCy-domeR*0.25f) };
                float br = domeR * 0.3f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float px = x + 0.5f, py = y + 0.5f;
                        Color outc = new Color(0, 0, 0, 0);
                        // base (lower rounded rect)
                        if (py < size * 0.34f)
                        {
                            float ba = RoundedAlpha(x, y, size, (int)(size * 0.34f), (int)(size * 0.1f));
                            float inset = Mathf.Clamp01((px - size * 0.28f)) * Mathf.Clamp01((size * 0.72f - px));
                            if (px > size * 0.28f && px < size * 0.72f) outc = new Color(baseCol.r, baseCol.g, baseCol.b, ba);
                        }
                        // dome glass
                        float dd = Mathf.Sqrt((px - c) * (px - c) + (py - domeCy) * (py - domeCy));
                        if (dd < domeR) outc = glass;
                        // gumballs
                        for (int i = 0; i < bpos.Length; i++)
                        {
                            float bd = Mathf.Sqrt((px - bpos[i].x) * (px - bpos[i].x) + (py - bpos[i].y) * (py - bpos[i].y));
                            if (bd < br) { var col = balls[i]; col.a = 1f; outc = col; }
                        }
                        tex.SetPixel(x, y, outc);
                    }
                tex.Apply();
                return ToSprite(tex);
            });
        }

        // ---- helpers -----------------------------------------------------

        private static float RoundedAlpha(int x, int y, int w, int h, int radius)
        {
            float r = Mathf.Min(radius, Mathf.Min(w, h) * 0.5f);
            float px = x + 0.5f, py = y + 0.5f;
            float qx = Mathf.Max(Mathf.Max(r - px, px - (w - r)), 0f);
            float qy = Mathf.Max(Mathf.Max(r - py, py - (h - r)), 0f);
            float d = Mathf.Sqrt(qx * qx + qy * qy);
            return Mathf.Clamp01(r - d + 1f) * (qx == 0f && qy == 0f ? 1f : Mathf.Clamp01(r - d + 0.5f));
        }

        private static Texture2D NewTex(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        private static Sprite ToSprite(Texture2D tex)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), PPU);
        }
    }
}
