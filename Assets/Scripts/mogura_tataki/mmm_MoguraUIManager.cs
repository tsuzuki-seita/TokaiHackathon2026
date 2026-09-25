using DG.Tweening;
using TMPro;
using UnityEngine;

public class mmm_MoguraUIManager : MonoBehaviour
{
    [SerializeField] mmm_MoguraController controller;

    [Header("ラウンドテキスト")]
    [SerializeField] GameObject round1Text;
    [SerializeField] GameObject round2Text;
    [SerializeField] GameObject round3Text;

    [Header("ラウンド開始バナー")]
    [SerializeField] RectTransform roundBanner;
    [SerializeField] TextMeshProUGUI roundBannerText;
    [SerializeField] float roundBannerDisplayTime = 1.0f;

    [Header("正誤演出（controllerのcorrectUI/incorrectUIとは別オブジェクトを割り当てること）")]
    [SerializeField] RectTransform correctPanel;
    [SerializeField] RectTransform incorrectPanel;
    [SerializeField] float judgedPanelDisplayTime = 0.8f;

    [Header("最終結果")]
    [SerializeField] CanvasGroup finalResultGroup;
    [SerializeField] TextMeshProUGUI finalResultText;

    void OnEnable()
    {
        controller.OnRoundStart += HandleRoundStart;
        controller.OnRoundJudged += HandleRoundJudged;
        controller.OnGameFinished += HandleGameFinished;
    }

    void OnDisable()
    {
        controller.OnRoundStart -= HandleRoundStart;
        controller.OnRoundJudged -= HandleRoundJudged;
        controller.OnGameFinished -= HandleGameFinished;
    }

    // ★ラウンド開始：バナーをポップさせてから引っ込める
    void HandleRoundStart(int roundNumber)
    {
        if (roundNumber == 1)
        {
            round1Text.SetActive(true);
        }
        else if (roundNumber == 2)
        {
            round2Text.SetActive(true);
        }
        else if (roundNumber == 3)
        {
            round3Text.SetActive(true);
        }

        roundBannerText.text = $"ラウンド{roundNumber}";

        roundBanner.gameObject.SetActive(true);
        roundBanner.localScale = Vector3.zero;

        roundBanner.DOScale(1f, 0.3f).SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                roundBanner.DOScale(0f, 0.25f)
                    .SetDelay(roundBannerDisplayTime)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => roundBanner.gameObject.SetActive(false));
            });
    }

    // ★正誤確定：正解ならWinner SE、不正解ならLoser SEを鳴らしつつパネルをポップさせる
    void HandleRoundJudged(bool cleared)
    {
        RectTransform panel = cleared ? correctPanel : incorrectPanel;

        panel.gameObject.SetActive(true);
        panel.localScale = Vector3.zero;

        panel.DOScale(1f, 0.4f).SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                panel.DOScale(0f, 0.3f)
                    .SetDelay(judgedPanelDisplayTime)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => panel.gameObject.SetActive(false));
            });
    }

    // ★最終結果：「n問中m問正解」をフェード表示
    void HandleGameFinished(int clearedRoundCount, int totalRoundCount)
    {
        finalResultText.text = $"{totalRoundCount}問中{clearedRoundCount}問正解";

        finalResultGroup.gameObject.SetActive(true);
        finalResultGroup.alpha = 0f;
        finalResultGroup.DOFade(1f, 0.6f);
    }
}
