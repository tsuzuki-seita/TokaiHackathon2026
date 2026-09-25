using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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
        /// ラウンド開始コール（例: "ROUND 1", "FINAL ROUND!!"）
        /// </summary>
        public void ShowReady(string text, float roundDuration = 3.5f)
        {
            KillAllUITweens();

            if (instructionText != null)
            {
                instructionText.gameObject.SetActive(true);
                instructionText.text = text;

                // スケールを確実に0から拡大バウンドさせる
                instructionText.transform.localScale = Vector3.zero;
                instructionText.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
            }

            // ラウンド全体の長さに合わせてタイマーを開始
            StartTimer(roundDuration);
        }

        /// <summary>
        /// 指示テキストをスッと縮小して消す
        /// </summary>
        public void HideInstruction()
        {
            if (instructionText != null && instructionText.gameObject.activeSelf)
            {
                DOTween.Kill(instructionText.transform);
                instructionText.transform.DOScale(Vector3.zero, 0.2f)
                               .OnComplete(() =>
                               {
                                   instructionText.gameObject.SetActive(false);
                                   instructionText.transform.localScale = Vector3.one; // 次回のために戻す
                               });
            }
        }

        /// <summary>
        /// タイマーゲージ減少とサドンデス赤点滅（ラウンド時間と同期）
        /// </summary>
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
                // チャンスゾーンに入る直前（残り30%）から赤点滅
                float warningDelay = Mathf.Max(0.5f, duration * 0.65f);
                _warningSequence = DOTween.Sequence();
                _warningSequence.AppendInterval(warningDelay)
                                .Append(timerFillImage.DOColor(Color.red, 0.12f).SetLoops(6, LoopType.Yoyo));
            }
        }

        /// <summary>
        /// A / L キー押下時のバッジ沈み込みフィードバック
        /// </summary>
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

        /// <summary>
        /// 成否リザルト演出（VICTORY王冠落下 / MISS表示）
        /// </summary>
        public void ShowResult(bool isSuccess, float diffMs = 0f)
        {
            KillAllUITweens();

            if (instructionText != null)
            {
                DOTween.Kill(instructionText.transform);
                instructionText.gameObject.SetActive(true);
                instructionText.transform.localScale = Vector3.one; // 確実に表示サイズに戻す！

                if (isSuccess)
                {
                    instructionText.text = "<color=#FFDD00><size=55>VICTORY!!</size></color>\n<size=34>完全撃退！ +100pt</size>";
                    // 王冠のバウンド落下
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

        /// <summary>
        /// 次のラウンドへ進むための案内テキストを下部に追記
        /// </summary>
        public void AppendNextRoundGuide()
        {
            if (instructionText != null && instructionText.gameObject.activeSelf)
            {
                instructionText.text += "\n\n<size=26><color=#FFFFFF>▶ PRESS [SPACE] FOR NEXT ROUND ◀</color></size>";
            }
        }

        /// <summary>
        /// 時間切れ演出
        /// </summary>
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

        /// <summary>
        /// ラウンドごとのUIリセット
        /// </summary>
        public void ResetUI()
        {
            KillAllUITweens();

            if (instructionText != null)
            {
                instructionText.gameObject.SetActive(false);
                instructionText.transform.localScale = Vector3.one;
            }
            if (crownIcon != null)
            {
                crownIcon.gameObject.SetActive(false);
            }
            if (bloodSplashOverlay != null)
            {
                bloodSplashOverlay.SetActive(false);
            }
            if (timerSlider != null)
            {
                timerSlider.value = 1f;
            }
            if (timerFillImage != null)
            {
                timerFillImage.color = _initialTimerColor;
            }
            if (keyGuideP1 != null) { keyGuideP1.localScale = Vector3.one; }
            if (keyGuideP2 != null) { keyGuideP2.localScale = Vector3.one; }
        }

        private void KillAllUITweens()
        {
            KillTimerTweens();
            if (instructionText != null) DOTween.Kill(instructionText.transform);
            if (crownIcon != null) DOTween.Kill(crownIcon);
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