using UnityEngine;
using UnityEngine.UI;

namespace PoseGame.UI
{
    // Small helpers for building UI purely from code, so the whole game can live in one
    // bootstrap component without hand-authored prefabs or scene wiring.
    public static class UIFactory
    {
        static Sprite s_WhiteSprite;
        public static Sprite WhiteSprite
        {
            get
            {
                if (s_WhiteSprite == null)
                {
                    var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = "PoseGameWhite" };
                    var pixels = new Color32[16];
                    for (var i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                    tex.SetPixels32(pixels);
                    tex.Apply();
                    s_WhiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
                }
                return s_WhiteSprite;
            }
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image CreatePanel(string name, Transform parent, Color color)
        {
            var rt = CreateRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = WhiteSprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        public static Image CreateDot(string name, Transform parent, Color color, float size)
        {
            var rt = CreateRect(name, parent);
            rt.sizeDelta = new Vector2(size, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = WhiteSprite;
            img.color = color;
            return img;
        }

        static Font s_JapaneseFont;
        // Legacy UI Text with a dynamic OS font, chosen so Japanese glyphs render correctly
        // without needing the TextMeshPro "Import TMP Essentials" one-time project setup step.
        public static Font JapaneseFont
        {
            get
            {
                if (s_JapaneseFont == null)
                {
                    var candidates = new[]
                    {
                        "Hiragino Sans", "Hiragino Kaku Gothic ProN", "Yu Gothic", "Meiryo",
                        "Noto Sans CJK JP", "Noto Sans JP", "MS Gothic", "Arial Unicode MS", "Arial",
                    };
                    s_JapaneseFont = Font.CreateDynamicFontFromOSFont(candidates, 32);
                }
                return s_JapaneseFont;
            }
        }

        public static Text CreateText(string name, Transform parent, string text, float fontSize, Color color, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
        {
            var rt = CreateRect(name, parent);
            var txt = rt.gameObject.AddComponent<Text>();
            txt.font = JapaneseFont;
            txt.text = text;
            txt.fontSize = Mathf.RoundToInt(fontSize);
            txt.color = color;
            txt.alignment = align;
            txt.fontStyle = style;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            return txt;
        }

        public static Button CreateButton(string name, Transform parent, string label, Color bg, Color textColor, float fontSize = 32f)
        {
            var img = CreatePanel(name, parent, bg);
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = Color.Lerp(bg, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(bg, Color.black, 0.15f);
            btn.colors = colors;

            var text = CreateText(name + "_Label", img.transform, label, fontSize, textColor);
            Stretch((RectTransform)text.transform);
            return btn;
        }

        public static RawImage CreateRawImage(string name, Transform parent, Texture tex)
        {
            var rt = CreateRect(name, parent);
            var raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.color = Color.white;
            return raw;
        }
    }
}
