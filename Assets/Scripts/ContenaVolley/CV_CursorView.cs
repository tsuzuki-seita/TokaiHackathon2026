using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// スパイクする位置を示すカーソル（残り時間リング付き）
/// </summary>
public class CV_CursorView : MonoBehaviour
{
    RectTransform rt;
    CanvasGroup group;
    Image glow;
    Image ring;
    Image timer;
    Image dot;
    Text label;
    Color baseColor = Color.white;

    public RectTransform Rect => rt;

    public void Build()
    {
        rt = (RectTransform)transform;
        group = gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        glow = CreateLayer("Glow", CV_UIUtil.Circle, 1.35f);
        ring = CreateLayer("Ring", CV_UIUtil.Ring, 1f);
        timer = CreateLayer("Timer", CV_UIUtil.Ring, 1.22f);
        timer.type = Image.Type.Filled;
        timer.fillMethod = Image.FillMethod.Radial360;
        timer.fillOrigin = (int)Image.Origin360.Top;
        timer.fillClockwise = false;
        dot = CreateLayer("Dot", CV_UIUtil.Circle, 0.22f);

        label = CV_UIUtil.CreateText("Label", rt, 44, Color.white);
        var lrt = label.rectTransform;
        lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0f);
        lrt.pivot = new Vector2(0.5f, 1f);
        lrt.anchoredPosition = new Vector2(0, -30);

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
        rt.DOKill();
        group.DOKill();

        baseColor = color;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = Vector2.one * diameter;
        glow.color = new Color(color.r, color.g, color.b, 0.25f);
        ring.color = color;
        dot.color = Color.white;
        timer.enabled = useTimer;
        timer.fillAmount = 1f;
        timer.color = Color.white;
        label.text = text;

        group.alpha = 0f;
        group.DOFade(1f, 0.15f);
        rt.localScale = Vector3.one * 0.3f;
        rt.DOScale(1f, 0.25f).SetEase(Ease.OutBack)
            .OnComplete(() => rt.DOScale(1.08f, 0.45f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine));
    }

    /// <summary>残り時間(1→0)</summary>
    public void SetTimer(float remain01)
    {
        timer.fillAmount = remain01;
        timer.color = Color.Lerp(new Color(1f, 0.25f, 0.2f), Color.white, remain01);
    }

    public void Hide(bool success)
    {
        if (!gameObject.activeSelf) return;
        rt.DOKill();
        group.DOKill();

        if (success)
        {
            ring.color = Color.white;
            rt.DOScale(1.8f, 0.25f).SetEase(Ease.OutQuad);
            group.DOFade(0f, 0.25f).OnComplete(() => gameObject.SetActive(false));
        }
        else
        {
            ring.color = glow.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            rt.DOShakeAnchorPos(0.35f, 20f, 30);
            group.DOFade(0f, 0.45f).OnComplete(() => gameObject.SetActive(false));
        }
    }

    public void HideImmediate()
    {
        rt.DOKill();
        group.DOKill();
        gameObject.SetActive(false);
    }
}
