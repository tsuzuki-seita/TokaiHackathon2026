using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace HaeTataki
{
    public class HaeTataki_MosquitoActor : MonoBehaviour
    {
        [SerializeField] private AudioSource buzzAudio;
        public bool IsCatchable { get; private set; } = false;

        private Vector3 _initialScale;
        private Vector3 _startPosition = new Vector3(8f, 4.5f, 0f);
        private SpriteRenderer _spriteRenderer;
        private Color _initialColor;

        private void Awake()
        {
            _initialScale = transform.localScale;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null) _initialColor = _spriteRenderer.color;
        }

        /// <summary>
        /// 蚊の一連の飛行タイムライン
        /// </summary>
        public async UniTask StartFlightSequenceAsync(CancellationToken ct)
        {
            ResetState();
            if (buzzAudio != null) { buzzAudio.pitch = 1.0f; buzzAudio.Play(); }

            // 1. 不規則旋回（Catmull-Rom パス移動: 2.2秒）
            Vector3[] waypoints = new Vector3[] {
                new Vector3(4f, 2f, 0f), new Vector3(-3f, -1.5f, 0f),
                new Vector3(2f, -2f, 0f), new Vector3(0f, 2.5f, 0f)
            };
            transform.DOPath(waypoints, 2.2f, PathType.CatmullRom).SetEase(Ease.Linear);
            await UniTask.Delay(2200, cancellationToken: ct);

            // 2. 予兆（一瞬静止 ＆ 拡大縮小 ＆ 赤点滅: 0.6秒）
            if (buzzAudio != null) buzzAudio.pitch = 1.5f;
            if (_spriteRenderer != null) _spriteRenderer.DOColor(Color.red, 0.15f).SetLoops(4, LoopType.Yoyo);
            transform.DOScale(_initialScale * 1.35f, 0.2f).SetLoops(3, LoopType.Yoyo);
            await UniTask.Delay(600, cancellationToken: ct);

            // 3. チャンスゾーン（中央突入: 0.6秒滞在）
            IsCatchable = true;
            transform.DOMove(Vector3.zero, 0.15f);
            await UniTask.Delay(150, cancellationToken: ct);
            await UniTask.Delay(600, cancellationToken: ct);
            IsCatchable = false;

            // 4. 逃走（叩かれなかった場合）
            if (buzzAudio != null) buzzAudio.pitch = 1.0f;
            transform.DOMove(new Vector3(11f, 6f, 0f), 0.5f).SetEase(Ease.InQuad);
            await UniTask.Delay(500, cancellationToken: ct);
        }

        /// <summary>
        /// 挟まれた時（ペシャンコに変形）
        /// </summary>
        public void Squash()
        {
            DOTween.Kill(transform);
            if (_spriteRenderer != null) DOTween.Kill(_spriteRenderer);
            if (buzzAudio != null) buzzAudio.Stop();
            IsCatchable = false;
            transform.position = Vector3.zero;
            transform.localScale = new Vector3(_initialScale.x * 1.8f, _initialScale.y * 0.25f, 1f);
        }

        /// <summary>
        /// ミス時に飛び去る
        /// </summary>
        public void Escape()
        {
            DOTween.Kill(transform);
            if (_spriteRenderer != null) DOTween.Kill(_spriteRenderer);
            if (buzzAudio != null) buzzAudio.pitch = 1.0f;
            IsCatchable = false;
            transform.DOMove(new Vector3(12f, 7f, 0f), 0.4f).SetEase(Ease.InQuad);
        }

        /// <summary>
        /// 時間切れ時のカメラ急接近攻撃
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
                _spriteRenderer.color = _initialColor;
            }
            if (buzzAudio != null) buzzAudio.Stop();
            IsCatchable = false;
            transform.position = _startPosition;
            transform.localScale = _initialScale;
        }
    }
}