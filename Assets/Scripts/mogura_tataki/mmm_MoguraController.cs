using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mmm_MoguraController : MonoBehaviour
{

    public GameObject[] rings = new GameObject[26];  //配列を作成
    public List<int> memory1 = new List<int>();  // 記憶用リスト1個目
    public List<int> memory2 = new List<int>();  // 記憶用リスト2個目
    public List<int> memory3 = new List<int>();  // 記憶用リスト3個目

    public List<int> answer1 = new List<int>();  // 回答用リスト1個目
    public List<int> answer2 = new List<int>();  // 回答用リスト2個目
    public List<int> answer3 = new List<int>();  // 回答用リスト3個目

    public List<int>[] memories = new List<int>[3]; // ↑の記憶用リストをまとめる配列

    public int number_number; // 各ラウンドの要素数を決める変数

    private Vector3[] originalScales = new Vector3[26]; // リングの元の大きさを保存する配列
    private int currentRound = 1;
    


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
        if(Input.GetKeyDown(KeyCode.Space))
        {
            if (currentRound == 1)
            {
                StartCoroutine(Round1());
                StartCoroutine(LightMemory(memory1));
            }
            else if (currentRound == 2)
            {
                Round2();
                StartCoroutine(LightMemory(memory2));
            }
            else if (currentRound == 3)
            {
                Round3();
                StartCoroutine(LightMemory(memory3));
            }

            if (currentRound < 3)
            {
                currentRound++;
            }
        }

        if(Input.GetKeyDown(KeyCode.A))
        {
            StartCoroutine(AnswerPhase());
        }
    }

    IEnumerator Round1()
    {
        number_number = 4;

        CreateMemory(memory1);
        CreateMemory(memory2);
        CreateMemory(memory3);

        yield return StartCoroutine(LightMemory(memory1));
        yield return StartCoroutine(LightMemory(memory2));
        yield return StartCoroutine(LightMemory(memory3));
    }

    IEnumerator Round2()
    {
        number_number = 6;

        CreateMemory(memory1);
        CreateMemory(memory2);
        CreateMemory(memory3);

        yield return StartCoroutine(LightMemory(memory1));
        yield return StartCoroutine(LightMemory(memory2));
        yield return StartCoroutine(LightMemory(memory3));
    }

    IEnumerator Round3()
    {
        number_number = 8;

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
        // リストに入っているリングを全部光らせる
        for (int i = 0; i < memory.Count; i++)
        {
            LightRing(memory[i]);
        }

        // 0.6秒待つ
        yield return new WaitForSeconds(0.6f);

        // リストに入っているリングを全部元に戻す
        for (int i = 0; i < memory.Count; i++)
        {
            ResetRing(memory[i]);
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

    IEnumerator AnswerPhase()
    {
        Debug.Log("回答開始");

        // 1回目の回答
        Debug.Log("パッ！ 1回目");

        // ここでプレイヤーの入力を受け付ける


        // 次のリズムまで待つ
        yield return new WaitForSeconds(1.0f);


        // 2回目の回答
        Debug.Log("パッ！ 2回目");

        // ここでプレイヤーの入力を受け付ける


        // 次のリズムまで待つ
        yield return new WaitForSeconds(1.0f);


        // 3回目の回答
        Debug.Log("パッ！ 3回目");

        // ここでプレイヤーの入力を受け付ける


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

        // 回答終了
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
