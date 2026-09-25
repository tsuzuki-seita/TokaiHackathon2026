using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ボタンにカーソルを乗せる・押すと、ぷにっと拡大縮小してクリック音を鳴らす
/// </summary>
public class CV_ButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public string clickSound = "CV_se_click";

    Vector3 baseScale = Vector3.one;
    bool hover;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void OnDisable()
    {
        transform.DOKill();
        transform.localScale = baseScale;
        hover = false;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        hover = true;
        ScaleTo(1.06f, 0.15f);
    }

    public void OnPointerExit(PointerEventData e)
    {
        hover = false;
        ScaleTo(1f, 0.15f);
    }

    public void OnPointerDown(PointerEventData e)
    {
        ScaleTo(0.92f, 0.08f);
        CV_Audio.Play(clickSound, 0.8f);
    }

    public void OnPointerUp(PointerEventData e)
    {
        transform.DOKill();
        transform.DOScale(baseScale * (hover ? 1.06f : 1f), 0.35f).SetEase(Ease.OutElastic);
    }

    void ScaleTo(float s, float t)
    {
        transform.DOKill();
        transform.DOScale(baseScale * s, t).SetEase(Ease.OutQuad);
    }
}
