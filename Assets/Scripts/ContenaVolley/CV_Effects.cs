using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 画面全体の演出（フラッシュ・画面揺れ・ヒットストップ・パーティクル）。
/// Kenney Particle Pack のスプライトを使う。位置はすべてワールド座標で受け取る
/// </summary>
public class CV_Effects : MonoBehaviour
{
    static readonly Color[] ConfettiColors =
    {
        new Color(1f, 0.35f, 0.4f), new Color(0.3f, 0.75f, 1f), new Color(1f, 0.85f, 0.2f),
        new Color(0.4f, 1f, 0.55f), new Color(0.85f, 0.5f, 1f), Color.white,
    };
    static readonly Color Gold = new Color(1f, 0.85f, 0.3f);

    CV_UIParticles particles;
    Image flash;
    RectTransform[] shakeTargets;
    Coroutine hitStop;
    Coroutine rain;

    public CV_UIParticles Particles => particles;

    /// <summary>shake: 画面揺れで動かすRect（アンカー位置が0のもの）</summary>
    public void Build(params RectTransform[] shake)
    {
        shakeTargets = shake;

        flash = CV_UIUtil.CreateImage("Flash", transform, null, new Color(1, 1, 1, 0), Vector2.zero);
        CV_UIUtil.Stretch(flash.rectTransform);

        particles = CV_UIUtil.CreateFullRect("Particles", transform).gameObject.AddComponent<CV_UIParticles>();
    }

