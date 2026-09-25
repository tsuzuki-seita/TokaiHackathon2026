using System.Collections.Generic;
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

    /// <summary>スタートボタンが押されたか</summary>
    public bool Submitted { get; private set; }

    void Init()
    {
        if (initialized) return;
        initialized = true;

        minusButton.onClick.AddListener(() => SetCount(fields.Count - 1));
        plusButton.onClick.AddListener(() => SetCount(fields.Count + 1));
        startButton.onClick.AddListener(() => Submitted = true);
        SetCount(defaultPlayers);
    }

    /// <summary>画面を開く。前回入力した名前はそのまま残る</summary>
    public void Open()
    {
        Init();
        gameObject.SetActive(true);
        Submitted = false;
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
