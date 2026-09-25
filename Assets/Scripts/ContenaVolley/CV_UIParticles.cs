using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// uGUI 上で動く簡易パーティクル。
/// Screen Space - Overlay のキャンバスには ParticleSystem が描画されないので、Image を使い回して動かす。
/// 座標はこのコンポーネントを付けた RectTransform のローカル座標。
/// Time.deltaTime で動くので、ヒットストップ(timeScale=0)中は一緒に止まる。
/// </summary>
public class CV_UIParticles : MonoBehaviour
{
    /// <summary>放出の設定。必要なものだけオブジェクト初期化子で指定する</summary>
    public class Emit
    {
        public Sprite sprite;
        public int count = 10;
        /// <summary>放出位置のばらつき（半径）</summary>
        public float radius;
        public Vector2 speed = new Vector2(200, 600);
        /// <summary>放出方向(度)と広がり(度)。360で全方向</summary>
        public float angle = 90f, spread = 360f;
        public Vector2 life = new Vector2(0.4f, 0.8f);
        public Vector2 size = new Vector2(40, 80);
        /// <summary>寿命の最後の大きさ倍率</summary>
        public float endScale = 0.2f;
        public Color colorA = Color.white, colorB = Color.white;
        /// <summary>寿命の最後の色(アルファ含む)。nullなら元の色でアルファだけ0</summary>
        public Color? endColor;
        public float gravity;
        /// <summary>速度の減衰(1秒あたりの割合)</summary>
        public float drag = 2f;
        public Vector2 spin = new Vector2(-180, 180);
        /// <summary>trueなら進行方向に向ける（流れ星・火花向け。縦長のスプライトを想定）</summary>
        public bool alignToVelocity;
        /// <summary>横方向のゆらゆら（紙吹雪）</summary>
        public float flutter;
        /// <summary>縦横比。1以外で細長くなる</summary>
        public float aspect = 1f;
        public float delay;
    }

    class Particle
    {
        public RectTransform rt;
        public Image img;
        public Vector2 pos, vel;
        public float rot, spin, age, life, delay, size, endScale, gravity, drag, flutter, phase, aspect;
        public Color c0, c1;
        public bool align;
    }

    readonly List<Particle> active = new List<Particle>();
    readonly Stack<Particle> pool = new Stack<Particle>();
    RectTransform rect;

    public RectTransform Rect => rect != null ? rect : rect = (RectTransform)transform;

    /// <summary>ワールド座標 → このレイヤーのローカル座標</summary>
    public Vector2 ToLocal(Vector3 world) => Rect.InverseTransformPoint(world);

    public void Play(Vector2 localPos, Emit e)
    {
        for (int i = 0; i < e.count; i++)
        {
            var p = Get();
            float a = (e.angle + Random.Range(-e.spread, e.spread) * 0.5f) * Mathf.Deg2Rad;
            p.pos = localPos + Random.insideUnitCircle * e.radius;
            p.vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(e.speed.x, e.speed.y);
            p.life = Random.Range(e.life.x, e.life.y);
            p.age = 0f;
            p.delay = e.delay;
            p.size = Random.Range(e.size.x, e.size.y);
            p.endScale = e.endScale;
            p.c0 = Color.Lerp(e.colorA, e.colorB, Random.value);
            p.c1 = e.endColor ?? new Color(p.c0.r, p.c0.g, p.c0.b, 0f);
            p.gravity = e.gravity;
            p.drag = e.drag;
            p.rot = Random.Range(0f, 360f);
            p.spin = Random.Range(e.spin.x, e.spin.y);
            p.align = e.alignToVelocity;
            p.flutter = e.flutter;
            p.phase = Random.Range(0f, Mathf.PI * 2f);
            p.aspect = e.aspect;
            p.img.sprite = e.sprite;
            p.img.enabled = p.delay <= 0f;
            Apply(p);
            active.Add(p);
        }
    }

    Particle Get()
    {
        var p = pool.Count > 0 ? pool.Pop() : null;
        if (p == null)
        {
            p = new Particle();
            p.img = CV_UIUtil.CreateImage("P", Rect, null, Color.white, Vector2.zero);
            p.rt = p.img.rectTransform;
        }
        p.rt.SetAsLastSibling();
        p.img.gameObject.SetActive(true);
        return p;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            var p = active[i];
            if (p.delay > 0f)
            {
                p.delay -= dt;
                if (p.delay > 0f) continue;
                p.img.enabled = true;
            }

            p.age += dt;
            if (p.age >= p.life)
            {
                p.img.gameObject.SetActive(false);
                active.RemoveAt(i);
                pool.Push(p);
                continue;
            }

            p.vel *= Mathf.Max(0f, 1f - p.drag * dt);
            p.vel.y -= p.gravity * dt;
            p.pos += p.vel * dt;
            if (p.flutter > 0f) p.pos.x += Mathf.Sin(p.age * 6f + p.phase) * p.flutter * dt;
            p.rot += p.spin * dt;
            Apply(p);
        }
    }

    static void Apply(Particle p)
    {
        float t = p.life > 0f ? p.age / p.life : 1f;
        p.rt.anchoredPosition = p.pos;
        float s = p.size * Mathf.Lerp(1f, p.endScale, t);
        p.rt.sizeDelta = new Vector2(s * p.aspect, s);
        // 縦長のスプライト(trace系)を進行方向に向ける
        float r = p.align && p.vel.sqrMagnitude > 1f ? Mathf.Atan2(p.vel.y, p.vel.x) * Mathf.Rad2Deg - 90f : p.rot;
        p.rt.localRotation = Quaternion.Euler(0, 0, r);
        // 紙吹雪がひらひら裏返るように見せる
        if (p.flutter > 0f) p.rt.localScale = new Vector3(Mathf.Cos(p.age * 9f + p.phase), 1f, 1f);
        else p.rt.localScale = Vector3.one;
        // 最後の方でフェードアウト
        var c = Color.Lerp(p.c0, p.c1, t);
        c.a = Mathf.Lerp(p.c0.a, p.c1.a, t * t);
        p.img.color = c;
    }

    public void Clear()
    {
        foreach (var p in active)
        {
            p.img.gameObject.SetActive(false);
            pool.Push(p);
        }
        active.Clear();
    }
}
