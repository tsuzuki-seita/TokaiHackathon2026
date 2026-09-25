using UnityEngine;
using UnityEngine.EventSystems;
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

        const int RoundedSpriteSize = 64;
        const int RoundedSpriteRadius = 22;

        static Sprite s_RoundedSprite;
        // A 9-sliced rounded-rect mask, generated once at runtime, so panels/buttons can have
        // soft corners without needing an imported image asset (matches the "丸こい・ポップ" look).
        public static Sprite RoundedSprite
        {
            get
            {
                if (s_RoundedSprite == null)
                {
                    const int size = RoundedSpriteSize;
                    const float radius = RoundedSpriteRadius;
                    var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
                    {
                        name = "PoseGameRounded",
                        filterMode = FilterMode.Bilinear,
                    };
                    var pixels = new Color32[size * size];
                    for (var y = 0; y < size; y++)
                    {
                        for (var x = 0; x < size; x++)
                        {
                            var px = x + 0.5f;
                            var py = y + 0.5f;
                            var cx = Mathf.Clamp(px, radius, size - radius);
                            var cy = Mathf.Clamp(py, radius, size - radius);
                            var dist = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                            var alpha = Mathf.Clamp01(radius - dist + 0.5f);
                            pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255));
                        }
                    }
                    tex.SetPixels32(pixels);
                    tex.Apply();
                    var border = new Vector4(RoundedSpriteRadius, RoundedSpriteRadius, RoundedSpriteRadius, RoundedSpriteRadius);
                    s_RoundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
                }
                return s_RoundedSprite;
            }
        }

        public static Image CreateRoundedPanel(string name, Transform parent, Color color)
        {
            var rt = CreateRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = RoundedSprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
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
        // Bundled round/pop-style font (Mochiy Pop One, SIL OFL — see Resources/Fonts/OFL.txt)
        // so the title/UI look is consistent across machines instead of depending on whichever
        // OS fonts happen to be installed. Falls back to a dynamic OS font if it's ever missing.
        public static Font JapaneseFont
        {
            get
            {
                if (s_JapaneseFont == null)
                {
                    s_JapaneseFont = Resources.Load<Font>("Fonts/MochiyPopOne-Regular");
                    if (s_JapaneseFont == null)
                    {
                        var candidates = new[]
                        {
                            "Hiragino Sans", "Hiragino Kaku Gothic ProN", "Yu Gothic", "Meiryo",
                            "Noto Sans CJK JP", "Noto Sans JP", "MS Gothic", "Arial Unicode MS", "Arial",
                        };
                        s_JapaneseFont = Font.CreateDynamicFontFromOSFont(candidates, 32);
                    }
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

        const float ButtonShadowDepth = 10f;

        // A rounded, chunky "pop" button: a darker rounded rect sits underneath as a shadow, and
        // the lighter face rect sits on top with its bottom lifted by ButtonShadowDepth so the
        // shadow peeks out — pressing drops the face flush onto the shadow for a tactile feel.
        // Button/EventTrigger live on the outer `root` (the returned transform) rather than the
        // face, so callers can keep positioning the whole button via `((RectTransform)btn.transform)`
        // exactly as before, while clicks on the face still bubble up to root's handlers.
        public static Button CreateButton(string name, Transform parent, string label, Color bg, Color textColor, float fontSize = 32f)
        {
            var root = CreateRect(name, parent);

            var shadow = CreateRoundedPanel(name + "_Shadow", root, Color.Lerp(bg, Color.black, 0.35f));
            Stretch((RectTransform)shadow.transform);
            shadow.raycastTarget = false;

            var face = CreateRoundedPanel(name + "_Face", root, bg);
            var faceRt = (RectTransform)face.transform;
            faceRt.anchorMin = Vector2.zero;
            faceRt.anchorMax = Vector2.one;
            faceRt.offsetMin = new Vector2(0, ButtonShadowDepth);
            faceRt.offsetMax = Vector2.zero;

            var text = CreateText(name + "_Label", face.transform, label, fontSize, textColor);
            Stretch((RectTransform)text.transform);

            var btn = root.gameObject.AddComponent<Button>();
            btn.targetGraphic = face;
            var colors = btn.colors;
            colors.highlightedColor = Color.Lerp(bg, Color.white, 0.15f);
            colors.pressedColor = bg; // pressed feedback is the drop animation below, not a color change
            btn.colors = colors;

            var trigger = root.gameObject.AddComponent<EventTrigger>();
            AddTriggerEntry(trigger, EventTriggerType.PointerDown, _ => faceRt.offsetMin = Vector2.zero);
            AddTriggerEntry(trigger, EventTriggerType.PointerUp, _ => faceRt.offsetMin = new Vector2(0, ButtonShadowDepth));
            AddTriggerEntry(trigger, EventTriggerType.PointerExit, _ => faceRt.offsetMin = new Vector2(0, ButtonShadowDepth));

            return btn;
        }

        static void AddTriggerEntry(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
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
