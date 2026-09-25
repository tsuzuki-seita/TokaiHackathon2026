using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレイ中の表示：左上に「今のプレイヤー・何球目か・結果」、右上に「コンボ」「合計ヒット」
/// </summary>
public class CV_Hud : MonoBehaviour
{
    public enum Judge { None, Miss, Nice, Perfect }

    const float IconSize = 76f;

    RectTransform card;
    Text nameText;
    RectTransform iconRow;
    readonly List<Image> icons = new List<Image>();
    readonly List<Image> badges = new List<Image>();
    RectTransform comboRoot;
    Text comboText;
    Text totalText;
    CanvasGroup group;
    int current = -1;

    public void Build()
    {
        var rt = (RectTransform)transform;
        CV_UIUtil.Stretch(rt);
        group = gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        card = CV_UIUtil.CreateImage("Card", rt, CV_Assets.Panel, new Color(0.25f, 0.33f, 0.6f, 0.92f), new Vector2(460, 190)).rectTransform;
        var cardImg = card.GetComponent<Image>();
        cardImg.type = Image.Type.Sliced;
        cardImg.pixelsPerUnitMultiplier = 1.2f;
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0, 1);
        card.anchoredPosition = new Vector2(30, -30);

        nameText = CV_UIUtil.CreateText("Name", card, 50, Color.white, TextAnchor.MiddleLeft);
        nameText.rectTransform.anchorMin = nameText.rectTransform.anchorMax = new Vector2(0, 1);
        nameText.rectTransform.pivot = new Vector2(0, 0.5f);
        nameText.rectTransform.anchoredPosition = new Vector2(30, -50);

        iconRow = CV_UIUtil.CreateRect("Icons", card);
        iconRow.anchorMin = iconRow.anchorMax = new Vector2(0, 1);
        iconRow.pivot = new Vector2(0, 0.5f);
        iconRow.anchoredPosition = new Vector2(30, -128);

        totalText = CV_UIUtil.CreateText("Total", rt, 44, Color.white, TextAnchor.MiddleRight);
        totalText.font = CV_Assets.KenneyFont;
        totalText.rectTransform.anchorMin = totalText.rectTransform.anchorMax = totalText.rectTransform.pivot = new Vector2(1, 1);
        totalText.rectTransform.anchoredPosition = new Vector2(-40, -40);

        comboRoot = CV_UIUtil.CreateRect("Combo", rt);
        comboRoot.anchorMin = comboRoot.anchorMax = comboRoot.pivot = new Vector2(1, 1);
        comboRoot.anchoredPosition = new Vector2(-40, -120);
        comboText = CV_UIUtil.CreateText("ComboText", comboRoot, 64, new Color(1f, 0.85f, 0.25f), TextAnchor.MiddleRight);
        comboText.font = CV_Assets.KenneyFont;
        comboText.rectTransform.pivot = new Vector2(1, 0.5f);

