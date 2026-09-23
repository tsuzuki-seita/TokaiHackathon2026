using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mmm_MoguraController : MonoBehaviour
{
    // モグラを動かすスクリプト
public mmm_MoguraMoveController moguraMoveController;

    public GameObject[] rings = new GameObject[26];  //配列を作成
    public List<int> memory1 = new List<int>();  // 記憶用リスト1個目
    public List<int> memory2 = new List<int>();  // 記憶用リスト2個目
    public List<int> memory3 = new List<int>();  // 記憶用リスト3個目

    public List<int> answer1 = new List<int>();  // 回答用リスト1個目
    public List<int> answer2 = new List<int>();  // 回答用リスト2個目
    public List<int> answer3 = new List<int>();  // 回答用リスト3個目

    public List<int>[] memories = new List<int>[3]; // ↑の記憶用リストをまとめる配列

    public int round; // ラウンドを示す関数
    public int number_number; // 各ラウンドの要素数を決める変数
    public float interval; // 各ラウンドのもぐらが出てくる間隔を決める変数
    int answerNumber = 0; // 1つずつ回答するラウンド1,2においていくつ目を回答しているかを示す

    private Vector3[] originalScales = new Vector3[26]; // リングの元の大きさを保存する配列
    private int currentRound = 1;

    float timingRange = 0.5f;    // 「パッ！」が出てから何秒以内ならOKか

    


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
    }

    // Update is called once per frame
    void Update()
    {

         // Z～Pのキーが押されたとき
        int number = GetKeyNumber();

        if (number != -1)
        {
            if (answerNumber == 1)
            {
                answer1.Add(number);
                Debug.Log("answer1に追加：" + number);
            }
            else if (answerNumber == 2)
            {
                answer2.Add(number);
                Debug.Log("answer2に追加：" + number);
            }
            else if (answerNumber == 3)
            {
                answer3.Add(number);
                Debug.Log("answer3に追加：" + number);
            }
        }


        if(Input.GetKeyDown(KeyCode.Space))
        {

            
            if (currentRound == 1)
            {
                StartCoroutine(Round1());
            }
            else if (currentRound == 2)
            {    
                StartCoroutine(Round2());
            }
            else if (currentRound == 3)
            {
                StartCoroutine(Round3());
            }

            if (currentRound < 3)
            {
                currentRound++;
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

        CreateMemory(memory1);
        CreateMemory(memory2);
        CreateMemory(memory3);

        yield return StartCoroutine(LightMemory(memory1));
        yield return StartCoroutine(LightMemory(memory2));
        yield return StartCoroutine(LightMemory(memory3));
    }

    IEnumerator Round2()
    {
        round = 2;
        number_number = 1;
        interval = 0.4f;

        CreateMemory(memory1);
        CreateMemory(memory2);
        CreateMemory(memory3);

        yield return StartCoroutine(LightMemory(memory1));
        yield return StartCoroutine(LightMemory(memory2));
        yield return StartCoroutine(LightMemory(memory3));
    }

    IEnumerator Round3()
    {
        round = 3;
        number_number = 2;
        interval = 0.6f;

        CreateMemory(memory1);
        CreateMemory(memory2);
        CreateMemory(memory3);

        yield return StartCoroutine(LightMemory(memory1));
        yield return StartCoroutine(LightMemory(memory2));
        yield return StartCoroutine(LightMemory(memory3));
    }

    void CreateMemory(List<int> memory)
    {
        memory.Clear();

        while (memory.Count < number_number)
        {
            int new_number = Random.Range(0, 26);

            if (!memory.Contains(new_number))
            {
                memory.Add(new_number);
            }
        }

        Debug.Log("memory: " + string.Join(", ", memory));
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
    
   IEnumerator LightMemory(List<int> memory)
    {
        // number_number個ずつ処理する
        for (int i = 0; i < memory.Count; i += number_number)
        {
            // number_number個のリングとモグラを同時に出す
            for (int j = 0; j < number_number && i + j < memory.Count; j++)
            {
                int number = memory[i + j];

                // リングを光らせる
                LightRing(number);

                // 同じ番号のモグラを出す
                moguraMoveController.MoguraUp(number);
            }

            // 0.6秒待つ
            yield return new WaitForSeconds(interval);

            // 出したリングを元に戻す
            for (int j = 0; j < number_number && i + j < memory.Count; j++)
            {
                ResetRing(memory[i + j]);
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

    // ラウンド1の正誤判定
    void Judge1(int answer)
    {
        // 今回答する番号と、正解を比較
        if (answer == memory1[answerNumber])
        {
            Debug.Log("正解！");

            // 次の回答へ
            answerNumber++;

            // 3個全部正解した
            if (answerNumber >= memory1.Count)
            {
                Debug.Log("ラウンド1クリア！");
                
                // 次のラウンドへ
                answerNumber = 0;   
                Round2();
            }
        }
        else
        {
            Debug.Log("不正解！");

            // 間違えたら次のラウンドへ
            answerNumber = 0;
            Round2();
        }
    }

    // ラウンド2の正誤判定
    void Judge2(int answer)
    {
        // 今回答する番号と、正解を比較
        if (answer == memory1[answerNumber])
        {
            Debug.Log("正解！");

            // 次の回答へ
            answerNumber++;

            // 3個全部正解した
            if (answerNumber >= memory1.Count)
            {
                Debug.Log("ラウンド2クリア！");
                
                // 次のラウンドへ
                answerNumber = 0;   
                Round3();
            }
        }
        else
        {
            Debug.Log("不正解！");

            // 間違えたら次のラウンドへ
            answerNumber = 0;
            Round3();
        }
    }

    IEnumerator AnswerPhase()  // ラウンド３用
    {
        Debug.Log("回答開始");

        // 前回の回答を消す
        answer1.Clear();
        answer2.Clear();
        answer3.Clear();

        // 1回目の回答
        answerNumber = 1;

        Debug.Log("パッ！ 1回目");

        // ここからanswer1への入力を受け付ける

        // 次のリズムまで待つ
        yield return new WaitForSeconds(1.0f);


        // 2回目の回答
        answerNumber = 2;

        Debug.Log("パッ！ 2回目");

        // ここからanswer2への入力を受け付ける

        // 次のリズムまで待つ
        yield return new WaitForSeconds(1.0f);


        // 3回目の回答
        answerNumber = 3;

        Debug.Log("パッ！ 3回目");

        // ここからanswer3への入力を受け付ける
        yield return new WaitForSeconds(1.0f);

        // 3回分の回答を判定
        bool result1 = CheckAnswer(memory1, answer1);
        bool result2 = CheckAnswer(memory2, answer2);
        bool result3 = CheckAnswer(memory3, answer3);

        if (result1 && result2 && result3)
        {
            Debug.Log("正解！");
        }
        else
        {
            Debug.Log("不正解！");
        }

        // 回答受付終了
        answerNumber = 0;

        Debug.Log("回答終了");
    }

    bool CheckAnswer(List<int> memory, List<int> answer)
    {
        // 要素数が違ったら不正解
        if (memory.Count != answer.Count)
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
}
