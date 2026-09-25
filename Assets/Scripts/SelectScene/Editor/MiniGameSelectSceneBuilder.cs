using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// SelectScene にミニゲーム選択UIを自動構築するエディタ拡張。
/// メニュー: Tools > MiniGame Select > Build Select Scene
/// </summary>
public static class MiniGameSelectSceneBuilder
{
    const string SelectScenePath = "Assets/Scenes/SelectScene.unity";
    const string CanvasName = "MiniGameSelectCanvas";
    // サムネイル画像の置き場所。ファイル名をシーン名と同じにする (例: sap.png)
    const string ThumbnailFolder = "Assets/Sprites/SelectScene/Thumbnails";

    struct GameEntry
    {
        public string sceneName;
        public string title;
        public Color color;
        public GameEntry(string sceneName, string title, Color color)
        {
            this.sceneName = sceneName;
            this.title = title;
            this.color = color;
        }
    }

    static readonly GameEntry[] Games =
    {
        new GameEntry("Chiroru-HaeTataki", "ハエたたき",     new Color(0.95f, 0.45f, 0.35f)),
        new GameEntry("ContenaVolley",     "コンテナバレー", new Color(0.30f, 0.60f, 0.95f)),
        new GameEntry("mumumu",            "もぐらたたき",   new Color(0.45f, 0.80f, 0.40f)),
        new GameEntry("sap",               "ポーズゲーム",   new Color(0.95f, 0.75f, 0.25f)),
    };

    [MenuItem("Tools/MiniGame Select/Build Select Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = File.Exists(SelectScenePath)
            ? EditorSceneManager.OpenScene(SelectScenePath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // 既存のUIを作り直す
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == CanvasName) Object.DestroyImmediate(go);
        }

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Canvas
        var canvasGo = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // 背景
        var bg = CreateImage("Background", canvasGo.transform, new Color(0.12f, 0.13f, 0.20f));
        Stretch(bg.rectTransform);
        bg.raycastTarget = false;

        // タイトル
        var title = CreateText("Title", canvasGo.transform, font, "ミニゲームをえらんでね", 80, Color.white);
        var titleRect = title.rectTransform;
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -80);
        titleRect.sizeDelta = new Vector2(1600, 120);

        // アイコン群
        var iconsRoot = new GameObject("Icons", typeof(RectTransform));
        var iconsRect = (RectTransform)iconsRoot.transform;
        iconsRect.SetParent(canvasGo.transform, false);
        Stretch(iconsRect);

        const float iconSize = 260f;
        const float spacing = 380f;
        const float iconY = -200f;
        float startX = -spacing * (Games.Length - 1) / 2f;

        for (int i = 0; i < Games.Length; i++)
        {
            CreateIcon(iconsRect, font, Games[i], new Vector2(startX + spacing * i, iconY), iconSize);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, SelectScenePath);

        RegisterBuildScenes();

        Debug.Log("[MiniGameSelectSceneBuilder] SelectScene を構築しました。");
    }

    static void CreateIcon(RectTransform parent, Font font, GameEntry game, Vector2 pos, float size)
    {
        // ルート: ホバー判定用 (大きさ固定なので拡大してもちらつかない)
        var root = new GameObject($"Icon_{game.sceneName}", typeof(RectTransform), typeof(Image), typeof(MiniGameSelectIcon));
        var rootRect = (RectTransform)root.transform;
        rootRect.SetParent(parent, false);
        rootRect.anchoredPosition = pos;
        rootRect.sizeDelta = new Vector2(size, size);
        root.GetComponent<Image>().color = new Color(1, 1, 1, 0);

        // アイコン本体 (ここに画像を設定)
        var body = CreateImage("Body", rootRect, game.color);
        body.rectTransform.sizeDelta = new Vector2(size, size);
        body.raycastTarget = false;

        var label = CreateText("Label", body.rectTransform, font, game.title, 40, Color.white);
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        label.rectTransform.pivot = new Vector2(0.5f, 1f);
        label.rectTransform.anchoredPosition = new Vector2(0, -16);
        label.rectTransform.sizeDelta = new Vector2(size + 100, 60);

        // サムネイル (アイコンの上にポップアップ)
        const float thumbW = 560f, thumbH = 315f, border = 10f;
        var thumbGo = new GameObject("Thumbnail", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        var thumbRect = (RectTransform)thumbGo.transform;
        thumbRect.SetParent(rootRect, false);
        thumbRect.pivot = new Vector2(0.5f, 0f);
        thumbRect.anchoredPosition = new Vector2(0, size / 2f + 30f);
        thumbRect.sizeDelta = new Vector2(thumbW + border * 2, thumbH + border * 2);
        var frame = thumbGo.GetComponent<Image>();
        frame.color = Color.white;
        frame.raycastTarget = false;

        var thumbImage = CreateImage("ThumbImage", thumbRect, game.color * 0.6f + Color.black * 0.4f);
        thumbImage.rectTransform.sizeDelta = new Vector2(thumbW, thumbH);
        thumbImage.raycastTarget = false;

        var sprite = LoadThumbnail(game.sceneName);
        if (sprite != null)
        {
            thumbImage.sprite = sprite;
            thumbImage.color = Color.white;
            thumbImage.preserveAspect = true;
        }
        else
        {
            var placeholder = CreateText("Placeholder", thumbImage.rectTransform, font, $"{game.title}\n(サムネイル未設定)", 36, Color.white);
            Stretch(placeholder.rectTransform);
            Debug.LogWarning($"[MiniGameSelectSceneBuilder] サムネイルが見つかりません: {ThumbnailFolder}/{game.sceneName}.png (jpg も可)");
        }

        var icon = root.GetComponent<MiniGameSelectIcon>();
        icon.sceneName = game.sceneName;
        icon.iconBody = body.rectTransform;
        icon.thumbnail = thumbGo.GetComponent<CanvasGroup>();

        // エディタ上では非表示にしておく (実行時は Awake で初期化される)
        thumbGo.SetActive(false);
    }

    static Sprite LoadThumbnail(string sceneName)
    {
        foreach (var ext in new[] { "png", "jpg", "jpeg" })
        {
            var path = $"{ThumbnailFolder}/{sceneName}.{ext}";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            // Sprite としてインポートされていなければ設定を変更
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return null;
    }

    static void RegisterBuildScenes()
    {
        var paths = new List<string> { SelectScenePath };
        paths.AddRange(Games.Select(g => $"Assets/Scenes/{g.sceneName}.unity"));

        var scenes = EditorBuildSettings.scenes.ToList();
        foreach (var p in paths)
        {
            if (scenes.All(s => s.path != p)) scenes.Add(new EditorBuildSettingsScene(p, true));
        }

        // SelectScene を先頭 (起動シーン) にする
        var select = scenes.First(s => s.path == SelectScenePath);
        select.enabled = true;
        scenes.Remove(select);
        scenes.Insert(0, select);

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    static Text CreateText(string name, Transform parent, Font font, string text, int size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
