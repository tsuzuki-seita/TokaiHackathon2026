using UnityEngine;
using DG.Tweening;

namespace HaeTataki
{
    public class HaeTataki_HandActor : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool isLeftHand = true;
        [SerializeField] private float swingTargetX = 0f;

        private Vector3 _initialPosition;
        private SpriteRenderer _spriteRenderer;
        private Color _initialColor;

        private void Awake()
        {
            // ゲーム起動時に初期位置と初期色を記録しておく
            _initialPosition = transform.position;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null) _initialColor = _spriteRenderer.color;
        }

        /// <summary>
        /// 中央へ向かって平手打ちを行う
        /// </summary>
        public void Swing()
        {
            DOTween.Kill(transform); // 実行中のアニメーションがあれば止める
            var seq = DOTween.Sequence();
            seq.Append(transform.DOMoveX(swingTargetX, 0.08f).SetEase(Ease.InQuad))
               .AppendInterval(0.15f)
               .Append(transform.DOMove(_initialPosition, 0.25f).SetEase(Ease.OutQuad));
        }

        /// <summary>
        /// ミス時に手を痛がってプルプル震わせる
        /// </summary>
        public void PlayHurt()
        {
            DOTween.Kill(transform);
            if (_spriteRenderer != null)
            {
                DOTween.Kill(_spriteRenderer);
                _spriteRenderer.DOColor(Color.red, 0.1f).SetLoops(4, LoopType.Yoyo)
                               .OnComplete(() => _spriteRenderer.color = _initialColor);
            }
            transform.DOShakePosition(0.5f, strength: 0.35f, vibrato: 20);
        }

        /// <summary>
        /// リトライ時に位置と色を初期化する
        /// </summary>
        public void ResetState()
        {
            DOTween.Kill(transform);
            if (_spriteRenderer != null)
            {
                DOTween.Kill(_spriteRenderer);
                _spriteRenderer.color = _initialColor;
            }
            transform.position = _initialPosition;
        }
    }
}