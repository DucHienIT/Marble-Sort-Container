using UnityEngine;
using UnityEngine.UI;
using MarbleSort.Config;

namespace MarbleSort.Utils
{
    /// <summary>
    /// Builds uGUI hierarchies in code. Uses legacy <see cref="Text"/> with the built-in
    /// LegacyRuntime font (no TextMeshPro Essential Resources dependency). Candy-styled panels
    /// and buttons via SpriteFactory 9-slice sprites.
    /// </summary>
    public static class UIFactory
    {
        private static Font _font;
        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _font;
            }
        }

        public static RectTransform Rect(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            return rt;
        }

        public static GameObject Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>Stretch a RectTransform to fill its parent with optional padding.</summary>
        public static RectTransform Fill(RectTransform rt, float pad = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
            return rt;
        }

        public static RectTransform Anchor(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Panel(Transform parent, string name, Color color, int radius = 32)
        {
            var go = Node(name, parent);
            var img = go.AddComponent<Image>();
            img.sprite = SpriteFactory.RoundedRectUI(96, radius);
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        public static Image RawImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = Node(name, parent);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            return img;
        }

        public static Text Label(Transform parent, string name, string text, int fontSize,
            Color color, TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
        {
            var go = Node(name, parent);
            var t = go.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>
        /// A candy button: rounded body + baked shadow + centred label. Returns the Button; the
        /// label is child "Label". Caller positions the returned RectTransform.
        /// </summary>
        public static Button Button(Transform parent, string name, string label, Color color,
            int fontSize = 44, int radius = 40)
        {
            var go = Node(name, parent);
            var shadow = go.AddComponent<Image>();
            shadow.sprite = SpriteFactory.RoundedRectUI(96, radius);
            shadow.type = Image.Type.Sliced;
            shadow.color = Palette.UiShadow;

            var bodyGo = Node("Body", go.transform);
            Fill(Rect(bodyGo));
            var body = bodyGo.GetComponent<RectTransform>();
            body.offsetMin = new Vector2(0, 8);
            body.offsetMax = new Vector2(0, 8);
            var bodyImg = bodyGo.AddComponent<Image>();
            bodyImg.sprite = SpriteFactory.RoundedRectUI(96, radius);
            bodyImg.type = Image.Type.Sliced;
            bodyImg.color = color;

            var lbl = Label(bodyGo.transform, "Label", label, fontSize, Palette.UiText);
            Fill(Rect(lbl.gameObject));

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bodyImg;
            var colors = btn.colors;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.fadeDuration = 0.06f;
            btn.colors = colors;
            return btn;
        }

        /// <summary>Full-screen dark overlay (for popups). Returns its Image (raycast blocker).</summary>
        public static Image Overlay(Transform parent, string name)
        {
            var img = RawImage(parent, name, SpriteFactory.Square(4), new Color(0f, 0f, 0f, 0.6f));
            Fill(Rect(img.gameObject));
            img.raycastTarget = true;
            return img;
        }
    }
}
