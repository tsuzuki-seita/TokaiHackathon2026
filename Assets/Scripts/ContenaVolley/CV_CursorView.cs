using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// スパイクする位置を示すカーソル。
/// 外側から縮んでくるリング（アプローチリング）がカーソルに重なった瞬間が叩くタイミング。
/// 見た目は Kenney Particle Pack のスプライトを重ねて作る
/// </summary>
public class CV_CursorView : MonoBehaviour
{
    static readonly Color HotColor = new Color(1f, 0.85f, 0.25f);

    RectTransform rt;
    CanvasGroup group;
    Image glow;
    Image twirl;
    Image ringGlow;
    Image ring;
    Image timer;
    Image approach;
    Image center;
    Text label;
    Color baseColor = Color.white;
    bool useTimer;
    bool hot;

    public RectTransform Rect => rt;

    public void Build()
    {
        rt = (RectTransform)transform;
        group = gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        glow = CreateLayer("Glow", CV_Assets.Fx("light_03"), 1.9f);
        twirl = CreateLayer("Twirl", CV_Assets.Fx("twirl_01"), 1.45f);
        ringGlow = CreateLayer("RingGlow", CV_Assets.Fx("circle_03"), 1.25f);
        ring = CreateLayer("Ring", CV_UIUtil.Ring, 1f);
        timer = CreateLayer("Timer", CV_UIUtil.Ring, 1.12f);
        timer.type = Image.Type.Filled;
        timer.fillMethod = Image.FillMethod.Radial360;
        timer.fillOrigin = (int)Image.Origin360.Top;
        timer.fillClockwise = false;
        center = CreateLayer("Center", CV_Assets.Fx("magic_03"), 0.7f);
        approach = CreateLayer("Approach", CV_Assets.Fx("circle_02"), 2.4f);

        label = CV_UIUtil.CreateText("Label", rt, 44, Color.white);
        var lrt = label.rectTransform;
        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0f);
        lrt.pivot = new Vector2(0.5f, 1f);
        lrt.anchoredPosition = new Vector2(0, -60);

        gameObject.SetActive(false);
    }

    Image CreateLayer(string name, Sprite sprite, float scale)
    {
        var img = CV_UIUtil.CreateImage(name, rt, sprite, Color.white, Vector2.zero);
        CV_UIUtil.Stretch(img.rectTransform);
        img.rectTransform.localScale = Vector3.one * scale;
        return img;
    }

    public void Show(Vector2 anchoredPos, float diameter, Color color, string text, bool useTimer)
    {
        gameObject.SetActive(true);
        KillTweens();

        baseColor = color;
        this.useTimer = useTimer;
        hot = false;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = Vector2.one * diameter;
        glow.color = new Color(color.r, color.g, color.b, 0.45f);
        twirl.color = new Color(color.r, color.g, color.b, 0.8f);
        ringGlow.color = new Color(color.r, color.g, color.b, 0.9f);
        ring.color = color;
        center.color = new Color(1f, 1f, 1f, 0.9f);
        timer.enabled = useTimer;
        timer.fillAmount = 1f;
        timer.color = Color.white;
        approach.enabled = useTimer;
        approach.color = new Color(1f, 1f, 1f, 0f);
        approach.rectTransform.localScale = Vector3.one * 2.4f;
        label.text = text;
        label.font = CV_Assets.FontFor(text);

        group.alpha = 0f;
        group.DOFade(1f, 0.15f);
        rt.localScale = Vector3.one * 0.2f;
        rt.localRotation = Quaternion.identity;
        rt.DOScale(1f, 0.35f).SetEase(Ease.OutElastic, 1.1f, 0.5f)
            .OnComplete(() => rt.DOScale(1.06f, 0.45f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine));
        center.rectTransform.DOScale(0.85f, 0.3f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }

    void Update()
    {
        // 光と渦をくるくる回して「生きている」感じを出す
        float dt = Time.deltaTime;
        twirl.rectTransform.Rotate(0, 0, (hot ? -540f : -140f) * dt);
        glow.rectTransform.Rotate(0, 0, 25f * dt);
        center.rectTransform.Rotate(0, 0, 60f * dt);
    }

    /// <summary>残り時間(1→0)</summary>
    public void SetTimer(float remain01)
    {
        timer.fillAmount = remain01;
        timer.color = Color.Lerp(new Color(1f, 0.3f, 0.25f), Color.white, remain01);

        // 残り時間に合わせて外側のリングが縮み、0でカーソルにぴったり重なる
        float s = Mathf.Lerp(1f, 2.4f, remain01);
        approach.rectTransform.localScale = Vector3.one * s;
        approach.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.25f, remain01));
    }

    /// <summary>今が叩くタイミング（判定受付中）かどうかを見た目に出す</summary>
    public void SetHot(bool value)
    {
        if (hot == value) return;
        hot = value;
        ring.color = value ? HotColor : baseColor;
        ringGlow.color = value ? HotColor : new Color(baseColor.r, baseColor.g, baseColor.b, 0.9f);
        glow.color = value ? new Color(1f, 0.9f, 0.4f, 0.8f) : new Color(baseColor.r, baseColor.g, baseColor.b, 0.45f);
        if (value) rt.DOPunchScale(Vector3.one * 0.18f, 0.2f, 8, 0.5f);
    }

    public void Hide(bool success)
    {
        if (!gameObject.activeSelf) return;
        KillTweens();
        approach.enabled = false;

        if (success)
        {
            ring.color = ringGlow.color = Color.white;
            rt.DOScale(2f, 0.3f).SetEase(Ease.OutQuad);
            rt.DOLocalRotate(new Vector3(0, 0, 90), 0.3f);
            group.DOFade(0f, 0.3f).OnComplete(() => gameObject.SetActive(false));
        }
        else
        {
            var grey = new Color(0.5f, 0.5f, 0.55f, 0.6f);
            ring.color = ringGlow.color = glow.color = twirl.color = grey;
            rt.DOShakeAnchorPos(0.35f, 20f, 30);
            rt.DOScale(0.6f, 0.45f).SetEase(Ease.InBack);
            group.DOFade(0f, 0.45f).OnComplete(() => gameObject.SetActive(false));
        }
    }

    public void HideImmediate()
    {
        KillTweens();
        gameObject.SetActive(false);
    }

    void KillTweens()
    {
        rt.DOKill();
        group.DOKill();
        center.rectTransform.DOKill();
    }
}
