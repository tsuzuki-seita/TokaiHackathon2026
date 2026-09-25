using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace PoseGame.UI
{
    public class PoseGameUI : MonoBehaviour
    {
        public Action OnStartRequested;
        public Action OnPlayAgainRequested;
        public Action OnBackToTitleRequested;

        Canvas m_Canvas;
        RectTransform m_TitlePanel;
        RectTransform m_PlayPanel;
        RectTransform m_ResultPanel;

        // Play
        RectTransform m_ZoneRow;
        readonly System.Collections.Generic.List<ZonePanel> m_ZonePanels = new System.Collections.Generic.List<ZonePanel>();
        Text m_ThemeText;
        string m_ThemeLabel;
        Text m_RoundText;
        Text m_CountdownText;
        RectTransform m_CountdownRoot;
        RectTransform m_ReadyRoot;
        Text m_ReadyHintText;
        Button m_ReadyButton;

        // Result
        Text m_ResultTitle;
        Text m_TeamScoreText;
        Text m_TeamScoreCommentText;
        RawImage m_PhotoThumbnail;
        GameObject m_PhotoThumbFrame;
        Text m_PhotoStatusText;
        Button m_PlayAgainButton;
        Button m_BackToTitleButton;
        Text m_NextRoundHintText;

        // Flash
        Image m_FlashOverlay;

        void Awake()
        {
            BuildRoot();
            BuildTitlePanel();
            BuildPlayPanel();
            BuildResultPanel();
            BuildFlashOverlay();
            ShowTitle();
        }

        void BuildRoot()
        {
            var canvasGo = new GameObject("PoseGameCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            m_Canvas = canvasGo.AddComponent<Canvas>();
            m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var eventSystemExists = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystemExists == null)
            {
                var esGo = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var bg = UIFactory.CreatePanel("Background", m_Canvas.transform, new Color(0.08f, 0.09f, 0.12f));
            UIFactory.Stretch((RectTransform)bg.transform);
        }

        void BuildTitlePanel()
        {
            m_TitlePanel = UIFactory.CreateRect("TitlePanel", m_Canvas.transform);
            UIFactory.Stretch(m_TitlePanel);

            // Life is Tech!-style bright, colorful look (colors pulled from their live site's
            // CSS): a clean off-white base with vivid blue as the primary accent, instead of the
            // shared dark-navy app background — only shown behind the title screen.
            var titleBackdrop = UIFactory.CreatePanel("TitleBackdrop", m_TitlePanel, new Color(0.965f, 0.965f, 0.97f));
            UIFactory.Stretch((RectTransform)titleBackdrop.transform);
            titleBackdrop.raycastTarget = false;

            BuildTitleBackgroundArt(m_TitlePanel);
            BuildTitleMascot(m_TitlePanel);

            var titleTextColor = new Color(0.11f, 0.11f, 0.12f); // #1D1D1F
            var title = UIFactory.CreateText("Title", m_TitlePanel, "せーのでポーズ！", 100, titleTextColor);
            var titleRt = (RectTransform)title.transform;
            titleRt.anchorMin = new Vector2(0.5f, 0.62f);
            titleRt.anchorMax = new Vector2(0.5f, 0.62f);
            titleRt.sizeDelta = new Vector2(1400, 140);

            var subtitle = UIFactory.CreateText("Subtitle", m_TitlePanel,
                "カメラの前でお題のポーズをキメて、チームの一致度をきそおう！\nラウンドが進むごとに、参加人数が増えていきます(最大3人)。", 32,
                new Color(0.11f, 0.11f, 0.12f, 0.75f), TextAnchor.MiddleCenter, FontStyle.Normal);
            var subRt = (RectTransform)subtitle.transform;
            subRt.anchorMin = new Vector2(0.5f, 0.46f);
            subRt.anchorMax = new Vector2(0.5f, 0.46f);
            subRt.sizeDelta = new Vector2(1500, 100);

            var startBtn = UIFactory.CreateButton("StartButton", m_TitlePanel, "スタート", new Color(0f, 0.37f, 1f), Color.white, 52);
            var startRt = (RectTransform)startBtn.transform;
            startRt.anchorMin = new Vector2(0.5f, 0.26f);
            startRt.anchorMax = new Vector2(0.5f, 0.26f);
            startRt.sizeDelta = new Vector2(420, 100);
            startBtn.onClick.AddListener(() => OnStartRequested?.Invoke());
            startRt.DOScale(1.08f, 0.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }

        // Two friendly mascots flanking the title — a gentle bob + sway so they read as "alive"
        // without the jerkiness that made the old title text animation feel off. Slightly offset
        // timing so the pair doesn't move in lockstep. Not mirrored: the asset pack's license
        // asks that the character's look not be altered, and flipping would reverse which hand
        // holds the camera.
        void BuildTitleMascot(RectTransform parent)
        {
            var texture = Resources.Load<Texture2D>("Sprites/FoxMascot");
            if (texture == null)
                return;

            // "space-between" style with the title occupying the middle, but pulled in from the
            // screen edges for breathing room, and sitting a bit lower than the title's baseline.
            CreateMascot(parent, texture, 0.15f, phaseOffset: 0f);
            CreateMascot(parent, texture, 0.85f, phaseOffset: 0.3f);
        }

        static void CreateMascot(RectTransform parent, Texture2D texture, float anchorX, float phaseOffset)
        {
            var mascot = UIFactory.CreateRawImage("Mascot", parent, texture);
            mascot.raycastTarget = false;
            var rt = (RectTransform)mascot.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(anchorX, 0.58f); // a bit lower than the title text
            var aspect = (float)texture.width / texture.height;
            const float height = 200f;
            rt.sizeDelta = new Vector2(height * aspect, height);

            rt.DOAnchorPosY(rt.anchoredPosition.y + 16f, 1.6f).SetDelay(phaseOffset).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            rt.DOLocalRotate(new Vector3(0, 0, 4f), 2.2f).SetDelay(phaseOffset).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }

        // Decorative, code-only art behind the title text: soft diamond color washes echoing the
        // title's multi-color LIT-brand accent palette (blue/yellow/green/purple), drifting
        // slowly so the title screen stays lively when idle (booth/camp setting). No image
        // assets — matches the project's flat-color style.
        void BuildTitleBackgroundArt(RectTransform parent)
        {
            var art = UIFactory.CreateRect("BackgroundArt", parent);
            UIFactory.Stretch(art);

            var blueWash = UIFactory.CreateDot("BlueWash", art, new Color(0f, 0.37f, 1f, 0.14f), 900f); // #005FFF
            blueWash.raycastTarget = false;
            var blueRt = (RectTransform)blueWash.transform;
            blueRt.anchorMin = blueRt.anchorMax = new Vector2(0.12f, 0.82f);
            blueRt.localRotation = Quaternion.Euler(0, 0, 45f);
            blueRt.DOAnchorPosY(blueRt.anchoredPosition.y + 40f, 4.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);

            var yellowWash = UIFactory.CreateDot("YellowWash", art, new Color(0.98f, 0.78f, 0f, 0.16f), 800f); // #FBC800
            yellowWash.raycastTarget = false;
            var yellowRt = (RectTransform)yellowWash.transform;
            yellowRt.anchorMin = yellowRt.anchorMax = new Vector2(0.9f, 0.15f);
            yellowRt.localRotation = Quaternion.Euler(0, 0, 45f);
            yellowRt.DOAnchorPosY(yellowRt.anchoredPosition.y - 40f, 5.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);

            var greenWash = UIFactory.CreateDot("GreenWash", art, new Color(0f, 0.64f, 0.27f, 0.12f), 700f); // #00A446
            greenWash.raycastTarget = false;
            var greenRt = (RectTransform)greenWash.transform;
            greenRt.anchorMin = greenRt.anchorMax = new Vector2(0.82f, 0.85f);
            greenRt.localRotation = Quaternion.Euler(0, 0, 45f);
            greenRt.DOAnchorPosX(greenRt.anchoredPosition.x - 30f, 4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);

            var purpleWash = UIFactory.CreateDot("PurpleWash", art, new Color(0.42f, 0.28f, 0.94f, 0.10f), 650f); // #6C48EF
            purpleWash.raycastTarget = false;
            var purpleRt = (RectTransform)purpleWash.transform;
            purpleRt.anchorMin = purpleRt.anchorMax = new Vector2(0.1f, 0.14f);
            purpleRt.localRotation = Quaternion.Euler(0, 0, 45f);
            purpleRt.DOAnchorPosX(purpleRt.anchoredPosition.x + 30f, 5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }

        void BuildPlayPanel()
        {
            m_PlayPanel = UIFactory.CreateRect("PlayPanel", m_Canvas.transform);
            UIFactory.Stretch(m_PlayPanel);
            m_PlayPanel.gameObject.SetActive(false);

            var topBar = UIFactory.CreateRect("TopBar", m_PlayPanel);
            topBar.anchorMin = new Vector2(0, 1);
            topBar.anchorMax = new Vector2(1, 1);
            topBar.pivot = new Vector2(0.5f, 1);
            topBar.sizeDelta = new Vector2(0, 160);

            m_RoundText = UIFactory.CreateText("RoundText", topBar, "", 26, new Color(1, 1, 1, 0.7f));
            var roundRt = (RectTransform)m_RoundText.transform;
            roundRt.anchorMin = new Vector2(0, 1);
            roundRt.anchorMax = new Vector2(0, 1);
            roundRt.pivot = new Vector2(0, 1);
            roundRt.anchoredPosition = new Vector2(24, -14);
            roundRt.sizeDelta = new Vector2(400, 36);

            m_ThemeText = UIFactory.CreateText("ThemeText", topBar, "", 64, Color.white);
            var themeRt = (RectTransform)m_ThemeText.transform;
            themeRt.anchorMin = new Vector2(0.5f, 1);
            themeRt.anchorMax = new Vector2(0.5f, 1);
            themeRt.pivot = new Vector2(0.5f, 1);
            themeRt.anchoredPosition = new Vector2(0, -20);
            themeRt.sizeDelta = new Vector2(1200, 100);

            m_ZoneRow = UIFactory.CreateRect("ZoneRow", m_PlayPanel);
            m_ZoneRow.anchorMin = new Vector2(0, 0);
            m_ZoneRow.anchorMax = new Vector2(1, 1);
            m_ZoneRow.offsetMin = new Vector2(24, 24);
            m_ZoneRow.offsetMax = new Vector2(-24, -170);

            var hlg = m_ZoneRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            BuildReadyOverlay();

            m_CountdownRoot = UIFactory.CreateRect("CountdownRoot", m_PlayPanel);
            UIFactory.Stretch(m_CountdownRoot);
            var countdownBg = UIFactory.CreatePanel("CountdownBg", m_CountdownRoot, new Color(0, 0, 0, 0.45f));
            UIFactory.Stretch((RectTransform)countdownBg.transform);
            m_CountdownText = UIFactory.CreateText("CountdownText", m_CountdownRoot, "3", 220, Color.white);
            var cdRt = (RectTransform)m_CountdownText.transform;
            cdRt.anchorMin = new Vector2(0.5f, 0.5f);
            cdRt.anchorMax = new Vector2(0.5f, 0.5f);
            cdRt.sizeDelta = new Vector2(600, 300);
            m_CountdownRoot.gameObject.SetActive(false);
        }

        // A translucent banner (not a full block) shown over the middle of the screen each round
        // so everyone can see themselves already in their camera zone and get into position
        // before tapping ready — instead of the round just starting on a fixed timer.
        void BuildReadyOverlay()
        {
            m_ReadyRoot = UIFactory.CreateRect("ReadyRoot", m_PlayPanel);
            m_ReadyRoot.anchorMin = new Vector2(0, 0.5f);
            m_ReadyRoot.anchorMax = new Vector2(1, 0.5f);
            m_ReadyRoot.pivot = new Vector2(0.5f, 0.5f);
            m_ReadyRoot.sizeDelta = new Vector2(0, 260);

            var bg = UIFactory.CreatePanel("ReadyBg", m_ReadyRoot, new Color(0, 0, 0, 0.55f));
            UIFactory.Stretch((RectTransform)bg.transform);

            m_ReadyHintText = UIFactory.CreateText("ReadyHint", m_ReadyRoot, "カメラに映ってる？位置を確認してね！", 34, Color.white);
            var hintRt = (RectTransform)m_ReadyHintText.transform;
            hintRt.anchorMin = new Vector2(0.5f, 0.62f);
            hintRt.anchorMax = new Vector2(0.5f, 0.62f);
            hintRt.sizeDelta = new Vector2(1400, 60);

            m_ReadyButton = UIFactory.CreateButton("ReadyButton", m_ReadyRoot, "準備OK！", new Color(0.3f, 0.75f, 0.4f), Color.white, 46);
            var readyBtnRt = (RectTransform)m_ReadyButton.transform;
            readyBtnRt.anchorMin = new Vector2(0.5f, 0.3f);
            readyBtnRt.anchorMax = new Vector2(0.5f, 0.3f);
            readyBtnRt.sizeDelta = new Vector2(360, 90);
            readyBtnRt.DOScale(1.06f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);

            m_ReadyRoot.gameObject.SetActive(false);
        }

        public void ShowReadyPrompt(Action onReady)
        {
            m_ReadyRoot.gameObject.SetActive(true);
            m_ReadyButton.onClick.RemoveAllListeners();
            m_ReadyButton.onClick.AddListener(() => onReady?.Invoke());
        }

        public void HideReadyPrompt()
        {
            m_ReadyRoot.gameObject.SetActive(false);
        }

        // Reuses the theme text's spot (already in a clearly visible, unobstructed part of the
        // top bar) to show the scoring countdown, instead of a separate badge that risked
        // overlapping the video zones below it.
        public void ShowScoringHint()
        {
            m_ThemeText.color = new Color(1f, 0.85f, 0.3f);
        }

        // Called every frame while the scoring window is active, so the label reads like a
        // countdown ("計測中！残り2秒" → "残り1秒" → …) instead of a fixed label.
        public void UpdateScoringHint(float secondsRemaining)
        {
            var secs = Mathf.CeilToInt(Mathf.Max(0f, secondsRemaining));
            m_ThemeText.text = $"{m_ThemeLabel}　計測中！残り{secs}秒";
        }

        public void HideScoringHint()
        {
            m_ThemeText.text = m_ThemeLabel;
            m_ThemeText.color = Color.white;
        }

        void BuildResultPanel()
        {
            m_ResultPanel = UIFactory.CreateRect("ResultPanel", m_Canvas.transform);
            UIFactory.Stretch(m_ResultPanel);
            m_ResultPanel.gameObject.SetActive(false);

            m_ResultTitle = UIFactory.CreateText("ResultTitle", m_ResultPanel, "結果発表！", 64, Color.white);
            var titleRt = (RectTransform)m_ResultTitle.transform;
            titleRt.anchorMin = new Vector2(0.5f, 0.8f);
            titleRt.anchorMax = new Vector2(0.5f, 0.8f);
            titleRt.sizeDelta = new Vector2(1200, 100);

            m_TeamScoreText = UIFactory.CreateText("TeamScore", m_ResultPanel, "みんなの一致度  0%", 100, new Color(1f, 0.85f, 0.3f));
            var teamScoreRt = (RectTransform)m_TeamScoreText.transform;
            teamScoreRt.anchorMin = new Vector2(0.5f, 0.58f);
            teamScoreRt.anchorMax = new Vector2(0.5f, 0.58f);
            teamScoreRt.sizeDelta = new Vector2(1700, 150);

            m_TeamScoreCommentText = UIFactory.CreateText("TeamScoreComment", m_ResultPanel, "", 34,
                new Color(1, 1, 1, 0.85f), TextAnchor.MiddleCenter, FontStyle.Normal);
            var commentRt = (RectTransform)m_TeamScoreCommentText.transform;
            commentRt.anchorMin = new Vector2(0.5f, 0.44f);
            commentRt.anchorMax = new Vector2(0.5f, 0.44f);
            commentRt.sizeDelta = new Vector2(1500, 60);

            // Bottom cluster, top to bottom: photo thumbnail, its status caption, then the
            // buttons/hint row — each given its own band so they can't overlap (they used to sit
            // almost on top of each other at y=0.16/0.17/0.28, which visibly collided).
            var thumbFrame = UIFactory.CreatePanel("PhotoThumbFrame", m_ResultPanel, new Color(1, 1, 1, 0.9f));
            var thumbFrameRt = (RectTransform)thumbFrame.transform;
            thumbFrameRt.anchorMin = new Vector2(0.5f, 0.30f);
            thumbFrameRt.anchorMax = new Vector2(0.5f, 0.30f);
            thumbFrameRt.sizeDelta = new Vector2(300, 176);
            m_PhotoThumbFrame = thumbFrame.gameObject;
            m_PhotoThumbFrame.SetActive(false);

            m_PhotoThumbnail = UIFactory.CreateRawImage("PhotoThumbnail", thumbFrame.transform, null);
            var thumbRt = (RectTransform)m_PhotoThumbnail.transform;
            thumbRt.anchorMin = Vector2.zero;
            thumbRt.anchorMax = Vector2.one;
            thumbRt.offsetMin = new Vector2(6, 6);
            thumbRt.offsetMax = new Vector2(-6, -6);

            m_PhotoStatusText = UIFactory.CreateText("PhotoStatus", m_ResultPanel, "", 20, new Color(1, 1, 1, 0.75f));
            var photoStatusRt = (RectTransform)m_PhotoStatusText.transform;
            photoStatusRt.anchorMin = new Vector2(0.5f, 0.185f);
            photoStatusRt.anchorMax = new Vector2(0.5f, 0.185f);
            photoStatusRt.sizeDelta = new Vector2(400, 30);

            m_PlayAgainButton = UIFactory.CreateButton("PlayAgainButton", m_ResultPanel, "もういちど", new Color(0.95f, 0.55f, 0.15f), Color.white, 40);
            var againRt = (RectTransform)m_PlayAgainButton.transform;
            againRt.anchorMin = new Vector2(0.32f, 0.10f);
            againRt.anchorMax = new Vector2(0.32f, 0.10f);
            againRt.sizeDelta = new Vector2(380, 90);
            m_PlayAgainButton.onClick.AddListener(() => OnPlayAgainRequested?.Invoke());

            m_BackToTitleButton = UIFactory.CreateButton("BackToTitleButton", m_ResultPanel, "タイトルへ", new Color(0.25f, 0.27f, 0.33f), Color.white, 40);
            var titleBtnRt = (RectTransform)m_BackToTitleButton.transform;
            titleBtnRt.anchorMin = new Vector2(0.68f, 0.10f);
            titleBtnRt.anchorMax = new Vector2(0.68f, 0.10f);
            titleBtnRt.sizeDelta = new Vector2(380, 90);
            m_BackToTitleButton.onClick.AddListener(() => OnBackToTitleRequested?.Invoke());

            // Shown in the same spot as the buttons during a mid-game round result, so it's
            // clear the screen will auto-advance rather than being the end of the game.
            m_NextRoundHintText = UIFactory.CreateText("NextRoundHint", m_ResultPanel, "つぎのラウンドへ…", 32,
                new Color(1, 1, 1, 0.75f), TextAnchor.MiddleCenter, FontStyle.Normal);
            var nextHintRt = (RectTransform)m_NextRoundHintText.transform;
            nextHintRt.anchorMin = new Vector2(0.5f, 0.10f);
            nextHintRt.anchorMax = new Vector2(0.5f, 0.10f);
            nextHintRt.sizeDelta = new Vector2(600, 60);
        }

        void BuildFlashOverlay()
        {
            m_FlashOverlay = UIFactory.CreatePanel("PhotoFlash", m_Canvas.transform, new Color(1, 1, 1, 0));
            UIFactory.Stretch((RectTransform)m_FlashOverlay.transform);
            m_FlashOverlay.raycastTarget = false;
        }

        // Quick photo-booth style flash, played the instant a pose is "locked in".
        public void PlayPhotoFlash()
        {
            m_FlashOverlay.DOKill();
            m_FlashOverlay.color = new Color(1, 1, 1, 0.9f);
            m_FlashOverlay.DOFade(0f, 0.4f).SetEase(Ease.OutQuad);
        }

        public void ShowTitle()
        {
            m_TitlePanel.gameObject.SetActive(true);
            m_PlayPanel.gameObject.SetActive(false);
            m_ResultPanel.gameObject.SetActive(false);
        }

        public void ShowPlay(int zoneCount)
        {
            m_TitlePanel.gameObject.SetActive(false);
            m_ResultPanel.gameObject.SetActive(false);
            m_PlayPanel.gameObject.SetActive(true);
            RebuildZonePanels(zoneCount);
        }

        void RebuildZonePanels(int zoneCount)
        {
            foreach (var panel in m_ZonePanels)
                UnityEngine.Object.Destroy(panel.Root.gameObject);
            m_ZonePanels.Clear();

            for (var i = 0; i < zoneCount; i++)
                m_ZonePanels.Add(new ZonePanel(m_ZoneRow, i));
        }

        public void UpdateZone(int index, Texture videoTexture, PersonPose pose, float score01)
        {
            if (index < 0 || index >= m_ZonePanels.Count)
                return;
            m_ZonePanels[index].UpdateView(videoTexture, pose, score01);
        }

        public void SetRoundText(int round, int totalRounds) => m_RoundText.text = $"ラウンド {round}/{totalRounds}";

        public void AnnounceTheme(PoseTheme theme)
        {
            m_ThemeLabel = $"{theme.Emoji}  お題：{theme.DisplayName}";
            m_ThemeText.text = m_ThemeLabel;
            m_ThemeText.color = Color.white;
            m_ThemeText.transform.localScale = Vector3.one * 0.6f;
            m_ThemeText.transform.DOKill();
            m_ThemeText.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        }

        public void ShowCountdown(string label)
        {
            m_CountdownRoot.gameObject.SetActive(true);
            m_CountdownText.text = label;
            m_CountdownText.transform.localScale = Vector3.one * 1.4f;
            m_CountdownText.transform.DOKill();
            m_CountdownText.transform.DOScale(1f, 0.25f).SetEase(Ease.OutCubic);
        }

        public void HideCountdown()
        {
            m_CountdownRoot.gameObject.SetActive(false);
        }

        public void ShowResults(PoseTheme theme, float teamScore01, bool isFinalRound, Texture2D photo = null, string photoPath = null)
        {
            m_PlayPanel.gameObject.SetActive(false);
            m_ResultPanel.gameObject.SetActive(true);
            m_ResultTitle.text = isFinalRound
                ? $"🎉 全ラウンド終了！ {theme.Emoji} {theme.DisplayName}"
                : $"{theme.Emoji} {theme.DisplayName}";

            // Only the final round is actually the end of the game — mid-game rounds auto-advance
            // on a timer, so hide the play-again/back-to-title buttons there instead of leaving
            // them clickable and making it ambiguous whether the game is already over.
            m_PlayAgainButton.gameObject.SetActive(isFinalRound);
            m_BackToTitleButton.gameObject.SetActive(isFinalRound);
            m_NextRoundHintText.gameObject.SetActive(!isFinalRound);

            var pct = Mathf.RoundToInt(Mathf.Clamp01(teamScore01) * 100f);
            m_TeamScoreText.text = $"みんなの一致度  {pct}%";
            m_TeamScoreCommentText.text = TeamScoreComment(pct);
            m_TeamScoreText.transform.localScale = Vector3.one * 0.6f;
            m_TeamScoreText.transform.DOKill();
            m_TeamScoreText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutElastic);

            if (photo != null)
            {
                m_PhotoThumbFrame.SetActive(true);
                m_PhotoThumbnail.texture = photo;
                m_PhotoStatusText.text = string.IsNullOrEmpty(photoPath) ? "📸 記念写真を撮影しました" : "📸 保存しました";
            }
            else
            {
                m_PhotoThumbFrame.SetActive(false);
                m_PhotoStatusText.text = "";
            }

            // Everyone gets some confetti regardless of score (this is a cooperative game, not
            // a competition), just scaled up a bit when the group really nailed the sync.
            ConfettiEffect.Burst(m_ResultPanel, Mathf.RoundToInt(Mathf.Lerp(30, 90, teamScore01)));
        }

        static string TeamScoreComment(int pct)
        {
            if (pct >= 90) return "神シンクロ！完璧に息が合ってた！";
            if (pct >= 75) return "かなりいい感じ！みんなバッチリ！";
            if (pct >= 55) return "まずまずシンクロ！もう一声いける！";
            if (pct >= 30) return "ちょっとバラバラ…もう一度合わせてみよう！";
            return "てんでバラバラ！次はせーので合わせよう！";
        }
    }
}
