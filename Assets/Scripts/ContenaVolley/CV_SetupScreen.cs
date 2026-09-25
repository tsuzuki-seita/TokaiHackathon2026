using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 人数と名前を入力する画面。
/// 人数を増減すると、名前入力欄(nameFieldPrefab)が nameListParent の下に増減する
/// </summary>
public class CV_SetupScreen : MonoBehaviour
{
    [SerializeField] Button minusButton;
    [SerializeField] Button plusButton;
    [SerializeField] Text countText;
    [Tooltip("名前入力欄を並べる親（Vertical/Grid Layout Group を付けておく）")]
    [SerializeField] Transform nameListParent;
    [SerializeField] InputField nameFieldPrefab;
    [SerializeField] Button startButton;

    [SerializeField, Range(1, 10)] int minPlayers = 1;
    [SerializeField, Range(1, 10)] int maxPlayers = 8;
    [SerializeField, Range(1, 10)] int defaultPlayers = 4;

    readonly List<InputField> fields = new List<InputField>();
    bool initialized;
    RectTransform logoRt;
    RectTransform tagRt;

    /// <summary>スタートボタンが押されたか</summary>
    public bool Submitted { get; private set; }

    void Init()
    {
        if (initialized) return;
        initialized = true;

        minusButton.onClick.AddListener(() => SetCount(fields.Count - 1));
        plusButton.onClick.AddListener(() => SetCount(fields.Count + 1));
        startButton.onClick.AddListener(() => Submitted = true);
        ApplySkin();
        SetCount(defaultPlayers);
    }

    /// <summary>Kenney UI Pack の見た目とタイトルロゴを付ける</summary>
    void ApplySkin()
    {
        CV_UISkin.SkinBackdrop(GetComponent<Image>());
        CV_UISkin.SkinButton(minusButton, CV_Assets.ButtonRound, Color.white, "CV_se_switch");
        CV_UISkin.SkinButton(plusButton, CV_Assets.ButtonRound, Color.white, "CV_se_switch");
        CV_UISkin.SkinButton(startButton, CV_Assets.ButtonYellow, CV_UISkin.TextDark);
        CV_UISkin.SkinLabel(countText, Color.white, 0, 0f);

        // 見やすさ優先で、ロゴ以外の文字は縁取り・影を付けない（ロゴは縁取りのみ、影なし）。
        // ロゴはタイトル画像に合わせて少し傾け、動きは開いたときの一度だけにする
        var tagText = CV_UISkin.CreateLogo(transform, "AIR VOLLEY", 48, Color.white, false);
        var tagBg = CV_UIUtil.CreateImage("TagBg", transform, CV_Assets.Panel, new Color(1f, 0.38f, 0.38f), new Vector2(tagText.preferredWidth + 70f, 76f));
        tagBg.type = Image.Type.Sliced;
        tagBg.pixelsPerUnitMultiplier = 0.7f;
        tagText.rectTransform.SetParent(tagBg.rectTransform, false);
        tagText.rectTransform.anchoredPosition = new Vector2(0, 4);
        tagRt = tagBg.rectTransform;
        tagRt.anchoredPosition = new Vector2(-250, 478);
        tagRt.localRotation = Quaternion.Euler(0, 0, 4f);

        var logo = CV_UISkin.CreateLogo(transform, "エアーバレー", 140, new Color(1f, 0.85f, 0.25f), false);
        // タイトル画像と同じ、濃紺の太くて丸い縁取り
        var outline = logo.gameObject.AddComponent<CV_SmoothOutline>();
        outline.color = CV_UISkin.Navy;
        outline.thickness = 9f;
        logoRt = logo.rectTransform;
        logoRt.anchoredPosition = new Vector2(0, 360);
        logoRt.localRotation = Quaternion.Euler(0, 0, 3f);

        var sub = CV_UISkin.CreateLogo(transform, "カメラの前でスパイク！ ガラスを粉々にしよう", 44, Color.white, false);
        sub.rectTransform.anchoredPosition = new Vector2(0, 250);

        LayoutScreen();
    }

    /// <summary>
    /// 上から「ロゴ → 人数 → 名前入力欄 → スタート」の順に、間隔をあけて並べ直す。
    /// 名前入力欄は2列で中央揃え（最大8人で4段）
    /// </summary>
    void LayoutScreen()
    {
        const float countY = 125f;
        Place(countText.rectTransform, new Vector2(0, countY));
        Place((RectTransform)minusButton.transform, new Vector2(-260, countY));
        Place((RectTransform)plusButton.transform, new Vector2(260, countY));

        if (nameListParent.TryGetComponent<GridLayoutGroup>(out var grid))
        {
            grid.cellSize = new Vector2(520, 88);
            grid.spacing = new Vector2(48, 22);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
        }
        var list = (RectTransform)nameListParent;
        list.anchorMin = list.anchorMax = new Vector2(0.5f, 0.5f);
        list.pivot = new Vector2(0.5f, 1f);
        list.sizeDelta = new Vector2(520 * 2 + 48, 88 * 4 + 22 * 3);
        list.anchoredPosition = new Vector2(0, 40);

        Place((RectTransform)startButton.transform, new Vector2(0, -445));
    }

