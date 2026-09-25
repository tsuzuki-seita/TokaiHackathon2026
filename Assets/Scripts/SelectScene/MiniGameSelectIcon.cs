using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// ミニゲーム選択画面のアイコン1つ分。
/// マウスオーバーでサムネイルをポップアップ表示し、クリックで指定シーンへ遷移する。
/// </summary>
public class MiniGameSelectIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("遷移先")]
    [Tooltip("Build Settings に登録されたシーン名")]
    public string sceneName;

    [Header("参照")]
    [Tooltip("拡大させるアイコン本体")]
    public RectTransform iconBody;
    [Tooltip("ポップアップさせるサムネイル (CanvasGroup 付き)")]
    public CanvasGroup thumbnail;

    [Header("アニメーション")]
    public float hoverIconScale = 1.12f;
    public float popupDuration = 0.25f;
    public float hideDuration = 0.15f;
    [Tooltip("サムネイルが下から浮き上がる距離")]
    public float popupRise = 30f;

    static bool isLoading;

    RectTransform thumbRect;
    Vector2 thumbBasePos;
    Sequence thumbSeq;

    void Awake()
    {
        isLoading = false;
        if (iconBody == null) iconBody = (RectTransform)transform;

        if (thumbnail != null)
        {
            thumbRect = (RectTransform)thumbnail.transform;
            thumbBasePos = thumbRect.anchoredPosition;
            // サムネイルがレイキャストを奪うとホバーがちらつくので無効化
            thumbnail.blocksRaycasts = false;
            thumbnail.interactable = false;
            thumbnail.alpha = 0f;
            thumbRect.localScale = Vector3.zero;
            thumbnail.gameObject.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isLoading) return;

        // 隣のアイコンより手前にサムネイルを描画する
        transform.SetAsLastSibling();

        iconBody.DOKill();
        iconBody.DOScale(hoverIconScale, 0.15f).SetEase(Ease.OutQuad).SetLink(gameObject);

        ShowThumbnail();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isLoading) return;

        iconBody.DOKill();
        iconBody.DOScale(1f, 0.15f).SetEase(Ease.OutQuad).SetLink(gameObject);

        HideThumbnail();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isLoading || eventData.button != PointerEventData.InputButton.Left) return;

        if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[MiniGameSelectIcon] シーン '{sceneName}' を読み込めません。Build Settings に登録されているか確認してください。");
            return;
        }

        isLoading = true;
        iconBody.DOKill();
        iconBody.localScale = Vector3.one * hoverIconScale;
        iconBody.DOPunchScale(Vector3.one * 0.15f, 0.25f, 8, 0.8f)
            .SetLink(gameObject)
            .OnComplete(() => SceneManager.LoadScene(sceneName));
    }

    void ShowThumbnail()
    {
        if (thumbnail == null) return;

        thumbSeq?.Kill();
        thumbnail.gameObject.SetActive(true);
        thumbRect.anchoredPosition = thumbBasePos - new Vector2(0f, popupRise);

        thumbSeq = DOTween.Sequence()
            .Join(thumbRect.DOScale(1f, popupDuration).SetEase(Ease.OutBack))
            .Join(thumbnail.DOFade(1f, popupDuration * 0.8f))
            .Join(thumbRect.DOAnchorPos(thumbBasePos, popupDuration).SetEase(Ease.OutCubic))
            .SetLink(gameObject);
    }

    void HideThumbnail()
    {
        if (thumbnail == null) return;

        thumbSeq?.Kill();
        thumbSeq = DOTween.Sequence()
            .Join(thumbRect.DOScale(0.6f, hideDuration).SetEase(Ease.InQuad))
            .Join(thumbnail.DOFade(0f, hideDuration))
            .OnComplete(() => thumbnail.gameObject.SetActive(false))
            .SetLink(gameObject);
    }
}
