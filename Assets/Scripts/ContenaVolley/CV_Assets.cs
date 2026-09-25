using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resources/ContenaVolley に置いた Kenney アセット（CC0, https://kenney.nl）の読み込み。
/// シーンに参照を持たせずに使えるよう、名前で引いてキャッシュする。
/// </summary>
public static class CV_Assets
{
    const string Root = "ContenaVolley/";

    static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    static Font kenneyFont;

    // よく使うスプライト
    public static Sprite Ball => Sprite("CV_Ball");
    public static Sprite Panel => Sprite("CV_ui_panel");
    public static Sprite ButtonYellow => Sprite("CV_ui_button_yellow");
    public static Sprite ButtonBlue => Sprite("CV_ui_button_blue");
    public static Sprite ButtonGreen => Sprite("CV_ui_button_green");
    public static Sprite ButtonRound => Sprite("CV_ui_button_round");
    public static Sprite Input => Sprite("CV_ui_input");
    public static Sprite StarFull => Sprite("CV_ui_star");
    public static Sprite StarEmpty => Sprite("CV_ui_star_empty");

    /// <summary>パーティクル用（白＋アルファなので color で自由に色を付けられる）</summary>
    public static Sprite Fx(string name) => Sprite("CV_fx_" + name);

    public static Sprite Sprite(string name)
    {
        if (sprites.TryGetValue(name, out var s)) return s;
        s = Resources.Load<Sprite>(Root + "Sprites/" + name);
        if (s == null) Debug.LogWarning($"[CV] スプライトが見つかりません: {name}");
        sprites[name] = s;
        return s;
    }

    public static AudioClip Clip(string name)
    {
        if (clips.TryGetValue(name, out var c)) return c;
        c = Resources.Load<AudioClip>(Root + "Audio/" + name);
        if (c == null) Debug.LogWarning($"[CV] 音が見つかりません: {name}");
        clips[name] = c;
        return c;
    }

    /// <summary>英数字用のポップなフォント（Kenney Future）。日本語は入っていない</summary>
    public static Font KenneyFont
    {
        get
        {
            if (kenneyFont == null) kenneyFont = Resources.Load<Font>(Root + "Fonts/CV_KenneyFuture");
            return kenneyFont != null ? kenneyFont : CV_UIUtil.DefaultFont;
        }
    }

    /// <summary>英数字だけなら Kenney フォント、日本語を含むなら通常フォント</summary>
    public static Font FontFor(string text)
    {
        foreach (char c in text)
        {
            if (c > 0x7E) return CV_UIUtil.DefaultFont;
        }
        return KenneyFont;
    }
}