        gameObject.SetActive(false);
    }

    public void Show(string playerName, Color color, int balls, int totalHits, int maxHits)
    {
        gameObject.SetActive(true);
        nameText.text = playerName;
        nameText.color = color;
        SetTotal(totalHits, maxHits, false);
        comboText.text = "";
        current = -1;

        foreach (var i in icons) Destroy(i.gameObject);
        icons.Clear();
        badges.Clear();
        for (int b = 0; b < balls; b++)
        {
            var icon = CV_UIUtil.CreateImage("Ball", iconRow, CV_Assets.Ball, new Color(1, 1, 1, 0.35f), Vector2.one * IconSize);
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(IconSize * 0.5f + b * (IconSize + 14), 0);
            var badge = CV_UIUtil.CreateImage("Badge", icon.rectTransform, null, Color.white, Vector2.one * IconSize * 0.6f);
            badge.rectTransform.anchoredPosition = new Vector2(IconSize * 0.32f, -IconSize * 0.3f);
            badge.enabled = false;
            icons.Add(icon);
            badges.Add(badge);
        }
        FitCard(balls);

        // 左からスライドイン
        card.DOKill();
        card.anchoredPosition = new Vector2(-card.sizeDelta.x, -30);
        card.DOAnchorPosX(30, 0.4f).SetEase(Ease.OutBack);
        group.alpha = 1f;
    }

    /// <summary>名前とボールのアイコンが全部収まる大きさにパネルを合わせる</summary>
    void FitCard(int balls)
    {
        const float padX = 36f, padTop = 22f, gap = 12f, padBottom = 34f; // 下はパネルの厚みの分だけ多めに
        const float iconGap = 14f;
        float iconPulse = IconSize * 1.25f; // 今のボールは拡大するので、その分も確保する

        float nameW = nameText.preferredWidth;
        float nameH = Mathf.Max(nameText.preferredHeight, nameText.fontSize * 1.2f);
        float iconsW = balls * IconSize + Mathf.Max(0, balls - 1) * iconGap;

        nameText.rectTransform.anchoredPosition = new Vector2(padX, -padTop - nameH * 0.5f);
        iconRow.anchoredPosition = new Vector2(padX, -(padTop + nameH + gap + iconPulse * 0.5f));
        card.sizeDelta = new Vector2(Mathf.Max(nameW, iconsW) + padX * 2f, padTop + nameH + gap + iconPulse + padBottom);
    }

    /// <summary>今から打つボールを強調する</summary>
    public void SetCurrent(int b)
    {
        if (current >= 0 && current < icons.Count)
        {
            icons[current].rectTransform.DOKill();
            icons[current].rectTransform.localScale = Vector3.one;
        }
        current = b;
        if (b < 0 || b >= icons.Count) return;
        var rt = icons[b].rectTransform;
        icons[b].color = Color.white;
        rt.localScale = Vector3.one;
        rt.DOScale(1.2f, 0.35f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }

    public void SetResult(int b, Judge judge)
    {
        if (b < 0 || b >= icons.Count) return;
        var icon = icons[b];
        icon.rectTransform.DOKill();
        icon.rectTransform.localScale = Vector3.one * 1.5f;
        icon.rectTransform.DOScale(1f, 0.35f).SetEase(Ease.OutBack);

        var badge = badges[b];
        badge.enabled = true;
        badge.rectTransform.localScale = Vector3.zero;
        badge.rectTransform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetDelay(0.05f);

        switch (judge)
        {
            case Judge.Perfect:
                icon.color = Color.white;
                badge.sprite = CV_Assets.StarFull;
                badge.color = Color.white;
                break;
            case Judge.Nice:
                icon.color = Color.white;
                badge.sprite = CV_Assets.Fx("star_06");
                badge.color = new Color(1f, 1f, 1f, 0.9f);
                break;
            default:
                icon.color = new Color(0.35f, 0.35f, 0.4f, 0.8f);
                badge.enabled = false;
                icon.rectTransform.DOShakeAnchorPos(0.3f, 8f, 30);
                break;
        }
    }

    public void SetCombo(int combo)
    {
        comboRoot.DOKill();
        comboRoot.localScale = Vector3.one;
        comboRoot.localRotation = Quaternion.identity;
        if (combo < 2)
        {
            comboText.text = "";
            return;
        }
        comboText.text = $"{combo} COMBO!";
        comboRoot.localScale = Vector3.one * 1.8f;
        comboRoot.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
        comboRoot.DOPunchRotation(new Vector3(0, 0, 10), 0.35f);
    }

    public void SetTotal(int hits, int max, bool punch = true)
    {
        totalText.text = $"HIT {hits} / {max}";
        if (!punch) return;
        totalText.rectTransform.DOKill();
        totalText.rectTransform.localScale = Vector3.one;
        totalText.rectTransform.DOPunchScale(Vector3.one * 0.35f, 0.35f, 6, 0.5f);
    }

    public void Hide()
    {
        if (!gameObject.activeSelf) return;
        group.DOKill();
        group.DOFade(0f, 0.25f).OnComplete(() => gameObject.SetActive(false));
    }

    public void HideImmediate()
    {
        group.DOKill();
        gameObject.SetActive(false);
    }
}
