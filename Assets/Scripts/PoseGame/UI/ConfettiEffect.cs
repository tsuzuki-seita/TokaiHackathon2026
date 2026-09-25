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
                    .OnComplete(() =>
                    {
                        // The fade can finish before the position/rotation tweens above (their
                        // durations and delays are randomized independently) — kill them first so
                        // destroying this piece doesn't leave a tween pointing at a dead RectTransform.
                        if (piece == null) return;
                        rt.DOKill();
                        Object.Destroy(piece.gameObject);
                    });
            }
        }

        // Party-popper style celebration for the final round: two "crackers" fire from the
        // bottom-left and bottom-right corners, confetti bursting up and inward before falling,
        // instead of the plain top-down rain used for mid-game rounds.
        public static void BurstFromSides(RectTransform container, int countPerSide = 45)
        {
            if (container == null)
                return;

            SpawnFromSide(container, countPerSide, fromLeft: true);
            SpawnFromSide(container, countPerSide, fromLeft: false);
        }

        static void SpawnFromSide(RectTransform container, int count, bool fromLeft)
        {
            for (var i = 0; i < count; i++)
            {
                var color = Palette[Random.Range(0, Palette.Length)];
                var piece = UIFactory.CreateDot($"cracker_{(fromLeft ? "L" : "R")}_{i}", container, color, Random.Range(16f, 30f));
                var rt = (RectTransform)piece.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(fromLeft ? 0f : 1f, 0.15f);
                // Spread the origin point itself, not just the direction, so it doesn't read as
                // firing from one exact spot.
                rt.anchoredPosition = new Vector2(0f, Random.Range(-80f, 140f));

                // Wide cone of directions, from nearly straight up to well past the center of the
                // screen, so pieces scatter instead of all following the same narrow line.
                var angleDeg = fromLeft ? Random.Range(10f, 95f) : Random.Range(85f, 170f);
                var angleRad = angleDeg * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

                var burstDistance = Random.Range(500f, 1000f);
                var burstDuration = Random.Range(0.45f, 0.7f);
                var burstTarget = dir * burstDistance;

                var fallDistance = Random.Range(600f, 1000f);
                var fallDuration = Random.Range(1.4f, 2.2f);
                var fallTarget = burstTarget + new Vector2(Random.Range(-80f, 80f), -fallDistance);

                var spinTarget = Random.Range(-720f, 720f);
                var totalDuration = burstDuration + fallDuration;

                var seq = DOTween.Sequence();
                seq.Append(rt.DOAnchorPos(burstTarget, burstDuration).SetEase(Ease.OutCubic));
                seq.Append(rt.DOAnchorPos(fallTarget, fallDuration).SetEase(Ease.InQuad));

                rt.DORotate(new Vector3(0, 0, spinTarget), totalDuration, RotateMode.FastBeyond360).SetEase(Ease.Linear);

                piece.DOFade(0f, 0.6f).SetDelay(totalDuration - 0.6f)
                    .OnComplete(() =>
                    {
                        if (piece == null) return;
                        rt.DOKill();
                        Object.Destroy(piece.gameObject);
                    });
            }
        }
    }
}
