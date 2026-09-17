using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting;

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
      _initialPosition = transform.position;
      _spriteRenderer = GetComponent<SpriteRenderer>();
      if(_spriteRenderer != null) _initialColor = _spriteRenderer.color;
    }

    public void Swing()
        {
            DOTween.Kill(transform);
            var seq = DOTween.Sequence();
            seq.Append(transform.DOMoveX(swingTargetX, 0.08f).SetEase(Ease.InQuad))
       .AppendInterval(0.15f)
       .Append(transform.DOMove(_initialPosition, 0.25f).SetEase(Ease.OutQuad));
        }
    public void PlayHurt()
        {
            DOTween.Kill(transform);
            if(_spriteRenderer != null)
            {
                DOTween.Kill(_spriteRenderer);
                _spriteRenderer.DOColor(Color.red, 0.1f).SetLoops(4,LoopType.Yoyo).OnComplete(() => _spriteRenderer.color = _initialColor);
            }
            transform.DOShakePosition(0.5f, strength: 0.35f, vibrato: 20);
        }
    public void ResetState()
        {
            DOTween.Kill(transform);
            if(_spriteRenderer != null)
            {
                DOTween.Kill(_spriteRenderer);
                _spriteRenderer.color = _initialColor;
            }
            transform.position = _initialPosition;
        }
    
      
    }



  }



