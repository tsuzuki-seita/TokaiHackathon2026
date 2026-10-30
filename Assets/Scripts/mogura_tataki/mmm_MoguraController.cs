using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class mmm_MoguraController : MonoBehaviour
{
    // ====== UI連携用イベント（UI側はこれらをSubscribeするだけでよい） ======
    public event Action<int> OnRoundStart;        // ラウンド開始時に発火（引数：開始するラウンド番号 1〜3）
    public event Action<bool> OnRoundJudged;      // そのラウンドの正誤が確定した直後に発火（引数：全問正解ならtrue）
    public event Action<int, int> OnGameFinished; // 全ラウンド終了時に発火（引数：正解ラウンド数, 全ラウンド数）
    // ================================================================

    // モグラを動かすスクリプト
    public mmm_MoguraMoveController moguraMoveController;
    // UIのスクリプト
    public mmm_MoguraUIManager moguraUIManager;

    public GameObject[] rings = new GameObject[26];  //配列を作成
    public List<int> memory1;  // 記憶用リスト1個目
    public List<int> memory2;  // 記憶用リスト2個目
    public List<int> memory3;  // 記憶用リスト3個目

    public List<int> answer1;  // 回答用リスト1個目
    public List<int> answer2;  // 回答用リスト2個目
    public List<int> answer3;  // 回答用リスト3個目

    public List<int>[] memories = new List<int>[3]; // ↑の記憶用リストをまとめる配列

    public int round; // ラウンドを示す関数
    public int number_number; // 各ラウンドの要素数を決める変数
    public float interval; // 各ラウンドのもぐらが出てくる間隔を決める変数
    int answerNumber = 0; // 1つずつ回答するラウンド1,2においていくつ目を回答しているかを示す

    public GameObject correctUI;   // 全問正解時に表示するUI
    public GameObject incorrectUI; // 不正解時に表示するUI
    public float resultDisplayTime = 1.5f; // 結果UIを表示しておく時間

    private Vector3[] originalScales = new Vector3[26]; // リングの元の大きさを保存する配列
    private bool isPlaying = false; // ゲーム進行中かどうか
    private bool isRemembering = false;  // 記憶フェーズ中かどうか、記憶中は押したキーの色を変えないようにするため

    private const int TotalRoundCount = 3; // ラウンド総数（最終結果表示に使用）
    private int clearedRoundCount = 0;     // 正解できたラウンド数（最終結果表示に使用）
    private int Score = 0;  // スコア計算用変数

    float timingRange = 0.5f;    // 「パッ！」が出てから何秒以内ならOKか

    public GameObject SpaceToStartText; // space to startのテキスト

    public GameObject FinalResultPanel; // 最終結果表示用パネル
    public Text ScoreText;  // スコア表示用テキスト
    public Text successRoundText;  // 成功ラウンド数表示用のテキスト

    public Text InputAnswerShowText;
    public Text CorrectAnswerShowText;

    public GameObject TitleCanvas;  // タイトル画面のキャンバス

    


    // Start is called before the first frame update
    void Start()
    {
        memories[0] = memory1;
        memories[1] = memory2;
        memories[2] = memory3;

        // 26個のリングの元の大きさを保存
        for (int i = 0; i < rings.Length; i++)
        {
            originalScales[i] = rings[i].transform.localScale;
        }

        // 結果UIは開始時は非表示にしておく
        if (correctUI != null) correctUI.SetActive(false);
        if (incorrectUI != null) incorrectUI.SetActive(false);

        // space to start の大きさを周期的に変える
        if(SpaceToStartText != null)
        {
            moguraUIManager.SpaceToStart();
        }

    }

    // Update is called once per frame
    void Update()
    {

         // Z～Pのキーが押されたとき
        int number = GetKeyNumber();

        if (number != -1)
        {
            if (answerNumber == 1 && answer1.Count < number_number)
            {
                answer1.Add(number);
                Debug.Log("answer1に追加：" + number);
            }
            else if (answerNumber == 2 && answer2.Count < number_number)
            {
                answer2.Add(number);
                Debug.Log("answer2に追加：" + number);
            }
            else if (answerNumber == 3 && answer3.Count < number_number)
            {
                answer3.Add(number);
                Debug.Log("answer3に追加：" + number);
            }
        }

        // space to startをぷかぷかさせておく


        if (Input.GetKeyDown(KeyCode.Space) && !isPlaying)
        {
            SpaceToStartText.SetActive(false);
            isPlaying = true;
            clearedRoundCount = 0;
            StartCoroutine(Round1());
        }

        if (!isRemembering)
        {
            KeyCode[] keys =
            {
                KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B,
                KeyCode.N, KeyCode.M, KeyCode.A, KeyCode.S, KeyCode.D,
                KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K,
                KeyCode.L, KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R,
                KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O,
                KeyCode.P
            };

            for (int i = 0; i < keys.Length; i++)
            {
                if (Input.GetKeyDown(keys[i]))
                {
                    LightRing(i);
                }

                if (Input.GetKeyUp(keys[i]))
                {
                    ResetRing(i);
                }
            }
        }
    }

    int GetKeyNumber()
    {
        if (Input.GetKeyDown(KeyCode.Z)) return 0;
        if (Input.GetKeyDown(KeyCode.X)) return 1;
        if (Input.GetKeyDown(KeyCode.C)) return 2;
        if (Input.GetKeyDown(KeyCode.V)) return 3;
        if (Input.GetKeyDown(KeyCode.B)) return 4;
        if (Input.GetKeyDown(KeyCode.N)) return 5;
        if (Input.GetKeyDown(KeyCode.M)) return 6;
        if (Input.GetKeyDown(KeyCode.A)) return 7;
        if (Input.GetKeyDown(KeyCode.S)) return 8;
        if (Input.GetKeyDown(KeyCode.D)) return 9;
        if (Input.GetKeyDown(KeyCode.F)) return 10;
        if (Input.GetKeyDown(KeyCode.G)) return 11;
        if (Input.GetKeyDown(KeyCode.H)) return 12;
        if (Input.GetKeyDown(KeyCode.J)) return 13;
        if (Input.GetKeyDown(KeyCode.K)) return 14;
        if (Input.GetKeyDown(KeyCode.L)) return 15;
        if (Input.GetKeyDown(KeyCode.Q)) return 16;
        if (Input.GetKeyDown(KeyCode.W)) return 17;
        if (Input.GetKeyDown(KeyCode.E)) return 18;
        if (Input.GetKeyDown(KeyCode.R)) return 19;
        if (Input.GetKeyDown(KeyCode.T)) return 20;
        if (Input.GetKeyDown(KeyCode.Y)) return 21;
        if (Input.GetKeyDown(KeyCode.U)) return 22;
        if (Input.GetKeyDown(KeyCode.I)) return 23;
        if (Input.GetKeyDown(KeyCode.O)) return 24;
        if (Input.GetKeyDown(KeyCode.P)) return 25;

        return -1;
    }

    IEnumerator Round1()
    {
        round = 1;
        number_number = 1;
        interval = 0.6f;

        OnRoundStart?.Invoke(round); // ★ラウンド開始タイミング（UIに「ラウンド1」等を表示する）

        yield return moguraUIManager.CountDownAnimation().WaitForCompletion(); // カウントダウン

        isRemembering = true;

        CreateMemory(memory1);
        CreateMemory(memory2);
        CreateMemory(memory3);

        yield return StartCoroutine(LightMemory(memory1));
        yield return StartCoroutine(LightMemory(memory2));
        yield return StartCoroutine(LightMemory(memory3));
        
        isRemembering = false;

        yield return StartCoroutine(AnswerPhase());
        yield return StartCoroutine(ShowResult());

        // 次のラウンドへ
        yield return StartCoroutine(Round2());
    }

    IEnumerator Round2()
    {
        round = 2;
        number_number = 1;
        interval = 0.4f;

        OnRoundStart?.Invoke(round); // ★ラウンド開始タイミング（UIに「ラウンド2」等を表示する）

        yield return moguraUIManager.CountDownAnimation().WaitForCompletion(); // カウントダウン

        CreateMemory(memory1);
        CreateMemory(memory2);
        CreateMemory(memory3);

        yield return StartCoroutine(LightMemory(memory1));
        yield return StartCoroutine(LightMemory(memory2));
        yield return StartCoroutine(LightMemory(memory3));

        yield return StartCoroutine(AnswerPhase());
        yield return StartCoroutine(ShowResult());

        // 次のラウンドへ
        yield return StartCoroutine(Round3());
    }

    IEnumerator Round3()
    {
        round = 3;
        number_number = 2;
        interval = 0.6f;

        OnRoundStart?.Invoke(round); // ★ラウンド開始タイミング（UIに「ラウンド3」等を表示する）

        yield return moguraUIManager.CountDownAnimation().WaitForCompletion(); // カウントダウン

        CreateMemory(memory1);
        CreateMemory(memory2);
        CreateMemory(memory3);

        yield return StartCoroutine(LightMemory(memory1));
        yield return StartCoroutine(LightMemory(memory2));
        yield return StartCoroutine(LightMemory(memory3));

        yield return StartCoroutine(AnswerPhase());
        yield return StartCoroutine(ShowResult());

        // 全ラウンド終了
        isPlaying = false;

        
        successRoundText.text = "成功ラウンド数 : " + clearedRoundCount;
        OnGameFinished?.Invoke(clearedRoundCount, TotalRoundCount); // ★最終結果タイミング（UIに「3問中2問正解」等を表示する）
        ScoreText.text = "スコア : " + Score.ToString();
        Debug.Log($"ゲーム終了：{clearedRoundCount}/{TotalRoundCount}問正解");
    }

    void CreateMemory(List<int> memories)
    {
        memories.Clear();

        while (memories.Count < number_number)
        {
            int new_number = UnityEngine.Random.Range(0, 26);

            if (!memories.Contains(new_number))
            {
                memories.Add(new_number);
            }
        }

        Debug.Log("memory: " + string.Join(", ", memories));
    }

    // 指定したリングを光らせる
    void LightRing(int number)
    {
        GameObject ring = rings[number];

        // リング本来の大きさを取得
        Vector3 originalScale = ring.transform.localScale;

        // 本来の大きさを基準に1.2倍する
        ring.transform.localScale = originalScale * 1.2f;

        // マテリアルを取得
        Renderer renderer = ring.GetComponent<Renderer>();

        // 黄色に変更
        renderer.material.color = Color.yellow;

        // EmissionをON
        renderer.material.EnableKeyword("_EMISSION");
        renderer.material.SetColor("_EmissionColor", Color.yellow * 2f);
    }
    
   IEnumerator LightMemory(List<int> memories)
    {
        // number_number個ずつ処理する
        for (int i = 0; i < memories.Count; i += number_number)
        {
            // number_number個のリングとモグラを同時に出す
            for (int j = 0; j < number_number && i + j < memories.Count; j++)
            {
                int number = memories[i + j];

                // リングを光らせる
                LightRing(number);

                // 同じ番号のモグラを出す
                moguraMoveController.MoguraUp(number);
            }

            // 0.6秒待つ
            yield return new WaitForSeconds(interval);

            // 出したリングを元に戻す
            for (int j = 0; j < number_number && i + j < memories.Count; j++)
            {
                ResetRing(memories[i + j]);
            }
        }
    }

    void ResetRing(int number)
    {
        GameObject ring = rings[number];

        // 大きさを元に戻す
        ring.transform.localScale = originalScales[number];

        // 色を元に戻す
        Renderer renderer = ring.GetComponent<Renderer>();
        renderer.material.color = Color.white;

        // 発光をOFF
        renderer.material.DisableKeyword("_EMISSION");
    }

    public bool LastRoundCleared { get; private set; } // 直前のラウンドに正解できたか

    IEnumerator AnswerPhase()
    {
        Debug.Log("回答開始");

        // 前回の回答を消す
        answer1.Clear();
        answer2.Clear();
        answer3.Clear();

        List<int>[] answers = { answer1, answer2, answer3 };

        LastRoundCleared = false; // 途中でミスしたら不正解のまま抜ける

        // memory1→memory2→memory3の順に、1グループ分の入力が揃うたびに即座に判定する
        for (int i = 0; i < memories.Length; i++)
        {
            answerNumber = i + 1; // Update()がこの値を見てanswer1〜3への入力を振り分ける

            Debug.Log($"{i + 1}回目の回答待ち");

            // number_number個の入力が揃うまで実際のキー入力を待つ
            yield return new WaitUntil(() => answers[i].Count >= number_number);

            bool isCorrect = CheckAnswer(memories[i], answers[i]);

            if (!isCorrect)
            {
                // ミス：即座にラウンド終了（残りのグループには回答させない）
                Debug.Log("不正解！ミス");
                answerNumber = 0;
                OnRoundJudged?.Invoke(false); // ★正誤確定タイミング（不正解）

                yield break;
            }

            Debug.Log($"{i + 1}回目 正解！次の入力へ");
            if(round == 1)  // ラウンドに応じたスコア計算
            {
                Score = Score + 7;
            }
            else if(round == 2)
            {
                Score = Score + 9;
            }
            else
            {
                Score = Score + 15;
            }
            // 正解：ループを継続し、次のグループの入力を受け付ける
        }

        // 3グループとも正解
        LastRoundCleared = true;
        clearedRoundCount++;
        if(clearedRoundCount == 3)  // 全問正解なら100点になるように調整する
        {
            Score = Score + 4;
        }
        Debug.Log("正解！");

        answerNumber = 0;
        OnRoundJudged?.Invoke(true); // ★正誤確定タイミング（全問正解）

        Debug.Log("回答終了");
    }

    // 判定結果のUIを一定時間表示する
    IEnumerator ShowResult()
    {
        if (LastRoundCleared)
        {
            if (correctUI != null) correctUI.SetActive(true);
        }
        else
        {
            ShowInputAnswer();
            ShowCorrectAnswer();
            if (incorrectUI != null) incorrectUI.SetActive(true);
        }

        yield return new WaitForSeconds(resultDisplayTime);

        if (correctUI != null) correctUI.SetActive(false);
        if (incorrectUI != null) incorrectUI.SetActive(false);
    }

    bool CheckAnswer(List<int> memory, List<int> answer)
    {
        // 要素数が違ったら不正解
        if (memory3.Count != answer.Count)
        {
            return false;
        }

        // memoryの中身をコピー
        List<int> checkList = new List<int>(memory);

        // answerを1個ずつ確認
        foreach (int number in answer)
        {
            // memoryにその数字がなければ不正解
            if (!checkList.Contains(number))
            {
                return false;
            }

            // 見つかった数字を削除
            // これによって同じ数字を2回回答するのを防ぐ
            checkList.Remove(number);
        }

        return true;
    }

    public void OnClickReplayButton()
    {
        FinalResultPanel.SetActive(false);
        isPlaying = false;
        SpaceToStartText.SetActive(true);
    }

    public void OnClickBacktoTitleButton()
    {
        FinalResultPanel.SetActive(false);
        isPlaying = false;
        TitleCanvas.SetActive(true);
    }

    public string NumberToAlphabet(int AN)  // AN = AlphabetNumber // 戻り値で文字列を返すときはvoidではなくてstringになる
    {
        if (AN == 0) return "Z";
        if (AN == 1) return "X";
        if (AN == 2) return "C";
        if (AN == 3) return "V";
        if (AN == 4) return "B";
        if (AN == 5) return "N";
        if (AN == 6) return "M";
        if (AN == 7) return "A";
        if (AN == 8) return "S";
        if (AN == 9) return "D";
        if (AN == 10) return "F";
        if (AN == 11) return "G";
        if (AN == 12) return "H";
        if (AN == 13) return "J";
        if (AN == 14) return "K";
        if (AN == 15) return "L";
        if (AN == 16) return "Q";
        if (AN == 17) return "W";
        if (AN == 18) return "E";
        if (AN == 19) return "R";
        if (AN == 20) return "T";
        if (AN == 21) return "Y";
        if (AN == 22) return "U";
        if (AN == 23) return "I";
        if (AN == 24) return "O";
        if (AN == 25) return "P";

        return ""; // どの条件にも当てはまらなかった場合　何かを返す関数で何も返さない場合があるとエラーになる
    }

    public void ShowInputAnswer()
    {
        string inputletter1;
        string inputletter2;
        string inputletter3;
        string inputletter4;
        string inputletter5;
        string inputletter6;

        Debug.Log("answer1.Count = " + answer1.Count);
        Debug.Log("answer2.Count = " + answer2.Count);
        Debug.Log("answer3.Count = " + answer3.Count);
        
        inputletter1 = answer1.Count > 0 ? NumberToAlphabet(answer1[0]) : "";
        inputletter2 = answer2.Count > 0 ? NumberToAlphabet(answer2[0]) : "";
        inputletter3 = answer3.Count > 0 ? NumberToAlphabet(answer3[0]) : "";

        inputletter4 = answer1.Count > 1 ? NumberToAlphabet(answer1[1]) : "";
        inputletter5 = answer2.Count > 1 ? NumberToAlphabet(answer2[1]) : "";
        inputletter6 = answer3.Count > 1 ? NumberToAlphabet(answer3[1]) : "";

        InputAnswerShowText.text = "あなたの回答: " + inputletter1 + " " + inputletter4 + " " + inputletter2 + " " + inputletter5 + " " + inputletter3 + " " + inputletter6;

    }
    public void ShowCorrectAnswer()
    {
        string answerletter1;
        string answerletter2;
        string answerletter3;
        string answerletter4;
        string answerletter5;
        string answerletter6;

        answerletter1 = memory1.Count > 0 ? NumberToAlphabet(memory1[0]) : "";
        answerletter2 = memory2.Count > 0 ? NumberToAlphabet(memory2[0]) : "";
        answerletter3 = memory3.Count > 0 ? NumberToAlphabet(memory3[0]) : "";

        answerletter4 = memory1.Count > 1 ? NumberToAlphabet(memory1[1]) : "";
        answerletter5 = memory2.Count > 2 ? NumberToAlphabet(memory2[1]) : "";
        answerletter6 = memory3.Count > 1 ? NumberToAlphabet(memory3[1]) : "";

        CorrectAnswerShowText.text = "正解 : " + answerletter1 + " " + answerletter4 + " " + answerletter2 + " " + answerletter5 + " " + answerletter3 + " " + answerletter6;

    }
}
