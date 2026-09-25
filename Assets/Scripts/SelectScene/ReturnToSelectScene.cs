using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// どのシーンからでも Tab キーを3回連続で押すと選択シーンに戻る。
/// DontDestroyOnLoad でシーンをまたいで常駐する。
/// </summary>
public class ReturnToSelectScene : MonoBehaviour
{
    public static ReturnToSelectScene Instance { get; private set; }

    [Tooltip("戻り先のシーン名")]
    public string selectSceneName = "SelectScene";
    [Tooltip("戻るのに必要な押下回数")]
    public int requiredPresses = 3;
    [Tooltip("この秒数以内に続けて押さないとカウントがリセットされる")]
    public float pressInterval = 1.0f;

    int pressCount;
    float lastPressTime;

    // どのシーンから再生を始めても常駐オブジェクトが存在するようにする
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        new GameObject(nameof(ReturnToSelectScene), typeof(ReturnToSelectScene));
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Tab)) return;

        // 選択シーンにいる間は何もしない
        if (SceneManager.GetActiveScene().name == selectSceneName)
        {
            pressCount = 0;
            return;
        }

        // ゲーム側で timeScale を止めていても判定できるよう unscaledTime を使う
        float now = Time.unscaledTime;
        if (now - lastPressTime > pressInterval) pressCount = 0;
        lastPressTime = now;
        pressCount++;

        if (pressCount >= requiredPresses)
        {
            pressCount = 0;
            ReturnToSelect();
        }
    }

    public void ReturnToSelect()
    {
        if (!Application.CanStreamedLevelBeLoaded(selectSceneName))
        {
            Debug.LogError($"[ReturnToSelectScene] シーン '{selectSceneName}' を読み込めません。Build Settings に登録されているか確認してください。");
            return;
        }

        // ゲーム側で変更されがちな状態を元に戻す
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SceneManager.LoadScene(selectSceneName);
    }
}