    static void Place(RectTransform rt, Vector2 pos)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
    }

    /// <summary>ロゴをドンと押し込む（開いたときだけ）</summary>
    void PlayLogoIntro()
    {
        logoRt.DOKill();
        logoRt.localScale = Vector3.one * 1.6f;
        logoRt.DOScale(1f, 0.22f).SetEase(Ease.InQuad)
            .OnComplete(() => logoRt.DOPunchAnchorPos(new Vector2(0, -12), 0.25f, 10, 0.6f));

        tagRt.DOKill();
        tagRt.localScale = Vector3.zero;
        tagRt.DOScale(1f, 0.3f).SetDelay(0.2f).SetEase(Ease.OutBack);
    }

    /// <summary>ときどき流れ弾のボールが画面の端に当たり、小さなヒビを残して落ちていく</summary>
    IEnumerator StrayBalls()
    {
        yield return new WaitForSeconds(1.2f);
        while (true)
        {
            StrayBall();
            yield return new WaitForSeconds(Random.Range(3f, 5f));
        }
    }

    void StrayBall()
    {
        // 入力欄やボタンにかぶらないよう、左右の端のどちらかに当てる
        float side = Random.value < 0.5f ? -1f : 1f;
        var target = new Vector2(side * Random.Range(640f, 860f), Random.Range(-420f, 200f));
        var from = new Vector2(side * 1150f, target.y - Random.Range(350f, 600f));

        var ball = CV_UIUtil.CreateImage("StrayBall", transform, CV_Assets.Ball, Color.white, Vector2.one * 90);
        var brt = ball.rectTransform;
        brt.SetSiblingIndex(0); // 入力欄などより奥に出す
        brt.anchoredPosition = from;
        brt.localScale = Vector3.one * 0.6f;

        var seq = DOTween.Sequence().SetLink(ball.gameObject);
        seq.Append(brt.DOJumpAnchorPos(target, 220f, 1, 0.45f).SetEase(Ease.Linear));
        seq.Join(brt.DOScale(1.4f, 0.45f).SetEase(Ease.InQuad));
        seq.Join(brt.DOLocalRotate(new Vector3(0, 0, -side * 540f), 0.45f, RotateMode.FastBeyond360));
        seq.AppendCallback(() =>
        {
            CV_Audio.Play("CV_se_tinkle", 0.25f, 1.2f);
            SmallCrack(target);
        });
        seq.Append(brt.DOAnchorPos(target + new Vector2(side * 120f, -800f), 0.6f).SetEase(Ease.InQuad));
        seq.Join(ball.DOFade(0f, 0.6f).SetEase(Ease.InQuad));
        seq.OnComplete(() => Destroy(ball.gameObject));
    }

    void SmallCrack(Vector2 at)
    {
        var root = CV_UIUtil.CreateRect("SmallCrack", transform);
        root.SetSiblingIndex(0);
        root.anchoredPosition = at;
        root.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        var crack = CV_UIUtil.CreateImage("Crack", root, CV_Assets.Fx("spark_01"), new Color(1, 1, 1, 0.7f), Vector2.one * 280);
        var dot = CV_UIUtil.CreateImage("Impact", root, CV_Assets.Fx("scorch_01"), new Color(1, 1, 1, 0.8f), Vector2.one * 110);
        root.localScale = Vector3.one * 0.3f;

        var seq = DOTween.Sequence().SetLink(root.gameObject);
        seq.Append(root.DOScale(1f, 0.12f).SetEase(Ease.OutQuad));
        seq.AppendInterval(1.6f);
        seq.Append(crack.DOFade(0f, 0.6f));
        seq.Join(dot.DOFade(0f, 0.6f));
        seq.OnComplete(() => Destroy(root.gameObject));
    }

    /// <summary>画面を開く。前回入力した名前はそのまま残る</summary>
    public void Open()
    {
        Init();
        gameObject.SetActive(true);
        Submitted = false;
        PlayLogoIntro();
        StartCoroutine(StrayBalls());
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    void SetCount(int count)
    {
        count = Mathf.Clamp(count, minPlayers, maxPlayers);

        while (fields.Count < count)
        {
            var field = Instantiate(nameFieldPrefab, nameListParent);
            field.name = $"NameField{fields.Count + 1}";
            if (field.placeholder is Text placeholder) placeholder.text = DefaultName(fields.Count);
            CV_UISkin.SkinInputField(field);
            field.transform.localScale = Vector3.one * 0.6f;
            field.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetLink(field.gameObject);
            fields.Add(field);
        }
        while (fields.Count > count)
        {
            Destroy(fields[fields.Count - 1].gameObject);
            fields.RemoveAt(fields.Count - 1);
        }

        countText.text = $"{count} 人";
        minusButton.interactable = count > minPlayers;
        plusButton.interactable = count < maxPlayers;
    }

    /// <summary>入力された名前（空欄は「プレイヤーN」）</summary>
    public List<string> GetNames()
    {
        var names = new List<string>();
        for (int i = 0; i < fields.Count; i++)
        {
            string n = fields[i].text.Trim();
            names.Add(string.IsNullOrEmpty(n) ? DefaultName(i) : n);
        }
        return names;
    }

    static string DefaultName(int index) => $"プレイヤー{index + 1}";
}
