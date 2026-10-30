using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class mmm_MoguraUIManager : MonoBehaviour
{
    [SerializeField] mmm_MoguraController controller;

    [Header("ラウンド開始テキスト（ラウンドごとに用意したGameObject）")]
    [SerializeField] GameObject round1Text;
    [SerializeField] GameObject round2Text;
    [SerializeField] GameObject round3Text;
    [SerializeField] float roundTextDisplayTime = 1.0f;

    [Header("正誤演出（controllerのcorrectUI/incorrectUIとは別オブジェクトを割り当てること）")]
    [SerializeField] RectTransform correctPanel;
    [SerializeField] RectTransform incorrectPanel;
    [SerializeField] float judgedPanelDisplayTime = 0.8f;

    [Header("最終結果")] // このパネルにスコアを表示させたい
    [SerializeField] GameObject finalResultPanel;

    [Header("カウントダウン")]  // 記憶フェーズ開始前のカウントダウン
    [SerializeField] Text CountDownText;

    // controllerのイベントを購読する。これ以降、各Handleメソッドはcontroller側の
    // 処理進行に合わせて自動で呼び出される（このスクリプト側から明示的に呼ぶ必要はない）

    void Start()
    {
        CountDownText.gameObject.SetActive(false);  // 非表示にする
        CountDownText.text = "3";
    }
    
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

    public void SpaceToStart()
    {   
        controller.SpaceToStartText.transform.DOScale(1.1f, 0.6f).SetLoops(-1, LoopType.Yoyo);
    }

    public Sequence CountDownAnimation()
    {
        CountDownText.gameObject.SetActive(true);

        Sequence sequence = DOTween.Sequence();

        sequence
        .AppendCallback(() => CountDownText.text = "3")
        .Append(CountDownText.transform.DOScale(1.0f, 0.8f))
        .Append(CountDownText.transform.DOScale(0, 0.2f))
        .AppendCallback(() => CountDownText.text = "2")
        .Append(CountDownText.transform.DOScale(1.0f, 0.8f))
        .Append(CountDownText.transform.DOScale(0, 0.2f))
        .AppendCallback(() => CountDownText.text = "1")
        .Append(CountDownText.transform.DOScale(1.0f, 0.8f))
        .Append(CountDownText.transform.DOScale(0, 0.2f));

        return sequence;
    }
    

    // ★ラウンド開始：controllerのRound1/2/3コルーチンの先頭でOnRoundStartがInvokeされた瞬間に自動で呼ばれる
    void HandleRoundStart(int roundNumber)
    {
        GameObject target = roundNumber switch
        {
            1 => round1Text,
            2 => round2Text,
            3 => round3Text,
            _ => null
        };

        if (target == null) return;

        PopAndHide(target, roundTextDisplayTime);
    }

    // ★正誤確定：controllerのAnswerPhase内でLastRoundClearedが確定しOnRoundJudgedがInvokeされた瞬間に自動で呼ばれる
    void HandleRoundJudged(bool cleared)
    {
        RectTransform panel = cleared ? correctPanel : incorrectPanel;
        PopAndHide(panel.gameObject, judgedPanelDisplayTime);
    }

    // ★最終結果：controllerのRound3コルーチン末尾でOnGameFinishedがInvokeされた瞬間に自動で呼ばれる
    void HandleGameFinished(int clearedRoundCount, int totalRoundCount)
    {
        // finalResultText.text = $"{totalRoundCount}問中{clearedRoundCount}問正解";

        finalResultPanel.SetActive(true);
    }

    // 指定した要素をポップ表示し、displayTime秒後に引っ込めて非表示にする共通処理
    void PopAndHide(GameObject target, float displayTime)
    {
        target.SetActive(true);
        target.transform.localScale = Vector3.zero;

        target.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                target.transform.DOScale(0f, 0.25f)
                    .SetDelay(displayTime)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => target.SetActive(false));
            });
    }
}
