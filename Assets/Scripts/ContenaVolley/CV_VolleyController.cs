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
///       → 全員終了でガラス粉砕 → リザルト → もう一度 / メンバー変更
///
/// 画面(パネル・文字)はエディターで作ってInspectorで割り当てる。
/// ガラス・カーソル・ボール・デバッグ表示はコードで生成する。
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

    // コードで生成するもの
    RectTransform canvasRect;
    AspectRatioFitter cameraFitter;
    CV_CursorView cursor;
    RectTransform ball;
    GameObject debugRoot;
    RawImage debugImage;
    Text debugText;

    // 状態
    Coroutine loop;
    bool cameraBound;
    List<string> playerNames = new List<string>();
    bool[,] results;
    int totalHits;
    Vector2 lastSpawnUV = new Vector2(-1, -1);
    bool spikeHit;
    Vector2 spikeUV;
    ResultChoice resultChoice;

    int PlayerCount => playerNames.Count;
    int MaxHits => PlayerCount * spikesPerPlayer;

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

        glass.Build();
        cursor = CV_UIUtil.CreateRect("Cursor", fxLayer).gameObject.AddComponent<CV_CursorView>();
        cursor.Build();
        ball = BuildBall(fxLayer);
        BuildDebugView();

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
        DOTween.Kill(ball);
        ball.gameObject.SetActive(false);
        cursor.HideImmediate();
        bigText.text = "";
        loop = StartCoroutine(GameLoop());
    }

    // ───────────────────────── ゲームの流れ ─────────────────────────

    IEnumerator GameLoop()
    {
        bool needSetup = true;
        while (true)
        {
            if (needSetup) yield return Setup();
            yield return Play();
            yield return Result();
            needSetup = resultChoice == ResultChoice.ChangeMember;
        }
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
        results = new bool[PlayerCount, spikesPerPlayer];
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
        ShowTurn($"{playerNames[p]} の番！", "カメラの前に立って、丸をスパイクしてスタート", "（Space でもスタート）", color);
        var visible = VisibleUV();
        yield return WaitForSpike(new Vector2(visible.center.x, Mathf.Lerp(visible.yMin, visible.yMax, 0.6f)), 0f, color);
        turnPanel.SetActive(false);
        PlaySE(SoundManager.SEData.SETYPE.CV_Spike);

        yield return ShowBigText("GO!", color, 0.5f);

        for (int b = 0; b < spikesPerPlayer; b++)
        {
            yield return Serve(p, b, color);
        }

        int hits = 0;
        for (int b = 0; b < spikesPerPlayer; b++) if (results[p, b]) hits++;

        bool last = p == PlayerCount - 1;
        ShowTurn($"{playerNames[p]} おつかれさま！", $"{hits} / {spikesPerPlayer} ヒット",
            last ? "Space で次へ" : $"次は {playerNames[p + 1]}！ 交代してね（Space で次へ）", color);
        yield return WaitForNext(last ? 3f : 0f);
    }

    IEnumerator Serve(int p, int b, Color color)
    {
        var uv = RandomSpawnUV();
        PlaySE(SoundManager.SEData.SETYPE.CV_CursorAppear);
        yield return WaitForSpike(uv, timeLimit, color);

        if (spikeHit)
        {
            results[p, b] = true;
            totalHits++;
            PlaySE(SoundManager.SEData.SETYPE.CV_Spike);
            PopText("NICE!", UVToFx(spikeUV) + new Vector2(0, 90), color);
            yield return BallFlight(spikeUV);
        }
        else
        {
            PlaySE(SoundManager.SEData.SETYPE.CV_Miss);
            PopText("MISS…", UVToFx(uv) + new Vector2(0, 90), new Color(0.75f, 0.75f, 0.75f));
        }

        yield return new WaitForSeconds(0.7f);
    }

    IEnumerator Finale()
    {
        ShowOnly(null);
        yield return ShowBigText("全員おつかれさま！", Color.white, 1.2f);

        PlaySE(SoundManager.SEData.SETYPE.CV_GlassShatter);
        yield return glass.Shatter();
    }

    IEnumerator Result()
    {
        PlaySE(SoundManager.SEData.SETYPE.CV_Clear);
        ShowOnly(resultPanel);
        resultBody.text = BuildResultText();
        resultChoice = ResultChoice.None;

        var rt = (RectTransform)resultPanel.transform;
        rt.localScale = Vector3.one * 0.6f;
        rt.DOScale(1f, 0.4f).SetEase(Ease.OutBack);

        yield return null;
        while (resultChoice == ResultChoice.None)
        {
            if (NextPressed()) resultChoice = ResultChoice.Retry;
            yield return null;
        }
    }

    string BuildResultText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"ガラス粉砕！  合計 {totalHits} / {MaxHits} ヒット\n");
        for (int p = 0; p < PlayerCount; p++)
        {
            sb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(PlayerColor(p))}>{playerNames[p]}</color>  ");
            for (int b = 0; b < spikesPerPlayer; b++) sb.Append(results[p, b] ? "●" : "○");
            sb.AppendLine();
        }
        return sb.ToString();
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
        cursor.Show(UVToFx(uv), CursorDiameter(), color, "", limit > 0);
        detector.ResetState();

        float t = 0f;
        yield return null;

        if (limit <= 0)
        {
            // 時間無制限：従来通りいつ叩いてもヒット
            while (true)
            {
                t += Time.deltaTime;
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

        cursor.Hide(spikeHit);
    }

    /// <summary>打った位置からガラスへボールが飛び、当たったところにヒビが入る</summary>
    IEnumerator BallFlight(Vector2 fromUV)
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

        var seq = DOTween.Sequence().SetTarget(ball);
        seq.Join(ball.DOJumpAnchorPos(end, 140f, 1, ballFlightTime).SetEase(Ease.Linear));
        seq.Join(ball.DOScale(ballScaleAtImpact, ballFlightTime).SetEase(Ease.InQuad));
        seq.Join(ball.DOLocalRotate(new Vector3(0, 0, 540f), ballFlightTime, RotateMode.FastBeyond360));
        yield return seq.WaitForCompletion();

        PlaySE(SoundManager.SEData.SETYPE.CV_GlassCrack);
        glass.AddCrack(fxLayer.TransformPoint(end), crackSize);

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
        turnPanel.SetActive(true);
        turnTitle.text = title;
        turnTitle.color = color;
        turnMessage.text = message;
        turnHint.text = hint;
    }

    IEnumerator ShowBigText(string text, Color color, float hold)
    {
        bigText.DOKill();
        bigText.rectTransform.DOKill();
        bigText.text = text;
        bigText.color = color;
        bigText.rectTransform.localScale = Vector3.one * 2f;
        yield return bigText.rectTransform.DOScale(1f, 0.25f).SetEase(Ease.OutBack).WaitForCompletion();
        yield return new WaitForSeconds(hold);
        yield return bigText.DOFade(0f, 0.2f).WaitForCompletion();
        bigText.text = "";
    }

    void PopText(string text, Vector2 fxPos, Color color)
    {
        var t = Instantiate(popTextPrefab, fxLayer);
        t.text = text;
        t.color = color;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = fxPos;
        rt.localScale = Vector3.one * 0.4f;
        var seq = DOTween.Sequence().SetLink(t.gameObject);
        seq.Append(rt.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
        seq.Join(rt.DOAnchorPosY(fxPos.y + 120f, 0.9f).SetEase(Ease.OutQuad));
        seq.Insert(0.5f, t.DOFade(0f, 0.4f));
        seq.OnComplete(() => Destroy(t.gameObject));
    }

    Color PlayerColor(int p) => playerColors.Length > 0 ? playerColors[p % playerColors.Length] : Color.white;

    // ───────────────────────── 音（SoundManagerが無いシーン単体でも動くように） ─────────────────────────

    static void PlaySE(SoundManager.SEData.SETYPE se)
    {
        if (SoundManager.Instance == null) return;
        try { SoundManager.Instance.PlaySE(se); }
        catch (System.Exception) { Debug.LogWarning($"[CV] SE未登録: {se}"); }
    }

    static void PlayBGM(SoundManager.BGMData.BGMTYPE bgm)
    {
        if (SoundManager.Instance == null) return;
        try { SoundManager.Instance.PlayBGM(bgm); }
        catch (System.Exception) { Debug.LogWarning($"[CV] BGM未登録: {bgm}"); }
    }

    // ───────────────────────── コードで生成する表示 ─────────────────────────

    RectTransform BuildBall(Transform parent)
    {
        var root = CV_UIUtil.CreateRect("Ball", parent);
        root.sizeDelta = Vector2.one * ballSize;

        var shadow = CV_UIUtil.CreateImage("Shadow", root, CV_UIUtil.Circle, new Color(0, 0, 0, 0.35f), Vector2.zero);
        CV_UIUtil.Stretch(shadow.rectTransform);
        shadow.rectTransform.anchoredPosition = new Vector2(6, -6);

        // 本体の円でマスクして、はみ出した模様を切り取る
        var body = CV_UIUtil.CreateImage("Body", root, CV_UIUtil.Circle, new Color(1f, 0.95f, 0.8f), Vector2.zero);
        CV_UIUtil.Stretch(body.rectTransform);
        body.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        // バレーボールっぽい模様（ずらしたリング）
        var stripeColors = new[] { new Color(0.15f, 0.4f, 0.95f), new Color(1f, 0.8f, 0.1f) };
        for (int i = 0; i < 2; i++)
        {
            var stripe = CV_UIUtil.CreateImage("Stripe", body.rectTransform, CV_UIUtil.Ring, stripeColors[i], Vector2.one * ballSize * 0.9f);
            stripe.rectTransform.anchoredPosition = new Vector2(i == 0 ? -ballSize * 0.42f : ballSize * 0.42f, i == 0 ? ballSize * 0.1f : -ballSize * 0.1f);
        }

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
