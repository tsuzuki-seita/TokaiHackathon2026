using System.Collections;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ContenaVolleyのゲーム進行（1シーンで完結）。
///
/// 流れ: 人数・名前入力 → (プレイヤーごとに) スタート用カーソルを叩く → N球スパイク
///       → 全員終了（ヒット率が shatterRatio 以上ならガラス粉砕） → リザルト → もう一度 / メンバー変更
///
/// 画面(パネル・文字)はエディターで作ってInspectorで割り当てる。
/// ガラス・カーソル・ボール・HUD・演出・デバッグ表示はコードで生成する。
/// 見た目と音は Kenney のアセット（Resources/ContenaVolley, CC0）を実行時に読み込んで使う。
///
/// キー操作（名前入力中は無効）:
///   Space/Enter : 次へ
///   D          : デバッグ表示(動き検出の様子)切り替え
///   H / クリック : 手動でヒット（カメラ無しでの動作確認用）
///   Esc        : 人数・名前入力に戻る
/// </summary>
public class CV_VolleyController : MonoBehaviour
{
    [Header("検出（空なら同じGameObjectから取得・自動追加）")]
    [SerializeField] CV_WebCamSource webCam;
    [Tooltip("フレーム差分版・骨格検出版など、CV_SpikeDetectorBaseを継承したもの")]
    [SerializeField] CV_SpikeDetectorBase detector;

    [Header("ゲーム表示")]
    [Tooltip("カメラ映像。AspectRatioFitter(Envelope Parent)を付けておく")]
    [SerializeField] RawImage cameraImage;
    [SerializeField] CV_GlassView glass;
    [Tooltip("カーソル・ボール・ポップアップ文字を置く、画面いっぱいの空オブジェクト")]
    [SerializeField] RectTransform fxLayer;
    [Tooltip("NICE!/MISS の文字のプレハブ")]
    [SerializeField] Text popTextPrefab;

    [Header("人数・名前入力画面")]
    [SerializeField] CV_SetupScreen setupScreen;

    [Header("交代画面（〇〇の番！ / おつかれさま）")]
    [SerializeField] GameObject turnPanel;
    [SerializeField] Text turnTitle;
    [SerializeField] Text turnMessage;
    [SerializeField] Text turnHint;

    [Tooltip("GO! などの中央の大きな文字")]
    [SerializeField] Text bigText;

    [Header("リザルト画面")]
    [SerializeField] GameObject resultPanel;
    [SerializeField] Text resultBody;
    [SerializeField] Button retryButton;
    [SerializeField] Button changeMemberButton;

    [Header("ルール")]
    [SerializeField, Range(1, 10)] int spikesPerPlayer = 3;
    [Tooltip("カーソルのゲージが0になるまでの秒数")]
    [SerializeField] float timeLimit = 4f;
    [Tooltip("ゲージが0になる瞬間の前後、この秒数だけスパイク判定を受け付ける（タイミング猶予）")]
    [SerializeField] float spikeTimingWindow = 0.15f;
    [Tooltip("カーソルが出てからこの秒数は反応しない（誤検出防止）")]
    [SerializeField] float armDelay = 0.5f;
    [Tooltip("ゲージが0になる瞬間とのズレがこの秒数以内なら PERFECT")]
    [SerializeField] float perfectWindow = 0.06f;
    [Tooltip("全員の合計ヒット率がこの割合以上ならガラスが割れる")]
    [SerializeField, Range(0f, 1f)] float shatterRatio = 0.7f;

    [Header("カーソル")]
    [Tooltip("判定半径（映像の高さに対する割合）")]
    [SerializeField, Range(0.03f, 0.3f)] float cursorRadius = 0.1f;
    [Tooltip("カーソルが出る範囲（画面に映っている映像に対する割合, x,y,幅,高さ）")]
    [SerializeField] Rect spawnArea = new Rect(0.15f, 0.5f, 0.7f, 0.35f);

    [Header("ボール")]
    [SerializeField] float ballSize = 110f;
    [SerializeField] float ballFlightTime = 0.35f;
    [Tooltip("着弾時の大きさ倍率。>1で画面手前(ガラス)に向かって飛んでくる、<1で奥に飛んでいく")]
    [SerializeField] float ballScaleAtImpact = 2.4f;
    [SerializeField] float crackSize = 420f;

    [Header("タイトル画面")]
    [Tooltip("起動時に最初に出す画像（Sprites/SelectScene/Thumbnails/ContenaVolley.png）")]
    [SerializeField] Sprite titleSprite;

    [Header("見た目")]
    [Tooltip("コードで生成する文字（デバッグ表示など）のフォント。空ならOSの日本語フォント")]
    [SerializeField] Font font;
    [SerializeField] Color[] playerColors =
    {
        new Color(1f, 0.35f, 0.35f),
        new Color(0.3f, 0.7f, 1f),
        new Color(1f, 0.85f, 0.2f),
        new Color(0.4f, 1f, 0.5f),
        new Color(0.85f, 0.5f, 1f),
    };

    [Header("デバッグ")]
    [SerializeField] bool showDebug;

    enum ResultChoice { None, Retry, ChangeMember }

    static readonly Color Gold = new Color(1f, 0.85f, 0.25f);
    static readonly Color MissColor = new Color(0.75f, 0.75f, 0.8f);

    // コードで生成するもの
    RectTransform canvasRect;
    AspectRatioFitter cameraFitter;
    CV_CursorView cursor;
    RectTransform ball;
    CV_Effects effects;
    CV_Hud hud;
    Image[] resultStars;
    GameObject debugRoot;
    RawImage debugImage;
    Text debugText;

    // 状態
    Coroutine loop;
    bool cameraBound;
    List<string> playerNames = new List<string>();
    CV_Hud.Judge[,] results;
    int totalHits;
    int combo;
    Vector2 lastSpawnUV = new Vector2(-1, -1);
    bool spikeHit;
    Vector2 spikeUV;
    float spikeOffset;
    ResultChoice resultChoice;
    Coroutine resultStarsRoutine;
    bool titleShown;
    GameObject titleRoot;
    Image titleImage;
    Image titlePressGlow;
    RectTransform turnSubPanel;
    const float TurnBandHeight = 170f;
    const float TurnSubHeight = 150f;

