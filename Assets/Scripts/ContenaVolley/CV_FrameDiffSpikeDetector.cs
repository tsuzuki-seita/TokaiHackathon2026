using UnityEngine;

/// <summary>
/// フレーム差分によるスパイク検出。
/// カメラ映像を粗いグリッドに縮小し、前フレームとの明るさの差が大きいセルを「動いた」とみなす。
/// カーソルの円内で動いたセルの割合が一定以上になったらスパイク成功。
/// </summary>
public class CV_FrameDiffSpikeDetector : CV_SpikeDetectorBase
{
    [SerializeField] CV_WebCamSource source;

    [Header("解析グリッド")]
    [Tooltip("横方向のセル数（多いほど細かいが重い）")]
    [SerializeField] int gridWidth = 96;

    [Header("判定しきい値（会場の明るさに合わせて調整）")]
    [Tooltip("このセルが『動いた』とみなす明るさの差 (0~255)")]
    [SerializeField, Range(1, 255)] int pixelThreshold = 28;
    [Tooltip("カーソル円内の何割のセルが動いたらスパイクとみなすか")]
    [SerializeField, Range(0.01f, 1f)] float triggerRatio = 0.22f;
    [Tooltip("最低限動いている必要があるセル数（ノイズ対策）")]
    [SerializeField] int minMovingCells = 4;
    [Tooltip("画面全体のこの割合以上が同時に変化したら照明・露出の変化とみなして無視する")]
    [SerializeField, Range(0.1f, 1f)] float maxGlobalRatio = 0.5f;

    Color32[] pixels;
    byte[] prevLum;
    byte[] curLum;
    bool[] moving;
    int gw, gh;
    int texW, texH;
    bool hasPrev;

    Texture2D debugTex;
    Color32[] debugPixels;
    float lastRatio;
    float globalRatio;
    Vector2 lastTarget = new Vector2(-1, -1);
    float lastRadius;

    public override bool IsReady => source != null && source.IsReady && hasPrev;
    public override Texture DebugTexture => debugTex;
    public override string DebugInfo => $"motion {lastRatio:P0} / trigger {triggerRatio:P0}  (全体 {globalRatio:P0})";

    void Start()
    {
        if (source == null) source = FindObjectOfType<CV_WebCamSource>();
    }

    void Update()
    {
        if (source == null || !source.IsReady || !source.Texture.didUpdateThisFrame) return;
        ProcessFrame();
    }

    void Allocate()
    {
        var tex = source.Texture;
        texW = tex.width;
        texH = tex.height;
        gw = Mathf.Max(8, gridWidth);
        gh = Mathf.Max(8, Mathf.RoundToInt(gw / source.Aspect));

        pixels = new Color32[texW * texH];
        prevLum = new byte[gw * gh];
        curLum = new byte[gw * gh];
        moving = new bool[gw * gh];
        debugPixels = new Color32[gw * gh];

        if (debugTex != null) Destroy(debugTex);
        debugTex = new Texture2D(gw, gh, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        hasPrev = false;
    }

    void ProcessFrame()
    {
        var tex = source.Texture;
        if (pixels == null || tex.width != texW || tex.height != texH) Allocate();

        tex.GetPixels32(pixels);

        // 1セルあたりの画素サイズの1/4ずらしで4点サンプリングして平均（ノイズ低減）
        float offU = 0.25f / gw;
        float offV = 0.25f / gh;
        int movingCount = 0;

        for (int y = 0; y < gh; y++)
        {
            for (int x = 0; x < gw; x++)
            {
                var uv = new Vector2((x + 0.5f) / gw, (y + 0.5f) / gh);
                int sum = Sample(uv.x - offU, uv.y - offV) + Sample(uv.x + offU, uv.y - offV)
                        + Sample(uv.x - offU, uv.y + offV) + Sample(uv.x + offU, uv.y + offV);
                int i = y * gw + x;
                curLum[i] = (byte)(sum / 4);
                moving[i] = hasPrev && Mathf.Abs(curLum[i] - prevLum[i]) >= pixelThreshold;
                if (moving[i]) movingCount++;
            }
        }

        globalRatio = (float)movingCount / (gw * gh);
        (prevLum, curLum) = (curLum, prevLum);
        hasPrev = true;

        UpdateDebugTexture();
    }

    /// <summary>表示UVの位置の輝度(0~255)</summary>
    int Sample(float u, float v)
    {
        var t = source.DisplayToTextureUV(new Vector2(u, v));
        int px = Mathf.Clamp((int)(t.x * texW), 0, texW - 1);
        int py = Mathf.Clamp((int)(t.y * texH), 0, texH - 1);
        var c = pixels[py * texW + px];
        return (c.r * 77 + c.g * 150 + c.b * 29) >> 8;
    }

    public override void ResetState()
    {
        lastRatio = 0;
    }

    public override bool Detect(Vector2 targetUV, float radius, out Vector2 hitUV)
    {
        hitUV = targetUV;
        lastTarget = targetUV;
        lastRadius = radius;
        if (!IsReady || globalRatio > maxGlobalRatio) return false;

        float aspect = source.Aspect;
        // 円を囲む範囲のセルだけ調べる
        int x0 = Mathf.Clamp(Mathf.FloorToInt((targetUV.x - radius / aspect) * gw), 0, gw - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt((targetUV.x + radius / aspect) * gw), 0, gw - 1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt((targetUV.y - radius) * gh), 0, gh - 1);
        int y1 = Mathf.Clamp(Mathf.CeilToInt((targetUV.y + radius) * gh), 0, gh - 1);

        int total = 0, count = 0;
        Vector2 sum = Vector2.zero;
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                var uv = new Vector2((x + 0.5f) / gw, (y + 0.5f) / gh);
                float dx = (uv.x - targetUV.x) * aspect;
                float dy = uv.y - targetUV.y;
                if (dx * dx + dy * dy > radius * radius) continue;

                total++;
                if (moving[y * gw + x])
                {
                    count++;
                    sum += uv;
                }
            }
        }

        lastRatio = total > 0 ? (float)count / total : 0;
        if (count < minMovingCells || lastRatio < triggerRatio) return false;

        hitUV = sum / count;
        return true;
    }

    void UpdateDebugTexture()
    {
        float aspect = source.Aspect;
        for (int y = 0; y < gh; y++)
        {
            for (int x = 0; x < gw; x++)
            {
                int i = y * gw + x;
                byte l = (byte)(prevLum[i] / 3); // prevLumには今フレームの値が入っている(swap後)
                Color32 c = moving[i] ? new Color32(255, 255, 255, 255) : new Color32(l, l, l, 255);

                if (lastRadius > 0)
                {
                    float dx = ((x + 0.5f) / gw - lastTarget.x) * aspect;
                    float dy = (y + 0.5f) / gh - lastTarget.y;
                    if (dx * dx + dy * dy <= lastRadius * lastRadius)
                    {
                        c = moving[i] ? new Color32(255, 230, 60, 255) : new Color32((byte)(l + 40), (byte)l, (byte)(l + 90), 255);
                    }
                }
                debugPixels[i] = c;
            }
        }
        debugTex.SetPixels32(debugPixels);
        debugTex.Apply(false);
    }

    void OnDestroy()
    {
        if (debugTex != null) Destroy(debugTex);
    }
}