    /// <summary>画面の端を暗くして、カメラ映像を落ち着いた雰囲気にする</summary>
    public static Image BuildVignette(Transform parent)
    {
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1.45f, d)) * 0.7f;
                px[y * size + x] = new Color32(5, 10, 30, (byte)(a * 255));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        var img = CV_UIUtil.CreateImage("CV_Vignette", parent, Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f), Color.white, Vector2.zero);
        CV_UIUtil.Stretch(img.rectTransform);
        return img;
    }

    void OnDisable()
    {
        Time.timeScale = 1f;
    }

    // ───────────────────────── 基本 ─────────────────────────

    public void Flash(Color color, float alpha, float duration)
    {
        flash.DOKill();
        flash.color = new Color(color.r, color.g, color.b, alpha);
        flash.DOFade(0f, duration).SetUpdate(true).SetLink(flash.gameObject);
    }

    public void Shake(float strength, float duration)
    {
        foreach (var t in shakeTargets)
        {
            if (t == null) continue;
            t.DOKill(true);
            t.DOShakeAnchorPos(duration, strength, 35, 90, false, true).SetLink(t.gameObject);
        }
    }

    /// <summary>一瞬だけ時間を止めて「当たった！」手応えを出す</summary>
    public void HitStop(float seconds)
    {
        if (hitStop != null) StopCoroutine(hitStop);
        hitStop = StartCoroutine(HitStopRoutine(seconds));
    }

    IEnumerator HitStopRoutine(float seconds)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(seconds);
        Time.timeScale = 1f;
        hitStop = null;
    }

    public void ResetAll()
    {
        if (hitStop != null) StopCoroutine(hitStop);
        if (rain != null) StopCoroutine(rain);
        hitStop = rain = null;
        Time.timeScale = 1f;
        particles.Clear();
        flash.DOKill();
        flash.color = new Color(1, 1, 1, 0);
    }

    Vector2 L(Vector3 world) => particles.ToLocal(world);

    // ───────────────────────── ゲームの演出 ─────────────────────────

    /// <summary>スパイクが決まった瞬間</summary>
    public void SpikeHit(Vector3 world, Color color, bool perfect)
    {
        var p = L(world);
        var white = Color.white;

        particles.Play(p, new CV_UIParticles.Emit
        {
            sprite = CV_Assets.Fx("scorch_01"), count = 1, speed = Vector2.zero,
            size = Vector2.one * (perfect ? 520 : 400), endScale = 1.5f, life = Vector2.one * 0.28f,
            colorA = white, endColor = new Color(color.r, color.g, color.b, 0f), spin = new Vector2(-60, 60),
        });
        particles.Play(p, new CV_UIParticles.Emit
        {
            sprite = CV_Assets.Fx("circle_02"), count = perfect ? 2 : 1, speed = Vector2.zero,
            size = new Vector2(140, 180), endScale = perfect ? 5f : 3.6f, life = new Vector2(0.35f, 0.45f),
            colorA = color, colorB = white, spin = Vector2.zero,
        });
        particles.Play(p, new CV_UIParticles.Emit
        {
            sprite = CV_Assets.Fx("star_06"), count = perfect ? 26 : 16, speed = new Vector2(500, 1300),
            size = new Vector2(50, 110), life = new Vector2(0.4f, 0.75f), drag = 3f, gravity = 700f,
            colorA = white, colorB = color, spin = new Vector2(-360, 360),
        });
        particles.Play(p, new CV_UIParticles.Emit
        {
            sprite = CV_Assets.Fx("trace_07"), count = 12, speed = new Vector2(1100, 1900),
            size = new Vector2(140, 240), aspect = 0.3f, life = new Vector2(0.18f, 0.32f), drag = 4f,
            colorA = white, colorB = color, alignToVelocity = true, endScale = 0.5f,
        });

        if (perfect)
        {
            particles.Play(p, new CV_UIParticles.Emit
            {
                sprite = CV_Assets.Fx("magic_05"), count = 1, speed = Vector2.zero,
                size = Vector2.one * 700, endScale = 0.2f, life = Vector2.one * 0.35f, colorA = Gold, spin = new Vector2(90, 90),
            });
            particles.Play(p, new CV_UIParticles.Emit
            {
                sprite = CV_Assets.Fx("star_08"), count = 14, radius = 40, speed = new Vector2(300, 700),
                size = new Vector2(60, 120), life = new Vector2(0.6f, 1f), drag = 2.5f,
                colorA = Gold, colorB = white, spin = new Vector2(-200, 200),
            });
            Flash(Gold, 0.3f, 0.3f);
        }
        else
        {
            Flash(color, 0.18f, 0.2f);
        }

        Shake(perfect ? 26f : 16f, 0.3f);
        HitStop(perfect ? 0.1f : 0.065f);
    }

    /// <summary>スパイク失敗</summary>
    public void Miss(Vector3 world)
    {
        var p = L(world);
        particles.Play(p, new CV_UIParticles.Emit
        {
            sprite = CV_Assets.Fx("smoke_07"), count = 6, radius = 40, speed = new Vector2(40, 140),
            size = new Vector2(150, 250), endScale = 1.7f, life = new Vector2(0.6f, 0.95f), drag = 2f, gravity = -60f,
            colorA = new Color(0.55f, 0.58f, 0.65f, 0.7f), colorB = new Color(0.35f, 0.38f, 0.45f, 0.6f),
        });
        Shake(8f, 0.2f);
    }

    /// <summary>ボールがガラスに当たった瞬間</summary>
    public void GlassImpact(Vector3 world, Color color)
    {
        var p = L(world);
        var ice = new Color(0.8f, 0.95f, 1f);

        particles.Play(p, new CV_UIParticles.Emit
        {
            sprite = CV_Assets.Fx("star_09"), count = 1, speed = Vector2.zero,
            size = Vector2.one * 360, endScale = 1.8f, life = Vector2.one * 0.22f, colorA = Color.white, spin = Vector2.zero,
        });
        particles.Play(p, new CV_UIParticles.Emit
        {
            sprite = CV_Assets.Fx("flare_01"), count = 1, speed = Vector2.zero,
            size = Vector2.one * 900, endScale = 0.6f, life = Vector2.one * 0.3f, colorA = ice, spin = Vector2.zero,
        });
        particles.Play(p, new CV_UIParticles.Emit
        {
            sprite = CV_Assets.Fx("circle_02"), count = 1, speed = Vector2.zero,
            size = Vector2.one * 120, endScale = 4.5f, life = Vector2.one * 0.4f, colorA = ice, spin = Vector2.zero,
        });
        // 飛び散るガラスのかけら
        particles.Play(p, new CV_UIParticles.Emit
        {
            sprite = CV_Assets.Fx("window_04"), count = 20, radius = 30, speed = new Vector2(250, 750),
            size = new Vector2(16, 40), endScale = 0.8f, life = new Vector2(0.7f, 1.2f), drag = 1f, gravity = 2200f,
            colorA = new Color(0.85f, 0.95f, 1f, 0.9f), colorB = new Color(0.6f, 0.85f, 1f, 0.8f), spin = new Vector2(-720, 720),
        });
        particles.Play(p, new CV_UIParticles.Emit
        {
            sprite = CV_Assets.Fx("star_08"), count = 8, radius = 80, speed = new Vector2(20, 120),
            size = new Vector2(40, 80), endScale = 0f, life = new Vector2(0.5f, 0.9f), colorA = Color.white, colorB = color,
        });

        Flash(Color.white, 0.25f, 0.25f);
        Shake(20f, 0.3f);
        HitStop(0.05f);
    }

    /// <summary>ボールの残像</summary>
    public void BallGhost(Vector3 world, float size, float rotZ, Color color)
    {
        particles.Play(L(world), new CV_UIParticles.Emit
        {
            sprite = CV_Assets.Ball, count = 1, speed = Vector2.zero, size = Vector2.one * size, endScale = 0.7f,
            life = Vector2.one * 0.16f, colorA = new Color(color.r, color.g, color.b, 0.55f), spin = Vector2.zero,
        });
        particles.Play(L(world), new CV_UIParticles.Emit
        {
            sprite = CV_Assets.Fx("star_06"), count = 1, radius = size * 0.3f, speed = new Vector2(30, 120),
            size = new Vector2(20, 45), life = new Vector2(0.25f, 0.45f), colorA = Color.white, colorB = color,
        });
    }

    /// <summary>ガラスが粉々に割れる瞬間</summary>
    public void ShatterBurst(RectTransform area)
    {
        var r = area.rect;
        var center = L(area.TransformPoint(r.center));
        float w = r.width, h = r.height;

        for (int i = 0; i < 6; i++)
        {
            var at = center + new Vector2(Random.Range(-0.4f, 0.4f) * w, Random.Range(-0.35f, 0.35f) * h);
            particles.Play(at, new CV_UIParticles.Emit
            {
                sprite = CV_Assets.Fx("star_09"), count = 1, speed = Vector2.zero, delay = i * 0.06f,
                size = Vector2.one * Random.Range(300, 520), endScale = 1.6f, life = Vector2.one * 0.3f, colorA = Color.white, spin = Vector2.zero,
            });
        }
        particles.Play(center, new CV_UIParticles.Emit
        {
            sprite = CV_Assets.Fx("window_04"), count = 70, radius = 250, speed = new Vector2(500, 1700),
            size = new Vector2(20, 60), endScale = 0.8f, life = new Vector2(0.9f, 1.6f), drag = 0.8f, gravity = 2400f,
            colorA = new Color(0.85f, 0.95f, 1f, 0.95f), colorB = new Color(0.6f, 0.85f, 1f, 0.8f), spin = new Vector2(-900, 900),
        });
        particles.Play(center, new CV_UIParticles.Emit
        {
            sprite = CV_Assets.Fx("star_08"), count = 50, radius = Mathf.Min(w, h) * 0.5f, speed = new Vector2(30, 200),
            size = new Vector2(40, 90), endScale = 0f, life = new Vector2(0.8f, 1.6f), gravity = 250f,
            colorA = Color.white, colorB = new Color(0.7f, 0.9f, 1f),
        });
        particles.Play(center, new CV_UIParticles.Emit
        {
            sprite = CV_Assets.Fx("circle_02"), count = 1, speed = Vector2.zero,
            size = Vector2.one * 300, endScale = 8f, life = Vector2.one * 0.6f, colorA = Color.white, spin = Vector2.zero,
        });

        Shake(45f, 0.7f);
    }

    /// <summary>左右下から紙吹雪がはじける</summary>
    public void ConfettiBurst(RectTransform area)
    {
        var r = area.rect;
        var left = L(area.TransformPoint(new Vector2(r.xMin + 60, r.yMin)));
        var right = L(area.TransformPoint(new Vector2(r.xMax - 60, r.yMin)));
        foreach (var c in ConfettiColors)
        {
            var e = Confetti(c, 12);
            e.angle = 68; particles.Play(left, e);
            e.angle = 112; particles.Play(right, e);
        }
        particles.Play(left, new CV_UIParticles.Emit
        {
            sprite = CV_Assets.Fx("star_06"), count = 12, angle = 65, spread = 30, speed = new Vector2(1100, 2000),
            size = new Vector2(50, 100), life = new Vector2(1f, 1.6f), drag = 1.2f, gravity = 900f, colorA = Gold, colorB = Color.white,
        });
        particles.Play(right, new CV_UIParticles.Emit
        {
            sprite = CV_Assets.Fx("star_06"), count = 12, angle = 115, spread = 30, speed = new Vector2(1100, 2000),
            size = new Vector2(50, 100), life = new Vector2(1f, 1.6f), drag = 1.2f, gravity = 900f, colorA = Gold, colorB = Color.white,
        });
    }

    /// <summary>上から紙吹雪が降り続ける（StopConfettiRainで止める）</summary>
    public void StartConfettiRain(RectTransform area)
    {
        if (rain != null) StopCoroutine(rain);
        rain = StartCoroutine(RainRoutine(area));
    }

    public void StopConfettiRain()
    {
        if (rain != null) StopCoroutine(rain);
        rain = null;
    }

    IEnumerator RainRoutine(RectTransform area)
    {
        while (true)
        {
            var r = area.rect;
            var at = L(area.TransformPoint(new Vector2(Random.Range(r.xMin, r.xMax), r.yMax + 40)));
            var e = Confetti(ConfettiColors[Random.Range(0, ConfettiColors.Length)], 2);
            e.angle = -90; e.spread = 40; e.speed = new Vector2(100, 250); e.gravity = 180f; e.drag = 0.3f; e.life = new Vector2(4.5f, 6f);
            particles.Play(at, e);
            yield return new WaitForSeconds(0.08f);
        }
    }

    static CV_UIParticles.Emit Confetti(Color c, int count) => new CV_UIParticles.Emit
    {
        sprite = null, count = count, spread = 34, speed = new Vector2(1200, 2100), size = new Vector2(18, 30), aspect = 0.6f,
        endScale = 1f, life = new Vector2(2.4f, 3.4f), drag = 1.3f, gravity = 900f, flutter = 140f,
        colorA = c, colorB = c * 0.85f, endColor = new Color(c.r, c.g, c.b, 0.6f), spin = new Vector2(-300, 300),
    };

    /// <summary>小さなキラキラ（星の獲得など）</summary>
    public void Sparkle(Vector3 world, Color color, int count = 12)
    {
        var p = L(world);
        particles.Play(p, new CV_UIParticles.Emit
        {
            sprite = CV_Assets.Fx("star_06"), count = count, speed = new Vector2(250, 650), size = new Vector2(40, 80),
            life = new Vector2(0.4f, 0.7f), drag = 3f, colorA = Color.white, colorB = color,
        });
        particles.Play(p, new CV_UIParticles.Emit
        {
            sprite = CV_Assets.Fx("circle_02"), count = 1, speed = Vector2.zero, size = Vector2.one * 100, endScale = 3f,
            life = Vector2.one * 0.35f, colorA = color, spin = Vector2.zero,
        });
    }

    /// <summary>スタート用カーソルの周りに広がる波紋</summary>
    public void Ripple(Vector3 world, float diameter, Color color)
    {
        particles.Play(L(world), new CV_UIParticles.Emit
        {
            sprite = CV_Assets.Fx("circle_02"), count = 1, speed = Vector2.zero, size = Vector2.one * diameter * 1.1f, endScale = 1.8f,
            life = Vector2.one * 0.9f, colorA = new Color(color.r, color.g, color.b, 0.8f), spin = Vector2.zero,
        });
    }
}
