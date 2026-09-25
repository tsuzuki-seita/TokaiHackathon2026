using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace HaeTataki
{
    public class HaeTataki_MosquitoActor : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioSource buzzAudio;

        [Header("Sprites")]
        [SerializeField] private Sprite squashedSprite;
        [SerializeField] private Sprite escapeSprite;

        [Header("Positions")]
        [Tooltip("画面外の初期待機位置")]
        [SerializeField] private Vector3 screenOutPosition = new Vector3(13f, 7f, 0f);

        public bool IsCatchable { get; private set; } = false;

        private Vector3 _initialScale;
        private SpriteRenderer _spriteRenderer;
        private Color _initialColor;
        private Sprite _initialSprite;

        private void Awake()
        {
            _initialScale = transform.localScale;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _initialColor = _spriteRenderer.color;
                _initialSprite = _spriteRenderer.sprite;
            }
            transform.position = screenOutPosition;
        }

        /// <summary>
        /// 蚊の飛行シーケンス（徐々に高速化する予兆点滅付き）
        /// </summary>
        public async UniTask StartFlightSequenceAsync(
            float flightDuration,
            float warningDuration,
            float catchableDuration,
            CancellationToken ct,
            System.Action onWarningCallback = null)
        {
            ResetState();
            if (buzzAudio != null) { buzzAudio.pitch = 1.0f; buzzAudio.Play(); }

            // 1. 旋回飛行（画面外から突入）
            Vector3[] waypoints = new Vector3[] {
                new Vector3(6f, 3.5f, 0f),
                new Vector3(-4f, 2f, 0f),
                new Vector3(-2f, -1.8f, 0f),
                new Vector3(3f, -1.5f, 0f),
                new Vector3(0f, 2.5f, 0f)
            };

            transform.DOPath(waypoints, flightDuration, PathType.CatmullRom).SetEase(Ease.Linear);
            await UniTask.Delay((int)(flightDuration * 1000f), cancellationToken: ct);

            // 2. 予兆（突入直前に向けて点滅と羽音がどんどん高速化！）
            onWarningCallback?.Invoke();

            float elapsed = 0f;
            float initialInterval = 0.22f; // 初期の点滅周期（ゆっくり）
            float finalInterval = 0.04f;   // 突入直前の点滅周期（超高速）

            while (elapsed < warningDuration)
            {
                float t = Mathf.Clamp01(elapsed / warningDuration);
                // 二乗イージングで終盤に急激に速くする
                float currentInterval = Mathf.Lerp(initialInterval, finalInterval, t * t);

                // 赤色 ＆ 脈動拡大 ＆ 羽音ピッチ上昇（1.2倍 -> 1.8倍）
                if (_spriteRenderer != null) _spriteRenderer.color = Color.red;
                transform.localScale = _initialScale * Mathf.Lerp(1.15f, 1.45f, t);
                if (buzzAudio != null) buzzAudio.pitch = Mathf.Lerp(1.2f, 1.8f, t);

                int halfDelay = Mathf.Max(16, (int)(currentInterval * 500f));
                await UniTask.Delay(halfDelay, cancellationToken: ct);
                elapsed += currentInterval * 0.5f;

                // 元の色に戻す
                if (_spriteRenderer != null) _spriteRenderer.color = _initialColor;
                transform.localScale = _initialScale;

                await UniTask.Delay(halfDelay, cancellationToken: ct);
                elapsed += currentInterval * 0.5f;
            }

            // 予兆終了時のリセット
            if (_spriteRenderer != null) _spriteRenderer.color = _initialColor;
            transform.localScale = _initialScale;

            // 3. チャンスゾーン（中央へ急降下突入！ここを挟む！）
            IsCatchable = true;
            transform.DOMove(Vector3.zero, 0.12f);
            await UniTask.Delay(120, cancellationToken: ct);
            await UniTask.Delay((int)(catchableDuration * 1000f), cancellationToken: ct);
            IsCatchable = false;

            // 4. 逃走（時間切れ）
            if (buzzAudio != null) buzzAudio.pitch = 1.0f;
            transform.DOMove(new Vector3(-13f, 7f, 0f), 0.4f).SetEase(Ease.InQuad);
            await UniTask.Delay(400, cancellationToken: ct);
        }

        public void HideInHands()
        {
            DOTween.Kill(transform);
            if (buzzAudio != null) buzzAudio.Stop();
            transform.position = Vector3.zero;
            if (_spriteRenderer != null) _spriteRenderer.enabled = false;
        }

        public void Squash()
        {
            DOTween.Kill(transform);
            if (_spriteRenderer != null)
            {
                DOTween.Kill(_spriteRenderer);
                if (squashedSprite != null) _spriteRenderer.sprite = squashedSprite;
                _spriteRenderer.enabled = true;
            }
            if (buzzAudio != null) buzzAudio.Stop();

            IsCatchable = false;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = _initialScale;

            transform.DOShakePosition(0.25f, strength: 0.15f, vibrato: 10);
            transform.DOMoveY(-7f, 1.2f).SetEase(Ease.InQuad).SetDelay(0.15f);
            transform.DOMoveX(0.5f, 0.28f).SetLoops(4, LoopType.Yoyo).SetEase(Ease.InOutSine).SetDelay(0.15f);
            transform.DORotate(new Vector3(0, 0, 35f), 0.3f).SetLoops(4, LoopType.Yoyo).SetEase(Ease.InOutSine).SetDelay(0.15f);
        }

        public void Escape()
        {
            DOTween.Kill(transform);
            if (_spriteRenderer != null)
            {
                DOTween.Kill(_spriteRenderer);
                if (escapeSprite != null) _spriteRenderer.sprite = escapeSprite;
                _spriteRenderer.enabled = true;
            }
            if (buzzAudio != null)
            {
                buzzAudio.pitch = 1.0f;
                buzzAudio.Play();
            }
            IsCatchable = false;
            transform.DOMove(new Vector3(13f, 7f, 0f), 0.5f).SetEase(Ease.InQuad);
        }

        public async UniTask CameraAttackAsync(CancellationToken ct)
        {
            DOTween.Kill(transform);
            IsCatchable = false;
            transform.DOMove(Vector3.zero, 0.2f);
            transform.DOScale(_initialScale * 7.0f, 0.35f);
            await UniTask.Delay(350, cancellationToken: ct);
        }

        public void ResetState()
        {
            DOTween.Kill(transform);
            if (_spriteRenderer != null)
            {
                DOTween.Kill(_spriteRenderer);
                _spriteRenderer.enabled = true;
                _spriteRenderer.color = _initialColor;
                if (_initialSprite != null) _spriteRenderer.sprite = _initialSprite;
            }
            if (buzzAudio != null)
            {
                buzzAudio.Stop();
                buzzAudio.pitch = 1.0f;
            }
            IsCatchable = false;
            transform.position = screenOutPosition;
            transform.rotation = Quaternion.identity;
            transform.localScale = _initialScale;
        }
    }
}