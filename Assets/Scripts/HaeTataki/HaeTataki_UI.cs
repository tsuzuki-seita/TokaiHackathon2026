using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

namespace HaeTataki
{
    public class HaeTataki_UI : MonoBehaviour
    {
        [Header("Text & Overlays")]
        [SerializeField] private Text instructionText;
        [SerializeField] private GameObject bloodSplashOverlay;

        [Header("Clash Royale Style UI Elements")]
        [SerializeField] private Slider timerSlider;
        [SerializeField] private Image timerFillImage;
        [SerializeField] private RectTransform keyGuideP1;
        [SerializeField] private RectTransform keyGuideP2;
        [SerializeField] private RectTransform crownIcon;

        [Header("Title UI Elements")]
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private Text pressStartText;

        [Header("Final Result UI Elements")]
        [SerializeField] private GameObject finalResultPanel;
        [SerializeField] private Text finalRoundRecordsText;
        [SerializeField] private Text finalTotalScoreText;
        [SerializeField] private Text finalRankText;
        [SerializeField] private Text finalCommentText;
        [SerializeField] private Text restartGuideText;

        private Color _initialTimerColor;
        private Tween _timerTween;
        private Sequence _warningSequence;

        private void Awake()
        {
            if (timerFillImage != null)
            {
                _initialTimerColor = timerFillImage.color;
            }
        }

        /// <summary>
        /// タイトル画面の表示
        /// </summary>
        public void ShowTitle()
        {
            KillAllUITweens();

            if (titlePanel != null) titlePanel.SetActive(true);
            if (finalResultPanel != null) finalResultPanel.SetActive(false);
            if (instructionText != null) instructionText.gameObject.SetActive(false);
            if (crownIcon != null) crownIcon.gameObject.SetActive(false);
            if (bloodSplashOverlay != null) bloodSplashOverlay.SetActive(false);

            // 「PRESS [SPACE] TO START」を滑らかに点滅
            if (pressStartText != null)
            {
                DOTween.Kill(pressStartText);
                pressStartText.color = new Color(pressStartText.color.r, pressStartText.color.g, pressStartText.color.b, 1f);
                pressStartText.DOFade(0.25f, 0.6f).SetLoops(-1, LoopType.Yoyo);
            }
        }

        /// <summary>
        /// タイトル画面の非表示（ゲーム本編開始時）
        /// </summary>
        public void HideTitle()
        {
            if (pressStartText != null) DOTween.Kill(pressStartText);
            if (titlePanel != null) titlePanel.SetActive(false);
        }

        /// <summary>
        /// ラウンド開始コール（例: "ROUND 1", "FINAL ROUND!!"）
        /// </summary>
        public void ShowReady(string text, float roundDuration = 3.5f)
        {
            KillAllUITweens();

            if (instructionText != null)
            {
                instructionText.gameObject.SetActive(true);
                instructionText.text = text;

                instructionText.transform.localScale = Vector3.zero;
                instructionText.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
            }

            StartTimer(roundDuration);
        }

        public void HideInstruction()
        {
            if (instructionText != null && instructionText.gameObject.activeSelf)
            {
                DOTween.Kill(instructionText.transform);
                instructionText.transform.DOScale(Vector3.zero, 0.2f)
                               .OnComplete(() =>
                               {
                                   instructionText.gameObject.SetActive(false);
                                   instructionText.transform.localScale = Vector3.one;
                               });
            }
        }

        public void StartTimer(float duration)
        {
            KillTimerTweens();

            if (timerSlider != null)
            {
                timerSlider.value = 1f;
                _timerTween = timerSlider.DOValue(0f, duration).SetEase(Ease.Linear);
            }

            if (timerFillImage != null)
            {
                timerFillImage.color = _initialTimerColor;
                float warningDelay = Mathf.Max(0.5f, duration * 0.65f);
                _warningSequence = DOTween.Sequence();
                _warningSequence.AppendInterval(warningDelay)
                                .Append(timerFillImage.DOColor(Color.red, 0.12f).SetLoops(6, LoopType.Yoyo));
            }
        }

        public void PunchKeyBadge(bool isP1)
        {
            RectTransform target = isP1 ? keyGuideP1 : keyGuideP2;
            if (target != null)
            {
                DOTween.Kill(target);
                target.localScale = Vector3.one;
                target.DOScale(0.82f, 0.08f).SetLoops(2, LoopType.Yoyo);
            }
        }

        public void ShowResult(bool isSuccess, float diffMs = 0f)
        {
            KillAllUITweens();

            if (instructionText != null)
            {
                DOTween.Kill(instructionText.transform);
                instructionText.gameObject.SetActive(true);
                instructionText.transform.localScale = Vector3.one;

                if (isSuccess)
                {
                    instructionText.text = "<color=#FFDD00><size=55>VICTORY!!</size></color>\n<size=34>完全撃退！</size>";
                    if (crownIcon != null)
                    {
                        crownIcon.gameObject.SetActive(true);
                        crownIcon.anchoredPosition = new Vector2(0, 250);
                        crownIcon.DOAnchorPosY(80, 0.45f).SetEase(Ease.OutBounce);
                    }
                }
                else
                {
                    instructionText.text = $"<color=#FF4444><size=55>MISS!!</size></color>\n<size=34>ズレ: {diffMs:F0}ms</size>";
                }
            }
        }