    int PlayerCount => playerNames.Count;
    int MaxHits => PlayerCount * spikesPerPlayer;
    float HitRatio => MaxHits > 0 ? (float)totalHits / MaxHits : 0f;
    /// <summary>ガラスを割るのに必要なヒット数</summary>
    int HitsToShatter => Mathf.CeilToInt(MaxHits * shatterRatio - 0.0001f);
    bool GlassCleared => totalHits >= HitsToShatter;

    void Awake()
    {
        if (webCam == null) webCam = GetComponent<CV_WebCamSource>();
        if (webCam == null) webCam = gameObject.AddComponent<CV_WebCamSource>();
        if (detector == null) detector = GetComponent<CV_SpikeDetectorBase>();
        if (detector == null) detector = gameObject.AddComponent<CV_FrameDiffSpikeDetector>();

        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        if (font != null) CV_UIUtil.DefaultFont = font;
        canvasRect = (RectTransform)cameraImage.canvas.rootCanvas.transform;
        cameraFitter = cameraImage.GetComponent<AspectRatioFitter>();
        cameraImage.color = Color.black;

        var cameraBG = (RectTransform)cameraImage.transform.parent;
        if (cameraBG.parent == canvasRect)
        {
            var vignette = CV_Effects.BuildVignette(canvasRect);
            vignette.transform.SetSiblingIndex(cameraBG.GetSiblingIndex() + 1);
        }

        glass.Build();
        cursor = CV_UIUtil.CreateRect("Cursor", fxLayer).gameObject.AddComponent<CV_CursorView>();
        cursor.Build();
        ball = BuildBall(fxLayer);

        hud = CV_UIUtil.CreateRect("CV_Hud", canvasRect).gameObject.AddComponent<CV_Hud>();
        hud.transform.SetSiblingIndex(fxLayer.GetSiblingIndex() + 1);
        hud.Build();

        ApplySkin();
        BuildDebugView();

        BuildTitle();

        // 演出は一番手前に出す
        effects = CV_UIUtil.CreateFullRect("CV_Effects", canvasRect).gameObject.AddComponent<CV_Effects>();
        effects.Build(cameraBG, fxLayer);

        retryButton.onClick.AddListener(() => resultChoice = ResultChoice.Retry);
        changeMemberButton.onClick.AddListener(() => resultChoice = ResultChoice.ChangeMember);
    }

    /// <summary>Inspectorの割り当て漏れをまとめて報告する</summary>
    bool ValidateReferences()
    {
        var refs = new (Object obj, string name)[]
        {
            (cameraImage, nameof(cameraImage)), (glass, nameof(glass)), (fxLayer, nameof(fxLayer)),
            (popTextPrefab, nameof(popTextPrefab)), (setupScreen, nameof(setupScreen)),
            (turnPanel, nameof(turnPanel)), (turnTitle, nameof(turnTitle)), (turnMessage, nameof(turnMessage)), (turnHint, nameof(turnHint)),
            (bigText, nameof(bigText)),
            (resultPanel, nameof(resultPanel)), (resultBody, nameof(resultBody)), (retryButton, nameof(retryButton)), (changeMemberButton, nameof(changeMemberButton)),
        };

        bool ok = true;
        foreach (var r in refs)
        {
            if (r.obj != null) continue;
            Debug.LogError($"[CV] CV_VolleyController の {r.name} が未設定です", this);
            ok = false;
        }
        return ok;
    }

    void Start()
    {
        loop = StartCoroutine(GameLoop());
    }

    void Update()
    {
        if (!cameraBound && webCam.IsReady) BindCamera();

        if (!IsTyping() && Input.GetKeyDown(KeyCode.D)) showDebug = !showDebug;
        debugRoot.SetActive(showDebug);
        if (showDebug)
        {
            debugImage.texture = detector.DebugTexture;
            debugText.text = detector.DebugInfo;
        }

        if (!IsTyping() && Input.GetKeyDown(KeyCode.Escape)) RestartToSetup();
    }

    void BindCamera()
    {
        cameraBound = true;
        cameraImage.texture = webCam.Texture;
        cameraImage.uvRect = webCam.DisplayUVRect;
        cameraImage.color = Color.white;
        if (cameraFitter != null) cameraFitter.aspectRatio = webCam.Aspect;

        var drt = debugImage.rectTransform;
        drt.sizeDelta = new Vector2(drt.sizeDelta.x, drt.sizeDelta.x / webCam.Aspect);
    }

    void RestartToSetup()
    {
        if (loop != null) StopCoroutine(loop);
        if (resultStarsRoutine != null) StopCoroutine(resultStarsRoutine);
        DOTween.Kill(ball);
        ball.gameObject.SetActive(false);
        cursor.HideImmediate();
        hud.HideImmediate();
        effects.ResetAll();
        if (titleRoot != null)
        {
            titlePressGlow.DOKill();
            titleRoot.SetActive(false);
        }
        bigText.text = "";
        loop = StartCoroutine(GameLoop());
    }

    // ───────────────────────── ゲームの流れ ─────────────────────────

    IEnumerator GameLoop()
    {
        if (!titleShown) yield return Title();

        bool needSetup = true;
        while (true)
        {
            if (needSetup) yield return Setup();
            yield return Play();
            yield return Result();
            needSetup = resultChoice == ResultChoice.ChangeMember;
        }
    }

