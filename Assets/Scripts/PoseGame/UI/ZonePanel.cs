using UnityEngine;
using UnityEngine.UI;

namespace PoseGame.UI
{
    // One player's video feed + skeleton overlay + live score gauge.
    public class ZonePanel
    {
        public readonly RectTransform Root;
        readonly RawImage m_Video;
        readonly SkeletonView m_Skeleton;
        readonly RectTransform m_GaugeFill;
        readonly Text m_ScoreText;
        readonly Text m_NameText;
        readonly Text m_NoDetectionText;
        readonly Color m_AccentColor;

        public static readonly Color[] PlayerColors =
        {
            new Color(1f, 0.35f, 0.4f),
            new Color(0.3f, 0.65f, 1f),
            new Color(0.4f, 0.9f, 0.45f),
            new Color(1f, 0.8f, 0.2f),
        };

        public ZonePanel(Transform parent, int playerIndex)
        {
            m_AccentColor = PlayerColors[playerIndex % PlayerColors.Length];

            var root = UIFactory.CreateRect($"ZonePanel_{playerIndex}", parent);
            Root = root;

            var frame = UIFactory.CreatePanel("Frame", root, m_AccentColor);
            UIFactory.Stretch((RectTransform)frame.transform);

            var videoArea = UIFactory.CreateRect("VideoArea", root);
            videoArea.anchorMin = Vector2.zero;
            videoArea.anchorMax = Vector2.one;
            videoArea.offsetMin = new Vector2(6, 46);
            videoArea.offsetMax = new Vector2(-6, -6);

            var videoBg = UIFactory.CreatePanel("VideoBg", videoArea, new Color(0.05f, 0.05f, 0.08f));
            UIFactory.Stretch((RectTransform)videoBg.transform);

            m_Video = UIFactory.CreateRawImage("Video", videoArea, null);
            UIFactory.Stretch((RectTransform)m_Video.transform);
            ApplyVideoFX(m_Video, m_AccentColor);

            m_Skeleton = new SkeletonView((RectTransform)m_Video.transform, Color.white);

            m_NoDetectionText = UIFactory.CreateText("NoDetection", videoArea, "検出中…", 24, new Color(1, 1, 1, 0.7f));
            UIFactory.Stretch((RectTransform)m_NoDetectionText.transform);

            m_NameText = UIFactory.CreateText("Name", root, $"プレイヤー{playerIndex + 1}", 22, m_AccentColor, TextAnchor.MiddleLeft);
            var nameRt = (RectTransform)m_NameText.transform;
            nameRt.anchorMin = new Vector2(0, 0);
            nameRt.anchorMax = new Vector2(0.55f, 0);
            nameRt.pivot = new Vector2(0, 0);
            nameRt.anchoredPosition = new Vector2(10, 6);
            nameRt.sizeDelta = new Vector2(0, 34);

            var gaugeBg = UIFactory.CreatePanel("GaugeBg", root, new Color(1, 1, 1, 0.15f));
            var gaugeBgRt = (RectTransform)gaugeBg.transform;
            gaugeBgRt.anchorMin = new Vector2(0.55f, 0);
            gaugeBgRt.anchorMax = new Vector2(1f, 0);
            gaugeBgRt.pivot = new Vector2(0, 0);
            gaugeBgRt.anchoredPosition = new Vector2(-6, 12);
            gaugeBgRt.sizeDelta = new Vector2(-4, 18);

            var gaugeFill = UIFactory.CreatePanel("GaugeFill", gaugeBg.transform, m_AccentColor);
            var fillRt = (RectTransform)gaugeFill.transform;
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(0, 1);
            fillRt.pivot = new Vector2(0, 0.5f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            m_GaugeFill = fillRt;

            m_ScoreText = UIFactory.CreateText("Score", root, "0%", 20, Color.white, TextAnchor.MiddleRight);
            var scoreRt = (RectTransform)m_ScoreText.transform;
            scoreRt.anchorMin = new Vector2(0.55f, 0);
            scoreRt.anchorMax = new Vector2(1f, 0);
            scoreRt.pivot = new Vector2(1, 0);
            scoreRt.anchoredPosition = new Vector2(-8, 6);
            scoreRt.sizeDelta = new Vector2(0, 34);
        }

        public bool IsAlive => Root != null;

        public void UpdateView(Texture videoTexture, PersonPose pose, float score01)
        {
            if (!IsAlive)
                return;

            m_Video.texture = videoTexture;
            m_Skeleton.UpdateFromPose(pose);
            m_NoDetectionText.gameObject.SetActive(!pose.IsDetected);

            var pct = Mathf.RoundToInt(Mathf.Clamp01(score01) * 100f);
            m_ScoreText.text = pose.IsDetected ? $"{pct}%" : "--%";
            var fillSize = m_GaugeFill.parent.GetComponent<RectTransform>().rect.width * Mathf.Clamp01(score01);
            m_GaugeFill.sizeDelta = new Vector2(fillSize, 0);
        }

        public void SetActive(bool active) => Root.gameObject.SetActive(active);

        static Shader s_VideoFxShader;

        static void ApplyVideoFX(RawImage video, Color accentColor)
        {
            if (s_VideoFxShader == null)
                s_VideoFxShader = Resources.Load<Shader>("Shaders/PoseGameVideoFX");
            if (s_VideoFxShader == null)
                return; // shader missing (e.g. stripped) — fall back to the plain feed

            var mat = new Material(s_VideoFxShader);
            mat.SetColor("_VignetteColor", new Color(accentColor.r * 0.5f, accentColor.g * 0.5f, accentColor.b * 0.5f, 0.85f));
            mat.SetFloat("_VignetteIntensity", 1.2f);
            mat.SetFloat("_Desaturate", 0.25f);
            mat.SetFloat("_Darken", 0.12f);
            video.material = mat;
        }
    }
}
