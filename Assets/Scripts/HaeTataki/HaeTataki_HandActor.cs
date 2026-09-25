using UnityEngine;
using DG.Tweening;

namespace HaeTataki
{
    public class HaeTataki_HandActor : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool isLeftHand = true;
        [SerializeField] private float swingTargetX = 0f;

        [Header("Sprites")]
        [SerializeField] private Sprite readySprite;  // 構え状態（パーの手）
        [SerializeField] private Sprite chopSprite;   // 挟み込み状態（今回のアタッチ画像）
        [SerializeField] private Sprite hurtSprite;   // 痛がり状態（hand_hurt_throbbing）

        private Vector3 _initialPosition;
        private SpriteRenderer _spriteRenderer;
        private Color _initialColor;

        private void Awake()
        {
            _initialPosition = transform.position;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _initialColor = _spriteRenderer.color;
                if (readySprite == null && _spriteRenderer.sprite != null)
                {
                    readySprite = _spriteRenderer.sprite;
                }
            }
        }

        /// <summary>
        /// 中央へ向かってスウィングし、中央到達時にチョップ画像へ切り替える
        /// </summary>
        public void SwingToCenter()
        {
            DOTween.Kill(transform);
            transform.DOMoveX(swingTargetX, 0.08f).SetEase(Ease.InQuad)
                     .OnComplete(() =>
                     {
                         // 中央で合わさった瞬間にチョップ画像に切り替える
                         if (chopSprite != null && _spriteRenderer != null)
                         {
                             _spriteRenderer.sprite = chopSprite;
                         }
                     });
        }

        /// <summary>
        /// ドキドキ中のプルプル緊張振動（指定時間だけ震え続ける）
        /// </summary>
        public void ShiverSuspense(float duration)
        {
            DOTween.Kill(transform);
            transform.DOShakePosition(duration, strength: 0.08f, vibrato: 15);
        }

        /// <summary>
        /// ミス時に痛がりスプライトに切り替えて激しく震える
        /// </summary>
        public void PlayHurt()
        {
            DOTween.Kill(transform);
            if (_spriteRenderer != null)
            {
                // 痛がりスプライト（真っ赤な痺れ手）に切り替え
                if (hurtSprite != null)
                {
                    _spriteRenderer.sprite = hurtSprite;
                }
                _spriteRenderer.DOColor(Color.red, 0.1f).SetLoops(4, LoopType.Yoyo)
                               .OnComplete(() => _spriteRenderer.color = _initialColor);
            }
            transform.DOShakePosition(0.5f, strength: 0.35f, vibrato: 20);
        }

        /// <summary>
        /// 判定終了後に元の待機位置へ戻る
        /// </summary>
        public void ReturnToInitial(float duration = 0.25f)
        {
            transform.DOMove(_initialPosition, duration).SetEase(Ease.OutQuad)
                     .OnComplete(() =>
                     {
                         if (readySprite != null && _spriteRenderer != null)
                         {
                             _spriteRenderer.sprite = readySprite;
                         }
                     });
        }

        /// <summary>
        /// リトライ時に初期化
        /// </summary>
        public void ResetState()
        {
            DOTween.Kill(transform);
            if (_spriteRenderer != null)
            {
                DOTween.Kill(_spriteRenderer);
                _spriteRenderer.color = _initialColor;
                if (readySprite != null) _spriteRenderer.sprite = readySprite;
            }
            transform.position = _initialPosition;
        }
    }
}