    /// <summary>タイトル画面。何かキーを押す（クリックする）と人数・名前入力へ</summary>
    IEnumerator Title()
    {
        titleShown = true;
        if (titleRoot == null) yield break;

        ShowOnly(null);
        titleRoot.SetActive(true);
        var group = titleRoot.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.DOFade(1f, 0.5f);
        titleImage.rectTransform.localScale = Vector3.one * 1.08f;
        titleImage.rectTransform.DOScale(1f, 0.8f).SetEase(Ease.OutCubic);

        // PRESS ANY KEY の文字をじわっと光らせ、ときどきキラッとさせる
        titlePressGlow.DOFade(0.35f, 0.6f).From(0f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        float nextSparkle = 0.6f, t = 0f;

        yield return new WaitForSeconds(0.3f);
        while (!Input.anyKeyDown || IsTyping())
        {
            t += Time.deltaTime;
            if (t >= nextSparkle)
            {
                nextSparkle = t + Random.Range(0.35f, 0.8f);
                var r = titleImage.rectTransform.rect;
                var local = new Vector2(Random.Range(r.xMin, r.xMax) * 0.9f, Random.Range(r.yMin, r.yMax) * 0.8f);
                effects.Sparkle(titleImage.rectTransform.TransformPoint(local), Color.white, 6);
            }
            yield return null;
        }

        CV_Audio.Play("CV_se_go");
        CV_Audio.Play("CV_se_crack1", 0.7f);
        effects.Flash(Color.white, 0.7f, 0.4f);
        effects.Shake(20f, 0.3f);
        titlePressGlow.DOKill();
        titleImage.rectTransform.DOScale(1.15f, 0.35f).SetEase(Ease.InQuad);
        yield return group.DOFade(0f, 0.35f).WaitForCompletion();
        titleRoot.SetActive(false);
    }

    IEnumerator Setup()
    {
        ShowOnly(null);
        glass.ResetGlass();
        setupScreen.Open();
        yield return new WaitUntil(() => setupScreen.Submitted);
        playerNames = setupScreen.GetNames();
        setupScreen.Close();
    }

    IEnumerator Play()
    {
        ShowOnly(null);
        glass.ResetGlass();
        results = new CV_Hud.Judge[PlayerCount, spikesPerPlayer];
        totalHits = 0;

        while (!webCam.IsReady)
        {
            ShowTurn("カメラ準備中...", webCam.ErrorMessage ?? "", "", Color.white);
            yield return null;
        }

        PlayBGM(SoundManager.BGMData.BGMTYPE.CV_InGame);

        for (int p = 0; p < PlayerCount; p++)
        {
            yield return PlayerTurn(p);
        }

        yield return Finale();
    }

    IEnumerator PlayerTurn(int p)
    {
        var color = PlayerColor(p);

        // カメラの前に立ってもらい、スタート用のカーソルを叩いたら開始
        CV_Audio.Play("CV_se_turn");
        ShowTurn($"{playerNames[p]} の番！", "カメラの前に立って、光る丸をスパイクしてスタート", "（Space でもスタート）", color);
        var visible = VisibleUV();
        // 上の帯（交代画面）に隠れないよう、画面中央より少し下に出す
        yield return WaitForSpike(new Vector2(visible.center.x, Mathf.Lerp(visible.yMin, visible.yMax, 0.42f)), 0f, color);
        HideTurn();
        CV_Audio.PlayRandom("CV_se_spike", 3);
        effects.SpikeHit(fxLayer.TransformPoint(UVToFx(spikeUV)), color, false);

        yield return new WaitForSeconds(0.3f);
        CV_Audio.Play("CV_se_tick");
        yield return ShowBigText("READY?", Color.white, 0.35f);
        CV_Audio.Play("CV_se_go");
        effects.Shake(12f, 0.25f);
        effects.Sparkle(bigText.transform.position, color, 18);
        hud.Show(playerNames[p], color, spikesPerPlayer, totalHits, MaxHits);
        yield return ShowBigText("GO!", color, 0.3f);

        combo = 0;
        for (int b = 0; b < spikesPerPlayer; b++)
        {
            yield return Serve(p, b, color);
        }
        hud.SetCurrent(-1);
        yield return new WaitForSeconds(0.4f);
        hud.Hide();

        int hits = 0, perfects = 0;
        for (int b = 0; b < spikesPerPlayer; b++)
        {
            if (results[p, b] >= CV_Hud.Judge.Nice) hits++;
            if (results[p, b] == CV_Hud.Judge.Perfect) perfects++;
        }

        bool last = p == PlayerCount - 1;
        string perfectText = perfects > 0 ? $"（PERFECT {perfects}）" : "";
        CV_Audio.Play(hits > 0 ? "CV_se_star" : "CV_se_miss2", 0.8f);
        ShowTurn($"{playerNames[p]} おつかれさま！", $"{hits} / {spikesPerPlayer} ヒット{perfectText}",
            last ? "Space で次へ" : $"次は {playerNames[p + 1]}！ 交代してね（Space で次へ）", color);
        if (hits == spikesPerPlayer) effects.ConfettiBurst(canvasRect);
        yield return WaitForNext(last ? 3f : 0f);
    }

    IEnumerator Serve(int p, int b, Color color)
    {
        var uv = RandomSpawnUV();
        hud.SetCurrent(b);
        CV_Audio.Play("CV_se_cursor", 0.7f);
        yield return WaitForSpike(uv, timeLimit, color);

        if (spikeHit)
        {
            bool perfect = Mathf.Abs(spikeOffset) <= perfectWindow;
            var judge = perfect ? CV_Hud.Judge.Perfect : CV_Hud.Judge.Nice;
            results[p, b] = judge;
            totalHits++;
            combo++;

            var fxPos = UVToFx(spikeUV);
            effects.SpikeHit(fxLayer.TransformPoint(fxPos), color, perfect);
            CV_Audio.PlayRandom("CV_se_spike", 3);
            if (perfect) CV_Audio.Play("CV_se_perfect", 0.7f);
            PopText(perfect ? "PERFECT!" : "NICE!", fxPos + new Vector2(0, 110), perfect ? Gold : color, perfect ? 1.25f : 1f);

            hud.SetResult(b, judge);
            hud.SetTotal(totalHits, MaxHits);
            hud.SetCombo(combo);
            if (combo >= 2) CV_Audio.Play("CV_se_combo", 0.6f, 1f + 0.12f * (combo - 2), 0f);

            yield return BallFlight(spikeUV, color);
        }
        else
        {
            results[p, b] = CV_Hud.Judge.Miss;
            combo = 0;
            var fxPos = UVToFx(uv);
            effects.Miss(fxLayer.TransformPoint(fxPos));
            CV_Audio.Play("CV_se_miss", 0.8f);
            CV_Audio.Play("CV_se_miss2", 0.5f);
            PopText("MISS", fxPos + new Vector2(0, 110), MissColor, 0.9f);
            hud.SetResult(b, CV_Hud.Judge.Miss);
            hud.SetCombo(0);
        }

        yield return new WaitForSeconds(0.7f);
    }

    IEnumerator Finale()
    {
        ShowOnly(null);
        yield return ShowBigText("全員おつかれさま！", Color.white, 1.0f);

        // ためてから一気に割る
        CV_Audio.Play("CV_se_tick", 1f, 0.8f, 0f);
        glass.Rect.DOShakeAnchorPos(0.6f, 10f, 50).SetLink(glass.gameObject);
        yield return new WaitForSeconds(0.65f);

        if (!GlassCleared)
        {
            yield return FinaleFailed();
            yield break;
        }

        effects.HitStop(0.12f);
        effects.Flash(Color.white, 0.9f, 0.6f);
        effects.ShatterBurst(glass.Rect);
        StartCoroutine(ShatterSounds());
        var shatter = StartCoroutine(glass.Shatter());
        yield return new WaitForSeconds(0.35f);
        effects.ConfettiBurst(canvasRect);
        yield return shatter;
    }

    /// <summary>ヒット率が足りずガラスが割れなかったとき：ミシッときしむが持ちこたえる</summary>
    IEnumerator FinaleFailed()
    {
        CV_Audio.Play("CV_se_crack2", 0.8f, 0.7f, 0f);
        effects.Shake(14f, 0.3f);
        effects.Flash(Color.white, 0.25f, 0.3f);
        glass.Rect.DOShakeAnchorPos(0.4f, 16f, 40).SetLink(glass.gameObject);
        yield return new WaitForSeconds(0.6f);

        CV_Audio.Play("CV_se_miss2", 0.8f);
        yield return ShowBigText("ガラスは割れなかった…", MissColor, 1.0f);
        yield return ShowBigText($"あと {HitsToShatter - totalHits} ヒット！", Gold, 0.8f);
    }

    IEnumerator ShatterSounds()
    {
        CV_Audio.Play("CV_se_shatter1", 1f, 0.9f);
        yield return new WaitForSecondsRealtime(0.07f);
        CV_Audio.Play("CV_se_shatter2", 1f);
        CV_Audio.Play("CV_se_crack1", 0.8f, 0.8f);
        yield return new WaitForSecondsRealtime(0.18f);
        CV_Audio.Play("CV_se_shatter3", 0.9f);
        yield return new WaitForSecondsRealtime(0.35f);
        CV_Audio.Play("CV_se_tinkle", 0.8f, 1.2f);
        yield return new WaitForSecondsRealtime(0.3f);
        CV_Audio.Play("CV_se_tinkle", 0.6f, 1.4f);
    }

    IEnumerator Result()
    {
        bool cleared = GlassCleared;
        CV_Audio.Play(cleared ? "CV_se_clear" : "CV_se_miss2");
        ShowOnly(resultPanel);
        resultBody.text = BuildResultText();
        // 星の下(y=225)からボタンの上(y=-300)までに収まる文字サイズにする。
        // 1行の高さはおよそ fontSize * 1.6（日本語フォント + 行間 1.1）
        int lines = PlayerCount + 2;
        resultBody.fontSize = Mathf.Clamp(Mathf.FloorToInt(520f / (lines * 1.6f)), 28, 54);
        resultChoice = ResultChoice.None;

        var rt = (RectTransform)resultPanel.transform;
        rt.localScale = Vector3.one * 0.6f;
        rt.DOScale(1f, 0.4f).SetEase(Ease.OutBack);

        if (resultStarsRoutine != null) StopCoroutine(resultStarsRoutine);
        resultStarsRoutine = StartCoroutine(ResultStars());
        if (cleared) effects.StartConfettiRain(canvasRect);

        yield return null;
        while (resultChoice == ResultChoice.None)
        {
            if (NextPressed()) resultChoice = ResultChoice.Retry;
            yield return null;
        }

        effects.StopConfettiRain();
        effects.Particles.Clear();
    }

    /// <summary>
    /// ヒット率で 0〜3 個の星を1つずつ光らせる。
    /// ★2 = ガラスを割れた、★3 = ほぼ全部ヒット、★1 = 割れなかったが1回以上ヒット
    /// </summary>
    IEnumerator ResultStars()
    {
        float ratio = HitRatio;
        int stars = GlassCleared ? (ratio >= 0.9f ? 3 : 2) : ratio > 0f ? 1 : 0;

        foreach (var s in resultStars)
        {
            s.rectTransform.DOKill();
            s.rectTransform.localScale = Vector3.zero;
        }
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < stars; i++)
        {
            var s = resultStars[i].rectTransform;
            s.localScale = Vector3.one * 2.2f;
            s.localRotation = Quaternion.Euler(0, 0, -40);
            s.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
            s.DOLocalRotate(Vector3.zero, 0.35f).SetEase(Ease.OutBack);
            CV_Audio.Play("CV_se_star", 1f, 1f + i * 0.15f, 0f);
            yield return new WaitForSeconds(0.2f);
            effects.Sparkle(s.position, Gold, 16);
            effects.Shake(8f + i * 6f, 0.2f);
            yield return new WaitForSeconds(0.2f);
        }
        if (stars == 3)
        {
            CV_Audio.Play("CV_se_perfect", 0.8f);
            effects.ConfettiBurst(canvasRect);
            foreach (var s in resultStars)
                s.rectTransform.DOScale(1.12f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
        resultStarsRoutine = null;
    }

    string BuildResultText()
    {
        // いちばんヒットが多い人（同数なら PERFECT が多い人）が MVP
        int best = -1, bestScore = 0;
        var scores = new int[PlayerCount];
        for (int p = 0; p < PlayerCount; p++)
        {
            for (int b = 0; b < spikesPerPlayer; b++) scores[p] += results[p, b] == CV_Hud.Judge.Perfect ? 3 : results[p, b] == CV_Hud.Judge.Nice ? 2 : 0;
            if (scores[p] > bestScore) { bestScore = scores[p]; best = p; }
        }
        int bestCount = 0;
        foreach (var sc in scores) if (sc == bestScore) bestCount++;
        if (PlayerCount < 2 || bestCount > 1) best = -1;

        string gold = ColorUtility.ToHtmlStringRGB(Gold);
        string grey = ColorUtility.ToHtmlStringRGB(new Color(0.55f, 0.55f, 0.6f));
        var sb = new StringBuilder();
        string headline = GlassCleared ? "ガラス粉砕！" : $"ガラスは割れなかった…（あと {HitsToShatter - totalHits} ヒット）";
        sb.AppendLine($"{headline}  合計 <color=#{gold}>{totalHits}</color> / {MaxHits} ヒット\n");
        for (int p = 0; p < PlayerCount; p++)
        {
            string pc = ColorUtility.ToHtmlStringRGB(PlayerColor(p));
            if (p == best) sb.Append($"<color=#{gold}>MVP</color> ");
            sb.Append($"<color=#{pc}>{playerNames[p]}</color>  ");
            for (int b = 0; b < spikesPerPlayer; b++)
            {
                switch (results[p, b])
                {
                    case CV_Hud.Judge.Perfect: sb.Append($"<color=#{gold}>★</color>"); break;
                    case CV_Hud.Judge.Nice: sb.Append($"<color=#{pc}>●</color>"); break;
                    default: sb.Append($"<color=#{grey}>×</color>"); break;
                }
            }
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// カーソルを出してスパイクを待つ。結果は spikeHit / spikeUV に入る。
    /// limit <= 0 なら時間無制限（いつ叩いてもよく、Spaceでもスキップ可）。
    /// limit > 0 のときはゲージが0になる瞬間（±spikeTimingWindow秒）だけ判定を受け付ける。
    /// </summary>
    IEnumerator WaitForSpike(Vector2 uv, float limit, Color color)
    {
        spikeHit = false;
        spikeUV = uv;
        spikeOffset = 0f;
        cursor.Show(UVToFx(uv), CursorDiameter(), color, limit > 0 ? "" : "ここをスパイク！", limit > 0);
        detector.ResetState();

        float t = 0f;
        yield return null;

        if (limit <= 0)
        {
            // 時間無制限：従来通りいつ叩いてもヒット
            float nextRipple = 0f;
            while (true)
            {
                t += Time.deltaTime;
                if (t >= nextRipple)
                {
                    nextRipple = t + 0.9f;
                    effects.Ripple(cursor.Rect.position, CursorDiameter(), color);
                }
                if (t >= armDelay)
                {
                    if (detector.Detect(uv, cursorRadius, out var hitUV))
                    {
                        spikeHit = true;
                        spikeUV = hitUV;
                        break;
                    }
                    if (DebugHitPressed() || NextPressed())
                    {
                        spikeHit = true;
                        break;
                    }
                }
                yield return null;
            }
            cursor.Hide(spikeHit);
            yield break;
        }

        // ゲージが0になる直前までは表示だけ更新し、判定はまだ行わない
        float windowStart = Mathf.Max(armDelay, limit - spikeTimingWindow);
        while (t < windowStart)
        {
            t += Time.deltaTime;
            cursor.SetTimer(1f - t / limit);
            yield return null;
        }

        // ゲージが0になる瞬間の前後（windowStart 〜 limit + spikeTimingWindow）だけ判定
        float windowEnd = limit + spikeTimingWindow;
        cursor.SetHot(true);
        CV_Audio.Play("CV_se_tick", 0.6f, 1.3f, 0f);
        while (t < windowEnd)
        {
            t += Time.deltaTime;
            cursor.SetTimer(Mathf.Max(0f, 1f - t / limit));

            if (detector.Detect(uv, cursorRadius, out var hitUV))
            {
                spikeHit = true;
                spikeUV = hitUV;
                break;
            }
            if (DebugHitPressed())
            {
                spikeHit = true;
                break;
            }
            yield return null;
        }

        spikeOffset = t - limit;
        cursor.Hide(spikeHit);
    }

    /// <summary>打った位置からガラスへボールが飛び、当たったところにヒビが入る</summary>
    IEnumerator BallFlight(Vector2 fromUV, Color color)
    {
        Vector2 start = UVToFx(fromUV);
        // 打った位置から少し画面中央寄りに着弾させる
        Vector2 end = Vector2.Lerp(start, Vector2.zero, 0.3f) + Random.insideUnitCircle * 120f;
        end = fxLayer.InverseTransformPoint(glass.ClampInside(fxLayer.TransformPoint(end), 150f));

        DOTween.Kill(ball);
        ball.gameObject.SetActive(true);
        ball.anchoredPosition = start;
        ball.localScale = Vector3.one;
        ball.localRotation = Quaternion.identity;

        CV_Audio.Play("CV_se_whoosh", 0.6f, 1.3f);
        var seq = DOTween.Sequence().SetTarget(ball);
        seq.Join(ball.DOJumpAnchorPos(end, 140f, 1, ballFlightTime).SetEase(Ease.Linear));
        seq.Join(ball.DOScale(ballScaleAtImpact, ballFlightTime).SetEase(Ease.InQuad));
        seq.Join(ball.DOLocalRotate(new Vector3(0, 0, 540f), ballFlightTime, RotateMode.FastBeyond360));

        // 飛んでいる間は残像を出す
        while (seq.IsActive() && !seq.IsComplete())
        {
            if (Time.deltaTime > 0f) effects.BallGhost(ball.position, ballSize * ball.localScale.x, 0f, color);
            yield return null;
        }

        var impactWorld = fxLayer.TransformPoint(end);
        CV_Audio.PlayRandom("CV_se_crack", 3);
        CV_Audio.Play("CV_se_tinkle", 0.5f, 1.1f);
        glass.AddCrack(impactWorld, crackSize);
        effects.GlassImpact(impactWorld, color);

        // 当たったボールは跳ね返って落ちる
        var fall = DOTween.Sequence().SetTarget(ball);
        fall.Join(ball.DOAnchorPos(end + new Vector2(Random.Range(-200f, 200f), -900f), 0.6f).SetEase(Ease.InQuad));
        fall.Join(ball.DOScale(ballScaleAtImpact * 0.7f, 0.6f));
        fall.OnComplete(() => ball.gameObject.SetActive(false));
    }

    // ───────────────────────── 座標 ─────────────────────────

    /// <summary>カメラ映像のUV → エフェクト層のローカル座標</summary>
    Vector2 UVToFx(Vector2 uv)
    {
        var rt = cameraImage.rectTransform;
        var r = rt.rect;
        var world = rt.TransformPoint(new Vector3(r.xMin + uv.x * r.width, r.yMin + uv.y * r.height, 0));
        return fxLayer.InverseTransformPoint(world);
    }

    Vector2 WorldToUV(Vector3 world)
    {
        var rt = cameraImage.rectTransform;
        var r = rt.rect;
        Vector2 local = rt.InverseTransformPoint(world);
        return new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height);
    }

    /// <summary>カメラ映像のうち画面に映っている範囲（UV）</summary>
    Rect VisibleUV()
    {
        var corners = new Vector3[4];
        canvasRect.GetWorldCorners(corners);
        var a = WorldToUV(corners[0]);
        var b = WorldToUV(corners[2]);
        return Rect.MinMaxRect(Mathf.Max(0, a.x), Mathf.Max(0, a.y), Mathf.Min(1, b.x), Mathf.Min(1, b.y));
    }

    Vector2 RandomSpawnUV()
    {
        var v = VisibleUV();
        Vector2 uv = Vector2.zero;
        for (int i = 0; i < 10; i++)
        {
            float x = spawnArea.x + Random.value * spawnArea.width;
            float y = spawnArea.y + Random.value * spawnArea.height;
            uv = new Vector2(v.xMin + x * v.width, v.yMin + y * v.height);
            if (Vector2.Distance(uv, lastSpawnUV) > 0.2f) break; // 同じ場所が続かないように
        }
        lastSpawnUV = uv;
        return uv;
    }

    float CursorDiameter() => cameraImage.rectTransform.rect.height * cursorRadius * 2f;

    // ───────────────────────── 入力 ─────────────────────────

    /// <summary>名前入力欄に文字を打っている最中か（ショートカットキーを無効にする）</summary>
    static bool IsTyping()
    {
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return selected != null && selected.TryGetComponent<InputField>(out var field) && field.isFocused;
    }

    static bool NextPressed() => !IsTyping() && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return));

    bool DebugHitPressed()
    {
        if (Input.GetKeyDown(KeyCode.H)) return true;
        return Input.GetMouseButtonDown(0) && RectTransformUtility.RectangleContainsScreenPoint(cursor.Rect, Input.mousePosition, null);
    }

    /// <summary>Space を待つ。autoSeconds > 0 ならその秒数で自動的に進む</summary>
    IEnumerator WaitForNext(float autoSeconds)
    {
        yield return null;
        float t = 0f;
        while (!NextPressed() && (autoSeconds <= 0 || t < autoSeconds))
        {
            t += Time.deltaTime;
            yield return null;
        }
    }

    // ───────────────────────── 画面の切り替え・演出 ─────────────────────────

    /// <summary>指定したパネルだけを表示する（nullなら全部隠す）</summary>
    void ShowOnly(GameObject panel)
    {
        setupScreen.gameObject.SetActive(panel == setupScreen.gameObject);
        turnPanel.SetActive(panel == turnPanel);
        resultPanel.SetActive(panel == resultPanel);
    }

    void ShowTurn(string title, string message, string hint, Color color)
    {
        bool wasActive = turnPanel.activeSelf;
        bool titleChanged = turnTitle.text != title;
        turnPanel.SetActive(true);
        turnTitle.text = title;
        turnTitle.color = color;
        turnMessage.text = message;
        turnHint.text = hint;
        if (turnSubPanel != null) turnSubPanel.gameObject.SetActive(!string.IsNullOrEmpty(message) || !string.IsNullOrEmpty(hint));

        // 上から帯が降りてくる
        var rt = (RectTransform)turnPanel.transform;
        if (!wasActive)
        {
            rt.DOKill();
            rt.anchoredPosition = new Vector2(0, 360);
            rt.DOAnchorPosY(0, 0.45f).SetEase(Ease.OutBack);
        }
        if (titleChanged)
        {
            turnTitle.rectTransform.DOKill();
            turnTitle.rectTransform.localScale = Vector3.one * 1.4f;
            turnTitle.rectTransform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        }
    }

    void HideTurn()
    {
        var rt = (RectTransform)turnPanel.transform;
        rt.DOKill();
        rt.DOAnchorPosY(360, 0.25f).SetEase(Ease.InBack).OnComplete(() =>
        {
            turnPanel.SetActive(false);
            rt.anchoredPosition = Vector2.zero;
        });
    }

    IEnumerator ShowBigText(string text, Color color, float hold)
    {
        bigText.DOKill();
        bigText.rectTransform.DOKill();
        bigText.text = text;
        bigText.font = CV_Assets.FontFor(text);
        bigText.color = color;
        bigText.rectTransform.localScale = Vector3.one * 3f;
        bigText.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(-12f, 12f));
        bigText.rectTransform.DOLocalRotate(Vector3.zero, 0.3f).SetEase(Ease.OutBack);
        yield return bigText.rectTransform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).WaitForCompletion();
        yield return new WaitForSeconds(hold);
        bigText.rectTransform.DOScale(1.3f, 0.2f);
        yield return bigText.DOFade(0f, 0.2f).WaitForCompletion();
        bigText.text = "";
    }

    void PopText(string text, Vector2 fxPos, Color color, float scale = 1f)
    {
        var t = Instantiate(popTextPrefab, fxLayer);
        t.text = text;
        t.font = CV_Assets.FontFor(text);
        t.color = color;
        CV_UISkin.AddOutline(t.gameObject, new Color(0.05f, 0.08f, 0.2f, 0.9f), 4f);
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = fxPos;
        rt.localScale = Vector3.one * 0.3f;
        rt.localRotation = Quaternion.Euler(0, 0, Random.Range(-10f, 10f));
        var seq = DOTween.Sequence().SetLink(t.gameObject);
        seq.Append(rt.DOScale(scale, 0.35f).SetEase(Ease.OutElastic, 1.2f, 0.4f));
        seq.Join(rt.DOAnchorPosY(fxPos.y + 140f, 1f).SetEase(Ease.OutQuad));
        seq.Insert(0.6f, t.DOFade(0f, 0.4f));
        seq.Insert(0.6f, rt.DOScale(scale * 1.2f, 0.4f));
        seq.OnComplete(() => Destroy(t.gameObject));
    }

    Color PlayerColor(int p) => playerColors.Length > 0 ? playerColors[p % playerColors.Length] : Color.white;

    // ───────────────────────── BGM（SoundManagerが無いシーン単体でも動くように。効果音は CV_Audio） ─────────────────────────

    static void PlayBGM(SoundManager.BGMData.BGMTYPE bgm)
    {
        if (SoundManager.Instance == null) return;
        try { SoundManager.Instance.PlayBGM(bgm); }
        catch (System.Exception) { Debug.LogWarning($"[CV] BGM未登録: {bgm}"); }
    }

    // ───────────────────────── コードで生成する表示 ─────────────────────────

    /// <summary>シーンのシンプルなUIに Kenney UI Pack の見た目を当て、足りない飾りを作る</summary>
    void ApplySkin()
    {
        CV_UISkin.SkinLabel(bigText, Color.white, 170, 6f);

        // 交代画面：画面上部の帯にまとめて、カーソルが隠れないようにする
        // 交代画面：上の濃い帯に「〇〇の番！」だけ、黄色い線をはさんで下の明るいパネルに説明とヒントを置く
        var band = turnPanel.transform.Find("Panel");
        if (band != null && band.TryGetComponent<Image>(out var bandImg))
        {
            var brt = bandImg.rectTransform;
            // シーンでは縦のスケールを縮めて帯の太さを調整しているので、1に戻して高さは sizeDelta で決める
            // （スケールが残っていると、子にした文字まで縦に潰れる）
            brt.localScale = Vector3.one;
            brt.anchorMin = new Vector2(0, 1);
            brt.anchorMax = new Vector2(1, 1);
            brt.pivot = new Vector2(0.5f, 1);
            brt.anchoredPosition = Vector2.zero;
            brt.sizeDelta = new Vector2(0, TurnBandHeight);
            CV_UISkin.SkinBackdrop(bandImg, 0.78f);

            var line = CV_UIUtil.CreateImage("Accent", brt, null, new Color(1f, 0.85f, 0.25f, 1f), new Vector2(0, 8));
            line.rectTransform.anchorMin = new Vector2(0, 0);
            line.rectTransform.anchorMax = new Vector2(1, 0);
            line.rectTransform.pivot = new Vector2(0.5f, 1);
            line.rectTransform.anchoredPosition = Vector2.zero;

            var sub = CV_UIUtil.CreateImage("SubPanel", brt, null, new Color(0.93f, 0.96f, 1f, 0.9f), new Vector2(0, TurnSubHeight));
            turnSubPanel = sub.rectTransform;
            turnSubPanel.anchorMin = new Vector2(0, 0);
            turnSubPanel.anchorMax = new Vector2(1, 0);
            turnSubPanel.pivot = new Vector2(0.5f, 1);
            turnSubPanel.anchoredPosition = new Vector2(0, -8);

            PlaceTurnText(turnTitle, brt, 0f, 96, Color.white, true);
            PlaceTurnText(turnMessage, turnSubPanel, 38f, 48, CV_UISkin.TextDark, false);
            PlaceTurnText(turnHint, turnSubPanel, -34f, 34, new Color(0.35f, 0.4f, 0.55f), false);
        }

        // リザルト画面
        CV_UISkin.SkinBackdrop(resultPanel.GetComponent<Image>(), 0.8f);
        var sceneTitle = resultPanel.transform.Find("TitleText");
        if (sceneTitle != null) sceneTitle.gameObject.SetActive(false);
        var logo = CV_UISkin.CreateLogo(resultPanel.transform, "RESULT", 120, Gold);
        logo.rectTransform.anchoredPosition = new Vector2(0, 450);
        logo.rectTransform.DOScale(1.05f, 0.8f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(logo.gameObject);

        resultStars = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            bool middle = i == 1;
            float size = middle ? 150 : 120;
            var slot = CV_UIUtil.CreateImage("StarSlot", resultPanel.transform, CV_Assets.StarEmpty, new Color(1, 1, 1, 0.5f), new Vector2(size, size * 120f / 128f));
            slot.rectTransform.anchoredPosition = new Vector2((i - 1) * 180, middle ? 330 : 305);
            resultStars[i] = CV_UIUtil.CreateImage("Star", slot.rectTransform, CV_Assets.StarFull, Color.white, Vector2.zero);
            CV_UIUtil.Stretch(resultStars[i].rectTransform);
            resultStars[i].rectTransform.localScale = Vector3.zero;
        }

        CV_UISkin.SkinLabel(resultBody, Color.white);
        // 星の真下から下に向かって並べる（行数が増えても上の星に重ならない）
        var bodyRt = resultBody.rectTransform;
        bodyRt.anchorMin = bodyRt.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRt.pivot = new Vector2(0.5f, 1f);
        bodyRt.anchoredPosition = new Vector2(0, 225);
        resultBody.alignment = TextAnchor.UpperCenter;
        resultBody.supportRichText = true;
        resultBody.lineSpacing = 1.1f;
        CV_UISkin.SkinButton(retryButton, CV_Assets.ButtonYellow, CV_UISkin.TextDark);
        CV_UISkin.SkinButton(changeMemberButton, CV_Assets.ButtonBlue, Color.white);
    }

    /// <summary>起動時のタイトル画面（画像は titleSprite）</summary>
    void BuildTitle()
    {
        if (titleSprite == null)
        {
            Debug.LogWarning("[CV] titleSprite が未設定なのでタイトル画面を飛ばします", this);
            return;
        }

        var root = CV_UIUtil.CreateFullRect("CV_Title", canvasRect);
        titleRoot = root.gameObject;
        root.gameObject.AddComponent<CanvasGroup>();

        var bg = CV_UIUtil.CreateImage("Back", root, null, Color.black, Vector2.zero);
        CV_UIUtil.Stretch(bg.rectTransform);

        // 画面の縦横比が違っても隙間が出ないように、はみ出す側に合わせて敷き詰める
        titleImage = CV_UIUtil.CreateImage("Image", root, titleSprite, Color.white, Vector2.zero);
        CV_UIUtil.Stretch(titleImage.rectTransform);
        var fitter = titleImage.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = titleSprite.rect.width / titleSprite.rect.height;

        // 画像下部の「PRESS ANY KEY」に重ねる光
        titlePressGlow = CV_UIUtil.CreateImage("PressGlow", titleImage.rectTransform, CV_Assets.Fx("circle_05"), new Color(1f, 1f, 0.85f, 0f), Vector2.zero);
        var grt = titlePressGlow.rectTransform;
        grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0.1f);
        grt.sizeDelta = new Vector2(900, 200);

        root.gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    const string DefaultTitleSpriteGuid = "685c3b9639ac849fcbb80e1952f273c4";

    /// <summary>エディター上でタイトル画像が空なら、Thumbnails の画像を自動で入れる</summary>
    void OnValidate()
    {
        if (titleSprite != null) return;
        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(DefaultTitleSpriteGuid);
        if (string.IsNullOrEmpty(path)) return;
        titleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        // シーンを保存すれば割り当てが残る（ビルドでも表示される）
        if (titleSprite != null && !Application.isPlaying) UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    /// <summary>交代画面の文字を panel の中央から y だけずらした位置に置く</summary>
    static void PlaceTurnText(Text t, RectTransform panel, float y, int fontSize, Color color, bool outline)
    {
        var rt = t.rectTransform;
        rt.SetParent(panel, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, y);
        t.alignment = TextAnchor.MiddleCenter;
        t.gameObject.SetActive(true);
        CV_UISkin.SkinLabel(t, color, fontSize, outline ? 3f : 0f);
    }

    RectTransform BuildBall(Transform parent)
    {
        var root = CV_UIUtil.CreateRect("Ball", parent);
        root.sizeDelta = Vector2.one * ballSize;

        // 後ろの光
        var glow = CV_UIUtil.CreateImage("Glow", root, CV_Assets.Fx("circle_05"), new Color(1f, 0.95f, 0.7f, 0.6f), Vector2.zero);
        CV_UIUtil.Stretch(glow.rectTransform);
        glow.rectTransform.localScale = Vector3.one * 1.7f;

        var shadow = CV_UIUtil.CreateImage("Shadow", root, CV_Assets.Ball, new Color(0, 0, 0, 0.35f), Vector2.zero);
        CV_UIUtil.Stretch(shadow.rectTransform);
        shadow.rectTransform.anchoredPosition = new Vector2(8, -8);

        // Kenney Sports Pack のバレーボール（配色を調整したもの）
        var body = CV_UIUtil.CreateImage("Body", root, CV_Assets.Ball, Color.white, Vector2.zero);
        CV_UIUtil.Stretch(body.rectTransform);

        root.gameObject.SetActive(false);
        return root;
    }

    /// <summary>右下に動き検出の様子を出す（Dキーで切り替え）</summary>
    void BuildDebugView()
    {
        var dbg = CV_UIUtil.CreateRect("CV_Debug", canvasRect);
        dbg.anchorMin = dbg.anchorMax = dbg.pivot = new Vector2(1, 0);
        dbg.anchoredPosition = new Vector2(-20, 20);
        dbg.sizeDelta = new Vector2(420, 236);
        debugRoot = dbg.gameObject;
        debugImage = dbg.gameObject.AddComponent<RawImage>();
        debugImage.raycastTarget = false;

        debugText = CV_UIUtil.CreateText("Info", dbg, 24, Color.white, TextAnchor.LowerLeft);
        var trt = debugText.rectTransform;
        trt.anchorMin = trt.anchorMax = new Vector2(0, 1);
        trt.pivot = Vector2.zero;
        trt.anchoredPosition = new Vector2(0, 40);

        var help = CV_UIUtil.CreateText("Keys", dbg, 22, Color.white, TextAnchor.LowerLeft);
        var hrt = help.rectTransform;
        hrt.anchorMin = hrt.anchorMax = new Vector2(0, 1);
        hrt.pivot = Vector2.zero;
        hrt.anchoredPosition = new Vector2(0, 8);
        help.text = "D:デバッグ  H/クリック:手動ヒット  Esc:メンバー入力へ";
    }
}