        public void AppendNextRoundGuide()
        {
            if (instructionText != null && instructionText.gameObject.activeSelf)
            {
                instructionText.text += "\n\n<size=26><color=#FFFFFF>▶ PRESS [SPACE] FOR NEXT ROUND ◀</color></size>";
            }
        }

        public void ShowTimeOut()
        {
            KillAllUITweens();

            if (instructionText != null)
            {
                DOTween.Kill(instructionText.transform);
                instructionText.gameObject.SetActive(true);
                instructionText.transform.localScale = Vector3.one;
                instructionText.text = "<color=#FF2222><size=55>TIME OVER...</size></color>\n<size=34>血を吸われてしまった！</size>";
            }
            if (bloodSplashOverlay != null)
            {
                bloodSplashOverlay.SetActive(true);
            }
        }

        public void ShowFinalResult(List<RoundRecord> records)
        {
            KillAllUITweens();

            if (instructionText != null) instructionText.gameObject.SetActive(false);
            if (crownIcon != null) crownIcon.gameObject.SetActive(false);

            if (finalResultPanel != null)
            {
                finalResultPanel.SetActive(true);
            }

            int totalScore = 0;
            int successCount = 0;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (var r in records)
            {
                totalScore += r.score;
                if (r.isSuccess) successCount++;

                string mark = r.isSuccess ? "<color=#22FF66>⭕ WIN</color>" : "<color=#FF4444>❌ MISS</color>";
                string diffStr = r.isSuccess ? $"({r.diffMs:F0}ms)" : "";
                sb.AppendLine($"ROUND {r.roundNumber}:  {mark}  +{r.score}pt {diffStr}");
            }

            if (finalRoundRecordsText != null) finalRoundRecordsText.text = sb.ToString();
            if (finalTotalScoreText != null) finalTotalScoreText.text = $"TOTAL SCORE: <color=#FFEE00>{totalScore}</color> pt";

            string rankStr;
            string titleStr;
            string commentStr;

            if (successCount == 3 && totalScore >= 280)
            {
                rankStr = "<color=#FFD700><size=95>S</size></color>";
                titleStr = "【害虫駆除の神コンビ】";
                commentStr = "完璧な息の合い方！この部屋に蚊は1匹も残らない！";
            }
            else if (successCount >= 2)
            {
                rankStr = "<color=#00D4FF><size=85>A</size></color>";
                titleStr = "【頼れるベテランハンター】";
                commentStr = "まずまずの連係プレー！実戦合格レベル！";
            }
            else
            {
                rankStr = "<color=#FF5555><size=85>B</size></color>";
                titleStr = "【息の合わない迷コンビ】";
                commentStr = "血を吸われまくり…！もっと手拍子を合わせよう！";
            }

            if (finalRankText != null)
            {
                finalRankText.text = $"RANK: {rankStr}\n<size=28>{titleStr}</size>";
                DOTween.Kill(finalRankText.transform);
                finalRankText.transform.localScale = Vector3.one * 2.5f;
                finalRankText.transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBounce);
            }

            if (finalCommentText != null) finalCommentText.text = commentStr;

            if (restartGuideText != null)
            {
                restartGuideText.text = "▶ PRESS [SPACE] FOR TITLE ◀";
                DOTween.Kill(restartGuideText);
                restartGuideText.DOFade(0.3f, 0.6f).SetLoops(-1, LoopType.Yoyo);
            }
        }

        public void ResetUI()
        {
            KillAllUITweens();

            if (instructionText != null)
            {
                instructionText.gameObject.SetActive(false);
                instructionText.transform.localScale = Vector3.one;
            }
            if (crownIcon != null) crownIcon.gameObject.SetActive(false);
            if (bloodSplashOverlay != null) bloodSplashOverlay.SetActive(false);
            if (finalResultPanel != null) finalResultPanel.SetActive(false);

            if (timerSlider != null) timerSlider.value = 1f;
            if (timerFillImage != null) timerFillImage.color = _initialTimerColor;
            if (keyGuideP1 != null) keyGuideP1.localScale = Vector3.one;
            if (keyGuideP2 != null) keyGuideP2.localScale = Vector3.one;
        }

        private void KillAllUITweens()
        {
            KillTimerTweens();
            if (instructionText != null) DOTween.Kill(instructionText.transform);
            if (crownIcon != null) DOTween.Kill(crownIcon);
            if (finalRankText != null) DOTween.Kill(finalRankText.transform);
            if (restartGuideText != null) DOTween.Kill(restartGuideText);
            if (pressStartText != null) DOTween.Kill(pressStartText);
        }

        private void KillTimerTweens()
        {
            if (_timerTween != null && _timerTween.IsActive()) _timerTween.Kill();
            if (_warningSequence != null && _warningSequence.IsActive()) _warningSequence.Kill();
        }

        private void OnDestroy()
        {
            KillAllUITweens();
        }
    }
}