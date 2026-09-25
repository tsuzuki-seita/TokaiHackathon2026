using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 画面手前のガラス。ボールが当たるとヒビが入り、最後に粉々に割れる
/// </summary>
public class CV_GlassView : MonoBehaviour
{
    [SerializeField] Color tintColor = new Color(0.75f, 0.9f, 1f, 0.07f);
    [SerializeField] Color frameColor = new Color(0.85f, 0.95f, 1f, 0.5f);
    [SerializeField] Color crackColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] Color shardColor = new Color(0.8f, 0.93f, 1f, 0.45f);
    [SerializeField] int shardColumns = 8;
    [SerializeField] int shardRows = 5;

    RectTransform rect;
    RectTransform crackLayer;
    CanvasGroup glassGroup;
    Image flash;
    readonly List<GameObject> shards = new List<GameObject>();

    public RectTransform Rect => rect;

    public void Build()
    {
        rect = (RectTransform)transform;

        var glass = CV_UIUtil.CreateFullRect("Glass", rect);
        glassGroup = glass.gameObject.AddComponent<CanvasGroup>();
        glassGroup.blocksRaycasts = false;

        var tint = CV_UIUtil.CreateImage("Tint", glass, null, tintColor, Vector2.zero);
        CV_UIUtil.Stretch(tint.rectTransform);

        // 反射のハイライト（斜めの帯）
        for (int i = 0; i < 2; i++)
        {
            var glare = CV_UIUtil.CreateImage("Glare", glass, null, new Color(1, 1, 1, i == 0 ? 0.06f : 0.035f), new Vector2(i == 0 ? 160 : 70, 2600));
            glare.rectTransform.anchoredPosition = new Vector2(-520 + i * 190, 0);
            glare.rectTransform.localRotation = Quaternion.Euler(0, 0, -28);
        }

        // 枠
        const float t = 14f;
        CreateEdge(glass, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, t));
        CreateEdge(glass, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, t));
        CreateEdge(glass, new Vector2(0, 0), new Vector2(0, 1), new Vector2(t, 0));
        CreateEdge(glass, new Vector2(1, 0), new Vector2(1, 1), new Vector2(t, 0));

        crackLayer = CV_UIUtil.CreateFullRect("Cracks", glass);

        flash = CV_UIUtil.CreateImage("Flash", rect, null, new Color(1, 1, 1, 0), Vector2.zero);
        CV_UIUtil.Stretch(flash.rectTransform);
    }

    void CreateEdge(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        var img = CV_UIUtil.CreateImage("Frame", parent, null, frameColor, size);
        img.rectTransform.anchorMin = anchorMin;
        img.rectTransform.anchorMax = anchorMax;
        img.rectTransform.anchoredPosition = Vector2.zero;
    }

    /// <summary>ワールド座標をガラスの内側に収める</summary>
    public Vector3 ClampInside(Vector3 world, float margin)
    {
        Vector2 local = rect.InverseTransformPoint(world);
        var r = rect.rect;
        local.x = Mathf.Clamp(local.x, r.xMin + margin, r.xMax - margin);
        local.y = Mathf.Clamp(local.y, r.yMin + margin, r.yMax - margin);
        return rect.TransformPoint(local);
    }

    /// <summary>当たった位置に放射状のヒビを入れる</summary>
    public void AddCrack(Vector3 worldPos, float size)
    {
        var root = CV_UIUtil.CreateRect("Crack", crackLayer);
        root.position = worldPos;

        int rays = Random.Range(7, 12);
        const int segments = 3;
        float baseAngle = Random.Range(0f, 360f);
        var rayPoints = new Vector2[rays][];

        for (int r = 0; r < rays; r++)
        {
            float angle = baseAngle + r * 360f / rays + Random.Range(-12f, 12f);
            float len = size * Random.Range(0.45f, 1f);
            var pts = new Vector2[segments + 1];
            pts[0] = Vector2.zero;
            for (int s = 1; s <= segments; s++)
            {
                angle += Random.Range(-18f, 18f);
                var dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                pts[s] = pts[s - 1] + dir * (len / segments) * Random.Range(0.7f, 1.3f);

                float thick = Mathf.Lerp(5f, 1.5f, (s - 1f) / segments);
                var line = CV_UIUtil.CreateLine(root, pts[s - 1], pts[s], thick, crackColor);
                AddShadow(line);
                GrowIn(line.rectTransform, (s - 1) * 0.04f);
            }
            rayPoints[r] = pts;
        }

        // 隣り合うヒビを横につなぐ（蜘蛛の巣状）
        for (int s = 1; s < segments; s++)
        {
            for (int r = 0; r < rays; r++)
            {
                if (Random.value > 0.65f) continue;
                var a = rayPoints[r][s];
                var b = rayPoints[(r + 1) % rays][s];
                var line = CV_UIUtil.CreateLine(root, a, b, 2f, crackColor * new Color(1, 1, 1, 0.8f));
                AddShadow(line);
                GrowIn(line.rectTransform, 0.08f + s * 0.04f);
            }
        }

        // 中心の白い衝撃痕
        var impact = CV_UIUtil.CreateImage("Impact", root, CV_UIUtil.Circle, new Color(1, 1, 1, 0.55f), Vector2.one * size * 0.18f);
        impact.rectTransform.localScale = Vector3.zero;
        impact.rectTransform.DOScale(1f, 0.15f).SetEase(Ease.OutBack).SetLink(impact.gameObject);

        rect.DOKill(true);
        rect.DOShakeAnchorPos(0.3f, 18f, 40).SetLink(gameObject);
        flash.DOKill();
        flash.color = new Color(1, 1, 1, 0.25f);
        flash.DOFade(0f, 0.25f).SetLink(flash.gameObject);
    }

    static void AddShadow(Graphic g)
    {
        var s = g.gameObject.AddComponent<Shadow>();
        s.effectColor = new Color(0, 0, 0, 0.45f);
        s.effectDistance = new Vector2(1.5f, -1.5f);
    }

    static void GrowIn(RectTransform line, float delay)
    {
        var target = line.localScale;
        line.localScale = new Vector3(0, target.y, target.z);
        line.DOScaleX(target.x, 0.12f).SetDelay(delay).SetEase(Ease.OutQuad).SetLink(line.gameObject);
    }

    /// <summary>ガラス全体が粉々に割れて落ちる演出</summary>
    public IEnumerator Shatter()
    {
        var r = rect.rect;

        // ランダムにずらした格子点で三角形に分割する
        var grid = new Vector2[shardColumns + 1, shardRows + 1];
        for (int x = 0; x <= shardColumns; x++)
        {
            for (int y = 0; y <= shardRows; y++)
            {
                var p = new Vector2(r.xMin + r.width * x / shardColumns, r.yMin + r.height * y / shardRows);
                bool edge = x == 0 || y == 0 || x == shardColumns || y == shardRows;
                if (!edge) p += new Vector2(Random.Range(-0.35f, 0.35f) * r.width / shardColumns, Random.Range(-0.35f, 0.35f) * r.height / shardRows);
                grid[x, y] = p;
            }
        }

        for (int x = 0; x < shardColumns; x++)
        {
            for (int y = 0; y < shardRows; y++)
            {
                var a = grid[x, y];
                var b = grid[x + 1, y];
                var c = grid[x + 1, y + 1];
                var d = grid[x, y + 1];
                if (Random.value < 0.5f)
                {
                    CreateShard(a, b, c);
                    CreateShard(a, c, d);
                }
                else
                {
                    CreateShard(a, b, d);
                    CreateShard(b, c, d);
                }
            }
        }

        glassGroup.alpha = 0f;
        flash.DOKill();
        flash.color = new Color(1, 1, 1, 0.85f);
        flash.DOFade(0f, 0.5f).SetLink(flash.gameObject);

        yield return new WaitForSeconds(1.6f);

        foreach (var s in shards) Destroy(s);
        shards.Clear();
    }

    void CreateShard(Vector2 a, Vector2 b, Vector2 c)
    {
        var center = (a + b + c) / 3f;
        var rt = CV_UIUtil.CreateRect("Shard", rect);
        rt.anchoredPosition = center;
        var g = rt.gameObject.AddComponent<CV_PolygonGraphic>();
        g.raycastTarget = false;
        g.color = shardColor * new Color(1, 1, 1, Random.Range(0.6f, 1.2f));
        g.SetPoints(new[] { a - center, b - center, c - center });
        shards.Add(rt.gameObject);

        // 中心から外へ飛び散りつつ、重力で落ちる
        var outDir = center.sqrMagnitude > 1f ? center.normalized : Random.insideUnitCircle.normalized;
        float delay = center.magnitude / 3000f + Random.Range(0f, 0.08f);
        float dur = Random.Range(0.9f, 1.3f);
        var spread = outDir * Random.Range(150f, 500f);
        float fall = Random.Range(900f, 1500f);
        var seq = DOTween.Sequence().SetLink(rt.gameObject);
        seq.Insert(delay, DOVirtual.Float(0f, 1f, dur, t =>
            rt.anchoredPosition = center + spread * (1f - (1f - t) * (1f - t)) + Vector2.down * fall * t * t).SetEase(Ease.Linear));
        seq.Insert(delay, rt.DOLocalRotate(new Vector3(0, 0, Random.Range(-540f, 540f)), dur, RotateMode.FastBeyond360));
        seq.Insert(delay, rt.DOScale(Random.Range(0.6f, 1.1f), dur));
        seq.Insert(delay + dur * 0.5f, g.DOFade(0f, dur * 0.5f));
    }

    public void ResetGlass()
    {
        for (int i = crackLayer.childCount - 1; i >= 0; i--) Destroy(crackLayer.GetChild(i).gameObject);
        foreach (var s in shards) Destroy(s);
        shards.Clear();
        glassGroup.alpha = 1f;
        rect.anchoredPosition = Vector2.zero;
    }
}
