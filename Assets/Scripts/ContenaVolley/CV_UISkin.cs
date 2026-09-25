using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// シーンに置いてあるシンプルなUIに、実行時に Kenney UI Pack の見た目を当てる。
/// （シーンやプレハブを直接書き換えずに見た目だけ差し替える）
/// </summary>
public static class CV_UISkin
{
    public static readonly Color Navy = new Color(0.06f, 0.1f, 0.24f);
    public static readonly Color TextDark = new Color(0.12f, 0.16f, 0.3f);

    /// <summary>全画面パネルの背景を、カメラ映像が少し透ける濃紺にする</summary>
    public static void SkinBackdrop(Image img, float alpha = 0.72f)
    {
        if (img == null) return;
        img.sprite = null;
        img.color = new Color(Navy.r, Navy.g, Navy.b, alpha);
    }

    /// <summary>ボタンを Kenney の立体ボタンにする</summary>
    public static void SkinButton(Button button, Sprite sprite, Color textColor, string clickSound = "CV_se_click")
    {
        if (button == null) return;
        if (button.targetGraphic is Image img)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1.4f;
            img.color = Color.white;
        }

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 0.92f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        colors.disabledColor = new Color(0.55f, 0.55f, 0.6f, 0.6f);
        button.colors = colors;

        foreach (var t in button.GetComponentsInChildren<Text>(true))
        {
            t.color = textColor;
            t.fontStyle = FontStyle.Bold;
            RemoveEffects(t);
            // 文字を少し上げて、ボタンの厚み(下側)の分を避ける
            t.rectTransform.offsetMin = new Vector2(t.rectTransform.offsetMin.x, 8f);
        }

        if (!button.TryGetComponent<CV_ButtonJuice>(out var juice)) juice = button.gameObject.AddComponent<CV_ButtonJuice>();
        juice.clickSound = clickSound;
    }

    /// <summary>名前入力欄</summary>
    public static void SkinInputField(InputField field)
    {
        if (field == null) return;
        if (field.targetGraphic is Image img)
        {
            img.sprite = CV_Assets.Input;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1.4f;
            img.color = Color.white;
        }
        if (field.textComponent != null) field.textComponent.color = TextDark;
        if (field.placeholder is Text ph) ph.color = new Color(TextDark.r, TextDark.g, TextDark.b, 0.45f);
        field.selectionColor = new Color(1f, 0.8f, 0.2f, 0.5f);
    }

    /// <summary>暗い背景の上に乗る白文字（黒い縁取り付き）</summary>
    /// <param name="outline">縁取りの太さ。0なら縁取り・影なしのすっきりした文字</param>
    public static void SkinLabel(Text t, Color color, int fontSize = 0, float outline = 3f)
    {
        if (t == null) return;
        t.color = color;
        if (fontSize > 0) t.fontSize = fontSize;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        RemoveEffects(t);
        if (outline > 0f) AddOutline(t.gameObject, new Color(Navy.r * 0.5f, Navy.g * 0.5f, Navy.b * 0.5f, 0.9f), outline);
    }

    /// <param name="dropShadow">下にずらした影も付けるか（付けると文字が重なって見えやすい）</param>
    public static void AddOutline(GameObject go, Color color, float distance, bool dropShadow = true)
    {
        var o = go.AddComponent<Outline>();
        o.effectColor = color;
        o.effectDistance = new Vector2(distance, -distance);
        if (!dropShadow) return;
        var s = go.AddComponent<Shadow>();
        s.effectColor = new Color(0, 0, 0, 0.45f);
        s.effectDistance = new Vector2(0, -distance * 2.5f);
    }

    static void RemoveEffects(Graphic g)
    {
        foreach (var e in g.GetComponents<Shadow>()) Object.Destroy(e); // Outline も Shadow の派生
    }

    /// <summary>ロゴ文字（英数字は Kenney フォント）。decorate=false なら縁取り・影なし</summary>
    public static Text CreateLogo(Transform parent, string text, int size, Color color, bool decorate = true)
    {
        var t = CV_UIUtil.CreateText("Logo", parent, size, color);
        t.font = CV_Assets.FontFor(text);
        t.text = text;
        RemoveEffects(t);
        if (decorate) AddOutline(t.gameObject, Navy, 6f);
        return t;
    }
}
