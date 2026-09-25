using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ContenaVolleyのUIをコードから組み立てるためのヘルパー
/// </summary>
public static class CV_UIUtil
{
    static Sprite circle;
    static Sprite ring;
    static Font font;

    public static Sprite Circle => circle != null ? circle : circle = MakeCircleSprite(256, 0f);
    public static Sprite Ring => ring != null ? ring : ring = MakeCircleSprite(256, 0.82f);

    /// <summary>日本語が表示できるOSフォント（Mac/Windows両対応）</summary>
    public static Font DefaultFont
    {
        get
        {
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Hiragino Sans", "Hiragino Kaku Gothic ProN", "Yu Gothic", "Meiryo", "Arial" }, 64);
            }
            return font;
        }
        set => font = value;
    }

    /// <summary>アンチエイリアス付きの白い円(innerRatio>0でリング)</summary>
    static Sprite MakeCircleSprite(int size, float innerRatio)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];
        float r = size * 0.5f;
        float inner = r * innerRatio;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                float a = Mathf.Clamp01(r - d);
                if (innerRatio > 0) a *= Mathf.Clamp01(d - inner);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
    }

    public static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    /// <summary>親いっぱいに広がるRect</summary>
    public static RectTransform CreateFullRect(string name, Transform parent)
    {
        var rt = CreateRect(name, parent);
        Stretch(rt);
        return rt;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, Vector2 size)
    {
        var rt = CreateRect(name, parent);
        rt.sizeDelta = size;
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static Text CreateText(string name, Transform parent, int fontSize, Color color, TextAnchor align = TextAnchor.MiddleCenter)
    {
        var rt = CreateRect(name, parent);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = DefaultFont;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = align;
        t.fontStyle = FontStyle.Bold;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;

        var outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.75f);
        outline.effectDistance = new Vector2(3, -3);
        return t;
    }

    /// <summary>a から b への線（細長いImage）</summary>
    public static Image CreateLine(Transform parent, Vector2 a, Vector2 b, float thickness, Color color)
    {
        var rt = CreateRect("Line", parent);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = a;
        var d = b - a;
        rt.sizeDelta = new Vector2(d.magnitude, thickness);
        rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }
}
