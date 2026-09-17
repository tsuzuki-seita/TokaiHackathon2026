using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace PoseGame.UI
{
    // A cheap UI-only confetti burst for result reveals — the kind of "会場が沸く" celebration
    // effect that reads well on a big screen at a camp/event, without needing a particle system.
    public static class ConfettiEffect
    {
        static readonly Color[] Palette =
        {
            new Color(1f, 0.35f, 0.4f),
            new Color(0.3f, 0.65f, 1f),
            new Color(0.4f, 0.9f, 0.45f),
            new Color(1f, 0.8f, 0.2f),
            new Color(0.85f, 0.5f, 1f),
            new Color(1f, 0.6f, 0.2f),
        };

        public static void Burst(RectTransform container, int count = 60)
        {
            if (container == null)
                return;

            var width = Mathf.Max(container.rect.width, 400f);
            var pieces = new List<Image>(count);

            for (var i = 0; i < count; i++)
            {
                var color = Palette[Random.Range(0, Palette.Length)];
                var piece = UIFactory.CreateDot($"confetti_{i}", container, color, Random.Range(10f, 20f));
                var rt = (RectTransform)piece.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                var startX = Random.Range(-width * 0.5f, width * 0.5f);
                rt.anchoredPosition = new Vector2(startX, Random.Range(0f, 80f));
                pieces.Add(piece);

                var fallDistance = Random.Range(500f, 900f);
                var duration = Random.Range(1.1f, 1.9f);
                var drift = Random.Range(-120f, 120f);
                var spinTarget = Random.Range(-540f, 540f);

                rt.DOAnchorPos(new Vector2(startX + drift, rt.anchoredPosition.y - fallDistance), duration)
                    .SetEase(Ease.InQuad)
                    .SetDelay(Random.Range(0f, 0.25f));
                rt.DORotate(new Vector3(0, 0, spinTarget), duration, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetDelay(Random.Range(0f, 0.25f));
                piece.DOFade(0f, 0.6f).SetDelay(duration * 0.6f)
                    .OnComplete(() => { if (piece != null) Object.Destroy(piece.gameObject); });
            }
        }
    }
}
