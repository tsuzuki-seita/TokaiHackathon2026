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
        [SerializeField] private Sprite squashedSprite; // 成功時のペちゃんこ画像
        [SerializeField] private Sprite escapeSprite;   // 逃走時のドヤ顔画像

        public bool IsCatchable { get; private set; } = false;

        private Vector3 _initialScale;
        private Vector3 _startPosition = new Vector3(8f, 4.5f, 0f);
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
        }

        /// <summary>
        /// 蚊の飛行シーケンス
        /// </summary>
        public async UniTask StartFlightSequenceAsync(CancellationToken ct)
        {
            ResetState();
            if (buzzAudio != null) { buzzAudio.pitch = 1.0f; buzzAudio.Play(); }

            // 1. 不規則旋回（2.2秒）
            Vector3[] waypoints = new Vector3[] {
                new Vector3(4f, 2f, 0f), new Vector3(-3f, -1.5f, 0f),
                new Vector3(2f, -2f, 0f), new Vector3(0f, 2.5f, 0f)
            };
            transform.DOPath(waypoints, 2.2f, PathType.CatmullRom).SetEase(Ease.Linear);
            await UniTask.Delay(2200, cancellationToken: ct);

            // 2. 予兆（0.6秒）
            if (buzzAudio != null) buzzAudio.pitch = 1.5f;
            if (_spriteRenderer != null) _spriteRenderer.DOColor(Color.red, 0.15f).SetLoops(4, LoopType.Yoyo);
            transform.DOScale(_initialScale * 1.35f, 0.2f).SetLoops(3, LoopType.Yoyo);
            await UniTask.Delay(600, cancellationToken: ct);

            // 3. チャンスゾーン（中央突入: 0.6秒）
            IsCatchable = true;
            transform.DOMove(Vector3.zero, 0.15f);
            await UniTask.Delay(150, cancellationToken: ct);
            await UniTask.Delay(600, cancellationToken: ct);
            IsCatchable = false;

            // 4. 逃走（時間切れ）
            if (buzzAudio != null) buzzAudio.pitch = 1.0f;
            transform.DOMove(new Vector3(11f, 6f, 0f), 0.5f).SetEase(Ease.InQuad);
            await UniTask.Delay(500, cancellationToken: ct);
        }

        /// <summary>
        /// タメ時間中：手の中に隠して非表示にし、羽音を完全に消す！
        /// </summary>
        public void HideInHands()
        {
            DOTween.Kill(transform);
            if (buzzAudio != null) buzzAudio.Stop(); // 羽音をピタッと止める！
            transform.position = Vector3.zero;
            if (_spriteRenderer != null) _spriteRenderer.enabled = false; // 手の中に隠す！
        }

        /// <summary>
        /// 成功時：ペちゃんこ蚊が出現し、左右によろけながら画面下にポトッと落ちていく！
        /// </summary>
        public void Squash()
        {
            DOTween.Kill(transform);
            if (_spriteRenderer != null)
            {
                DOTween.Kill(_spriteRenderer);
                if (squashedSprite != null) _spriteRenderer.sprite = squashedSprite;
                _spriteRenderer.enabled = true; // 姿を現す！
            }
            if (buzzAudio != null) buzzAudio.Stop();

            IsCatchable = false;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = _initialScale; // 画像自体が潰れているので自然なスケールでOK

            // 1. 最初の一瞬、頭をピヨピヨ震わせる（0.25秒）
            transform.DOShakePosition(0.25f, strength: 0.15f, vibrato: 10);

            // 2. 左右にヨロヨロ揺れながら、画面下（Y: -7）へヒラヒラと重力落下（1.2秒）
            transform.DOMoveY(-7f, 1.2f).SetEase(Ease.InQuad).SetDelay(0.15f);

            // 左右のふらつき（左右にゆらゆら）
            transform.DOMoveX(0.5f, 0.28f).SetLoops(4, LoopType.Yoyo).SetEase(Ease.InOutSine).SetDelay(0.15f);

            // 体の傾き（左右にパタパタ傾きながら落ちる）
            transform.DORotate(new Vector3(0, 0, 35f), 0.3f).SetLoops(4, LoopType.Yoyo).SetEase(Ease.InOutSine).SetDelay(0.15f);
        }

        /// <summary>
        /// 失敗時：手の中からドヤ顔蚊が飛び去る！
        /// </summary>
        public void Escape()
        {
            DOTween.Kill(transform);
            if (_spriteRenderer != null)
            {
                DOTween.Kill(_spriteRenderer);
                if (escapeSprite != null) _spriteRenderer.sprite = escapeSprite; // ドヤ顔スプライトに切り替え！
                _spriteRenderer.enabled = true; // 姿を現す！
            }
            if (buzzAudio != null)
            {
                buzzAudio.pitch = 1.0f;
                buzzAudio.Play();
            }
            IsCatchable = false;
            transform.DOMove(new Vector3(12f, 7f, 0f), 0.5f).SetEase(Ease.InQuad);
        }

        /// <summary>
        /// 時間切れ時のカメラ急接近
        /// </summary>
        public async UniTask CameraAttackAsync(CancellationToken ct)
        {
            DOTween.Kill(transform);
            IsCatchable = false;
            transform.DOMove(Vector3.zero, 0.2f);
            transform.DOScale(_initialScale * 7.0f, 0.35f);
            await UniTask.Delay(350, cancellationToken: ct);
        }

        /// <summary>
        /// 状態リセット
        /// </summary>
        public void ResetState()
        {
            DOTween.Kill(transform);
            if (_spriteRenderer != null)
            {
                DOTween.Kill(_spriteRenderer);
                _spriteRenderer.enabled = true; // 表示に戻す
                _spriteRenderer.color = _initialColor;
                if (_initialSprite != null) _spriteRenderer.sprite = _initialSprite;
            }
            if (buzzAudio != null)
            {
                buzzAudio.Stop();
                buzzAudio.pitch = 1.0f;
            }
            IsCatchable = false;
            transform.position = _startPosition;
            transform.rotation = Quaternion.identity;
            transform.localScale = _initialScale;
        }
    }
}