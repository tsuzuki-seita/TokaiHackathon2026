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
        /// バトル開始合図（リボンバウンド拡大）
        /// </summary>
        public void ShowReady(string text = "挟め！")
        {
            if (instructionText != null)
            {
                instructionText.gameObject.SetActive(true);
                instructionText.text = text;
                
                // クラロワのバトル告知風バウンド出現（OutBack）
                instructionText.transform.localScale = Vector3.zero;
                instructionText.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
            }

            // タイマーバーのリセットと5秒カウント開始
            StartTimer(5.0f);
        }

        public void HideInstruction()
        {
            if (instructionText != null)
            {
                // 縮小フェードアウト
                instructionText.transform.DOScale(Vector3.zero, 0.2f)
                               .OnComplete(() => instructionText.gameObject.SetActive(false));
            }
        }

        /// <summary>
        /// タイマーゲージ減少とサドンデス赤点滅
        /// </summary>
        private void StartTimer(float duration)
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
                // 3.2秒後（チャージ開始時）にサドンデス風赤点滅
                _warningSequence = DOTween.Sequence();
                _warningSequence.AppendInterval(3.2f)
                                .Append(timerFillImage.DOColor(Color.red, 0.15f).SetLoops(6, LoopType.Yoyo));
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
        /// リザルト演出（VICTORY王冠落下 / MISSバッジ）
        /// </summary>
        public void ShowResult(bool isSuccess, float diffMs = 0f)
        {
            KillTimerTweens();

            if (instructionText != null)
            {
                instructionText.gameObject.SetActive(true);
                instructionText.transform.localScale = Vector3.one;

                if (isSuccess)
                {
                    instructionText.text = "<color=#FFDD00>VICTORY!!</color>\n<size=38>完全撃退！ +100pt</size>";
                    // クラロワ風 勝利王冠のバウンド落下演出
                    if (crownIcon != null)
                    {
                        crownIcon.gameObject.SetActive(true);
                        crownIcon.anchoredPosition = new Vector2(0, 250);
                        crownIcon.DOAnchorPosY(80, 0.45f).SetEase(Ease.OutBounce);
                    }
                }
                else
                {
                    instructionText.text = $"<color=#FF4444>MISS!!</color>\n<size=32>ズレ: {diffMs:F0}ms</size>";
                }
            }
        }

        public void ShowTimeOut()
        {
            KillTimerTweens();

            if (instructionText != null)
            {
                instructionText.gameObject.SetActive(true);
                instructionText.transform.localScale = Vector3.one;
                instructionText.text = "<color=#990000>TIME OVER...</color>\n<size=32>血を吸われてしまった！</size>";
            }
            if (bloodSplashOverlay != null)
            {
                bloodSplashOverlay.SetActive(true);
            }
        }

        public void ResetUI()
        {
            KillTimerTweens();

            if (instructionText != null)
            {
                DOTween.Kill(instructionText.transform);
                instructionText.gameObject.SetActive(false);
            }
            if (crownIcon != null)
            {
                DOTween.Kill(crownIcon);
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
            if (keyGuideP1 != null) { DOTween.Kill(keyGuideP1); keyGuideP1.localScale = Vector3.one; }
            if (keyGuideP2 != null) { DOTween.Kill(keyGuideP2); keyGuideP2.localScale = Vector3.one; }
        }

        private void KillTimerTweens()
        {
            if (_timerTween != null && _timerTween.IsActive()) _timerTween.Kill();
            if (_warningSequence != null && _warningSequence.IsActive()) _warningSequence.Kill();
        }

        private void OnDestroy()
        {
            KillTimerTweens();
        }
    }
}